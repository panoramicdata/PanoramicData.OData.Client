using PanoramicData.OData.Client.Exceptions;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataClient retry logic.
/// </summary>
public class ODataClientRetryTests : MockedODataClientTestBase
{
	private const string EmptyPage = """{"value":[]}""";

	private int _callCount;

	/// <summary>
	/// Tests that client retries on a 503 error.
	/// </summary>
	[Fact]
	public async Task Request_ServerError_Retries()
	{
		SetupFailuresThenSuccess(2, () => Respond(HttpStatusCode.ServiceUnavailable, "{}"));
		using var client = CreateRetryClient(3);

		var response = await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		_callCount.Should().Be(3);
		response.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that client does not retry on 4xx errors.
	/// </summary>
	[Fact]
	public async Task Request_ClientError_DoesNotRetry()
	{
		SetupSendAsync().ReturnsAsync(() => CountedResponse(HttpStatusCode.BadRequest, "{}"));
		using var client = CreateRetryClient(3);

		var act = () => client.GetAsync(client.For<Product>("Products"), CancellationToken);

		await act.Should().ThrowAsync<ODataClientException>();
		_callCount.Should().Be(1);
	}

	/// <summary>
	/// Tests that client retries on transient HTTP exceptions.
	/// </summary>
	[Fact]
	public async Task Request_TransientException_Retries()
	{
		SetupFailuresThenSuccess(1, () => throw new HttpRequestException("Connection failed"));
		using var client = CreateRetryClient(3);

		var response = await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		_callCount.Should().Be(2);
		response.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that client gives up after max retries.
	/// </summary>
	[Fact]
	public async Task Request_MaxRetries_ThrowsException()
	{
		SetupSendAsync().ReturnsAsync(() => CountedResponse(HttpStatusCode.ServiceUnavailable, "{}"));
		using var client = CreateRetryClient(2);

		var act = () => client.GetAsync(client.For<Product>("Products"), CancellationToken);

		await act.Should().ThrowAsync<ODataClientException>();
		_callCount.Should().Be(3);
	}

	/// <summary>
	/// Tests that a transient failure that recovers on retry logs the attempt at Debug
	/// (the default RetryAttemptLogLevel) and logs no Warning.
	/// </summary>
	[Fact]
	public async Task Request_TransientFailureThatRecovers_LogsAttemptAtDebugAndNoWarning()
	{
		SetupFailuresThenSuccess(1, () => Respond(HttpStatusCode.ServiceUnavailable, "{}"));
		var logger = new CapturingLogger();
		using var client = CreateRetryClient(3, logger);

		await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		logger.Entries.Should().Contain(entry => entry.EventId.Id == 18 && entry.Level == LogLevel.Debug);
		logger.Entries.Should().NotContain(entry => entry.Level == LogLevel.Warning);
	}

	/// <summary>
	/// Tests that exhausting all retries on 5xx responses logs exactly one Warning.
	/// </summary>
	[Fact]
	public async Task Request_RetriesExhaustedOnServerError_LogsSingleWarning()
	{
		SetupSendAsync().ReturnsAsync(() => Respond(HttpStatusCode.ServiceUnavailable, "{}"));
		var logger = new CapturingLogger();
		using var client = CreateRetryClient(2, logger);

		var act = () => client.GetAsync(client.For<Product>("Products"), CancellationToken);

		await act.Should().ThrowAsync<ODataClientException>();
		AssertSingleWarningAndDebugAttempts(logger, warningEventId: 22, attemptEventId: 18);
	}

	/// <summary>
	/// Tests that exhausting all retries on transient exceptions logs exactly one Warning
	/// and rethrows the final exception.
	/// </summary>
	[Fact]
	public async Task Request_RetriesExhaustedOnException_LogsSingleWarningAndThrows()
	{
		SetupSendAsync().ThrowsAsync(new HttpRequestException("Connection failed"));
		var logger = new CapturingLogger();
		using var client = CreateRetryClient(2, logger);

		var act = () => client.GetAsync(client.For<Product>("Products"), CancellationToken);

		await act.Should().ThrowAsync<HttpRequestException>();
		AssertSingleWarningAndDebugAttempts(logger, warningEventId: 23, attemptEventId: 19);
	}

	/// <summary>
	/// Tests that RetryAttemptLogLevel is respected when set to a non-default level.
	/// </summary>
	[Fact]
	public async Task Request_RetryAttemptLogLevelWarning_LogsAttemptsAtWarning()
	{
		SetupFailuresThenSuccess(1, () => Respond(HttpStatusCode.ServiceUnavailable, "{}"));
		var logger = new CapturingLogger();
		using var client = CreateRetryClient(3, logger, LogLevel.Warning);

		await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		logger.Entries.Should().Contain(entry => entry.EventId.Id == 18 && entry.Level == LogLevel.Warning);
	}

	/// <summary>
	/// Tests that RetryAttemptLogLevel None disables per-attempt logging entirely.
	/// </summary>
	[Fact]
	public async Task Request_RetryAttemptLogLevelNone_LogsNoAttempts()
	{
		SetupFailuresThenSuccess(1, () => Respond(HttpStatusCode.ServiceUnavailable, "{}"));
		var logger = new CapturingLogger();
		using var client = CreateRetryClient(3, logger, LogLevel.None);

		await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		logger.Entries.Should().NotContain(entry => entry.EventId.Id == 18 || entry.EventId.Id == 19);
	}

	private static HttpResponseMessage Respond(HttpStatusCode status, string body)
		=> new(status) { Content = new StringContent(body) };

	private HttpResponseMessage CountedResponse(HttpStatusCode status, string body)
	{
		_callCount++;
		return Respond(status, body);
	}

	private void SetupFailuresThenSuccess(int failures, Func<HttpResponseMessage> failure)
		=> SetupSendAsync().ReturnsAsync(() =>
		{
			_callCount++;
			return _callCount <= failures ? failure() : Respond(HttpStatusCode.OK, EmptyPage);
		});

	private ODataClient CreateRetryClient(int retryCount, ILogger? logger = null, LogLevel? attemptLogLevel = null)
		=> CreateClient(options =>
		{
			options.Logger = logger ?? NullLogger.Instance;
			options.RetryCount = retryCount;
			options.RetryDelay = TimeSpan.FromMilliseconds(1);
			options.RetryAttemptLogLevel = attemptLogLevel ?? options.RetryAttemptLogLevel;
		});

	private static void AssertSingleWarningAndDebugAttempts(CapturingLogger logger, int warningEventId, int attemptEventId)
	{
		logger.Entries.Where(entry => entry.Level == LogLevel.Warning).Should().ContainSingle()
			.Which.EventId.Id.Should().Be(warningEventId);
		logger.Entries.Where(entry => entry.EventId.Id == attemptEventId).Should().HaveCount(2)
			.And.OnlyContain(entry => entry.Level == LogLevel.Debug);
	}

	/// <summary>
	/// A minimal ILogger that captures log entries for assertions.
	/// </summary>
	private sealed class CapturingLogger : ILogger
	{
		public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = [];

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
	{
		ArgumentNullException.ThrowIfNull(state);
		return null;
	}

		public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
			=> Entries.Add((logLevel, eventId, formatter(state, exception)));
	}
}
