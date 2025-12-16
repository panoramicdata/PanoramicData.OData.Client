using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.OData.Client.Test.Models;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataQueryBuilder filter expression parsing.
/// Tests all OData V4 comparison and logical operators.
/// </summary>
public class QueryBuilderFilterTests
{
	private readonly ODataQueryBuilder<Product> _builder;

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	public QueryBuilderFilterTests()
	{
		_builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance);
	}

	#region Comparison Operators

	/// <summary>
	/// Tests equality filter (eq).
	/// </summary>
	[Fact]
	public void Filter_WithEquality_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Name == "Widget")
			.BuildUrl();

		// Assert
		url.Should().Contain("$filter=");
		url.Should().Contain("Name%20eq%20'Widget'");
	}

	/// <summary>
	/// Tests inequality filter (ne).
	/// </summary>
	[Fact]
	public void Filter_WithInequality_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Name != "Widget")
			.BuildUrl();

		// Assert
		url.Should().Contain("Name%20ne%20'Widget'");
	}

	/// <summary>
	/// Tests greater than filter (gt).
	/// </summary>
	[Fact]
	public void Filter_WithGreaterThan_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Price > 100)
			.BuildUrl();

		// Assert
		url.Should().Contain("Price%20gt%20100");
	}

	/// <summary>
	/// Tests greater than or equal filter (ge).
	/// </summary>
	[Fact]
	public void Filter_WithGreaterThanOrEqual_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Price >= 100)
			.BuildUrl();

		// Assert
		url.Should().Contain("Price%20ge%20100");
	}

	/// <summary>
	/// Tests less than filter (lt).
	/// </summary>
	[Fact]
	public void Filter_WithLessThan_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Price < 100)
			.BuildUrl();

		// Assert
		url.Should().Contain("Price%20lt%20100");
	}

	/// <summary>
	/// Tests less than or equal filter (le).
	/// </summary>
	[Fact]
	public void Filter_WithLessThanOrEqual_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Price <= 100)
			.BuildUrl();

		// Assert
		url.Should().Contain("Price%20le%20100");
	}

	#endregion

	#region Logical Operators

	/// <summary>
	/// Tests AND logical operator.
	/// </summary>
	[Fact]
	public void Filter_WithAnd_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Price > 100 && p.Rating > 3)
			.BuildUrl();

		// Assert
		url.Should().Contain("Price%20gt%20100%20and%20Rating%20gt%203");
	}

	/// <summary>
	/// Tests OR logical operator.
	/// </summary>
	[Fact]
	public void Filter_WithOr_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Price < 50 || p.Rating > 4)
			.BuildUrl();

		// Assert
		url.Should().Contain("Price%20lt%2050%20or%20Rating%20gt%204");
	}

	/// <summary>
	/// Tests NOT logical operator.
	/// </summary>
	[Fact]
	public void Filter_WithNot_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => !(p.Price > 100))
			.BuildUrl();

		// Assert
		url.Should().Contain("not%20(Price%20gt%20100)");
	}

	/// <summary>
	/// Tests multiple filters combined with AND.
	/// </summary>
	[Fact]
	public void Filter_MultipleFilters_CombinesWithAnd()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Price > 100)
			.Filter(p => p.Rating > 3)
			.BuildUrl();

		// Assert
		url.Should().Contain("$filter=");
		url.Should().Contain("and");
	}

	#endregion

	#region Null Handling

	/// <summary>
	/// Tests null equality check.
	/// </summary>
	[Fact]
	public void Filter_WithNullCheck_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Description == null)
			.BuildUrl();

		// Assert
		url.Should().Contain("Description%20eq%20null");
	}

	/// <summary>
	/// Tests not null check.
	/// </summary>
	[Fact]
	public void Filter_WithNotNullCheck_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Description != null)
			.BuildUrl();

		// Assert
		url.Should().Contain("Description%20ne%20null");
	}

	#endregion

	#region Raw Filter Strings

	/// <summary>
	/// Tests raw filter string.
	/// </summary>
	[Fact]
	public void Filter_WithRawString_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("Price gt 100 and Rating gt 3")
			.BuildUrl();

		// Assert
		url.Should().Contain("$filter=Price%20gt%20100%20and%20Rating%20gt%203");
	}

	#endregion

	#region Collection Contains (IN clause)

	/// <summary>
	/// Tests collection Contains with array.
	/// </summary>
	[Fact]
	public void Filter_WithArrayContains_GeneratesInClause()
	{
		// Arrange
		var ids = new[] { 1, 2, 3 };

		// Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => ids.Contains(p.Id))
			.BuildUrl();

		// Assert
		url.Should().Contain("Id%20in%20(1%2C2%2C3)");
	}

	/// <summary>
	/// Tests collection Contains with list.
	/// </summary>
	[Fact]
	public void Filter_WithListContains_GeneratesInClause()
	{
		// Arrange
		var names = new List<string> { "Widget", "Gadget" };

		// Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => names.Contains(p.Name))
			.BuildUrl();

		// Assert
		url.Should().Contain("Name%20in%20('Widget'%2C'Gadget')");
	}

	/// <summary>
	/// Tests empty collection Contains.
	/// </summary>
	[Fact]
	public void Filter_WithEmptyCollection_GeneratesFalse()
	{
		// Arrange
		var ids = Array.Empty<int>();

		// Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => ids.Contains(p.Id))
			.BuildUrl();

		// Assert
		url.Should().Contain("$filter=(false)");
	}

	#endregion

	#region Captured Variables

	/// <summary>
	/// Tests filter with captured variable.
	/// </summary>
	[Fact]
	public void Filter_WithCapturedVariable_EvaluatesVariable()
	{
		// Arrange
		var minPrice = 100m;

		// Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Price > minPrice)
			.BuildUrl();

		// Assert
		url.Should().Contain("Price%20gt%20100");
	}

	/// <summary>
	/// Tests filter with DateTime.UtcNow.
	/// </summary>
	[Fact]
	public void Filter_WithDateTimeUtcNow_EvaluatesCurrentTime()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.ReleaseDate < DateTime.UtcNow)
			.BuildUrl();

		// Assert
		url.Should().Contain("ReleaseDate%20lt%20");
		// Should contain a date in ISO format
		url.Should().MatchRegex(@"ReleaseDate%20lt%20\d{4}-\d{2}-\d{2}T");
	}

	#endregion
}
