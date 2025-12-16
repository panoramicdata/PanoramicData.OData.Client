using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.OData.Client.Test.Models;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataQueryBuilder query options.
/// Tests $select, $expand, $orderby, $skip, $top, $count, $search, $apply.
/// </summary>
public class QueryBuilderQueryOptionsTests
{
	#region $select

	/// <summary>
	/// Tests select with string fields.
	/// </summary>
	[Fact]
	public void Select_WithStringFields_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Select("ID,Name,Price")
			.BuildUrl();

		url.Should().Contain("$select=ID,Name,Price");
	}

	/// <summary>
	/// Tests select with expression.
	/// </summary>
	[Fact]
	public void Select_WithExpression_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Select(p => new { p.Id, p.Name })
			.BuildUrl();

		url.Should().Contain("$select=");
		url.Should().Contain("Id");
		url.Should().Contain("Name");
	}

	/// <summary>
	/// Tests select with multiple calls combines fields.
	/// </summary>
	[Fact]
	public void Select_MultipleCalls_CombinesFields()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Select("ID,Name")
			.Select("Price")
			.BuildUrl();

		url.Should().Contain("$select=ID,Name,Price");
	}

	#endregion

	#region $expand

	/// <summary>
	/// Tests expand with string.
	/// </summary>
	[Fact]
	public void Expand_WithString_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Expand("Friends,Trips")
			.BuildUrl();

		url.Should().Contain("$expand=Friends,Trips");
	}

	/// <summary>
	/// Tests expand with expression.
	/// </summary>
	[Fact]
	public void Expand_WithExpression_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Expand(p => p.Friends!)
			.BuildUrl();

		url.Should().Contain("$expand=Friends");
	}

	/// <summary>
	/// Tests nested expand via raw string.
	/// </summary>
	[Fact]
	public void Expand_Nested_RawString_Works()
	{
		// OData V4 supports: $expand=Friends($select=FirstName;$expand=BestFriend)
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Expand("Friends($select=FirstName;$expand=BestFriend)")
			.BuildUrl();

		url.Should().Contain("$expand=Friends");
	}

	/// <summary>
	/// Tests expand with filter via raw string.
	/// </summary>
	[Fact]
	public void Expand_WithFilter_RawString_Works()
	{
		// OData V4 supports: $expand=Trips($filter=Budget gt 1000)
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Expand("Trips($filter=Budget gt 1000)")
			.BuildUrl();

		url.Should().Contain("$expand=Trips");
	}

	/// <summary>
	/// Tests expand with top/orderby via raw string.
	/// </summary>
	[Fact]
	public void Expand_WithTopOrderBy_RawString_Works()
	{
		// OData V4 supports: $expand=Trips($orderby=Budget desc;$top=5)
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Expand("Trips($orderby=Budget desc;$top=5)")
			.BuildUrl();

		url.Should().Contain("$expand=Trips");
	}

	/// <summary>
	/// Tests expand with count via raw string.
	/// </summary>
	[Fact]
	public void Expand_WithCount_RawString_Works()
	{
		// OData V4 supports: $expand=Trips($count=true)
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Expand("Trips($count=true)")
			.BuildUrl();

		url.Should().Contain("$expand=Trips");
	}

	/// <summary>
	/// Tests expand with levels via raw string.
	/// </summary>
	[Fact]
	public void Expand_WithLevels_RawString_Works()
	{
		// OData V4 supports: $expand=Friends($levels=2)
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Expand("Friends($levels=2)")
			.BuildUrl();

		url.Should().Contain("$expand=Friends");
	}

	#endregion

	#region $orderby

	/// <summary>
	/// Tests orderby ascending.
	/// </summary>
	[Fact]
	public void OrderBy_Ascending_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.OrderBy("Name")
			.BuildUrl();

		url.Should().Contain("$orderby=Name");
	}

	/// <summary>
	/// Tests orderby descending.
	/// </summary>
	[Fact]
	public void OrderBy_Descending_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.OrderBy("Price desc")
			.BuildUrl();

		url.Should().Contain("$orderby=Price%20desc");
	}

	/// <summary>
	/// Tests orderby with expression.
	/// </summary>
	[Fact]
	public void OrderBy_WithExpression_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.OrderBy(p => p.Name)
			.BuildUrl();

		url.Should().Contain("$orderby=Name");
	}

	/// <summary>
	/// Tests orderby with expression descending.
	/// </summary>
	[Fact]
	public void OrderBy_WithExpressionDescending_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.OrderBy(p => p.Price, descending: true)
			.BuildUrl();

		url.Should().Contain("$orderby=Price%20desc");
	}

	/// <summary>
	/// Tests multiple orderby clauses.
	/// </summary>
	[Fact]
	public void OrderBy_Multiple_CombinesClauses()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.OrderBy("Name")
			.OrderBy("Price desc")
			.BuildUrl();

		url.Should().Contain("$orderby=Name,Price%20desc");
	}

	#endregion

	#region $skip and $top

	/// <summary>
	/// Tests skip.
	/// </summary>
	[Fact]
	public void Skip_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Skip(10)
			.BuildUrl();

		url.Should().Contain("$skip=10");
	}

	/// <summary>
	/// Tests top.
	/// </summary>
	[Fact]
	public void Top_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Top(5)
			.BuildUrl();

		url.Should().Contain("$top=5");
	}

	/// <summary>
	/// Tests skip and top together for paging.
	/// </summary>
	[Fact]
	public void SkipAndTop_ForPaging_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Skip(20)
			.Top(10)
			.BuildUrl();

		url.Should().Contain("$skip=20");
		url.Should().Contain("$top=10");
	}

	#endregion

	#region $count

	/// <summary>
	/// Tests count.
	/// </summary>
	[Fact]
	public void Count_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Count()
			.BuildUrl();

		url.Should().Contain("$count=true");
	}

	/// <summary>
	/// Tests count with false.
	/// </summary>
	[Fact]
	public void Count_False_DoesNotInclude()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Count(false)
			.BuildUrl();

		url.Should().NotContain("$count");
	}

	#endregion

	#region $search

	/// <summary>
	/// Tests search.
	/// </summary>
	[Fact]
	public void Search_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Search("widget")
			.BuildUrl();

		url.Should().Contain("$search=widget");
	}

	/// <summary>
	/// Tests search with phrase.
	/// </summary>
	[Fact]
	public void Search_WithPhrase_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Search("\"blue widget\"")
			.BuildUrl();

		url.Should().Contain("$search=");
	}

	/// <summary>
	/// Tests search with AND/OR/NOT.
	/// </summary>
	[Fact]
	public void Search_WithLogicalOperators_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Search("widget AND blue NOT red")
			.BuildUrl();

		url.Should().Contain("$search=");
	}

	#endregion

	#region $apply (Aggregations)

	/// <summary>
	/// Tests apply with groupby.
	/// </summary>
	[Fact]
	public void Apply_WithGroupBy_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Apply("groupby((Name), aggregate(Price with sum as TotalPrice))")
			.BuildUrl();

		url.Should().Contain("$apply=");
		url.Should().Contain("groupby");
	}

	/// <summary>
	/// Tests apply with filter.
	/// </summary>
	[Fact]
	public void Apply_WithFilter_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Apply("filter(Price gt 100)")
			.BuildUrl();

		url.Should().Contain("$apply=");
	}

	/// <summary>
	/// Tests apply with aggregate.
	/// </summary>
	[Fact]
	public void Apply_WithAggregate_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Apply("aggregate(Price with sum as TotalPrice, Price with average as AvgPrice)")
			.BuildUrl();

		url.Should().Contain("$apply=");
		url.Should().Contain("aggregate");
	}

	/// <summary>
	/// Tests apply with compute.
	/// </summary>
	[Fact]
	public void Apply_WithCompute_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Apply("compute(Price mul 1.1 as PriceWithTax)")
			.BuildUrl();

		url.Should().Contain("$apply=");
		url.Should().Contain("compute");
	}

	/// <summary>
	/// Tests apply with topcount.
	/// </summary>
	[Fact]
	public void Apply_WithTopCount_GeneratesCorrectUrl()
	{
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Apply("topcount(5, Price)")
			.BuildUrl();

		url.Should().Contain("$apply=");
		url.Should().Contain("topcount");
	}

	#endregion

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
		url.Should().Contain("$orderby=Price%20desc");
		url.Should().Contain("$skip=10");
		url.Should().Contain("$top=5");
		url.Should().Contain("$count=true");
	}

	#endregion
}
