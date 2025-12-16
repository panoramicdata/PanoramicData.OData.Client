using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.OData.Client.Test.Models;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataQueryBuilder collection operators and lambda expressions.
/// Tests OData V4 any/all operators with lambda expressions.
/// </summary>
public class QueryBuilderCollectionLambdaTests
{
	#region Lambda Expressions - any/all (Raw Filters)

	/// <summary>
	/// Tests any operator with lambda via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithAnyLambda_RawFilter_Works()
	{
		// OData V4 supports: Emails/any(e: e eq 'john@example.com')
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Filter("Emails/any(e: e eq 'john@example.com')")
			.BuildUrl();

		url.Should().Contain("Emails%2Fany(e%3A");
	}

	/// <summary>
	/// Tests any operator without lambda (check if collection has items) via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithAnyNoLambda_RawFilter_Works()
	{
		// OData V4 supports: Emails/any()
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Filter("Emails/any()")
			.BuildUrl();

		url.Should().Contain("Emails%2Fany()");
	}

	/// <summary>
	/// Tests all operator with lambda via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithAllLambda_RawFilter_Works()
	{
		// OData V4 supports: Emails/all(e: endswith(e, '@example.com'))
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Filter("Emails/all(e: endswith(e, '@example.com'))")
			.BuildUrl();

		url.Should().Contain("Emails%2Fall(e%3A");
	}

	/// <summary>
	/// Tests nested any with navigation property via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithNestedAny_RawFilter_Works()
	{
		// OData V4 supports: Friends/any(f: f/FirstName eq 'John')
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Filter("Friends/any(f: f/FirstName eq 'John')")
			.BuildUrl();

		url.Should().Contain("Friends%2Fany(f%3A");
	}

	/// <summary>
	/// Tests complex any with multiple conditions via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithComplexAny_RawFilter_Works()
	{
		// OData V4 supports: Trips/any(t: t/Budget gt 1000 and t/Name eq 'Paris Trip')
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Filter("Trips/any(t: t/Budget gt 1000 and t/Name eq 'Paris Trip')")
			.BuildUrl();

		url.Should().Contain("Trips%2Fany(t%3A");
	}

	#endregion

	#region Type Casting and Type Functions (Raw Filters)

	/// <summary>
	/// Tests cast function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithCast_RawFilter_Works()
	{
		// OData V4 supports: cast(Price, 'Edm.Int32') gt 100
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("cast(Price, 'Edm.Int32') gt 100")
			.BuildUrl();

		url.Should().Contain("cast(Price");
	}

	/// <summary>
	/// Tests isof function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithIsOf_RawFilter_Works()
	{
		// OData V4 supports: isof(HomeAddress, 'Microsoft.OData.SampleService.Models.TripPin.EventLocation')
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Filter("isof(HomeAddress, 'Microsoft.OData.SampleService.Models.TripPin.EventLocation')")
			.BuildUrl();

		url.Should().Contain("isof(HomeAddress");
	}

	/// <summary>
	/// Tests type segment (derived type filtering) via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithDerivedType_RawFilter_Works()
	{
		// OData V4 supports filtering by derived type
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Filter("HomeAddress/Microsoft.OData.SampleService.Models.TripPin.EventLocation/BuildingInfo ne null")
			.BuildUrl();

		url.Should().Contain("HomeAddress%2F");
	}

	#endregion

	#region Geo Functions (Raw Filters)

	/// <summary>
	/// Tests geo.distance function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithGeoDistance_RawFilter_Works()
	{
		// OData V4 supports: geo.distance(Location, geography'POINT(-122.131577 47.678581)') lt 1000
		var url = new ODataQueryBuilder<Airport>("Airports", NullLogger.Instance)
			.Filter("geo.distance(Location/Loc, geography'POINT(-122.131577 47.678581)') lt 1000")
			.BuildUrl();

		url.Should().Contain("geo.distance");
	}

	/// <summary>
	/// Tests geo.intersects function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithGeoIntersects_RawFilter_Works()
	{
		// OData V4 supports: geo.intersects(Location, geography'POLYGON(...)')
		var url = new ODataQueryBuilder<Airport>("Airports", NullLogger.Instance)
			.Filter("geo.intersects(Location/Loc, geography'POLYGON((-122 47, -122 48, -121 48, -121 47, -122 47))')")
			.BuildUrl();

		url.Should().Contain("geo.intersects");
	}

	/// <summary>
	/// Tests geo.length function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithGeoLength_RawFilter_Works()
	{
		// OData V4 supports: geo.length(Route) lt 1000
		var url = new ODataQueryBuilder<Airport>("Airports", NullLogger.Instance)
			.Filter("geo.length(Route) lt 1000")
			.BuildUrl();

		url.Should().Contain("geo.length");
	}

	#endregion

	#region $has Operator for Enum Flags (Raw Filters)

	/// <summary>
	/// Tests has operator for enum flags via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithHasOperator_RawFilter_Works()
	{
		// OData V4 supports: Features has Microsoft.OData.SampleService.Models.TripPin.Feature'Feature1'
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Filter("FavoriteFeature has Microsoft.OData.SampleService.Models.TripPin.Feature'Feature1'")
			.BuildUrl();

		url.Should().Contain("has");
	}

	#endregion

	#region Case-Insensitive Comparison (OData V4.01)

	/// <summary>
	/// Tests case-insensitive equals via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithCaseInsensitivePattern_RawFilter_Works()
	{
		// Common pattern: use tolower for case-insensitive comparison
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Filter("tolower(FirstName) eq tolower('john')")
			.BuildUrl();

		url.Should().Contain("tolower(FirstName)");
	}

	#endregion
}
