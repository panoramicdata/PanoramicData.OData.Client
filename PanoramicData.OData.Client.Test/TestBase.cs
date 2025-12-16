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
}
