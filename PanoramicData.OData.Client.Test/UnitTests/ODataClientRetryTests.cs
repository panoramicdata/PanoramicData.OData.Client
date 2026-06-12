using PanoramicData.OData.Client.Exceptions;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataClient retry logic.
/// </summary>
public class ODataClientRetryTests : TestBase, IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	public ODataClientRetryTests()
	{
		_mockHandler = new Mock<HttpMessageHandler>();
		_httpClient = new HttpClient(_mockHandler.Object)
		{
			BaseAddress = new Uri("https://test.odata.org/")
		};
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		_httpClient.Dispose();
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Tests that client retries on 500 error.
	/// </summary>
	[Fact]
	public async Task Request_ServerError_Retries()
	{
		// Arrange
		var callCount = 0;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() =>
			{
				callCount++;
				if (callCount < 3)
				{
					return new HttpResponseMessage(HttpStatusCode.InternalServerError)
					{
						Content = new StringContent("{}")
					};
				}

				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("""{"value":[]}""")
				};
			});

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 3,
			RetryDelay = TimeSpan.FromMilliseconds(1)
		});

		// Act
		var response = await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		// Assert
		callCount.Should().Be(3);
		response.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that client does not retry on 4xx errors.
	/// </summary>
	[Fact]
	public async Task Request_ClientError_DoesNotRetry()
	{
		// Arrange
		var callCount = 0;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() =>
			{
				callCount++;
				return new HttpResponseMessage(HttpStatusCode.BadRequest)
				{
					Content = new StringContent("{}")
				};
			});

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 3,
			RetryDelay = TimeSpan.FromMilliseconds(1)
		});

		// Act
		var act = async () => await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ODataClientException>();
		callCount.Should().Be(1); // No retries for 4xx
	}

	/// <summary>
	/// Tests that client retries on transient HTTP exceptions.
	/// </summary>
	[Fact]
	public async Task Request_TransientException_Retries()
	{
		// Arrange
		var callCount = 0;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() =>
			{
				callCount++;
				if (callCount < 2)
				{
					throw new HttpRequestException("Connection failed");
				}

				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("""{"value":[]}""")
				};
			});

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 3,
			RetryDelay = TimeSpan.FromMilliseconds(1)
		});

		// Act
		var response = await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		// Assert
		callCount.Should().Be(2);
		response.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that client gives up after max retries.
	/// </summary>
	[Fact]
	public async Task Request_MaxRetries_ThrowsException()
	{
		// Arrange
		var callCount = 0;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() =>
			{
				callCount++;
				return new HttpResponseMessage(HttpStatusCode.InternalServerError)
				{
					Content = new StringContent("{}")
				};
			});

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 2,
			RetryDelay = TimeSpan.FromMilliseconds(1)
		});

		// Act
		var act = async () => await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ODataClientException>();
		callCount.Should().Be(3); // Initial + 2 retries
	}

	/// <summary>
	/// Tests that a transient failure that recovers on retry logs the attempt at Debug
	/// (the default RetryAttemptLogLevel) and logs no Warning.
	/// </summary>
	[Fact]
	public async Task Request_TransientFailureThatRecovers_LogsAttemptAtDebugAndNoWarning()
	{
		// Arrange
		var callCount = 0;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() =>
			{
				callCount++;
				if (callCount < 2)
				{
					return new HttpResponseMessage(HttpStatusCode.InternalServerError)
					{
						Content = new StringContent("{}")
					};
				}

				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("""{"value":[]}""")
				};
			});

		var logger = new CapturingLogger();
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = logger,
			RetryCount = 3,
			RetryDelay = TimeSpan.FromMilliseconds(1)
		});

		// Act
		await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		// Assert
		logger.Entries.Should().Contain(entry => entry.EventId.Id == 18 && entry.Level == LogLevel.Debug);
		logger.Entries.Should().NotContain(entry => entry.Level == LogLevel.Warning);
	}

	/// <summary>
	/// Tests that exhausting all retries on 5xx responses logs exactly one Warning.
	/// </summary>
	[Fact]
	public async Task Request_RetriesExhaustedOnServerError_LogsSingleWarning()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.InternalServerError)
			{
				Content = new StringContent("{}")
			});

		var logger = new CapturingLogger();
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = logger,
			RetryCount = 2,
			RetryDelay = TimeSpan.FromMilliseconds(1)
		});

		// Act
		var act = async () => await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ODataClientException>();
		logger.Entries.Where(entry => entry.Level == LogLevel.Warning).Should().ContainSingle()
			.Which.EventId.Id.Should().Be(22);
		logger.Entries.Where(entry => entry.EventId.Id == 18).Should().HaveCount(2)
			.And.OnlyContain(entry => entry.Level == LogLevel.Debug);
	}

	/// <summary>
	/// Tests that exhausting all retries on transient exceptions logs exactly one Warning
	/// and rethrows the final exception.
	/// </summary>
	[Fact]
	public async Task Request_RetriesExhaustedOnException_LogsSingleWarningAndThrows()
	{
		// Arrange
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ThrowsAsync(new HttpRequestException("Connection failed"));

		var logger = new CapturingLogger();
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = logger,
			RetryCount = 2,
			RetryDelay = TimeSpan.FromMilliseconds(1)
		});

		// Act
		var act = async () => await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		// Assert
		await act.Should().ThrowAsync<HttpRequestException>();
		logger.Entries.Where(entry => entry.Level == LogLevel.Warning).Should().ContainSingle()
			.Which.EventId.Id.Should().Be(23);
		logger.Entries.Where(entry => entry.EventId.Id == 19).Should().HaveCount(2)
			.And.OnlyContain(entry => entry.Level == LogLevel.Debug);
	}

	/// <summary>
	/// Tests that RetryAttemptLogLevel is respected when set to a non-default level.
	/// </summary>
	[Fact]
	public async Task Request_RetryAttemptLogLevelWarning_LogsAttemptsAtWarning()
	{
		// Arrange
		var callCount = 0;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() =>
			{
				callCount++;
				if (callCount < 2)
				{
					return new HttpResponseMessage(HttpStatusCode.InternalServerError)
					{
						Content = new StringContent("{}")
					};
				}

				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("""{"value":[]}""")
				};
			});

		var logger = new CapturingLogger();
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = logger,
			RetryCount = 3,
			RetryDelay = TimeSpan.FromMilliseconds(1),
			RetryAttemptLogLevel = LogLevel.Warning
		});

		// Act
		await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		// Assert
		logger.Entries.Should().Contain(entry => entry.EventId.Id == 18 && entry.Level == LogLevel.Warning);
	}

	/// <summary>
	/// Tests that RetryAttemptLogLevel None disables per-attempt logging entirely.
	/// </summary>
	[Fact]
	public async Task Request_RetryAttemptLogLevelNone_LogsNoAttempts()
	{
		// Arrange
		var callCount = 0;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() =>
			{
				callCount++;
				if (callCount < 2)
				{
					return new HttpResponseMessage(HttpStatusCode.InternalServerError)
					{
						Content = new StringContent("{}")
					};
				}

				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("""{"value":[]}""")
				};
			});

		var logger = new CapturingLogger();
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = logger,
			RetryCount = 3,
			RetryDelay = TimeSpan.FromMilliseconds(1),
			RetryAttemptLogLevel = LogLevel.None
		});

		// Act
		await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		// Assert
		logger.Entries.Should().NotContain(entry => entry.EventId.Id == 18 || entry.EventId.Id == 19);
	}

	/// <summary>
	/// A minimal ILogger that captures log entries for assertions.
	/// </summary>
	private sealed class CapturingLogger : ILogger
	{
		public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = [];

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
			=> Entries.Add((logLevel, eventId, formatter(state, exception)));
	}

	/// <summary>
	/// Tests that client respects retry delay.
	/// </summary>
	[Fact]
	public async Task Request_RetryDelay_IsRespected()
	{
		// Arrange
		var callTimes = new List<DateTime>();
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() =>
			{
				callTimes.Add(DateTime.UtcNow);
				if (callTimes.Count < 2)
				{
					return new HttpResponseMessage(HttpStatusCode.InternalServerError)
					{
						Content = new StringContent("{}")
					};
				}

				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("""{"value":[]}""")
				};
			});

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 2,
			RetryDelay = TimeSpan.FromMilliseconds(100)
		});

		// Act
		await client.GetAsync(client.For<Product>("Products"), CancellationToken);

		// Assert
		callTimes.Should().HaveCount(2);
		var delay = callTimes[1] - callTimes[0];
		delay.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(90); // Allow some tolerance
	}
}
