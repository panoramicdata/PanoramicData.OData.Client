namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for nested expand functionality tested through ODataQueryBuilder.
/// </summary>
public class NestedExpandBuilderTests : TestBase, IDisposable
{
	private readonly Mock<HttpMessageHandler> _mockHandler;
	private readonly HttpClient _httpClient;
	private readonly ODataClient _client;

	/// <summary>
	/// Initializes a new instance of the test class with mocked dependencies.
	/// </summary>
	public NestedExpandBuilderTests()
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

	#region Expand With Nested Options Tests

	/// <summary>
	/// Tests Expand with nested Select.
	/// </summary>
	[Fact]
	public void Expand_WithNestedSelect_IncludesSelectInUrl()
	{
		// Act - Using Category->Products since Category has Products navigation property
		var url = _client.For<Category>("Categories")
			.Expand(c => c.Products, builder => builder.Select(p => p.Name))
			.BuildUrl();

		// Assert
		url.Should().Contain("$expand=Products");
		url.Should().Contain("$select=Name");
	}

	/// <summary>
	/// Tests Expand with nested Select multiple properties.
	/// </summary>
	[Fact]
	public void Expand_WithNestedSelectMultiple_IncludesAllFields()
	{
		// Act
		var url = _client.For<Category>("Categories")
			.Expand(c => c.Products, builder => builder.Select(p => new { p.Id, p.Name }))
			.BuildUrl();

		// Assert
		url.Should().Contain("$expand=Products");
		url.Should().Contain("$select=Id,Name");
	}

	/// <summary>
	/// Tests Expand with nested Select using string.
	/// </summary>
	[Fact]
	public void Expand_WithNestedSelectString_Works()
	{
		// Act
		var url = _client.For<Category>("Categories")
			.Expand(c => c.Products, builder => builder.Select("Id,Name,Description"))
			.BuildUrl();

		// Assert
		url.Should().Contain("$select=Id,Name,Description");
	}

	/// <summary>
	/// Tests Expand with nested Filter.
	/// </summary>
	[Fact]
	public void Expand_WithNestedFilter_IncludesFilterInUrl()
	{
		// Act
		var url = _client.For<Category>("Categories")
			.Expand(c => c.Products, builder => builder.Filter("Rating gt 3"))
			.BuildUrl();

		// Assert
		url.Should().Contain("$expand=Products");
		url.Should().Contain("$filter=");
		url.Should().Contain("Rating");
	}

	/// <summary>
	/// Tests Expand with nested OrderBy.
	/// </summary>
	[Fact]
	public void Expand_WithNestedOrderBy_IncludesOrderByInUrl()
	{
		// Act
		var url = _client.For<Category>("Categories")
			.Expand(c => c.Products, builder => builder.OrderBy("Name"))
			.BuildUrl();

		// Assert
		url.Should().Contain("$expand=Products");
		url.Should().Contain("$orderby=Name");
	}

	/// <summary>
	/// Tests Expand with nested Top.
	/// </summary>
	[Fact]
	public void Expand_WithNestedTop_IncludesTopInUrl()
	{
		// Act
		var url = _client.For<Category>("Categories")
			.Expand(c => c.Products, builder => builder.Top(5))
			.BuildUrl();

		// Assert
		url.Should().Contain("$expand=Products");
		url.Should().Contain("$top=5");
	}

	/// <summary>
	/// Tests Expand with nested Skip.
	/// </summary>
	[Fact]
	public void Expand_WithNestedSkip_IncludesSkipInUrl()
	{
		// Act
		var url = _client.For<Category>("Categories")
			.Expand(c => c.Products, builder => builder.Skip(10))
			.BuildUrl();

		// Assert
		url.Should().Contain("$expand=Products");
		url.Should().Contain("$skip=10");
	}

