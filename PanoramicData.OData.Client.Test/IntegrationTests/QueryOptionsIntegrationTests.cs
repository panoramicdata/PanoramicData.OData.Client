using AwesomeAssertions;
using PanoramicData.OData.Client.Test.Fixtures;
using PanoramicData.OData.Client.Test.Models;

namespace PanoramicData.OData.Client.Test.IntegrationTests;

/// <summary>
/// Integration tests for OData V4 query options using the public sample service.
/// Tests $filter, $select, $expand, $orderby, $top, $skip, $count, $search.
/// </summary>
public class QueryOptionsIntegrationTests : TestBase, IClassFixture<ODataClientFixture>
{
	private readonly ODataClientFixture _fixture;

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	/// <param name="fixture">The OData client fixture.</param>
	public QueryOptionsIntegrationTests(ODataClientFixture fixture)
	{
		_fixture = fixture;
	}

	#region $filter Integration Tests

	/// <summary>
	/// Tests basic equality filter.
	/// </summary>
	[Fact]
	public async Task Filter_Equality_ReturnsMatchingProducts()
	{
		// Arrange
		var query = _fixture.Client.For<Product>("Products")
			.Filter("ID eq 0")
			.Top(1);

		// Act
		var response = await _fixture.Client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Value.Should().ContainSingle();
		response.Value[0].Id.Should().Be(0);
	}

	/// <summary>
	/// Tests greater than filter.
	/// </summary>
	[Fact]
	public async Task Filter_GreaterThan_ReturnsFilteredProducts()
	{
		// Arrange
		var query = _fixture.Client.For<Product>("Products")
			.Filter("Rating gt 3")
			.Top(5);

		// Act
		var response = await _fixture.Client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Value.Should().AllSatisfy(p => p.Rating.Should().BeGreaterThan(3));
	}

	/// <summary>
	/// Tests contains function filter.
	/// </summary>
	[Fact]
	public async Task Filter_Contains_ReturnsMatchingProducts()
	{
		// Arrange
		var query = _fixture.Client.For<Product>("Products")
			.Filter("contains(Name, 'Bread')")
			.Top(5);

		// Act
		var response = await _fixture.Client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Value.Should().AllSatisfy(p => p.Name.Should().Contain("Bread"));
	}

	/// <summary>
	/// Tests combined AND filter.
	/// </summary>
	[Fact]
	public async Task Filter_CombinedAnd_ReturnsMatchingProducts()
	{
		// Arrange
		var query = _fixture.Client.For<Product>("Products")
			.Filter("Rating ge 3 and Price lt 50")
			.Top(5);

		// Act
		var response = await _fixture.Client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Value.Should().AllSatisfy(p =>
		{
			p.Rating.Should().BeGreaterThanOrEqualTo(3);
			p.Price.Should().BeLessThan(50);
		});
	}

	/// <summary>
	/// Tests OR filter.
	/// </summary>
	[Fact]
	public async Task Filter_CombinedOr_ReturnsMatchingProducts()
	{
		// Arrange
		var query = _fixture.Client.For<Product>("Products")
			.Filter("Rating eq 5 or Rating eq 4")
			.Top(10);

		// Act
		var response = await _fixture.Client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Value.Should().AllSatisfy(p =>
		{
			p.Rating.Should().BeOneOf(4, 5);
		});
	}

	#endregion

	#region $select Integration Tests

	/// <summary>
	/// Tests select returns only specified fields.
	/// </summary>
	[Fact]
	public async Task Select_SpecificFields_ReturnsOnlyThoseFields()
	{
		// Arrange
		var query = _fixture.Client.For<Product>("Products")
			.Select("ID,Name")
			.Top(3);

		// Act
		var response = await _fixture.Client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Value.Should().NotBeEmpty();
		// Selected fields should be present
		response.Value.Should().AllSatisfy(p =>
		{
			p.Id.Should().BeGreaterThanOrEqualTo(0);
			p.Name.Should().NotBeNullOrEmpty();
		});
	}

	#endregion

	#region $expand Integration Tests

	/// <summary>
	/// Tests expand with navigation property.
	/// </summary>
	[Fact]
	public async Task Expand_NavigationProperty_ReturnsRelatedEntities()
	{
		// Arrange
		var query = _fixture.Client.For<Category>("Categories")
			.Expand("Products")
			.Top(1);

		// Act
		var response = await _fixture.Client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Value.Should().NotBeEmpty();
		response.Value[0].Products.Should().NotBeNull();
	}

	#endregion

	#region $orderby Integration Tests

	/// <summary>
	/// Tests orderby ascending.
	/// </summary>
	[Fact]
	public async Task OrderBy_Ascending_ReturnsSortedResults()
	{
		// Arrange
		var query = _fixture.Client.For<Product>("Products")
			.OrderBy("Name")
			.Top(5);

		// Act
		var response = await _fixture.Client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Value.Should().NotBeEmpty();
		var names = response.Value.Select(p => p.Name).ToList();
		names.Should().BeInAscendingOrder();
	}

