using PanoramicData.OData.Client.Exceptions;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataClient retry logic.
/// </summary>
public class ODataClientRetryTests : MockedODataClientTestBase
{
	private const string EmptyPage = """{"value":[]}""";

	private int _callCount;
	private readonly CapturingLogger _logger = new();
	private readonly List<DateTime> _callTimes = [];

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

		await GetExpectingFailureAsync<ODataClientException>(3);
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

		await GetExpectingFailureAsync<ODataClientException>(2);
		_callCount.Should().Be(3);
	}

	/// <summary>
	/// Tests that a transient failure that recovers on retry logs the attempt at Information
	/// (the default RetryAttemptLogLevel) and logs no Warning.
	/// </summary>
	[Fact]
	public async Task Request_TransientFailureThatRecovers_LogsAttemptAtInformationAndNoWarning()
	{
		await GetAfterOneServerErrorAsync(3);

		_logger.Entries.Should().Contain(entry => entry.EventId.Id == 18 && entry.Level == LogLevel.Information);
		_logger.Entries.Should().NotContain(entry => entry.Level == LogLevel.Warning);
	}

	/// <summary>
	/// Tests that exhausting all retries on 5xx responses logs exactly one Warning.
	/// </summary>
	[Fact]
	public async Task Request_RetriesExhaustedOnServerError_LogsSingleWarning()
	{
		SetupSendAsync().ReturnsAsync(() => Respond(HttpStatusCode.ServiceUnavailable, "{}"));

		await GetExpectingFailureAsync<ODataClientException>(2);
		AssertSingleWarningAndInformationAttempts(warningEventId: 22, attemptEventId: 18);
	}

	/// <summary>
	/// Tests that exhausting all retries on transient exceptions logs exactly one Warning
	/// and rethrows the final exception.
	/// </summary>
	[Fact]
	public async Task Request_RetriesExhaustedOnException_LogsSingleWarningAndThrows()
	{
		SetupSendAsync().ThrowsAsync(new HttpRequestException("Connection failed"));

		await GetExpectingFailureAsync<HttpRequestException>(2);
		AssertSingleWarningAndInformationAttempts(warningEventId: 23, attemptEventId: 19);
	}

	/// <summary>
	/// Tests that RetryAttemptLogLevel is respected when set to a non-default level.
	/// </summary>
	[Fact]
	public async Task Request_RetryAttemptLogLevelWarning_LogsAttemptsAtWarning()
	{
		await GetAfterOneServerErrorAsync(3, LogLevel.Warning);

		_logger.Entries.Should().Contain(entry => entry.EventId.Id == 18 && entry.Level == LogLevel.Warning);
	}

	/// <summary>
	/// Tests that RetryAttemptLogLevel None disables per-attempt logging entirely.
	/// </summary>
	[Fact]
	public async Task Request_RetryAttemptLogLevelNone_LogsNoAttempts()
	{
		await GetAfterOneServerErrorAsync(3, LogLevel.None);

		_logger.Entries.Should().NotContain(entry => entry.EventId.Id == 18 || entry.EventId.Id == 19);
	}

	/// <summary>
	/// Tests that client respects retry delay.
	/// </summary>
	[Fact]
	public async Task Request_RetryDelay_IsRespected()
	{
		await GetAfterOneServerErrorAsync(2, retryDelay: TimeSpan.FromMilliseconds(100));

		_callTimes.Should().HaveCount(2);
		(_callTimes[1] - _callTimes[0]).TotalMilliseconds.Should().BeGreaterThanOrEqualTo(90);
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
			_callTimes.Add(DateTime.UtcNow);
			return _callCount <= failures ? failure() : Respond(HttpStatusCode.OK, EmptyPage);
		});

	private ODataClient CreateRetryClient(int retryCount, LogLevel? attemptLogLevel = null, TimeSpan? retryDelay = null)
		=> CreateClient(options =>
		{
			options.Logger = _logger;
			options.RetryCount = retryCount;
			options.RetryDelay = retryDelay ?? TimeSpan.FromMilliseconds(1);
			options.RetryAttemptLogLevel = attemptLogLevel ?? options.RetryAttemptLogLevel;
		});

	private async Task GetAfterOneServerErrorAsync(int retryCount, LogLevel? attemptLogLevel = null, TimeSpan? retryDelay = null)
	{
		SetupFailuresThenSuccess(1, () => Respond(HttpStatusCode.ServiceUnavailable, "{}"));
		using var client = CreateRetryClient(retryCount, attemptLogLevel, retryDelay);

		await client.GetAsync(client.For<Product>("Products"), CancellationToken);
	}

	private async Task GetExpectingFailureAsync<TException>(int retryCount)
		where TException : Exception
	{
		using var client = CreateRetryClient(retryCount);

		var act = () => client.GetAsync(client.For<Product>("Products"), CancellationToken);

		await act.Should().ThrowAsync<TException>();
	}

	private void AssertSingleWarningAndInformationAttempts(int warningEventId, int attemptEventId)
	{
		_logger.Entries.Where(entry => entry.Level == LogLevel.Warning).Should().ContainSingle()
			.Which.EventId.Id.Should().Be(warningEventId);
		_logger.Entries.Where(entry => entry.EventId.Id == attemptEventId).Should().HaveCount(2)
			.And.OnlyContain(entry => entry.Level == LogLevel.Information);
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
