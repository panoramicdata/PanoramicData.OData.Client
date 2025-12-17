namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for OData stream/media entity support.
/// </summary>
public class ODataClientStreamTests : TestBase, IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;
	private readonly ODataClient _client;

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	public ODataClientStreamTests()
	{
		_mockHandler = new Mock<HttpMessageHandler>();
		_httpClient = new HttpClient(_mockHandler.Object)
		{
			BaseAddress = new Uri("https://test.odata.org/")
		};
		_client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = _httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 0
		});
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		_client.Dispose();
		_httpClient.Dispose();
		GC.SuppressFinalize(this);
	}

	#region GetStreamAsync Tests

	/// <summary>
	/// Tests that GetStreamAsync sends GET to /$value URL.
	/// </summary>
	[Fact]
	public async Task GetStreamAsync_SendsGetToValueUrl()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;
		var content = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new ByteArrayContent(content)
			});

		// Act
		var stream = await _client.GetStreamAsync("Photos", 1, cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Method.Should().Be(HttpMethod.Get);
		capturedRequest.RequestUri!.PathAndQuery.Should().Be("/Photos(1)/$value");
	}

	/// <summary>
	/// Tests that GetStreamAsync returns stream with binary content.
	/// </summary>
	[Fact]
	public async Task GetStreamAsync_ReturnsStreamWithContent()
	{
		// Arrange
		var expectedContent = new byte[] { 1, 2, 3, 4, 5 };

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new ByteArrayContent(expectedContent)
			});

		// Act
		using var stream = await _client.GetStreamAsync("Photos", 1, cancellationToken: CancellationToken.None);
		using var memoryStream = new MemoryStream();
		await stream.CopyToAsync(memoryStream, CancellationToken);
		var actualContent = memoryStream.ToArray();

		// Assert
		actualContent.Should().BeEquivalentTo(expectedContent);
	}

	/// <summary>
	/// Tests that GetStreamAsync works with string keys.
	/// </summary>
	[Fact]
	public async Task GetStreamAsync_WithStringKey_FormatsCorrectly()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new ByteArrayContent([])
			});

		// Act
		await _client.GetStreamAsync("Documents", "report.pdf", cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.RequestUri!.PathAndQuery.Should().Be("/Documents('report.pdf')/$value");
	}

	#endregion

	#region SetStreamAsync Tests

	/// <summary>
	/// Tests that SetStreamAsync sends PUT with stream content.
	/// </summary>
	[Fact]
	public async Task SetStreamAsync_SendsPutWithContent()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;
		var content = new byte[] { 1, 2, 3, 4, 5 };

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

		// Act
		using var stream = new MemoryStream(content);
		await _client.SetStreamAsync("Photos", 1, stream, cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Method.Should().Be(HttpMethod.Put);
		capturedRequest.RequestUri!.PathAndQuery.Should().Be("/Photos(1)/$value");
		capturedRequest.Content.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that SetStreamAsync sends correct content type.
	/// </summary>
	[Fact]
	public async Task SetStreamAsync_SendsContentType()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

		// Act
		using var stream = new MemoryStream([1, 2, 3]);
		await _client.SetStreamAsync("Photos", 1, stream, "image/png", cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Content!.Headers.ContentType!.MediaType.Should().Be("image/png");
	}

	#endregion

	#region GetStreamPropertyAsync Tests

	/// <summary>
	/// Tests that GetStreamPropertyAsync sends GET to property URL.
	/// </summary>
	[Fact]
	public async Task GetStreamPropertyAsync_SendsGetToPropertyUrl()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new ByteArrayContent([])
			});

		// Act
		await _client.GetStreamPropertyAsync("Products", 1, "Thumbnail", cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Method.Should().Be(HttpMethod.Get);
		capturedRequest.RequestUri!.PathAndQuery.Should().Be("/Products(1)/Thumbnail");
	}

	/// <summary>
	/// Tests that GetStreamPropertyAsync returns stream with content.
	/// </summary>
	[Fact]
	public async Task GetStreamPropertyAsync_ReturnsStreamWithContent()
	{
		// Arrange
		var expectedContent = Encoding.UTF8.GetBytes("thumbnail data");

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new ByteArrayContent(expectedContent)
			});

		// Act
		using var stream = await _client.GetStreamPropertyAsync("Products", 1, "Thumbnail", cancellationToken: CancellationToken.None);
		using var memoryStream = new MemoryStream();
		await stream.CopyToAsync(memoryStream, CancellationToken);
		var actualContent = memoryStream.ToArray();

		// Assert
		actualContent.Should().BeEquivalentTo(expectedContent);
	}

	#endregion

	#region SetStreamPropertyAsync Tests

	/// <summary>
	/// Tests that SetStreamPropertyAsync sends PUT to property URL.
	/// </summary>
	[Fact]
	public async Task SetStreamPropertyAsync_SendsPutToPropertyUrl()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

		// Act
		using var stream = new MemoryStream([1, 2, 3]);
		await _client.SetStreamPropertyAsync("Products", 1, "Thumbnail", stream, "image/jpeg", cancellationToken: CancellationToken.None);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Method.Should().Be(HttpMethod.Put);
		capturedRequest.RequestUri!.PathAndQuery.Should().Be("/Products(1)/Thumbnail");
		capturedRequest.Content!.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
	}

	#endregion
}