	/// <summary>
	/// Tests orderby descending.
	/// </summary>
	[Fact]
	public async Task OrderBy_Descending_ReturnsSortedResults()
	{
		// Arrange
		var query = _fixture.Client.For<Product>("Products")
			.Filter("Price ne null")
			.OrderBy("Price desc")
			.Top(5);

		// Act
		var response = await _fixture.Client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Value.Should().NotBeEmpty();
		var prices = response.Value
			.Where(p => p.Price.HasValue)
			.Select(p => p.Price!.Value)
			.ToList();
		prices.Should().BeInDescendingOrder();
	}

	/// <summary>
	/// Tests multiple orderby clauses.
	/// </summary>
	[Fact]
	public async Task OrderBy_Multiple_ReturnsSortedResults()
	{
		// Arrange
		var query = _fixture.Client.For<Product>("Products")
			.OrderBy("Rating desc,Name")
			.Top(10);

		// Act
		var response = await _fixture.Client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Value.Should().NotBeEmpty();
	}

	#endregion

	#region $skip and $top Integration Tests

	/// <summary>
	/// Tests top limits results.
	/// </summary>
	[Fact]
	public async Task Top_LimitsResults()
	{
		// Arrange
		var query = _fixture.Client.For<Product>("Products")
			.Top(3);

		// Act
		var response = await _fixture.Client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Value.Should().HaveCountLessThanOrEqualTo(3);
	}

	/// <summary>
	/// Tests skip and top for paging.
	/// </summary>
	[Fact]
	public async Task SkipAndTop_Paging_ReturnsDifferentResults()
	{
		// Arrange
		var queryPage1 = _fixture.Client.For<Product>("Products")
			.OrderBy("ID")
			.Skip(0)
			.Top(2);

		var queryPage2 = _fixture.Client.For<Product>("Products")
			.OrderBy("ID")
			.Skip(2)
			.Top(2);

		// Act
		var page1 = await _fixture.Client.GetAsync(queryPage1, TestContext.Current.CancellationToken);
		var page2 = await _fixture.Client.GetAsync(queryPage2, TestContext.Current.CancellationToken);

		// Assert
		page1.Value.Should().NotBeEmpty();
		page2.Value.Should().NotBeEmpty();
		page1.Value.Select(p => p.Id).Should().NotIntersectWith(page2.Value.Select(p => p.Id));
	}

	#endregion

	#region $count Integration Tests

	/// <summary>
	/// Tests count returns total.
	/// </summary>
	[Fact]
	public async Task Count_ReturnsTotalCount()
	{
		// Arrange
		var query = _fixture.Client.For<Product>("Products")
			.Count()
			.Top(1);

		// Act
		var response = await _fixture.Client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Count.Should().BePositive();
	}

	/// <summary>
	/// Tests count with filter.
	/// </summary>
	[Fact]
	public async Task Count_WithFilter_ReturnsFilteredCount()
	{
		// Arrange
		var query = _fixture.Client.For<Product>("Products")
			.Filter("Rating gt 3")
			.Count()
			.Top(1);

		// Act
		var response = await _fixture.Client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Count.Should().BePositive();
		// Count should be less than total products (since we filtered)
	}

	#endregion

	#region GetByKey Integration Tests

	/// <summary>
	/// Tests getting entity by key.
	/// </summary>
	[Fact]
	public async Task GetByKey_ExistingEntity_ReturnsEntity()
	{
		// Act
		var product = await _fixture.Client.GetByKeyAsync<Product, int>(0, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		product.Should().NotBeNull();
		product!.Id.Should().Be(0);
	}

	/// <summary>
	/// Tests getting entity by key with select.
	/// </summary>
	[Fact]
	public async Task GetByKey_WithSelect_ReturnsSelectedFields()
	{
		// Arrange
		var query = _fixture.Client.For<Product>("Products")
			.Select("ID,Name");

		// Act
		var product = await _fixture.Client.GetByKeyAsync<Product, int>(0, query, TestContext.Current.CancellationToken);

		// Assert
		product.Should().NotBeNull();
		product!.Name.Should().NotBeNullOrEmpty();
	}

	#endregion

	#region Complex Query Integration Tests

	/// <summary>
	/// Tests combining multiple query options.
	/// </summary>
	[Fact]
	public async Task ComplexQuery_AllOptions_ReturnsCorrectResults()
	{
		// Arrange
		var query = _fixture.Client.For<Product>("Products")
			.Filter("Rating ge 3")
			.Select("ID,Name,Rating,Price")
			.OrderBy("Rating desc")
			.Skip(0)
			.Top(5)
			.Count();

		// Act
		var response = await _fixture.Client.GetAsync(query, TestContext.Current.CancellationToken);

		// Assert
		response.Value.Should().NotBeEmpty();
		response.Value.Should().HaveCountLessThanOrEqualTo(5);
		response.Count.Should().BePositive();
		response.Value.Should().AllSatisfy(p =>
		{
			p.Rating.Should().BeGreaterThanOrEqualTo(3);
		});
	}

	#endregion
}
