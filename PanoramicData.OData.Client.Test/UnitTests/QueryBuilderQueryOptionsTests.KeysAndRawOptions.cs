namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataQueryBuilder keys, functions, custom headers and raw query options.
/// </summary>
public partial class QueryBuilderQueryOptionsTests
{
	#region Key Queries

	/// <summary>
	/// Tests query by integer key.
	/// </summary>
	[Fact]
	public void Key_Integer_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Key(123)
			.BuildUrl();

		url.Should().Be("Products(123)");
	}

	/// <summary>
	/// Tests query by string key.
	/// </summary>
	[Fact]
	public void Key_String_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Key("russellwhyte")
			.BuildUrl();

		url.Should().Be("People('russellwhyte')");
	}

	/// <summary>
	/// Tests query by GUID key.
	/// </summary>
	[Fact]
	public void Key_Guid_GeneratesCorrectUrl()
	{
		var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
		var url = new ODataQueryBuilder<Trip>("Trips", NullLogger.Instance)
			.Key(guid)
			.BuildUrl();

		url.Should().Contain("12345678-1234-1234-1234-123456789012");
	}

	/// <summary>
	/// Tests key with additional query options.
	/// </summary>
	[Fact]
	public void Key_WithSelect_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Key(123)
			.Select("ID,Name")
			.BuildUrl();

		url.Should().Contain("Products(123)");
		url.Should().Contain("$select=ID,Name");
	}

	#endregion

	#region Functions

	/// <summary>
	/// Tests function call.
	/// </summary>
	[Fact]
	public void Function_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Function("Microsoft.OData.SampleService.Models.TripPin.GetFavoriteAirline", null)
			.BuildUrl();

		url.Should().Contain("Microsoft.OData.SampleService.Models.TripPin.GetFavoriteAirline");
	}

	/// <summary>
	/// Tests function call with parameters.
	/// </summary>
	[Fact]
	public void Function_WithParameters_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Function("GetNearestAirport", new { Lat = 47.6, Lon = -122.1 })
			.BuildUrl();

		url.Should().Contain("GetNearestAirport(");
	}

	#endregion

	#region Custom Headers

	/// <summary>
	/// Tests adding custom header.
	/// </summary>
	[Fact]
	public void WithHeader_AddsToCustomHeaders()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.WithHeader("Prefer", "return=representation");

		builder.CustomHeaders.Should().ContainKey("Prefer");
		builder.CustomHeaders["Prefer"].Should().Be("return=representation");
	}

	/// <summary>
	/// Tests multiple headers.
	/// </summary>
	[Fact]
	public void WithHeader_Multiple_AddsAll()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.WithHeader("Prefer", "return=representation")
			.WithHeader("OData-MaxVersion", "4.0");

		builder.CustomHeaders.Should().HaveCount(2);
	}

	#endregion

	#region Combined Query Options

	/// <summary>
	/// Tests all query options combined.
	/// </summary>
	[Fact]
	public void AllOptions_Combined_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("Price gt 100")
			.Select("ID,Name,Price")
			.OrderBy("Price desc")
			.Skip(10)
			.Top(5)
			.Count()
			.BuildUrl();

		url.Should().Contain("Products?");
		url.Should().Contain("$filter=");
		url.Should().Contain("$select=ID,Name,Price");
		url.Should().Contain("$orderby=Price desc");
		url.Should().Contain("$skip=10");
		url.Should().Contain("$top=5");
		url.Should().Contain("$count=true");
	}

	#endregion

	#region QueryOptions

	/// <summary>
	/// Tests QueryOptions appends a raw vendor-specific parameter verbatim.
	/// </summary>
	[Fact]
	public void QueryOptions_AppendsRawParameterVerbatim()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.QueryOptions("PropertySet=Minimum,AddressList")
			.BuildUrl();

		url.Should().Contain("PropertySet=Minimum,AddressList");
	}

	/// <summary>
	/// Tests QueryOptions does not quote or URL-encode comma-separated values.
	/// </summary>
	[Fact]
	public void QueryOptions_DoesNotQuoteOrEncodeValues()
	{
		var propertySets = string.Join(",", "Minimum", "AddressList");
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.QueryOptions($"PropertySet={propertySets}")
			.BuildUrl();

		// Must NOT contain single quotes around the value
		url.Should().Contain("PropertySet=Minimum,AddressList");
		url.Should().NotContain("PropertySet='Minimum,AddressList'");
	}

	/// <summary>
	/// Tests multiple QueryOptions calls are combined with &amp;.
	/// </summary>
	[Fact]
	public void QueryOptions_MultipleCalls_CombinedWithAmpersand()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.QueryOptions("PropertySet=Minimum")
			.QueryOptions("CustomParam=Value")
			.BuildUrl();

		url.Should().Contain("PropertySet=Minimum");
		url.Should().Contain("CustomParam=Value");
		url.Should().Contain("PropertySet=Minimum&CustomParam=Value");
	}

	/// <summary>
	/// Tests QueryOptions combined with standard OData query options.
	/// </summary>
	[Fact]
	public void QueryOptions_CombinedWithStandardOptions_AppendsAfterStandardParams()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Select("ID,Name")
			.Filter("Price gt 10")
			.QueryOptions("PropertySet=Minimum,AddressList")
			.BuildUrl();

		url.Should().Contain("$select=ID,Name");
		url.Should().Contain("$filter=");
		url.Should().Contain("PropertySet=Minimum,AddressList");
	}

	/// <summary>
	/// Tests QueryOptions with key segment appends correctly.
	/// </summary>
	[Fact]
	public void QueryOptions_WithKey_AppendsToQueryString()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Key("shared@example.com")
			.QueryOptions("PropertySet=Delivery")
			.BuildUrl();

		url.Should().Contain("Products('shared@example.com')");
		url.Should().Contain("PropertySet=Delivery");
	}

	#endregion
}
