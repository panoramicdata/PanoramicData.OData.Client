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
