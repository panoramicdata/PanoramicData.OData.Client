using AwesomeAssertions;
using PanoramicData.OData.Client.Test.Fixtures;
using System.Globalization;

namespace PanoramicData.OData.Client.Test.IntegrationTests;

/// <summary>
/// Integration tests for the fluent (non-generic) API pattern.
/// Tests the For(string).GetAsync() fluent execution pattern.
/// </summary>
/// <remarks>
/// Initializes a new instance of the test class.
/// </remarks>
public class FluentApiIntegrationTests(ODataClientFixture fixture) : TestBase, IClassFixture<ODataClientFixture>
{

	#region For(string).GetAsync() Tests

	/// <summary>
	/// Tests that For(string).GetAsync() returns results.
	/// </summary>
	[Fact]
	public async Task For_GetAsync_ReturnsResults()
	{
		// Arrange & Act
		var response = await fixture.Client
			.For("Products")
			.Top(5)
			.GetAsync(CancellationToken);

		// Assert
		response.Should().NotBeNull();
		response.Value.Should().NotBeEmpty();
		response.Value.Should().HaveCountLessThanOrEqualTo(5);
	}

	/// <summary>
	/// Tests that For(string).GetAsync() with filter returns filtered results.
	/// </summary>
	[Fact]
	public async Task For_WithFilter_GetAsync_ReturnsFilteredResults()
	{
		// Arrange & Act
		var response = await fixture.Client
			.For("Products")
			.Filter("Rating gt 3")
			.Top(5)
			.GetAsync(CancellationToken);

		// Assert
		response.Should().NotBeNull();
		response.Value.Should().AllSatisfy(p =>
		{
			var rating = p["Rating"];
			rating.Should().NotBeNull();
			Convert.ToInt32(rating, CultureInfo.InvariantCulture).Should().BeGreaterThan(3);
		});
	}

	/// <summary>
	/// Tests that For(string).GetAsync() with select returns selected fields.
	/// </summary>
	[Fact]
	public async Task For_WithSelect_GetAsync_ReturnsSelectedFields()
	{
		// Arrange & Act
		var response = await fixture.Client
			.For("Products")
			.Select("ID,Name,Price")
			.Top(3)
			.GetAsync(CancellationToken);

		// Assert
		response.Should().NotBeNull();
		response.Value.Should().NotBeEmpty();
		response.Value.Should().AllSatisfy(p =>
		{
			p.Should().ContainKey("ID");
			p.Should().ContainKey("Name");
		});
	}

	/// <summary>
	/// Tests that For(string).GetAsync() with orderby returns ordered results.
	/// </summary>
	[Fact]
	public async Task For_WithOrderBy_GetAsync_ReturnsOrderedResults()
	{
		// Arrange & Act
		var response = await fixture.Client
			.For("Products")
			.OrderBy("Name")
			.Top(5)
			.GetAsync(CancellationToken);

		// Assert
		response.Should().NotBeNull();
		response.Value.Should().NotBeEmpty();
		var names = response.Value
			.Select(p => p["Name"]?.ToString())
			.Where(n => n is not null)
			.ToList();
		names.Should().BeInAscendingOrder();
	}

	/// <summary>
	/// Tests that For(string).GetAsync() with orderby descending returns correctly ordered results.
	/// </summary>
	[Fact]
	public async Task For_WithOrderByDescending_GetAsync_ReturnsOrderedResults()
	{
		// Arrange & Act
		var response = await fixture.Client
			.For("Products")
			.Filter("Price ne null")
			.OrderByDescending("Price")
			.Top(5)
			.GetAsync(CancellationToken);

		// Assert
		response.Should().NotBeNull();
		response.Value.Should().NotBeEmpty();
		var prices = response.Value
			.Where(p => p["Price"] is not null)
			.Select(p => Convert.ToDecimal(p["Price"], CultureInfo.InvariantCulture))
			.ToList();
		prices.Should().BeInDescendingOrder();
	}

	/// <summary>
	/// Tests that For(string).GetAsync() with skip and top returns paged results.
	/// </summary>
	[Fact]
	public async Task For_WithSkipAndTop_GetAsync_ReturnsPaginatedResults()
	{
		// Arrange & Act
		var page1 = await fixture.Client
			.For("Products")
			.OrderBy("ID")
			.Skip(0)
			.Top(2)
			.GetAsync(CancellationToken);

		var page2 = await fixture.Client
			.For("Products")
			.OrderBy("ID")
			.Skip(2)
			.Top(2)
			.GetAsync(CancellationToken);

		// Assert
		page1.Value.Should().NotBeEmpty();
		page2.Value.Should().NotBeEmpty();
		var page1Ids = page1.Value.Select(p => p["ID"]).ToList();
		var page2Ids = page2.Value.Select(p => p["ID"]).ToList();
		page1Ids.Should().NotIntersectWith(page2Ids);
	}

	/// <summary>
	/// Tests that For(string).GetAsync() with count returns count.
	/// </summary>
	[Fact]
	public async Task For_WithCount_GetAsync_ReturnsCount()
	{
		// Arrange & Act
		var response = await fixture.Client
			.For("Products")
			.Count()
			.Top(1)
			.GetAsync(CancellationToken);

		// Assert
		response.Should().NotBeNull();
		response.Count.Should().BePositive();
	}

	/// <summary>
	/// Tests that For(string).GetAsync() with expand returns related entities.
	/// </summary>
	[Fact]
	public async Task For_WithExpand_GetAsync_ReturnsRelatedEntities()
	{
		// Arrange & Act
		var response = await fixture.Client
			.For("Categories")
			.Expand("Products")
			.Top(1)
			.GetAsync(CancellationToken);

		// Assert
		response.Should().NotBeNull();
		response.Value.Should().NotBeEmpty();
		response.Value[0].Should().ContainKey("Products");
	}

