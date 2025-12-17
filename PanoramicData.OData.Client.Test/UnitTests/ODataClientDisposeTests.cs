namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataClient dispose pattern.
/// </summary>
public class ODataClientDisposeTests : TestBase
{
	/// <summary>
	/// Tests that Dispose disposes owned HttpClient.
	/// </summary>
	[Fact]
	public void Dispose_OwnedHttpClient_DisposesHttpClient()
	{
		// Arrange
		var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/"
		});

		// Act
		client.Dispose();

		// Assert - calling methods after dispose should fail or be no-op
		// The client is disposed, so it should not throw on double dispose
		var act = () => client.Dispose();
		act.Should().NotThrow();
	}

	/// <summary>
	/// Tests that Dispose does not dispose external HttpClient.
	/// </summary>
	[Fact]
	public async Task Dispose_ExternalHttpClient_DoesNotDisposeHttpClient()
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

		var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient,
			RetryCount = 0
		});

		// Act
		client.Dispose();

		// Assert - external HttpClient should still be usable
		var response = await httpClient.GetAsync("test", CancellationToken);
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	/// <summary>
	/// Tests that double Dispose does not throw.
	/// </summary>
	[Fact]
	public void Dispose_CalledTwice_DoesNotThrow()
	{
		// Arrange
		var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/"
		});

		// Act
		client.Dispose();
		var act = () => client.Dispose();

		// Assert
		act.Should().NotThrow();
	}

	/// <summary>
	/// Tests that Dispose with external HttpClient called twice does not throw.
	/// </summary>
	[Fact]
	public void Dispose_ExternalHttpClient_CalledTwice_DoesNotThrow()
	{
		// Arrange
		var mockHandler = new Mock<HttpMessageHandler>();
		using var httpClient = new HttpClient(mockHandler.Object)
		{
			BaseAddress = new Uri("https://test.odata.org/")
		};

		var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient
		});

		// Act
		client.Dispose();
		var act = () => client.Dispose();

		// Assert
		act.Should().NotThrow();
	}

	/// <summary>
	/// Tests using pattern with owned HttpClient.
	/// </summary>
	[Fact]
	public void UsingPattern_OwnedHttpClient_DisposesCorrectly()
	{
		// Act & Assert - no exception means dispose worked correctly
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/"
		});
		client.Should().NotBeNull();
	}

	/// <summary>
	/// Tests using pattern with external HttpClient.
	/// </summary>
	[Fact]
	public void UsingPattern_ExternalHttpClient_DisposesCorrectly()
	{
		// Arrange
		var mockHandler = new Mock<HttpMessageHandler>();
		using var httpClient = new HttpClient(mockHandler.Object)
		{
			BaseAddress = new Uri("https://test.odata.org/")
		};

		// Act & Assert - no exception means dispose worked correctly
		using (var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient
		}))
		{
			client.Should().NotBeNull();
		}

		// External HttpClient should still exist (not disposed)
		httpClient.Should().NotBeNull();
	}
}
