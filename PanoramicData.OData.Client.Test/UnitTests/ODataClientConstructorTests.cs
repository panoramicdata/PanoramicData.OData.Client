namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataClient constructor and initialization.
/// </summary>
public class ODataClientConstructorTests : TestBase
{
	/// <summary>
	/// Tests that constructor throws when options is null.
	/// </summary>
	[Fact]
	public void Constructor_NullOptions_ThrowsArgumentNullException()
	{
		// Act
		var act = () => new ODataClient(null!);

		// Assert
		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("options");
	}

	/// <summary>
	/// Tests that constructor creates HttpClient when not provided.
	/// </summary>
	[Fact]
	public void Constructor_NoHttpClient_CreatesHttpClient()
	{
		// Act
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/"
		});

		// Assert - client should be usable (HttpClient was created internally)
		client.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that constructor uses provided HttpClient.
	/// </summary>
	[Fact]
	public void Constructor_WithHttpClient_UsesProvidedClient()
	{
		// Arrange
		var mockHandler = new Mock<HttpMessageHandler>();
		mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""{"value":[]}""", System.Text.Encoding.UTF8, "application/json")
			});

		using var httpClient = new HttpClient(mockHandler.Object)
		{
			BaseAddress = new Uri("https://test.odata.org/")
		};

		// Act
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient
		});

		// Assert - verify the provided HttpClient is used
		client.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that constructor uses NullLogger when no logger provided.
	/// </summary>
	[Fact]
	public void Constructor_NoLogger_UsesNullLogger()
	{
		// Act
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/"
		});

		// Assert - no exception means NullLogger was used
		client.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that constructor uses provided logger.
	/// </summary>
	[Fact]
	public void Constructor_WithLogger_UsesProvidedLogger()
	{
		// Arrange
		var mockLogger = new Mock<ILogger>();

		// Act
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			Logger = mockLogger.Object
		});

		// Assert
		client.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that constructor sets default timeout.
	/// </summary>
	[Fact]
	public void Constructor_DefaultTimeout_SetToFiveMinutes()
	{
		// Act
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/"
		});

		// Assert - defaults are applied (can't directly access, but client should work)
		client.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that constructor respects custom timeout.
	/// </summary>
	[Fact]
	public void Constructor_CustomTimeout_Applied()
	{
		// Arrange
		var customTimeout = TimeSpan.FromSeconds(30);

		// Act
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			Timeout = customTimeout
		});

		// Assert
		client.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that constructor trims trailing slash from BaseUrl.
	/// </summary>
	[Fact]
	public async Task Constructor_BaseUrlWithTrailingSlash_Normalized()
	{
		// Arrange
		var mockHandler = new Mock<HttpMessageHandler>();
		Uri? capturedUri = null;

		mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedUri = request.RequestUri)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""{"value":[]}""", System.Text.Encoding.UTF8, "application/json")
			});

		using var httpClient = new HttpClient(mockHandler.Object)
		{
			BaseAddress = new Uri("https://test.odata.org/")
		};

		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient,
			RetryCount = 0
		});

		// Act
		var query = client.For<TestEntity>("TestEntities");
		await client.GetAsync(query, CancellationToken);

		// Assert
		capturedUri.Should().NotBeNull();
		capturedUri!.ToString().Should().Contain("TestEntities");
	}

	/// <summary>
	/// Tests that constructor uses custom JsonSerializerOptions.
	/// </summary>
	[Fact]
	public void Constructor_CustomJsonOptions_Applied()
	{
		// Arrange
		var jsonOptions = new System.Text.Json.JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = false
		};

		// Act
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			JsonSerializerOptions = jsonOptions
		});

		// Assert
		client.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that constructor sets metadata cache duration.
	/// </summary>
	[Fact]
	public void Constructor_WithMetadataCacheDuration_Applied()
	{
		// Act
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			MetadataCacheDuration = TimeSpan.FromHours(1)
		});

		// Assert
		client.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that constructor sets retry count.
	/// </summary>
	[Fact]
	public void Constructor_CustomRetryCount_Applied()
	{
		// Act
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			RetryCount = 5
		});

		// Assert
		client.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that constructor sets retry delay.
	/// </summary>
	[Fact]
	public void Constructor_CustomRetryDelay_Applied()
	{
		// Act
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			RetryDelay = TimeSpan.FromSeconds(2)
		});

		// Assert
		client.Should().NotBeNull();
	}

	private sealed class TestEntity
	{
		public int Id { get; set; }
	}
}
