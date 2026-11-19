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

/// <summary>
/// An application-defined entity-set attribute used to exercise attribute-based resolvers.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class CollectionNameAttribute(string name) : Attribute
{
	public string Name { get; } = name;
}

[CollectionName("service_products")]
internal sealed class AttributeResolvedEntity { }

/// <summary>
/// Unit tests for how ODataClient resolves entity set names and navigation paths.
/// </summary>
public class ODataClientQueryTests : MockedODataClientTestBase
{
	#region For<T>() Tests

	/// <summary>
	/// Tests the entity set name derived by For&lt;T&gt;() with auto pluralization.
	/// </summary>
	[Fact]
	public void For_AutoPluralization_DerivesEntitySetNames()
	{
		Client.For<Product>().BuildUrl().Should().Be("Products");
		Client.For<Category>().BuildUrl().Should().Be("Categories");
		Client.For<Address>().BuildUrl().Should().Be("Addresses");
		Client.For<Product>("CustomProducts").BuildUrl().Should().Be("CustomProducts");
	}

	/// <summary>
	/// Tests that an [EntitySet] attribute overrides pluralization.
	/// </summary>
	[Fact]
	public void For_EntitySetAttribute_OverridesPluralization()
		=> Client.For<GeneratedMailbox>().BuildUrl().Should().Be("Mailbox");

	/// <summary>
	/// Tests that the configured resolver overrides the [EntitySet] attribute and pluralization.
	/// </summary>
	[Fact]
	public void For_EntitySetNameResolver_OverridesAttributeAndPluralization()
	{
		using var client = CreateClient(options => options.EntitySetNameResolver =
			type => type == typeof(GeneratedMailbox) ? "custom_mailboxes" : type == typeof(Product) ? "custom_products" : null);

		client.For<GeneratedMailbox>().BuildUrl().Should().Be("custom_mailboxes");
		client.For<Product>().BuildUrl().Should().Be("custom_products");
		client.For<Category>().BuildUrl().Should().Be("Categories");
	}

	/// <summary>
	/// Tests that a null, empty or whitespace resolver result falls back to the existing conventions.
	/// </summary>
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	public void For_EntitySetNameResolverReturnsNothing_UsesExistingConventions(string? resolved)
	{
		using var client = CreateClient(options => options.EntitySetNameResolver = _ => resolved);

		client.For<GeneratedMailbox>().BuildUrl().Should().Be("Mailbox");
		client.For<Product>().BuildUrl().Should().Be("Products");
	}

	/// <summary>
	/// Tests that an explicit entity set name does not invoke the resolver.
	/// </summary>
	[Fact]
	public void For_WithExplicitEntitySetName_DoesNotInvokeResolver()
	{
		using var client = CreateClient(options => options.EntitySetNameResolver =
			_ => throw new InvalidOperationException("Resolver should not be invoked."));

		client.For<Product>("CustomProducts").BuildUrl().Should().Be("CustomProducts");
	}

	/// <summary>
	/// Tests that the resolver receives the requested type.
	/// </summary>
	[Fact]
	public void For_EntitySetNameResolver_ReceivesRequestedType()
	{
		Type? observedType = null;
		using var client = CreateClient(options => options.EntitySetNameResolver = type =>
		{
			observedType = type;
			return null;
		});

		client.For<Product>().BuildUrl();

		observedType.Should().Be<Product>();
	}

	/// <summary>
	/// Tests that an attribute-based resolver reads the name from a custom attribute on the model.
	/// </summary>
	[Fact]
	public void For_EntitySetNameResolver_ResolvesFromCustomAttribute()
	{
		using var client = CreateClient(options => options.EntitySetNameResolver =
			type => type.GetCustomAttributes(typeof(CollectionNameAttribute), inherit: false)
				.OfType<CollectionNameAttribute>()
				.FirstOrDefault()?.Name);

		client.For<AttributeResolvedEntity>().BuildUrl().Should().Be("service_products");
	}

	/// <summary>
	/// Tests that disabling AutoPluralization keeps the type name as is.
	/// </summary>
	[Fact]
	public void For_AutoPluralizationDisabled_UsesTypeNameAsIs()
	{
		using var client = CreateClient(options => options.AutoPluralization = false);

		client.For<Product>().BuildUrl().Should().Be("Product");
		client.For<Address>().BuildUrl().Should().Be("Address");
	}

	#endregion

	#region NavigateTo Tests

