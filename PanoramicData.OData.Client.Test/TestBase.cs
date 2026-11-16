namespace PanoramicData.OData.Client.Test;

/// <summary>
/// Base class for OData client tests providing common test endpoints.
/// </summary>
public abstract class TestBase
{
	/// <summary>
	/// OData V4 sample service (read-only).
	/// Provides: Products, Categories, Suppliers, etc.
	/// </summary>
	protected const string ODataV4ReadOnlyUri = "https://services.odata.org/V4/OData/OData.svc/";

	/// <summary>
	/// OData V4 sample service (read-write).
	/// Creates a unique session for write operations.
	/// </summary>
	protected const string ODataV4ReadWriteUri = "https://services.odata.org/V4/OData/%28S%28readwrite%29%29/OData.svc/";

	/// <summary>
	/// Northwind V4 sample service (read-only).
	/// Provides: Customers, Orders, Products, Employees, etc.
	/// </summary>
	protected const string NorthwindV4ReadOnlyUri = "https://services.odata.org/V4/Northwind/Northwind.svc/";

	/// <summary>
	/// TripPin V4 sample service (read-write).
	/// Provides: People, Airlines, Airports, Trips, etc.
	/// </summary>
	protected const string TripPinV4ReadWriteUri = "https://services.odata.org/V4/TripPinServiceRW/";

	/// <summary>
	/// TripPin RESTier service.
	/// Alternative TripPin implementation using RESTier.
	/// </summary>
	protected const string TripPinRESTierUri = "https://services.odata.org/TripPinRESTierService/";

	/// <summary>
	/// Gets a CancellationToken from the current test context.
	/// </summary>
	protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

	/// <summary>
	/// Runs a typed query over <paramref name="entitySet"/> shaped by <paramref name="shape"/> and returns one page.
	/// </summary>
	protected static Task<ODataResponse<T>> GetPageAsync<T>(ODataClient client, string entitySet, Func<ODataQueryBuilder<T>, ODataQueryBuilder<T>> shape)
		where T : class
		=> client.GetAsync(shape(client.For<T>(entitySet)), CancellationToken);

	/// <summary>
	/// Asserts a product page is not empty and every product has an ID and a name.
	/// </summary>
	protected static void AssertProductsHaveIdAndName(ODataResponse<Product> response)
	{
		response.Value.Should().NotBeEmpty();
		response.Value.Should().AllSatisfy(p =>
		{
			p.Id.Should().BeGreaterThanOrEqualTo(0);
			p.Name.Should().NotBeNullOrEmpty();
		});
	}

	/// <summary>
	/// Asserts every product on the page has a rating above 3.
	/// </summary>
	protected static void AssertRatingsAboveThree(ODataResponse<Product> response)
		=> response.Value.Should().AllSatisfy(p => p.Rating.Should().BeGreaterThan(3));

	/// <summary>
	/// Asserts the priced products on the page are in descending price order.
	/// </summary>
	protected static void AssertPricesDescending(ODataResponse<Product> response)
	{
		response.Value.Should().NotBeEmpty();
		response.Value
			.Where(p => p.Price.HasValue)
			.Select(p => p.Price!.Value)
			.Should().BeInDescendingOrder();
	}
}
