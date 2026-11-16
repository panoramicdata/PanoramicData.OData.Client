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
	/// Tests that a client can be built with each option set on its own.
	/// </summary>
	[Fact]
	public void Constructor_WithEachOption_BuildsAClient()
	{
		Action<ODataClientOptions>[] configurations =
		[
			_ => { },
			options => options.Logger = new Mock<ILogger>().Object,
			options => options.Timeout = TimeSpan.FromSeconds(30),
			options => options.JsonSerializerOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = false },
			options => options.MetadataCacheDuration = TimeSpan.FromHours(1),
			options => options.RetryCount = 5,
			options => options.RetryDelay = TimeSpan.FromSeconds(2)
		];

		foreach (var configure in configurations)
		{
			var options = new ODataClientOptions { BaseUrl = "https://test.odata.org/" };
			configure(options);

			using var client = new ODataClient(options);

			client.Should().NotBeNull();
		}
	}

	private sealed class TestEntity
	{
		public int Id { get; set; }
	}

	/// <summary>
	/// Tests that a provided HttpClient without a BaseAddress gets its BaseAddress set from options.
	/// Regression test for issue where passing HttpClient without BaseAddress caused "Invalid request URI".
	/// </summary>
	[Fact]
	public void Constructor_HttpClientWithoutBaseAddress_SetsBaseAddressFromOptions()
	{
		// Arrange
		using var httpClient = new HttpClient(); // no BaseAddress set

		// Act
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://services.odata.org/V4/Northwind/Northwind.svc",
			HttpClient = httpClient
		});

		// Assert - BaseAddress should have been set automatically
		httpClient.BaseAddress.Should().NotBeNull();
		httpClient.BaseAddress!.ToString().Should().Be("https://services.odata.org/V4/Northwind/Northwind.svc/");
	}

	/// <summary>
	/// Tests that a provided HttpClient with an existing BaseAddress is not overwritten.
	/// </summary>
	[Fact]
	public void Constructor_HttpClientWithBaseAddress_PreservesExistingBaseAddress()
	{
		// Arrange
		using var httpClient = new HttpClient
		{
			BaseAddress = new Uri("https://services.odata.org/V4/Northwind/Northwind.svc/")
		};

		// Act
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://services.odata.org/V4/Northwind/Northwind.svc",
			HttpClient = httpClient
		});

		// Assert - existing BaseAddress must not be changed
		httpClient.BaseAddress!.ToString().Should().Be("https://services.odata.org/V4/Northwind/Northwind.svc/");
	}
}
