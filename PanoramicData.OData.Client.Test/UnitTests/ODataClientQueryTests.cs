namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Simulates a Microsoft.OData.Client-generated DTO with [EntitySet] attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class EntitySetAttribute(string entitySet) : Attribute
{
	public string EntitySet { get; } = entitySet;
}

[EntitySet("Mailbox")]
internal sealed class GeneratedMailbox { }

internal sealed class CustomResolvedEntity { }

/// <summary>
/// Mirrors an application-defined entity-set attribute (as used by the Integration Team OData
/// extensions) to exercise attribute-based <see cref="ODataClientOptions.EntitySetNameResolver"/> wiring.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class CollectionNameAttribute(string name) : Attribute
{
	public string Name { get; } = name;
}

[CollectionName("service_products")]
internal sealed class AttributeResolvedEntity { }

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
	public void For_EntityNameEndingInS_Uses_Es()
	{
		// Act
		var query = _client.For<Address>();

		// Assert
		var url = query.BuildUrl();
		url.Should().Be("Addresses");
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

	/// <summary>
	/// Tests that [EntitySet("...")] attribute on generated DTOs is respected, overriding pluralization.
	/// </summary>
	[Fact]
	public void For_EntitySetAttribute_OverridesPluralization()
	{
		// Act - GeneratedMailbox has [EntitySet("Mailbox")] so should produce "Mailbox" not "GeneratedMailboxes"
		var url = _client.For<GeneratedMailbox>().BuildUrl();

		// Assert
		url.Should().Be("Mailbox");
	}

	/// <summary>
	/// Tests that the configured entity set name resolver overrides the built-in conventions.
	/// </summary>
	[Fact]
	public void For_EntitySetNameResolver_UsesResolvedName()
	{
		using var httpClient = new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("https://test.odata.org/") };
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 0,
			EntitySetNameResolver = type => type == typeof(CustomResolvedEntity) ? "custom_entities" : null
		});

		var url = client.For<CustomResolvedEntity>().BuildUrl();

		url.Should().Be("custom_entities");
	}

	/// <summary>
	/// Tests that the configured entity set name resolver takes precedence over the entity set attribute.
	/// </summary>
	[Fact]
	public void For_EntitySetNameResolver_OverridesEntitySetAttribute()
	{
		using var httpClient = new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("https://test.odata.org/") };
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 0,
			EntitySetNameResolver = type => type == typeof(GeneratedMailbox) ? "custom_mailboxes" : null
		});

		var url = client.For<GeneratedMailbox>().BuildUrl();

		url.Should().Be("custom_mailboxes");
	}

	/// <summary>
	/// Tests that a whitespace resolver result uses the existing entity set attribute convention.
	/// </summary>
	[Fact]
	public void For_EntitySetNameResolverReturnsWhitespace_UsesEntitySetAttribute()
	{
		using var httpClient = new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("https://test.odata.org/") };
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 0,
			EntitySetNameResolver = _ => " "
		});

		var url = client.For<GeneratedMailbox>().BuildUrl();

		url.Should().Be("Mailbox");
	}

	/// <summary>
	/// Tests that a null resolver result uses the existing pluralization convention.
	/// </summary>
	[Fact]
	public void For_EntitySetNameResolverReturnsNull_UsesPluralization()
	{
		using var httpClient = new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("https://test.odata.org/") };
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 0,
			EntitySetNameResolver = _ => null
		});

		var url = client.For<Product>().BuildUrl();

		url.Should().Be("Products");
	}

	/// <summary>
	/// Tests that an explicit entity set name does not invoke the configured resolver.
	/// </summary>
	[Fact]
	public void For_WithExplicitEntitySetName_DoesNotInvokeResolver()
	{
		using var httpClient = new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("https://test.odata.org/") };
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 0,
			EntitySetNameResolver = _ => throw new InvalidOperationException("Resolver should not be invoked.")
		});

		var url = client.For<Product>("CustomProducts").BuildUrl();

		url.Should().Be("CustomProducts");
	}

	/// <summary>
	/// Tests that an empty resolver result falls back to the existing pluralization convention.
	/// </summary>
	[Fact]
	public void For_EntitySetNameResolverReturnsEmpty_UsesPluralization()
	{
		using var httpClient = new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("https://test.odata.org/") };
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 0,
			EntitySetNameResolver = _ => string.Empty
		});

		var url = client.For<Product>().BuildUrl();

		url.Should().Be("Products");
	}

	/// <summary>
	/// Tests that the resolver is invoked with the requested entity type.
	/// </summary>
	[Fact]
	public void For_EntitySetNameResolver_ReceivesRequestedType()
	{
		Type? observedType = null;
		using var httpClient = new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("https://test.odata.org/") };
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 0,
			EntitySetNameResolver = type =>
			{
				observedType = type;
				return null;
			}
		});

		client.For<Product>().BuildUrl();

		observedType.Should().Be<Product>();
	}

	/// <summary>
	/// Tests that an attribute-based resolver (the Integration Team OData extensions pattern)
	/// resolves the entity set name from a custom attribute on the model type.
	/// </summary>
	[Fact]
	public void For_EntitySetNameResolver_ResolvesFromCustomAttribute()
	{
		using var httpClient = new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("https://test.odata.org/") };
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 0,
			EntitySetNameResolver = type => type
				.GetCustomAttributes(typeof(CollectionNameAttribute), inherit: false)
				.OfType<CollectionNameAttribute>()
				.FirstOrDefault()?.Name
		});

		var url = client.For<AttributeResolvedEntity>().BuildUrl();

		url.Should().Be("service_products");
	}

	/// <summary>
	/// Tests that AutoPluralization = false uses the type name as-is.
	/// </summary>
	[Fact]
	public void For_AutoPluralizationDisabled_UsesTypeNameAsIs()
	{
		// Arrange
		using var httpClient = new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("https://test.odata.org/") };
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 0,
			AutoPluralization = false
		});

		// Act - type name is "Product"; with AutoPluralization=false it should stay "Product"
		var url = client.For<Product>().BuildUrl();

		// Assert
		url.Should().Be("Product");
	}

	/// <summary>
	/// Tests that AutoPluralization = false also works for types that would otherwise get 'es' appended.
	/// </summary>
	[Fact]
	public void For_AutoPluralizationDisabled_AddressStaysSingular()
	{
		// Arrange - simulates APIs where endpoint is /Address not /Addresses
		using var httpClient = new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("https://test.odata.org/") };
		using var client = new ODataClient(new ODataClientOptions
		{
			BaseUrl = "https://test.odata.org/",
			HttpClient = httpClient,
			Logger = NullLogger.Instance,
			RetryCount = 0,
			AutoPluralization = false
		});

		// Act
		var url = client.For<Address>().BuildUrl();

		// Assert - "Address" should NOT become "Addresses"
		url.Should().Be("Address");
	}

	#endregion

	#region NavigateTo Tests

	/// <summary>
	/// Tests NavigateTo with string property name produces the correct path.
	/// </summary>
	[Fact]
	public void NavigateTo_WithStringPropertyName_ProducesCorrectPath()
	{
		// Act
		var url = _client.For<Product>("Products").Key(1).NavigateTo<Product>("RelatedProducts").BuildUrl();

		// Assert
		url.Should().Be("Products(1)/RelatedProducts");
	}

	/// <summary>
	/// Tests NavigateTo using a string name for a collection navigation property.
	/// </summary>
	[Fact]
	public void NavigateTo_WithCollectionExpression_ProducesCorrectPath()
	{
		// Act
		var url = _client.For<Person>("People").Key("russellwhyte").NavigateTo<Person>("Friends").BuildUrl();

		// Assert
		url.Should().Be("People('russellwhyte')/Friends");
	}

	/// <summary>
	/// Tests NavigateTo with additional query options produces the correct URL.
	/// </summary>
	[Fact]
	public void NavigateTo_WithSelect_ProducesCorrectUrl()
	{
		// Act
		var url = _client.For<Person>("People").Key("russellwhyte").NavigateTo<Person>("Friends").Select(f => new { f.UserName, f.FirstName }).BuildUrl();

		// Assert
		url.Should().StartWith("People('russellwhyte')/Friends");
		url.Should().Contain("$select=");
		url.Should().Contain("UserName");
		url.Should().Contain("FirstName");
	}

	/// <summary>
	/// Tests NavigateTo without a key throws InvalidOperationException.
	/// </summary>
	[Fact]
	public void NavigateTo_WithoutKey_ThrowsInvalidOperationException()
	{
		// Act
		var act = () => _client.For<Person>("People").NavigateTo<Person>("Friends");

		// Assert
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*Key()*");
	}

	/// <summary>
	/// Tests that non-generic NavigateTo(expr) extracts the property name and returns a FluentODataQueryBuilder.
	/// This mirrors the Simple.OData.Client NavigateTo(x => x.NavProp) pattern.
	/// </summary>
	[Fact]
	public void NavigateTo_NonGenericExpr_ProducesCorrectPath()
	{
		// Act
		var url = _client.For<Person>("People").Key("russellwhyte").NavigateTo(x => x.Friends).BuildUrl();

		// Assert
		url.Should().Be("People('russellwhyte')/Friends");
	}

	/// <summary>
	/// Tests that non-generic NavigateTo(expr) with a nested (dotted) member path resolves the
	/// full navigation path. NavigateTo shares GetMemberName with OrderBy; both now walk the
	/// full chain instead of returning just the leaf segment.
	/// </summary>
	[Fact]
	public void NavigateTo_NonGenericExprNested_ResolvesFullPath()
	{
		// Act
		var url = _client.For<Person>("People").Key("russellwhyte").NavigateTo(x => x.BestFriend!.Friends).BuildUrl();

		// Assert
		url.Should().Be("People('russellwhyte')/BestFriend/Friends");
	}

	/// <summary>
	/// Tests that As&lt;T&gt;() after non-generic NavigateTo preserves the navigation path.
	/// </summary>
	[Fact]
	public void NavigateTo_NonGenericExpr_As_PreservesPath()
	{
		// Act
		var url = _client.For<Person>("People").Key("russellwhyte").NavigateTo(x => x.Friends).As<Person>().BuildUrl();

		// Assert
		url.Should().Be("People('russellwhyte')/Friends");
	}

	/// <summary>
	/// Tests the full Simple.OData.Client-compatible chain:
	/// NavigateTo(expr).As&lt;T&gt;().GetAsync() sends a request to the correct URL.
	/// </summary>
	[Fact]
	public async Task NavigateTo_NonGenericExpr_As_GetAsync_SendsCorrectRequest()
	{
		// Arrange
		string? capturedUrl = null;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUrl = req.RequestUri?.PathAndQuery)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""{"value":[]}""", System.Text.Encoding.UTF8, "application/json")
			});

		// Act
		await _client.For<Person>("People")
			.Key("russellwhyte")
			.NavigateTo(x => x.Friends)
			.As<Person>()
			.GetAsync(CancellationToken);

		// Assert
		capturedUrl.Should().Be("/People('russellwhyte')/Friends");
	}

	/// <summary>
	/// Tests the exact issue-thread pattern: NavigateTo(expr).As&lt;T&gt;().FindEntriesAsync() returns typed results.
	/// </summary>
	[Fact]
	public async Task NavigateTo_NonGenericExpr_As_FindEntriesAsync_ReturnsTypedResults()
	{
		// Arrange
		string? capturedUrl = null;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUrl = req.RequestUri?.PathAndQuery)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(
					"""{"value":[{"UserName":"scottketchum"},{"UserName":"russellwhyte"}]}""",
					System.Text.Encoding.UTF8, "application/json")
			});

		// Act
		var results = (await _client.For<Person>("People")
			.Key("russellwhyte")
			.NavigateTo(x => x.Friends)
			.As<Person>()
			.FindEntriesAsync(CancellationToken))
			.ToList();

		// Assert
		capturedUrl.Should().Be("/People('russellwhyte')/Friends");
		results.Should().HaveCount(2);
	}

	/// <summary>
	/// Tests that FindEntriesAsync on FluentODataQueryBuilder returns results.
	/// </summary>
	[Fact]
	public async Task NavigateTo_NonGenericExpr_FindEntriesAsync_ReturnsResults()
	{
		// Arrange
		string? capturedUrl = null;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUrl = req.RequestUri?.PathAndQuery)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(
					"""{"value":[{"UserName":"scottketchum"}]}""",
					System.Text.Encoding.UTF8, "application/json")
			});

		// Act
		var results = (await _client.For<Person>("People")
			.Key("russellwhyte")
			.NavigateTo(x => x.Friends)
			.FindEntriesAsync(CancellationToken)).ToList();

		// Assert
		capturedUrl.Should().Be("/People('russellwhyte')/Friends");
		results.Should().HaveCount(1);
		results[0].Should().ContainKey("UserName");
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

	/// <summary>
	/// Tests GetFirstOrDefaultAsync with a key uses single-object deserialization (no $top, no {"value":[...]} wrapper).
	/// </summary>
	[Fact]
	public async Task GetFirstOrDefaultAsync_WithKey_DeserializesSingleObject()
	{
		// Arrange - single entity endpoints return a plain object, not {"value":[...]}
		Uri? capturedUri = null;
		_mockHandler.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUri = req.RequestUri)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""{"ID": 1, "Name": "Test"}""", System.Text.Encoding.UTF8, "application/json")
			});

		// Act
		var result = await _client.GetFirstOrDefaultAsync(
			_client.For<Product>("Products").Key(1).QueryOptions("PropertySet=Delivery"),
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