	#endregion

	#region For(string).GetAllAsync() Tests

	/// <summary>
	/// Tests that For(string).GetAllAsync() returns all results.
	/// </summary>
	[Fact]
	public async Task For_GetAllAsync_ReturnsAllResults()
	{
		// Arrange & Act
		var response = await fixture.Client
			.For("Products")
			.Filter("Rating gt 4")
			.GetAllAsync(CancellationToken);

		// Assert
		response.Should().NotBeNull();
		response.Value.Should().NotBeEmpty();
	}

	#endregion

	#region For(string).Key().GetEntryAsync() Tests

	/// <summary>
	/// Tests that For(string).Key().GetEntryAsync() returns a single entity.
	/// </summary>
	[Fact]
	public async Task For_Key_GetEntryAsync_ReturnsEntity()
	{
		// Arrange & Act
		var entity = await fixture.Client
			.For("Products")
			.Key(0)
			.GetEntryAsync(CancellationToken);

		// Assert
		entity.Should().NotBeNull();
		entity!["ID"].Should().Be(0L);
		entity["Name"].Should().NotBeNull();
	}

	/// <summary>
	/// Tests that For(string).Key().GetEntryAsync() with select returns selected fields.
	/// </summary>
	[Fact]
	public async Task For_Key_WithSelect_GetEntryAsync_ReturnsSelectedFields()
	{
		// Arrange & Act
		var entity = await fixture.Client
			.For("Products")
			.Key(0)
			.Select("ID,Name")
			.GetEntryAsync(CancellationToken);

		// Assert
		entity.Should().NotBeNull();
		entity!.Should().ContainKey("ID");
		entity.Should().ContainKey("Name");
	}

	#endregion

	#region For(string).GetFirstOrDefaultAsync() Tests

	/// <summary>
	/// Tests that For(string).GetFirstOrDefaultAsync() returns the first entity.
	/// </summary>
	[Fact]
	public async Task For_GetFirstOrDefaultAsync_ReturnsFirstEntity()
	{
		// Arrange & Act
		var entity = await fixture.Client
			.For("Products")
			.Filter("Rating eq 5")
			.GetFirstOrDefaultAsync(CancellationToken);

		// Assert
		entity.Should().NotBeNull();
		Convert.ToInt32(entity!["Rating"], CultureInfo.InvariantCulture).Should().Be(5);
	}

	#endregion

	#region For(string).GetJsonAsync() Tests

	/// <summary>
	/// Tests that For(string).GetJsonAsync() returns a JsonDocument.
	/// </summary>
	[Fact]
	public async Task For_GetJsonAsync_ReturnsJsonDocument()
	{
		// Arrange & Act
		using var json = await fixture.Client
			.For("Products")
			.Top(5)
			.GetJsonAsync(CancellationToken);

		// Assert
		json.Should().NotBeNull();
		json.RootElement.TryGetProperty("value", out var valueElement).Should().BeTrue();
		valueElement.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
		valueElement.GetArrayLength().Should().BePositive();
	}

	/// <summary>
	/// Tests that For(string).GetJsonAsync() with filter returns filtered JSON.
	/// </summary>
	[Fact]
	public async Task For_WithFilter_GetJsonAsync_ReturnsFilteredJson()
	{
		// Arrange & Act
		using var json = await fixture.Client
			.For("Products")
			.Filter("Rating gt 3")
			.Top(5)
			.GetJsonAsync(CancellationToken);

		// Assert
		json.Should().NotBeNull();
		var valueElement = json.RootElement.GetProperty("value");
		foreach (var item in valueElement.EnumerateArray())
		{
			var rating = item.GetProperty("Rating").GetInt32();
			rating.Should().BeGreaterThan(3);
		}
	}

	/// <summary>
	/// Tests that For(string).GetJsonAsync() preserves OData annotations.
	/// </summary>
	[Fact]
	public async Task For_GetJsonAsync_PreservesODataAnnotations()
	{
		// Arrange & Act
		using var json = await fixture.Client
			.For("Products")
			.Count()
			.Top(1)
			.GetJsonAsync(CancellationToken);

		// Assert
		json.Should().NotBeNull();
		json.RootElement.TryGetProperty("@odata.context", out _).Should().BeTrue();
		json.RootElement.TryGetProperty("@odata.count", out var countElement).Should().BeTrue();
		countElement.GetInt64().Should().BePositive();
	}

	#endregion

	#region Complex Query Tests

	/// <summary>
	/// Tests a complex query with multiple options.
	/// </summary>
	[Fact]
	public async Task For_ComplexQuery_ReturnsCorrectResults()
	{
		// Arrange & Act
		var response = await fixture.Client
			.For("Products")
			.Filter("Rating ge 3")
			.Select("ID,Name,Rating,Price")
			.OrderBy("Rating desc")
			.Skip(0)
			.Top(5)
			.Count()
			.GetAsync(CancellationToken);

		// Assert
		response.Should().NotBeNull();
		response.Value.Should().NotBeEmpty();
		response.Value.Should().HaveCountLessThanOrEqualTo(5);
		response.Count.Should().BePositive();
		response.Value.Should().AllSatisfy(p =>
		{
			Convert.ToInt32(p["Rating"], CultureInfo.InvariantCulture).Should().BeGreaterThanOrEqualTo(3);
		});
	}

	#endregion
}
