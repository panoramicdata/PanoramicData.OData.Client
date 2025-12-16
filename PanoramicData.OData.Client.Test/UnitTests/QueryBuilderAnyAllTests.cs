using System.Diagnostics.CodeAnalysis;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.OData.Client.Test.Models;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for expression-based any/all lambda operators.
/// </summary>
[SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "Testing OData expression translation, not actual string operations")]
[SuppressMessage("Globalization", "CA1310:Specify StringComparison for correctness", Justification = "Testing OData expression translation, not actual string operations")]
[SuppressMessage("Globalization", "CA1311:Specify a culture or use an invariant version", Justification = "Testing OData expression translation")]
[SuppressMessage("Performance", "CA1862:Use the 'StringComparison' method overloads", Justification = "Testing OData expression translation")]
[SuppressMessage("Performance", "CA1847:Use char literal for single character lookup", Justification = "Testing OData expression translation")]
public class QueryBuilderAnyAllTests
{
	private readonly ODataQueryBuilder<Person> _queryBuilder;

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	public QueryBuilderAnyAllTests()
	{
		_queryBuilder = new ODataQueryBuilder<Person>("People", NullLogger.Instance);
	}

	#region Any Tests - Basic

	/// <summary>
	/// Tests that Any() without predicate generates correct URL.
	/// </summary>
	[Fact]
	public void Filter_WithAnyNoPredicate_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Filter(p => p.Emails!.Any())
			.BuildUrl();

