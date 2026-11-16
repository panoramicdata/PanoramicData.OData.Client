using PanoramicData.OData.Client.Test.Fixtures;

namespace PanoramicData.OData.Client.Test;

/// <summary>
/// Integration tests demonstrating ODataClient usage with dependency injection and logging.
/// </summary>
/// <remarks>
/// These tests use the XUnit 3 IClassFixture pattern to share a configured ODataClient
/// instance with full logging enabled. The console output will show request/response details.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="ODataClientIntegrationTests"/> class.
/// </remarks>
/// <param name="fixture">The OData client fixture providing a configured client instance.</param>
[Trait("Category", "Integration")]
public class ODataClientIntegrationTests(ODataClientFixture fixture) : TestBase, IClassFixture<ODataClientFixture>
{

	/// <summary>
	/// Demonstrates querying products from the OData sample service.
	/// The logger output will show the full request URL and response details.
	/// </summary>
	[Fact]
	public async Task GetProducts_ReturnsProducts_WithFullLogging()
	{
		var response = await GetProductsAsync(query => query.Top(5));

		// Assert
		response.Value.Should().NotBeEmpty();
		response.Value.Should().AllSatisfy(p =>
		{
			p.Id.Should().BeGreaterThanOrEqualTo(0);
			p.Name.Should().NotBeNullOrEmpty();
		});
	}

	/// <summary>
	/// Demonstrates querying a single product by ID.
	/// </summary>
	[Fact]
	public async Task GetProductById_ReturnsProduct_WithFullLogging()
	{
		// Arrange & Act
		var product = await fixture.Client.GetByKeyAsync<Product, int>(0, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		product.Should().NotBeNull();
		product!.Name.Should().NotBeNullOrEmpty();
	}

	/// <summary>
	/// Demonstrates filtering products with a query.
	/// </summary>
	[Fact]
	public async Task GetProducts_WithFilter_ReturnsFilteredProducts()
	{
		var response = await GetProductsAsync(query => query.Filter("Rating gt 3").Top(3));

		// Assert
		response.Value.Should().AllSatisfy(p =>
		{
			p.Rating.Should().BeGreaterThan(3);
		});
	}

	/// <summary>
	/// Demonstrates selecting specific fields from products.
	/// </summary>
	[Fact]
	public async Task GetProducts_WithSelect_ReturnsSelectedFields()
	{
		var response = await GetProductsAsync(query => query.Select("ID,Name,Price").Top(3));

		// Assert
		response.Value.Should().NotBeEmpty();
		response.Value.Should().AllSatisfy(p =>
		{
			p.Id.Should().BeGreaterThan(-1);
			p.Name.Should().NotBeNullOrEmpty();
		});
	}

	/// <summary>
	/// Demonstrates ordering products by price.
	/// </summary>
	[Fact]
	public async Task GetProducts_WithOrderBy_ReturnsOrderedProducts()
	{
		var response = await GetProductsAsync(query => query.OrderBy("Price desc").Top(5));

		// Assert
		response.Value.Should().NotBeEmpty();
		var prices = response.Value
			.Where(p => p.Price.HasValue)
			.Select(p => p.Price!.Value)
			.ToList();
		prices.Should().BeInDescendingOrder();
	}

	private Task<ODataResponse<Product>> GetProductsAsync(Func<ODataQueryBuilder<Product>, ODataQueryBuilder<Product>> shape)
		=> GetPageAsync(fixture.Client, "Products", shape);
}
