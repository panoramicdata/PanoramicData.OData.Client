using System.Diagnostics.CodeAnalysis;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.OData.Client.Test.Models;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for ODataQueryBuilder string function support.
/// Tests all OData V4 string functions.
/// </summary>
[SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "Testing OData expression translation, not actual string operations")]
[SuppressMessage("Globalization", "CA1311:Specify a culture or use an invariant version", Justification = "Testing OData expression translation, not actual string operations")]
public class QueryBuilderStringFunctionTests
{
	#region Basic String Functions

	/// <summary>
	/// Tests contains function.
	/// </summary>
	[Fact]
	public void Filter_WithContains_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Name.Contains("widget"))
			.BuildUrl();

		// Assert
		url.Should().Contain("contains(Name%2C'widget')");
	}

	/// <summary>
	/// Tests startswith function.
	/// </summary>
	[Fact]
	public void Filter_WithStartsWith_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Name.StartsWith("Widget"))
			.BuildUrl();

		// Assert
		url.Should().Contain("startswith(Name%2C'Widget')");
	}

	/// <summary>
	/// Tests endswith function.
	/// </summary>
	[Fact]
	public void Filter_WithEndsWith_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Name.EndsWith("Pro"))
			.BuildUrl();

		// Assert
		url.Should().Contain("endswith(Name%2C'Pro')");
	}

	/// <summary>
	/// Tests tolower function.
	/// </summary>
	[Fact]
	public void Filter_WithToLower_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Name.ToLower() == "widget")
			.BuildUrl();

		// Assert
		url.Should().Contain("tolower(Name)%20eq%20'widget'");
	}

	/// <summary>
	/// Tests toupper function.
	/// </summary>
	[Fact]
	public void Filter_WithToUpper_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Name.ToUpper() == "WIDGET")
			.BuildUrl();

		// Assert
		url.Should().Contain("toupper(Name)%20eq%20'WIDGET'");
	}

	/// <summary>
	/// Tests trim function.
	/// </summary>
	[Fact]
	public void Filter_WithTrim_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Name.Trim() == "Widget")
			.BuildUrl();

		// Assert
		url.Should().Contain("trim(Name)%20eq%20'Widget'");
	}

	/// <summary>
	/// Tests nested string functions (tolower + contains).
	/// </summary>
	[Fact]
	public void Filter_WithNestedStringFunctions_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Name.ToLower().Contains("widget"))
			.BuildUrl();

		// Assert
		url.Should().Contain("contains(tolower(Name)%2C'widget')");
	}

	#endregion

	#region Missing String Functions (Expected to Fail)

	/// <summary>
	/// Tests concat function - NOT YET IMPLEMENTED.
	/// </summary>
	[Fact]
	public void Filter_WithConcat_ShouldGenerateCorrectUrl()
	{
		// This test documents missing functionality
		// OData V4 supports: concat(FirstName, LastName)
		
		// Arrange & Act - Raw filter should work
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Filter("concat(FirstName, LastName) eq 'JohnDoe'")
			.BuildUrl();

		// Assert
		url.Should().Contain("concat(FirstName");
	}

	/// <summary>
	/// Tests substring function - NOT YET IMPLEMENTED via expression.
	/// </summary>
	[Fact]
	public void Filter_WithSubstring_RawFilter_Works()
	{
		// OData V4 supports: substring(Name, 0, 3) eq 'Wid'
		
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("substring(Name, 0, 3) eq 'Wid'")
			.BuildUrl();

		// Assert
		url.Should().Contain("substring(Name");
	}

	/// <summary>
	/// Tests length function - NOT YET IMPLEMENTED via expression.
	/// </summary>
	[Fact]
	public void Filter_WithLength_RawFilter_Works()
	{
		// OData V4 supports: length(Name) gt 5
		
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("length(Name) gt 5")
			.BuildUrl();

		// Assert
		url.Should().Contain("length(Name)");
	}

	/// <summary>
	/// Tests indexof function - NOT YET IMPLEMENTED via expression.
	/// </summary>
	[Fact]
	public void Filter_WithIndexOf_RawFilter_Works()
	{
		// OData V4 supports: indexof(Name, 'Widget') eq 0
		
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("indexof(Name, 'Widget') eq 0")
			.BuildUrl();

		// Assert
		url.Should().Contain("indexof(Name");
	}

	/// <summary>
	/// Tests matchesPattern function - OData V4.01 feature.
	/// </summary>
	[Fact]
	public void Filter_WithMatchesPattern_RawFilter_Works()
	{
		// OData V4.01 supports: matchesPattern(Name, '^Wid.*')
		
		// Arrange & Act
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("matchesPattern(Name, '^Wid.*')")
			.BuildUrl();

		// Assert
		url.Should().Contain("matchesPattern");
	}

	#endregion
}
