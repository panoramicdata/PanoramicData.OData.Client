using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.OData.Client.Test.Models;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataQueryBuilder date/time and math function support.
/// Tests OData V4 date, time, and arithmetic functions.
/// </summary>
public class QueryBuilderDateTimeMathTests
{
	#region Date/Time Functions (Raw Filters - Expression support not yet implemented)

	/// <summary>
	/// Tests year function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithYear_RawFilter_Works()
	{
		// OData V4 supports: year(ReleaseDate) eq 2024
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("year(ReleaseDate) eq 2024")
			.BuildUrl();

		// URL encoding: ( becomes %28, ) becomes %29
		url.Should().Contain("year%28ReleaseDate%29");
	}

	/// <summary>
	/// Tests month function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithMonth_RawFilter_Works()
	{
		// OData V4 supports: month(ReleaseDate) eq 6
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("month(ReleaseDate) eq 6")
			.BuildUrl();

		url.Should().Contain("month%28ReleaseDate%29");
	}

	/// <summary>
	/// Tests day function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithDay_RawFilter_Works()
	{
		// OData V4 supports: day(ReleaseDate) eq 15
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("day(ReleaseDate) eq 15")
			.BuildUrl();

		url.Should().Contain("day%28ReleaseDate%29");
	}

	/// <summary>
	/// Tests hour function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithHour_RawFilter_Works()
	{
		// OData V4 supports: hour(ReleaseDate) eq 14
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("hour(ReleaseDate) eq 14")
			.BuildUrl();

		url.Should().Contain("hour%28ReleaseDate%29");
	}

	/// <summary>
	/// Tests minute function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithMinute_RawFilter_Works()
	{
		// OData V4 supports: minute(ReleaseDate) eq 30
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("minute(ReleaseDate) eq 30")
			.BuildUrl();

		url.Should().Contain("minute%28ReleaseDate%29");
	}

	/// <summary>
	/// Tests second function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithSecond_RawFilter_Works()
	{
		// OData V4 supports: second(ReleaseDate) eq 0
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("second(ReleaseDate) eq 0")
			.BuildUrl();

		url.Should().Contain("second%28ReleaseDate%29");
	}

	/// <summary>
	/// Tests fractionalseconds function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithFractionalSeconds_RawFilter_Works()
	{
		// OData V4 supports: fractionalseconds(ReleaseDate) lt 0.5
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("fractionalseconds(ReleaseDate) lt 0.5")
			.BuildUrl();

		url.Should().Contain("fractionalseconds%28ReleaseDate%29");
	}

	/// <summary>
	/// Tests date function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithDate_RawFilter_Works()
	{
		// OData V4 supports: date(ReleaseDate) eq 2024-06-15
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("date(ReleaseDate) eq 2024-06-15")
			.BuildUrl();

		url.Should().Contain("date%28ReleaseDate%29");
	}

	/// <summary>
	/// Tests time function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithTime_RawFilter_Works()
	{
		// OData V4 supports: time(ReleaseDate) eq 14:30:00
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("time(ReleaseDate) eq 14:30:00")
			.BuildUrl();

		url.Should().Contain("time%28ReleaseDate%29");
	}

	/// <summary>
	/// Tests now function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithNow_RawFilter_Works()
	{
		// OData V4 supports: ReleaseDate lt now()
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("ReleaseDate lt now()")
			.BuildUrl();

		url.Should().Contain("now%28%29");
	}

	/// <summary>
	/// Tests mindatetime function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithMinDateTime_RawFilter_Works()
	{
		// OData V4 supports: ReleaseDate ne mindatetime()
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("ReleaseDate ne mindatetime()")
			.BuildUrl();

		url.Should().Contain("mindatetime%28%29");
	}

	/// <summary>
	/// Tests maxdatetime function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithMaxDateTime_RawFilter_Works()
	{
		// OData V4 supports: ReleaseDate ne maxdatetime()
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("ReleaseDate ne maxdatetime()")
			.BuildUrl();

		url.Should().Contain("maxdatetime%28%29");
	}

	/// <summary>
	/// Tests totaloffsetminutes function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithTotalOffsetMinutes_RawFilter_Works()
	{
		// OData V4 supports: totaloffsetminutes(ReleaseDate) eq 0
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("totaloffsetminutes(ReleaseDate) eq 0")
			.BuildUrl();

		url.Should().Contain("totaloffsetminutes%28ReleaseDate%29");
	}

	#endregion

	#region Math Functions (Raw Filters)

	/// <summary>
	/// Tests round function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithRound_RawFilter_Works()
	{
		// OData V4 supports: round(Price) eq 100
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("round(Price) eq 100")
			.BuildUrl();

		url.Should().Contain("round%28Price%29");
	}

	/// <summary>
	/// Tests floor function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithFloor_RawFilter_Works()
	{
		// OData V4 supports: floor(Price) eq 99
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("floor(Price) eq 99")
			.BuildUrl();

		url.Should().Contain("floor%28Price%29");
	}

	/// <summary>
	/// Tests ceiling function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithCeiling_RawFilter_Works()
	{
		// OData V4 supports: ceiling(Price) eq 100
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("ceiling(Price) eq 100")
			.BuildUrl();

		url.Should().Contain("ceiling%28Price%29");
	}

	#endregion

	#region Arithmetic Operators (Raw Filters)

	/// <summary>
	/// Tests add operator via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithAdd_RawFilter_Works()
	{
		// OData V4 supports: Price add 10 gt 100
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("Price add 10 gt 100")
			.BuildUrl();

		url.Should().Contain("Price%20add%2010");
	}

	/// <summary>
	/// Tests sub operator via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithSub_RawFilter_Works()
	{
		// OData V4 supports: Price sub 10 lt 90
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("Price sub 10 lt 90")
			.BuildUrl();

		url.Should().Contain("Price%20sub%2010");
	}

	/// <summary>
	/// Tests mul operator via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithMul_RawFilter_Works()
	{
		// OData V4 supports: Price mul 2 gt 200
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("Price mul 2 gt 200")
			.BuildUrl();

		url.Should().Contain("Price%20mul%202");
	}

	/// <summary>
	/// Tests div operator via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithDiv_RawFilter_Works()
	{
		// OData V4 supports: Price div 2 lt 50
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("Price div 2 lt 50")
			.BuildUrl();

		url.Should().Contain("Price%20div%202");
	}

	/// <summary>
	/// Tests divby operator via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithDivBy_RawFilter_Works()
	{
		// OData V4 supports: Price divby 2 lt 50.5
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("Price divby 2 lt 50.5")
			.BuildUrl();

		url.Should().Contain("Price%20divby%202");
	}

	/// <summary>
	/// Tests mod operator via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithMod_RawFilter_Works()
	{
		// OData V4 supports: Rating mod 2 eq 0
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("Rating mod 2 eq 0")
			.BuildUrl();

		url.Should().Contain("Rating%20mod%202");
	}

	#endregion
}
