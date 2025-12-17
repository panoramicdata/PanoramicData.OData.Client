namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for FluentODataQueryBuilder.
/// </summary>
public class FluentODataQueryBuilderTests : TestBase, IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;
	private readonly ODataClient _client;

	/// <summary>
	/// Initializes a new instance of the test class with mocked dependencies.
	/// </summary>
	public FluentODataQueryBuilderTests()
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

	#region BuildUrl Tests

	/// <summary>
	/// Tests basic entity set URL.
	/// </summary>
	[Fact]
	public void BuildUrl_EntitySetOnly_ReturnsEntitySet()
	{
		// Act
		var url = _client.For("Products").BuildUrl();

		// Assert
		url.Should().Be("Products");
	}

	/// <summary>
	/// Tests Key with integer.
	/// </summary>
	[Fact]
	public void BuildUrl_WithIntKey_FormatsCorrectly()
	{
		// Act
		var url = _client.For("Products").Key(123).BuildUrl();

		// Assert
		url.Should().Be("Products(123)");
	}

	/// <summary>
	/// Tests Key with string.
	/// </summary>
	[Fact]
	public void BuildUrl_WithStringKey_FormatsWithQuotes()
	{
		// Act
		var url = _client.For("Products").Key("abc").BuildUrl();

		// Assert
		url.Should().Be("Products('abc')");
	}

	/// <summary>
	/// Tests Key with string containing single quote.
	/// </summary>
	[Fact]
	public void BuildUrl_WithStringKeyContainingSingleQuote_EscapesQuote()
	{
		// Act
		var url = _client.For("Products").Key("O'Brien").BuildUrl();

		// Assert
		url.Should().Be("Products('O''Brien')");
	}

	/// <summary>
	/// Tests Key with Guid.
	/// </summary>
	[Fact]
	public void BuildUrl_WithGuidKey_FormatsCorrectly()
	{
		// Arrange
		var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");

		// Act
		var url = _client.For("Products").Key(guid).BuildUrl();

		// Assert
		url.Should().Be("Products(12345678-1234-1234-1234-123456789012)");
	}

	/// <summary>
	/// Tests Key with long.
	/// </summary>
	[Fact]
	public void BuildUrl_WithLongKey_FormatsCorrectly()
	{
		// Act
		var url = _client.For("Products").Key(9876543210L).BuildUrl();

		// Assert
		url.Should().Be("Products(9876543210)");
	}

	/// <summary>
	/// Tests Filter.
	/// </summary>
	[Fact]
	public void BuildUrl_WithFilter_AddsFilterParameter()
	{
		// Act
		var url = _client.For("Products").Filter("Price gt 100").BuildUrl();

		// Assert
		url.Should().Contain("$filter=");
		url.Should().Contain("Price%20gt%20100");
	}

	/// <summary>
	/// Tests multiple filters are combined with AND.
	/// </summary>
	[Fact]
	public void BuildUrl_MultipleFilters_CombinesWithAnd()
	{
		// Act
		var url = _client.For("Products")
			.Filter("Price gt 100")
			.Filter("Name eq 'Widget'")
			.BuildUrl();

		// Assert
		url.Should().Contain("$filter=");
		url.Should().Contain("and");
	}

	/// <summary>
	/// Tests Search.
	/// </summary>
	[Fact]
	public void BuildUrl_WithSearch_AddsSearchParameter()
	{
		// Act
		var url = _client.For("Products").Search("widget").BuildUrl();

		// Assert
		url.Should().Contain("$search=widget");
	}

	/// <summary>
	/// Tests Select.
	/// </summary>
	[Fact]
	public void BuildUrl_WithSelect_AddsSelectParameter()
	{
		// Act
		var url = _client.For("Products").Select("Name,Price").BuildUrl();

		// Assert
		url.Should().Contain("$select=Name,Price");
	}

	/// <summary>
	/// Tests Expand.
	/// </summary>
	[Fact]
	public void BuildUrl_WithExpand_AddsExpandParameter()
	{
		// Act
		var url = _client.For("Products").Expand("Category,Supplier").BuildUrl();

		// Assert
		url.Should().Contain("$expand=Category,Supplier");
	}

	/// <summary>
	/// Tests OrderBy.
	/// </summary>
	[Fact]
	public void BuildUrl_WithOrderBy_AddsOrderByParameter()
	{
		// Act
		var url = _client.For("Products").OrderBy("Name").BuildUrl();

		// Assert
		url.Should().Contain("$orderby=Name");
	}

	/// <summary>
	/// Tests OrderByDescending.
	/// </summary>
	[Fact]
	public void BuildUrl_WithOrderByDescending_AddsDescendingOrderBy()
	{
		// Act
		var url = _client.For("Products").OrderByDescending("Price").BuildUrl();

		// Assert
		url.Should().Contain("$orderby=Price%20desc");
	}

	/// <summary>
	/// Tests Skip.
	/// </summary>
	[Fact]
	public void BuildUrl_WithSkip_AddsSkipParameter()
	{
		// Act
		var url = _client.For("Products").Skip(10).BuildUrl();

		// Assert
		url.Should().Contain("$skip=10");
	}

	/// <summary>
	/// Tests Top.
	/// </summary>
	[Fact]
	public void BuildUrl_WithTop_AddsTopParameter()
	{
		// Act
		var url = _client.For("Products").Top(5).BuildUrl();

		// Assert
		url.Should().Contain("$top=5");
	}

	/// <summary>
	/// Tests Count.
	/// </summary>
	[Fact]
	public void BuildUrl_WithCount_AddsCountParameter()
	{
		// Act
		var url = _client.For("Products").Count().BuildUrl();

		// Assert
		url.Should().Contain("$count=true");
	}

	/// <summary>
	/// Tests Function.
	/// </summary>
	[Fact]
	public void BuildUrl_WithFunction_AddsFunctionPath()
	{
		// Act
		var url = _client.For("Products").Function("GetTopSelling").BuildUrl();

		// Assert
		url.Should().Contain("Products/GetTopSelling()");
	}

	/// <summary>
	/// Tests Function with parameters.
	/// </summary>
	[Fact]
	public void BuildUrl_WithFunctionAndParameters_FormatsParameters()
	{
		// Act
		var url = _client.For("Products")
			.Function("Search", new { Term = "widget", MaxResults = 10 })
			.BuildUrl();

		// Assert
		url.Should().Contain("Products/Search(");
		url.Should().Contain("Term='widget'");
		url.Should().Contain("MaxResults=10");
	}

	/// <summary>
	/// Tests Apply.
	/// </summary>
	[Fact]
	public void BuildUrl_WithApply_AddsApplyParameter()
	{
		// Act
		var url = _client.For("Products").Apply("groupby((Category),aggregate(Price with sum as TotalPrice))").BuildUrl();

		// Assert
		url.Should().Contain("$apply=");
	}

	/// <summary>
	/// Tests WithHeader.
	/// </summary>
	[Fact]
	public void WithHeader_AddsToCustomHeaders()
	{
		// Act
		var builder = _client.For("Products").WithHeader("X-Custom", "Value");

		// Assert
		builder.CustomHeaders.Should().ContainKey("X-Custom");
		builder.CustomHeaders["X-Custom"].Should().Be("Value");
	}

	/// <summary>
	/// Tests combined query options.
	/// </summary>
	[Fact]
	public void BuildUrl_CombinedOptions_IncludesAllParameters()
	{
		// Act
		var url = _client.For("Products")
			.Filter("Price gt 100")
			.Select("Name,Price")
			.OrderBy("Name")
			.Skip(10)
			.Top(5)
			.Count()
			.BuildUrl();

		// Assert
		url.Should().Contain("$filter=");
		url.Should().Contain("$select=Name,Price");
		url.Should().Contain("$orderby=Name");
		url.Should().Contain("$skip=10");
		url.Should().Contain("$top=5");
		url.Should().Contain("$count=true");
	}

	#endregion

	#region Execution Tests

	/// <summary>
	/// Tests GetAsync returns results.
	/// </summary>
	[Fact]
	public async Task GetAsync_ReturnsResults()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, """{"value": [{"ID": 1, "Name": "Test"}]}""");

		// Act
		var response = await _client.For("Products").GetAsync(CancellationToken);

		// Assert
		response.Value.Should().ContainSingle();
	}

	/// <summary>
	/// Tests GetJsonAsync returns JsonDocument.
	/// </summary>
	[Fact]
	public async Task GetJsonAsync_ReturnsJsonDocument()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, """{"value": [{"ID": 1}], "custom": "data"}""");

		// Act
		using var json = await _client.For("Products").GetJsonAsync(CancellationToken);

		// Assert
		json.RootElement.TryGetProperty("custom", out var customProp).Should().BeTrue();
		customProp.GetString().Should().Be("data");
	}

	/// <summary>
	/// Tests GetAllAsync follows pagination.
	/// </summary>
	[Fact]
	public async Task GetAllAsync_FollowsPagination()
	{
		// Arrange
		var responses = new Queue<string>();
		responses.Enqueue("""
			{
				"value": [{"ID": 1}],
				"@odata.nextLink": "https://test.odata.org/Products?$skip=1"
			}
			""");
		responses.Enqueue("""{"value": [{"ID": 2}]}""");

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
		var response = await _client.For("Products").GetAllAsync(CancellationToken);

		// Assert
		response.Value.Should().HaveCount(2);
	}

	/// <summary>
	/// Tests GetEntryAsync returns single entity.
	/// </summary>
	[Fact]
	public async Task GetEntryAsync_ReturnsSingleEntity()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, """{"ID": 1, "Name": "Test"}""");

		// Act
		var entry = await _client.For("Products").Key(1).GetEntryAsync(CancellationToken);

		// Assert
		entry.Should().NotBeNull();
		entry!["Name"].Should().Be("Test");
	}

	/// <summary>
	/// Tests GetFirstOrDefaultAsync returns first entity.
	/// </summary>
	[Fact]
	public async Task GetFirstOrDefaultAsync_ReturnsFirstEntity()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, """{"value": [{"ID": 1, "Name": "First"}]}""");

		// Act
		var entry = await _client.For("Products").GetFirstOrDefaultAsync(CancellationToken);

		// Assert
		entry.Should().NotBeNull();
		entry!["Name"].Should().Be("First");
	}

	/// <summary>
	/// Tests GetFirstOrDefaultAsync returns null for empty.
	/// </summary>
	[Fact]
	public async Task GetFirstOrDefaultAsync_NoResults_ReturnsNull()
	{
		// Arrange
		SetupMockResponse(HttpStatusCode.OK, """{"value": []}""");

		// Act
		var entry = await _client.For("Products").GetFirstOrDefaultAsync(CancellationToken);

		// Assert
		entry.Should().BeNull();
	}

	/// <summary>
	/// Tests DeleteAsync sends delete request.
	/// </summary>
	[Fact]
	public async Task DeleteAsync_SendsDeleteRequest()
	{
		// Arrange
		HttpMethod? capturedMethod = null;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedMethod = req.Method)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

		// Act
		await _client.For("Products").Key(1).DeleteAsync(CancellationToken);

		// Assert
		capturedMethod.Should().Be(HttpMethod.Delete);
	}

	/// <summary>
	/// Tests DeleteEntryAsync is alias for DeleteAsync.
	/// </summary>
	[Fact]
	public async Task DeleteEntryAsync_SendsDeleteRequest()
	{
		// Arrange
		HttpMethod? capturedMethod = null;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedMethod = req.Method)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

		// Act
		await _client.For("Products").Key(1).DeleteEntryAsync(CancellationToken);

		// Assert
		capturedMethod.Should().Be(HttpMethod.Delete);
	}

	#endregion

	#region Function Parameter Formatting Tests

	/// <summary>
	/// Tests function parameter formatting for DateTime.
	/// </summary>
	[Fact]
	public void BuildUrl_FunctionWithDateTimeParameter_FormatsCorrectly()
	{
		// Arrange
		var date = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

		// Act
		var url = _client.For("Orders").Function("GetByDate", new { Date = date }).BuildUrl();

		// Assert
		url.Should().Contain("Date=");
		url.Should().Contain("2024");
	}

	/// <summary>
	/// Tests function parameter formatting for boolean.
	/// </summary>
	[Fact]
	public void BuildUrl_FunctionWithBooleanParameter_FormatsLowercase()
	{
		// Act
		var url = _client.For("Products").Function("Filter", new { Active = true }).BuildUrl();

		// Assert
		url.Should().Contain("Active=true");
	}

	/// <summary>
	/// Tests function parameter formatting for null.
	/// </summary>
	[Fact]
	public void BuildUrl_FunctionWithNullParameter_FormatsAsNull()
	{
		// Act
		var url = _client.For("Products").Function("Search", new { Term = (string?)null }).BuildUrl();

		// Assert
		url.Should().Contain("Term=null");
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
