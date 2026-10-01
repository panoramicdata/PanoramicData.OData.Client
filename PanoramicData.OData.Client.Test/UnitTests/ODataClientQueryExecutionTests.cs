namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for the query execution methods on ODataClient - the Get* family, paging,
/// counting and raw responses.
/// </summary>
public class ODataClientQueryExecutionTests : MockedODataClientTestBase
{
	#region GetAsync Tests

	/// <summary>
	/// Tests GetAsync returns empty list for empty response.
	/// </summary>
	[Fact]
	public async Task GetAsync_EmptyResponse_ReturnsEmptyList()
	{
		// Arrange
		SetupResponse(HttpStatusCode.OK, """{"value": []}""");

		// Act
		var response = await Client.GetAsync(Client.For<Product>("Products"), CancellationToken);

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

		SetupResponse(response);

		// Act
		var result = await Client.GetAsync(Client.For<Product>("Products"), CancellationToken);

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

		SetupSendAsync()
			.ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responses.Dequeue(), System.Text.Encoding.UTF8, "application/json")
			});

		// Act
		var result = await Client.GetAllAsync(Client.For<Product>("Products"), CancellationToken);

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

		SetupSendAsync()
			.ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responses.Dequeue(), System.Text.Encoding.UTF8, "application/json")
			});

		// Act
		var result = await Client.GetAllAsync(Client.For<Product>("Products").Count(), CancellationToken);

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
		SetupResponse(HttpStatusCode.OK, """
			{
				"value": [{"ID": 1, "Name": "Product1"}],
				"@odata.nextLink": "https://test.odata.org/Products?$skip=1"
			}
			""");

		using var cts = new CancellationTokenSource();
		cts.Cancel();

		// Act
		var act = async () => await Client.GetAllAsync(Client.For<Product>("Products"), cts.Token);

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
		SetupResponse(HttpStatusCode.OK, "42");

		// Act
		var count = await Client.GetCountAsync<Product>(CancellationToken);

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
		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUri = req.RequestUri)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("10", System.Text.Encoding.UTF8, "text/plain")
			});

		// Act
		var query = Client.For<Product>("Products").Filter(p => p.Price > 100);
		await Client.GetCountAsync(query, CancellationToken);

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
		SetupResponse(HttpStatusCode.OK, """{"value": [{"ID": 1, "Name": "First"}]}""");

		// Act
		var result = await Client.GetFirstOrDefaultAsync(Client.For<Product>("Products"), CancellationToken);

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
		SetupResponse(HttpStatusCode.OK, """{"value": []}""");

		// Act
		var result = await Client.GetFirstOrDefaultAsync(Client.For<Product>("Products"), CancellationToken);

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
		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUri = req.RequestUri)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""{"value": []}""", System.Text.Encoding.UTF8, "application/json")
			});

		// Act
		await Client.GetFirstOrDefaultAsync(Client.For<Product>("Products"), CancellationToken);

		// Assert
		capturedUri.Should().NotBeNull();
		capturedUri!.ToString().Should().Contain("$top=1");
	}

	/// <summary>
	/// Tests GetFirstOrDefaultAsync with a key uses single-object deserialization (no $top, no {"value":[...]} wrapper).
	/// </summary>
	[Fact]
	public async Task GetFirstOrDefaultAsync_WithKey_DeserializesSingleObject()
	{
		// Arrange - single entity endpoints return a plain object, not {"value":[...]}
		Uri? capturedUri = null;
		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUri = req.RequestUri)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""{"ID": 1, "Name": "Test"}""", System.Text.Encoding.UTF8, "application/json")
			});

		// Act
		var result = await Client.GetFirstOrDefaultAsync(
			Client.For<Product>("Products").Key(1).QueryOptions("PropertySet=Delivery"),
			CancellationToken);

		// Assert
		capturedUri!.ToString().Should().NotContain("$top");
		capturedUri.ToString().Should().Contain("PropertySet=Delivery");
		result.Should().NotBeNull();
		result!.Name.Should().Be("Test");
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
		SetupResponse(HttpStatusCode.OK, """{"value": [{"ID": 1, "Name": "Single"}]}""");

		// Act
		var result = await Client.GetSingleAsync(Client.For<Product>("Products"), CancellationToken);

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
		SetupResponse(HttpStatusCode.OK, """{"value": []}""");

		// Act
		var act = async () => await Client.GetSingleAsync(Client.For<Product>("Products"), CancellationToken);

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
		SetupResponse(HttpStatusCode.OK, """{"value": [{"ID": 1, "Name": "First"}, {"ID": 2, "Name": "Second"}]}""");

		// Act
		var act = async () => await Client.GetSingleAsync(Client.For<Product>("Products"), CancellationToken);

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
		SetupResponse(HttpStatusCode.OK, """{"value": [{"ID": 1, "Name": "Single"}]}""");

		// Act
		var result = await Client.GetSingleOrDefaultAsync(Client.For<Product>("Products"), CancellationToken);

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
		SetupResponse(HttpStatusCode.OK, """{"value": []}""");

		// Act
		var result = await Client.GetSingleOrDefaultAsync(Client.For<Product>("Products"), CancellationToken);

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
		SetupResponse(HttpStatusCode.OK, """{"value": [{"ID": 1, "Name": "First"}, {"ID": 2, "Name": "Second"}]}""");

		// Act
		var act = async () => await Client.GetSingleOrDefaultAsync(Client.For<Product>("Products"), CancellationToken);

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
		SetupResponse(HttpStatusCode.OK, """{"value": [{"ID": 1}], "custom": "property"}""");

		// Act
		using var result = await Client.GetRawAsync("Products", cancellationToken: CancellationToken);

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
		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""{}""", System.Text.Encoding.UTF8, "application/json")
			});

		var headers = new Dictionary<string, string> { { "X-Custom", "Value" } };

		// Act
		using var result = await Client.GetRawAsync("Products", headers, CancellationToken);

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

		SetupResponse(response);

		// Act
		var result = await Client.GetByKeyWithETagAsync<Product, int>(1, cancellationToken: CancellationToken);

		// Assert
		result.Value.Should().NotBeNull();
		result.Value!.Id.Should().Be(1);
		result.ETag.Should().Be("\"etag-value\"");
	}

	#endregion
}