	/// <summary>
	/// Tests the paths produced by NavigateTo.
	/// </summary>
	[Fact]
	public void NavigateTo_ProducesExpectedPaths()
	{
		Client.For<Product>("Products").Key(1).NavigateTo<Product>("RelatedProducts").BuildUrl()
			.Should().Be("Products(1)/RelatedProducts");
		Client.For<Person>("People").Key("russellwhyte").NavigateTo<Person>("Friends").BuildUrl()
			.Should().Be("People('russellwhyte')/Friends");
		Client.For<Person>("People").Key("russellwhyte").NavigateTo(x => x.Friends).BuildUrl()
			.Should().Be("People('russellwhyte')/Friends");
		Client.For<Person>("People").Key("russellwhyte").NavigateTo(x => x.BestFriend!.Friends).BuildUrl()
			.Should().Be("People('russellwhyte')/BestFriend/Friends");
		Client.For<Person>("People").Key("russellwhyte").NavigateTo(x => x.Friends).As<Person>().BuildUrl()
			.Should().Be("People('russellwhyte')/Friends");
	}

	/// <summary>
	/// Tests that a select applies to the navigated collection.
	/// </summary>
	[Fact]
	public void NavigateTo_WithSelect_ProducesCorrectUrl()
	{
		var url = Client.For<Person>("People").Key("russellwhyte").NavigateTo<Person>("Friends").Select(f => new { f.UserName, f.FirstName }).BuildUrl();

		url.Should().StartWith("People('russellwhyte')/Friends");
		url.Should().Contain("$select=");
		url.Should().Contain("UserName");
		url.Should().Contain("FirstName");
	}

	/// <summary>
	/// Tests that navigating without a key throws.
	/// </summary>
	[Fact]
	public void NavigateTo_WithoutKey_ThrowsInvalidOperationException()
	{
		var act = () => Client.For<Person>("People").NavigateTo<Person>("Friends");

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*Key()*");
	}

	/// <summary>
	/// Tests the full Simple.OData.Client-compatible chain:
	/// NavigateTo(expr).As&lt;T&gt;().GetAsync() sends a request to the correct URL.
	/// </summary>
	[Fact]
	public async Task NavigateTo_NonGenericExpr_As_GetAsync_SendsCorrectRequest()
	{
		// Arrange
		SetupCapturingResponse(JsonResponse("""{"value":[]}"""));

		// Act
		await Client.For<Person>("People")
			.Key("russellwhyte")
			.NavigateTo(x => x.Friends)
			.As<Person>()
			.GetAsync(CancellationToken);

		// Assert
		CapturedRequest!.RequestUri!.PathAndQuery.Should().Be("/People('russellwhyte')/Friends");
	}

	/// <summary>
	/// Tests the exact issue-thread pattern: NavigateTo(expr).As&lt;T&gt;().FindEntriesAsync() returns typed results.
	/// </summary>
	[Fact]
	public async Task NavigateTo_NonGenericExpr_As_FindEntriesAsync_ReturnsTypedResults()
	{
		// Arrange
		SetupCapturingResponse(JsonResponse("""{"value":[{"UserName":"scottketchum"},{"UserName":"russellwhyte"}]}"""));

		// Act
		var results = (await Client.For<Person>("People")
			.Key("russellwhyte")
			.NavigateTo(x => x.Friends)
			.As<Person>()
			.FindEntriesAsync(CancellationToken))
			.ToList();

		// Assert
		CapturedRequest!.RequestUri!.PathAndQuery.Should().Be("/People('russellwhyte')/Friends");
		results.Should().HaveCount(2);
	}

	/// <summary>
	/// Tests that FindEntriesAsync on FluentODataQueryBuilder returns results.
	/// </summary>
	[Fact]
	public async Task NavigateTo_NonGenericExpr_FindEntriesAsync_ReturnsResults()
	{
		// Arrange
		SetupCapturingResponse(JsonResponse("""{"value":[{"UserName":"scottketchum"}]}"""));

		// Act
		var results = (await Client.For<Person>("People")
			.Key("russellwhyte")
			.NavigateTo(x => x.Friends)
			.FindEntriesAsync(CancellationToken)).ToList();

		// Assert
		CapturedRequest!.RequestUri!.PathAndQuery.Should().Be("/People('russellwhyte')/Friends");
		results.Should().HaveCount(1);
		results[0].Should().ContainKey("UserName");
	}

	#endregion

	private static HttpResponseMessage JsonResponse(string json)
		=> new(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
}