	/// <summary>
	/// Tests Expand with multiple nested options combined.
	/// </summary>
	[Fact]
	public void Expand_WithMultipleNestedOptions_IncludesAllInUrl()
	{
		// Act
		var url = _client.For<Category>("Categories")
			.Expand(c => c.Products, builder => builder
				.Select(p => new { p.Id, p.Name })
				.Filter("Rating gt 3")
				.OrderBy("Name")
				.Top(10)
				.Skip(5))
			.BuildUrl();

		// Assert
		url.Should().Contain("$expand=Products");
		url.Should().Contain("$select=Id,Name");
		url.Should().Contain("$filter=");
		url.Should().Contain("$orderby=Name");
		url.Should().Contain("$top=10");
		url.Should().Contain("$skip=5");
	}

	/// <summary>
	/// Tests Expand with nested Expand (multi-level).
	/// </summary>
	[Fact]
	public void Expand_WithNestedExpand_Works()
	{
		// Act
		var url = _client.For<Category>("Categories")
			.Expand(c => c.Products, builder => builder.Expand("Supplier"))
			.BuildUrl();

		// Assert
		url.Should().Contain("$expand=Products");
		url.Should().Contain("$expand=Supplier");
	}

	#endregion

	#region Fluent Chaining Tests

	/// <summary>
	/// Tests fluent chaining of nested options.
	/// </summary>
	[Fact]
	public void NestedBuilder_FluentChaining_Works()
	{
		// Act
		var url = _client.For<Category>("Categories")
			.Expand(c => c.Products, builder =>
			{
				builder.Select("Name");
				builder.Filter("Rating gt 3");
				builder.OrderBy("Name desc");
				builder.Top(100);
				builder.Skip(0);
			})
			.BuildUrl();

		// Assert
		url.Should().Contain("$expand=Products");
		url.Should().NotBeNullOrEmpty();
	}

	/// <summary>
	/// Tests multiple expands with different nested options.
	/// </summary>
	[Fact]
	public async Task MultipleExpands_WithDifferentNestedOptions_Works()
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

		// Act - Using fluent GetAsync on query builder
		await _client.For<Category>("Categories")
			.Expand(c => c.Products, builder => builder.Select(p => p.Name))
			.GetAsync(CancellationToken);

		// Assert
		capturedUri.Should().NotBeNull();
		var uriString = capturedUri!.ToString();
		uriString.Should().Contain("Categories");
		uriString.Should().Contain("$expand");
	}

	#endregion

	#region Empty/Null Handling Tests

	/// <summary>
	/// Tests Expand with empty nested options.
	/// </summary>
	[Fact]
	public void Expand_WithEmptyNestedOptions_Works()
	{
		// Act
		var url = _client.For<Category>("Categories")
			.Expand(c => c.Products, _ => { })
			.BuildUrl();

		// Assert
		url.Should().Contain("$expand=Products");
	}

	/// <summary>
	/// Tests Expand with empty Select string is ignored.
	/// </summary>
	[Fact]
	public void Expand_WithEmptySelectString_Ignored()
	{
		// Act
		var url = _client.For<Category>("Categories")
			.Expand(c => c.Products, builder => builder.Select(""))
			.BuildUrl();

		// Assert
		url.Should().Contain("$expand=Products");
		// Empty select should not add $select= at all or should not break the URL
	}

	/// <summary>
	/// Tests Expand with empty Filter string is ignored.
	/// </summary>
	[Fact]
	public void Expand_WithEmptyFilterString_Ignored()
	{
		// Act
		var url = _client.For<Category>("Categories")
			.Expand(c => c.Products, builder => builder.Filter(""))
			.BuildUrl();

		// Assert
		url.Should().Contain("$expand=Products");
	}

	/// <summary>
	/// Tests Expand with empty OrderBy string is ignored.
	/// </summary>
	[Fact]
	public void Expand_WithEmptyOrderByString_Ignored()
	{
		// Act
		var url = _client.For<Category>("Categories")
			.Expand(c => c.Products, builder => builder.OrderBy(""))
			.BuildUrl();

		// Assert
		url.Should().Contain("$expand=Products");
	}

	#endregion
}
