using System.Diagnostics.CodeAnalysis;

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

		// Assert - URL encoding: , becomes %2C, ' becomes %27
		url.Should().Contain("contains%28Name%2C%27widget%27%29");
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

		// Assert - URL encoding
		url.Should().Contain("startswith%28Name%2C%27Widget%27%29");
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

		// Assert - URL encoding
		url.Should().Contain("endswith%28Name%2C%27Pro%27%29");
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

		// Assert - URL encoding
		url.Should().Contain("tolower%28Name%29%20eq%20%27widget%27");
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

		// Assert - URL encoding
		url.Should().Contain("toupper%28Name%29%20eq%20%27WIDGET%27");
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

		// Assert - URL encoding
		url.Should().Contain("trim%28Name%29%20eq%20%27Widget%27");
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

		// Assert - URL encoding
		url.Should().Contain("contains%28tolower%28Name%29%2C%27widget%27%29");
	}

	#endregion

	#region String Functions via Raw Filters

	/// <summary>
	/// Tests concat function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithConcat_RawFilter_Works()
	{
		// OData V4 supports: concat(FirstName, LastName)
		var url = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Filter("concat(FirstName, LastName) eq 'JohnDoe'")
			.BuildUrl();

		// Assert
		url.Should().Contain("concat");
	}

	/// <summary>
	/// Tests substring function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithSubstring_RawFilter_Works()
	{
		// OData V4 supports: substring(Name, 0, 3) eq 'Wid'
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("substring(Name, 0, 3) eq 'Wid'")
			.BuildUrl();

		// Assert
		url.Should().Contain("substring");
	}

	/// <summary>
	/// Tests length function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithLength_RawFilter_Works()
	{
		// OData V4 supports: length(Name) gt 5
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("length(Name) gt 5")
			.BuildUrl();

		// Assert
		url.Should().Contain("length");
	}

	/// <summary>
	/// Tests indexof function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithIndexOf_RawFilter_Works()
	{
		// OData V4 supports: indexof(Name, 'Widget') eq 0
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("indexof(Name, 'Widget') eq 0")
			.BuildUrl();

		// Assert
		url.Should().Contain("indexof");
	}

	/// <summary>
	/// Tests matchesPattern function via raw filter.
	/// </summary>
	[Fact]
	public void Filter_WithMatchesPattern_RawFilter_Works()
	{
		// OData V4.01 supports: matchesPattern(Name, '^Wid.*')
		var url = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("matchesPattern(Name, '^Wid.*')")
			.BuildUrl();

		// Assert
		url.Should().Contain("matchesPattern");
	}

	#endregion
}
