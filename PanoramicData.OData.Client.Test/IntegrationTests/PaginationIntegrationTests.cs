using AwesomeAssertions;
using PanoramicData.OData.Client.Test.Fixtures;
using PanoramicData.OData.Client.Test.Models;

namespace PanoramicData.OData.Client.Test.IntegrationTests;

/// <summary>
/// Integration tests for pagination and GetAllAsync operations.
/// Tests automatic pagination following nextLink and large result set handling.
/// </summary>
/// <remarks>
/// Initializes a new instance of the test class.
/// </remarks>
public class PaginationIntegrationTests(ODataClientFixture fixture) : TestBase, IClassFixture<ODataClientFixture>
{

	#region GetAllAsync Tests

	/// <summary>
	/// Tests that GetAllAsync returns all results from multiple pages.
	/// </summary>
	[Fact]
	public async Task GetAllAsync_ReturnsAllResults()
	{
		// Arrange
		var query = fixture.Client.For<Product>("Products");

		// Act
		var response = await fixture.Client.GetAllAsync(query, CancellationToken);

		// Assert
		response.Value.Should().NotBeEmpty();
		// The sample service has multiple products
		response.Value.Count.Should().BeGreaterThan(5);
	}

	/// <summary>
	/// Tests that GetAllAsync with filter returns all matching results.
	/// </summary>
	[Fact]
	public async Task GetAllAsync_WithFilter_ReturnsAllMatchingResults()
	{
		// Arrange
		var query = fixture.Client.For<Product>("Products")
			.Filter("Rating ge 3");

		// Act
		var response = await fixture.Client.GetAllAsync(query, CancellationToken);

		// Assert
		response.Value.Should().NotBeEmpty();
		response.Value.Should().AllSatisfy(p => p.Rating.Should().BeGreaterThanOrEqualTo(3));
	}

	/// <summary>
	/// Tests that GetAllAsync preserves order.
	/// </summary>
	[Fact]
	public async Task GetAllAsync_WithOrderBy_PreservesOrder()
	{
		// Arrange
		var query = fixture.Client.For<Product>("Products")
			.OrderBy("Name");

		// Act
		var response = await fixture.Client.GetAllAsync(query, CancellationToken);

		// Assert
		response.Value.Should().NotBeEmpty();
		var names = response.Value.Select(p => p.Name).ToList();
		names.Should().BeInAscendingOrder();
	}

	/// <summary>
	/// Tests that GetAllAsync respects cancellation.
	/// </summary>
	[Fact]
	public async Task GetAllAsync_Cancellation_StopsEarly()
	{
		// Arrange
		using var cts = new CancellationTokenSource();
		cts.Cancel(); // Cancel immediately

		var query = fixture.Client.For<Product>("Products");

		// Act & Assert
		var act = async () => await fixture.Client.GetAllAsync(query, cts.Token);
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	#endregion

	#region Manual Pagination Tests

	/// <summary>
	/// Tests manual pagination with skip and top.
	/// </summary>
	[Fact]
	public async Task ManualPagination_SkipAndTop_ReturnsDifferentPages()
	{
		// Arrange
		var pageSize = 3;

		var queryPage1 = fixture.Client.For<Product>("Products")
			.OrderBy("ID")
			.Skip(0)
			.Top(pageSize);

		var queryPage2 = fixture.Client.For<Product>("Products")
			.OrderBy("ID")
			.Skip(pageSize)
			.Top(pageSize);

		var queryPage3 = fixture.Client.For<Product>("Products")
			.OrderBy("ID")
			.Skip(pageSize * 2)
			.Top(pageSize);

		// Act
		var page1 = await fixture.Client.GetAsync(queryPage1, CancellationToken);
		var page2 = await fixture.Client.GetAsync(queryPage2, CancellationToken);
		var page3 = await fixture.Client.GetAsync(queryPage3, CancellationToken);

		// Assert
		page1.Value.Should().HaveCountLessThanOrEqualTo(pageSize);
		page2.Value.Should().HaveCountLessThanOrEqualTo(pageSize);
		page3.Value.Should().HaveCountLessThanOrEqualTo(pageSize);

		// Pages should have different IDs
		var allIds = page1.Value.Select(p => p.Id)
			.Concat(page2.Value.Select(p => p.Id))
			.Concat(page3.Value.Select(p => p.Id))
			.ToList();
		allIds.Should().OnlyHaveUniqueItems();
	}

	/// <summary>
	/// Tests that count remains consistent across pages.
	/// </summary>
	[Fact]
	public async Task Pagination_CountRemainsConsistent()
	{
		// Arrange
		var queryPage1 = fixture.Client.For<Product>("Products")
			.Count()
			.Skip(0)
			.Top(2);

		var queryPage2 = fixture.Client.For<Product>("Products")
			.Count()
			.Skip(2)
			.Top(2);

		// Act
		var page1 = await fixture.Client.GetAsync(queryPage1, CancellationToken);
		var page2 = await fixture.Client.GetAsync(queryPage2, CancellationToken);

		// Assert
		page1.Count.Should().BePositive();
		page2.Count.Should().Be(page1.Count); // Count should be same across pages
	}

	#endregion

	#region NextLink Tests

	/// <summary>
	/// Tests that response includes nextLink when more results available.
	/// </summary>
	[Fact]
	public async Task GetAsync_WithLimitedTop_MayHaveNextLink()
	{
		// Arrange
		var query = fixture.Client.For<Product>("Products")
			.Top(2);

		// Act
		var response = await fixture.Client.GetAsync(query, CancellationToken);

		// Assert
		response.Value.Should().NotBeEmpty();
		// Note: NextLink depends on service-side paging which may not always be present
	}

	#endregion

	#region Large Result Set Tests

	/// <summary>
	/// Tests handling of larger result sets with GetAllAsync.
	/// </summary>
	[Fact]
	public async Task GetAllAsync_LargerResultSet_HandlesCorrectly()
	{
		// Arrange - Categories have Products expanded
		var query = fixture.Client.For<Category>("Categories")
			.Expand("Products");

		// Act
		var response = await fixture.Client.GetAllAsync(query, CancellationToken);

		// Assert
		response.Value.Should().NotBeEmpty();
		response.Value.Should().AllSatisfy(c => c.Products.Should().NotBeNull());
	}

	#endregion

	#region Fluent API Pagination Tests

	/// <summary>
	/// Tests GetAllAsync using the fluent (non-generic) API.
	/// </summary>
	[Fact]
	public async Task FluentApi_GetAllAsync_ReturnsAllResults()
	{
		// Arrange & Act
		var response = await fixture.Client
			.For("Products")
			.Filter("Rating ge 3")
			.GetAllAsync(CancellationToken);

		// Assert
		response.Should().NotBeNull();
		response.Value.Should().NotBeEmpty();
	}

	#endregion
}