		// Assert - URL encodes ( as %28, ) as %29, / as %2F
		// Emails/any() -> Emails%2Fany%28%29
		url.Should().Contain("Emails%2Fany%28%29");
	}

	/// <summary>
	/// Tests that Any() with simple predicate generates correct URL.
	/// </summary>
	[Fact]
	public void Filter_WithAnySimplePredicate_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Filter(p => p.Emails!.Any(e => e.Contains("@example.com")))
			.BuildUrl();

		// Assert
		// Emails/any(e: contains(e, '@example.com'))
		url.Should().Contain("Emails%2Fany%28e%3A%20contains%28e%2C%27%40example.com%27%29%29");
	}

	/// <summary>
	/// Tests that Any() on navigation property collection generates correct URL.
	/// </summary>
	[Fact]
	public void Filter_WithAnyOnNavigationProperty_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Filter(p => p.Friends!.Any(f => f.FirstName == "John"))
			.BuildUrl();

		// Assert
		// Friends/any(f: f/FirstName eq 'John')
		url.Should().Contain("Friends%2Fany%28f%3A%20f%2FFirstName%20eq%20%27John%27%29");
	}

	/// <summary>
	/// Tests that Any() with greater than comparison generates correct URL.
	/// </summary>
	[Fact]
	public void Filter_WithAnyGreaterThan_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Filter(p => p.Friends!.Any(f => f.Age > 18))
			.BuildUrl();

		// Assert
		// Friends/any(f: f/Age gt 18)
		url.Should().Contain("Friends%2Fany%28f%3A%20f%2FAge%20gt%2018%29");
	}

	#endregion

	#region All Tests - Basic

	/// <summary>
	/// Tests that All() with predicate generates correct URL.
	/// </summary>
	[Fact]
	public void Filter_WithAllPredicate_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Filter(p => p.Friends!.All(f => f.Age >= 21))
			.BuildUrl();

		// Assert
		// Friends/all(f: f/Age ge 21)
		url.Should().Contain("Friends%2Fall%28f%3A%20f%2FAge%20ge%2021%29");
	}

	/// <summary>
	/// Tests that All() with string comparison generates correct URL.
	/// </summary>
	[Fact]
	public void Filter_WithAllStringEquals_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Filter(p => p.Emails!.All(e => e.EndsWith("@company.com")))
			.BuildUrl();

		// Assert
		// Emails/all(e: endswith(e, '@company.com'))
		url.Should().Contain("Emails%2Fall%28e%3A%20endswith%28e%2C%27%40company.com%27%29%29");
	}

	#endregion

	#region Combined Filters

	/// <summary>
	/// Tests that Any() can be combined with other filters.
	/// </summary>
	[Fact]
	public void Filter_AnyWithOtherFilters_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Filter(p => p.Age > 25 && p.Emails!.Any(e => e.Contains("@")))
			.BuildUrl();

		// Assert - Should contain both conditions
		url.Should().Contain("Age%20gt%2025");
		url.Should().Contain("Emails%2Fany%28e%3A%20contains%28e%2C%27%40%27%29%29");
		url.Should().Contain("and");
	}

	/// <summary>
	/// Tests that Any() can be used in OR conditions.
	/// </summary>
	[Fact]
	public void Filter_AnyWithOrCondition_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Filter(p => p.Friends!.Any(f => f.FirstName == "John") || p.Friends!.Any(f => f.FirstName == "Jane"))
			.BuildUrl();

		// Assert - Should contain both conditions with 'or'
		url.Should().Contain("Friends%2Fany");
		url.Should().Contain("%27John%27");
		url.Should().Contain("%27Jane%27");
		url.Should().Contain("or");
	}

	#endregion

	#region String Methods in Lambda

	/// <summary>
	/// Tests that StartsWith works inside Any lambda.
	/// </summary>
	[Fact]
	public void Filter_AnyWithStartsWith_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Filter(p => p.Emails!.Any(e => e.StartsWith("admin@")))
			.BuildUrl();

		// Assert
		url.Should().Contain("startswith%28e%2C%27admin%40%27%29");
	}

	/// <summary>
	/// Tests that ToLower works inside Any lambda.
	/// </summary>
	[Fact]
	public void Filter_AnyWithToLower_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Filter(p => p.Friends!.Any(f => f.FirstName.ToLower() == "john"))
			.BuildUrl();

		// Assert
		url.Should().Contain("tolower%28f%2FFirstName%29%20eq%20%27john%27");
	}

	#endregion

	#region Complex Predicates

	/// <summary>
	/// Tests that complex AND predicates work inside Any.
	/// </summary>
	[Fact]
	public void Filter_AnyWithComplexAndPredicate_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Filter(p => p.Friends!.Any(f => f.Age > 18 && f.FirstName != ""))
			.BuildUrl();

		// Assert
		url.Should().Contain("f%2FAge%20gt%2018");
		url.Should().Contain("and");
		url.Should().Contain("f%2FFirstName%20ne%20%27%27");
	}

	/// <summary>
	/// Tests that NOT predicate works inside Any.
	/// </summary>
	[Fact]
	public void Filter_AnyWithNotPredicate_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Filter(p => p.Emails!.Any(e => !e.Contains("spam")))
			.BuildUrl();

		// Assert
		url.Should().Contain("not%20%28contains%28e%2C%27spam%27%29%29");
	}

	#endregion

	#region Variable Capture

	/// <summary>
	/// Tests that captured variables work in Any lambda.
	/// </summary>
	[Fact]
	public void Filter_AnyWithCapturedVariable_GeneratesCorrectUrl()
	{
		// Arrange
		var minAge = 21;

		// Act
		var url = _queryBuilder
			.Filter(p => p.Friends!.Any(f => f.Age >= minAge))
			.BuildUrl();

		// Assert
		url.Should().Contain("f%2FAge%20ge%2021");
	}

	/// <summary>
	/// Tests that captured string variables work in Any lambda.
	/// </summary>
	[Fact]
	public void Filter_AnyWithCapturedStringVariable_GeneratesCorrectUrl()
	{
		// Arrange
		var domain = "@example.com";

		// Act
		var url = _queryBuilder
			.Filter(p => p.Emails!.Any(e => e.Contains(domain)))
			.BuildUrl();

		// Assert
		url.Should().Contain("contains%28e%2C%27%40example.com%27%29");
	}

	#endregion
}
