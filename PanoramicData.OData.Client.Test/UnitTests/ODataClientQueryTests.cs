namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataClient query operations.
/// </summary>
public class ODataClientQueryTests : TestBase, IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;
	private readonly ODataClient _client;

	/// <summary>
	/// Initializes a new instance of the test class with mocked dependencies.
	/// </summary>
	public ODataClientQueryTests()
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

	#region For<T>() Tests

	/// <summary>
	/// Tests that For&lt;T&gt;() auto-generates entity set name with simple pluralization.
	/// </summary>
	[Fact]
	public void For_AutoEntitySetName_Pluralizes()
	{
		// Act
		var query = _client.For<Product>();

		// Assert - verify URL contains pluralized name
		var url = query.BuildUrl();
		url.Should().Be("Products");
	}

	/// <summary>
	/// Tests that For&lt;T&gt;() handles entity names ending in 'y'.
	/// </summary>
	[Fact]
	public void For_EntityNameEndingInY_PluralizesCorrectly()
	{
		// Act
		var query = _client.For<Category>();

		// Assert
		var url = query.BuildUrl();
		url.Should().Be("Categories");
	}

	/// <summary>
	/// Tests that For&lt;T&gt;() handles entity names already ending in 's'.
	/// </summary>
	[Fact]
	public void For_EntityNameEndingInS_DoesNotDoublePluralize()
	{
		// Act
		var query = _client.For<Address>();

		// Assert
		var url = query.BuildUrl();
		url.Should().Be("Addresss"); // Simple pluralization adds 's'
	}

	/// <summary>
	/// Tests that For&lt;T&gt;(entitySetName) uses provided name.
	/// </summary>
	[Fact]
	public void For_WithEntitySetName_UsesProvidedName()
	{
		// Act
		var query = _client.For<Product>("CustomProducts");

		// Assert
		var url = query.BuildUrl();
		url.Should().Be("CustomProducts");
	}

	#endregion

	#region GetAsync Tests

	/// <summary>
	/// Tests GetAsync returns empty list for empty response.
	/// </summary>
	[Fact]
	public async Task GetAsync_EmptyResponse_ReturnsEmptyList()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, """{"value": []}""");

		// Act
		var response = await _client.GetAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		response.Value.Should().BeEmpty();
	}

	/// <summary>
	/// Tests GetAsync parses ETag from response headers.
	/// </summary>
	[Fact]
	public async Task GetAsync_WithETagHeader_ParsesETag()
	{
		// Arrange
		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("""{"value": [{"ID": 1, "Name": "Test"}]}""", System.Text.Encoding.UTF8, "application/json")
		};
		response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"abc123\"");

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);

		// Act
		var result = await _client.GetAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		result.ETag.Should().Be("\"abc123\"");
	}

	#endregion

	#region GetAllAsync Tests

	/// <summary>
	/// Tests GetAllAsync follows pagination.
	/// </summary>
	[Fact]
	public async Task GetAllAsync_MultiplePages_ReturnsAllResults()
	{
		// Arrange
		var responses = new Queue<string>();
		responses.Enqueue("""
			{
				"value": [{"ID": 1, "Name": "Product1"}],
				"@odata.nextLink": "https://test.odata.org/Products?$skip=1"
			}
			""");
		responses.Enqueue("""
			{
				"value": [{"ID": 2, "Name": "Product2"}]
			}
			""");

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responses.Dequeue(), System.Text.Encoding.UTF8, "application/json")
			});

		// Act
		var result = await _client.GetAllAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		result.Value.Should().HaveCount(2);
		result.Value[0].Name.Should().Be("Product1");
		result.Value[1].Name.Should().Be("Product2");
	}

	/// <summary>
	/// Tests GetAllAsync preserves count from first page.
	/// </summary>
	[Fact]
	public async Task GetAllAsync_WithCount_PreservesCountFromFirstPage()
	{
		// Arrange
		var responses = new Queue<string>();
		responses.Enqueue("""
			{
				"@odata.count": 100,
				"value": [{"ID": 1, "Name": "Product1"}],
				"@odata.nextLink": "https://test.odata.org/Products?$skip=1"
			}
			""");
		responses.Enqueue("""
			{
				"value": [{"ID": 2, "Name": "Product2"}]
			}
			""");

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responses.Dequeue(), System.Text.Encoding.UTF8, "application/json")
			});

		// Act
		var result = await _client.GetAllAsync(_client.For<Product>("Products").Count(), CancellationToken);

		// Assert
		result.Count.Should().Be(100);
	}

	/// <summary>
	/// Tests GetAllAsync respects cancellation.
	/// </summary>
	[Fact]
	public async Task GetAllAsync_Cancelled_ThrowsOperationCancelledException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, """
			{
				"value": [{"ID": 1, "Name": "Product1"}],
				"@odata.nextLink": "https://test.odata.org/Products?$skip=1"
			}
			""");

		using var cts = new CancellationTokenSource();
		cts.Cancel();

		// Act
		var act = async () => await _client.GetAllAsync(_client.For<Product>("Products"), cts.Token);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	#endregion

	#region GetCountAsync Tests

	/// <summary>
	/// Tests GetCountAsync returns count.
	/// </summary>
	[Fact]
	public async Task GetCountAsync_ReturnsCount()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, "42");

		// Act
		var count = await _client.GetCountAsync<Product>(CancellationToken);

		// Assert
		count.Should().Be(42);
	}

	/// <summary>
	/// Tests GetCountAsync with query filter.
	/// </summary>
	[Fact]
	public async Task GetCountAsync_WithFilter_AppliesFilter()
	{
		// Arrange
		Uri? capturedUri = null;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUri = req.RequestUri)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("10", System.Text.Encoding.UTF8, "text/plain")
			});

		// Act
		var query = _client.For<Product>("Products").Filter(p => p.Price > 100);
		await _client.GetCountAsync(query, CancellationToken);

		// Assert
		capturedUri.Should().NotBeNull();
		capturedUri!.ToString().Should().Contain("$count");
		capturedUri.ToString().Should().Contain("$filter");
	}

	#endregion

	#region GetFirstOrDefaultAsync Tests

	/// <summary>
	/// Tests GetFirstOrDefaultAsync returns first entity.
	/// </summary>
	[Fact]
	public async Task GetFirstOrDefaultAsync_WithResults_ReturnsFirst()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, """{"value": [{"ID": 1, "Name": "First"}]}""");

		// Act
		var result = await _client.GetFirstOrDefaultAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		result.Should().NotBeNull();
		result!.Name.Should().Be("First");
	}

	/// <summary>
	/// Tests GetFirstOrDefaultAsync returns null for empty results.
	/// </summary>
	[Fact]
	public async Task GetFirstOrDefaultAsync_NoResults_ReturnsNull()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, """{"value": []}""");

		// Act
		var result = await _client.GetFirstOrDefaultAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		result.Should().BeNull();
	}

	/// <summary>
	/// Tests GetFirstOrDefaultAsync sets Top(1).
	/// </summary>
	[Fact]
	public async Task GetFirstOrDefaultAsync_SetsTopOne()
	{
		// Arrange
		Uri? capturedUri = null;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUri = req.RequestUri)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""{"value": []}""", System.Text.Encoding.UTF8, "application/json")
			});

		// Act
		await _client.GetFirstOrDefaultAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		capturedUri.Should().NotBeNull();
		capturedUri!.ToString().Should().Contain("$top=1");
	}

	#endregion

	#region GetSingleAsync Tests

	/// <summary>
	/// Tests GetSingleAsync returns single entity.
	/// </summary>
	[Fact]
	public async Task GetSingleAsync_OneResult_ReturnsEntity()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, """{"value": [{"ID": 1, "Name": "Single"}]}""");

		// Act
		var result = await _client.GetSingleAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		result.Should().NotBeNull();
		result.Name.Should().Be("Single");
	}

	/// <summary>
	/// Tests GetSingleAsync throws for no results.
	/// </summary>
	[Fact]
	public async Task GetSingleAsync_NoResults_ThrowsInvalidOperationException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, """{"value": []}""");

		// Act
		var act = async () => await _client.GetSingleAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*no elements*");
	}

	/// <summary>
	/// Tests GetSingleAsync throws for multiple results.
	/// </summary>
	[Fact]
	public async Task GetSingleAsync_MultipleResults_ThrowsInvalidOperationException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, """{"value": [{"ID": 1, "Name": "First"}, {"ID": 2, "Name": "Second"}]}""");

		// Act
		var act = async () => await _client.GetSingleAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*more than one*");
	}

	#endregion

	#region GetSingleOrDefaultAsync Tests

	/// <summary>
	/// Tests GetSingleOrDefaultAsync returns single entity.
	/// </summary>
	[Fact]
	public async Task GetSingleOrDefaultAsync_OneResult_ReturnsEntity()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, """{"value": [{"ID": 1, "Name": "Single"}]}""");

		// Act
		var result = await _client.GetSingleOrDefaultAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		result.Should().NotBeNull();
		result!.Name.Should().Be("Single");
	}

	/// <summary>
	/// Tests GetSingleOrDefaultAsync returns null for no results.
	/// </summary>
	[Fact]
	public async Task GetSingleOrDefaultAsync_NoResults_ReturnsNull()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, """{"value": []}""");

		// Act
		var result = await _client.GetSingleOrDefaultAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		result.Should().BeNull();
	}

	/// <summary>
	/// Tests GetSingleOrDefaultAsync throws for multiple results.
	/// </summary>
	[Fact]
	public async Task GetSingleOrDefaultAsync_MultipleResults_ThrowsInvalidOperationException()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, """{"value": [{"ID": 1, "Name": "First"}, {"ID": 2, "Name": "Second"}]}""");

		// Act
		var act = async () => await _client.GetSingleOrDefaultAsync(_client.For<Product>("Products"), CancellationToken);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*more than one*");
	}

	#endregion

	#region GetRawAsync Tests

	/// <summary>
	/// Tests GetRawAsync returns JsonDocument.
	/// </summary>
	[Fact]
	public async Task GetRawAsync_ReturnsJsonDocument()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, """{"value": [{"ID": 1}], "custom": "property"}""");

		// Act
		using var result = await _client.GetRawAsync("Products", cancellationToken: CancellationToken);

		// Assert
		result.RootElement.TryGetProperty("custom", out var customProp).Should().BeTrue();
		customProp.GetString().Should().Be("property");
	}

	/// <summary>
	/// Tests GetRawAsync with custom headers.
	/// </summary>
	[Fact]
	public async Task GetRawAsync_WithHeaders_IncludesHeaders()
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
				Content = new StringContent("""{}""", System.Text.Encoding.UTF8, "application/json")
			});

		var headers = new Dictionary<string, string> { { "X-Custom", "Value" } };

		// Act
		using var result = await _client.GetRawAsync("Products", headers, CancellationToken);

		// Assert
		capturedRequest.Should().NotBeNull();
		capturedRequest!.Headers.TryGetValues("X-Custom", out var values).Should().BeTrue();
		values.Should().Contain("Value");
	}

	#endregion

	#region GetByKeyWithETagAsync Tests

	/// <summary>
	/// Tests GetByKeyWithETagAsync returns entity with ETag.
	/// </summary>
	[Fact]
	public async Task GetByKeyWithETagAsync_ReturnsEntityWithETag()
	{
		// Arrange
		var response = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("""{"ID": 1, "Name": "Test"}""", System.Text.Encoding.UTF8, "application/json")
		};
		response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"etag-value\"");

		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);

		// Act
		var result = await _client.GetByKeyWithETagAsync<Product, int>(1, cancellationToken: CancellationToken);

		// Assert
		result.Value.Should().NotBeNull();
		result.Value!.Id.Should().Be(1);
		result.ETag.Should().Be("\"etag-value\"");
	}

	#endregion

	#region Helper Methods

	private void SetupMockResponse(HttpStatusCode statusCode, string content) => _mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage(statusCode)
			{
				Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
			});

	#endregion
}
