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
/// Unit tests for how ODataClient resolves entity set names and navigation paths.
/// </summary>
public class ODataClientQueryTests : MockedODataClientTestBase
{
	#region For<T>() Tests

	/// <summary>
	/// Tests that For&lt;T&gt;() auto-generates entity set name with simple pluralization.
	/// </summary>
	[Fact]
	public void For_AutoEntitySetName_Pluralizes()
	{
		// Act
		var query = Client.For<Product>();

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
		var query = Client.For<Category>();

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
		var query = Client.For<Address>();

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
		var query = Client.For<Product>("CustomProducts");

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
		var url = Client.For<GeneratedMailbox>().BuildUrl();

		// Assert
		url.Should().Be("Mailbox");
	}

	/// <summary>
	/// Tests that AutoPluralization = false uses the type name as-is.
	/// </summary>
	[Fact]
	public void For_AutoPluralizationDisabled_UsesTypeNameAsIs()
	{
		// Arrange
		using var httpClient = new HttpClient(MockHandler.Object) { BaseAddress = new Uri("https://test.odata.org/") };
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
		using var httpClient = new HttpClient(MockHandler.Object) { BaseAddress = new Uri("https://test.odata.org/") };
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
		var url = Client.For<Product>("Products").Key(1).NavigateTo<Product>("RelatedProducts").BuildUrl();

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
		var url = Client.For<Person>("People").Key("russellwhyte").NavigateTo<Person>("Friends").BuildUrl();

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
		var url = Client.For<Person>("People").Key("russellwhyte").NavigateTo<Person>("Friends").Select(f => new { f.UserName, f.FirstName }).BuildUrl();

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
		var act = () => Client.For<Person>("People").NavigateTo<Person>("Friends");

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
		var url = Client.For<Person>("People").Key("russellwhyte").NavigateTo(x => x.Friends).BuildUrl();

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
		var url = Client.For<Person>("People").Key("russellwhyte").NavigateTo(x => x.BestFriend!.Friends).BuildUrl();

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
		var url = Client.For<Person>("People").Key("russellwhyte").NavigateTo(x => x.Friends).As<Person>().BuildUrl();

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
		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUrl = req.RequestUri?.PathAndQuery)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("""{"value":[]}""", System.Text.Encoding.UTF8, "application/json")
			});

		// Act
		await Client.For<Person>("People")
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
		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUrl = req.RequestUri?.PathAndQuery)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(
					"""{"value":[{"UserName":"scottketchum"},{"UserName":"russellwhyte"}]}""",
					System.Text.Encoding.UTF8, "application/json")
			});

		// Act
		var results = (await Client.For<Person>("People")
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
		SetupSendAsync()
			.Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUrl = req.RequestUri?.PathAndQuery)
			.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(
					"""{"value":[{"UserName":"scottketchum"}]}""",
					System.Text.Encoding.UTF8, "application/json")
			});

		// Act
		var results = (await Client.For<Person>("People")
			.Key("russellwhyte")
			.NavigateTo(x => x.Friends)
			.FindEntriesAsync(CancellationToken)).ToList();

		// Assert
		capturedUrl.Should().Be("/People('russellwhyte')/Friends");
		results.Should().HaveCount(1);
		results[0].Should().ContainKey("UserName");
	}

	#endregion
}
