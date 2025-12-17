namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests for nested $expand expressions using anonymous types.
/// See: https://github.com/panoramicdata/PanoramicData.OData.Client/issues/3
/// </summary>
public class NestedExpandExpressionTests
{
	/// <summary>
	/// Tests that a nested expand via anonymous type produces correct OData syntax.
	/// Example: p => new { p.CurrentTenant, p.CurrentTenant!.Subscriptions }
	/// Should produce: $expand=CurrentTenant($expand=Subscriptions)
	/// </summary>
	[Fact]
	public void Expand_WithNestedPropertyInAnonymousType_ProducesNestedExpandSyntax()
	{
		// Arrange
		var builder = new ODataQueryBuilder<Person>("People", NullLogger.Instance);

		// Act - This is the pattern from issue 3
		var url = builder
			.Expand(p => new { p.BestFriend, p.BestFriend!.Trips })
			.BuildUrl();

		// Assert - Should produce nested $expand syntax
		url.Should().Contain("$expand=BestFriend($expand=Trips)");
	}

	/// <summary>
	/// Tests that a single nested expand produces correct OData syntax.
	/// Example: p => p.BestFriend!.Trips
	/// Should produce: $expand=BestFriend($expand=Trips)
	/// </summary>
	[Fact]
	public void Expand_WithSingleNestedProperty_ProducesNestedExpandSyntax()
	{
		// Arrange
		var builder = new ODataQueryBuilder<Person>("People", NullLogger.Instance);

		// Act
		var url = builder
			.Expand(p => p.BestFriend!.Trips!)
			.BuildUrl();

		// Assert
		url.Should().Contain("$expand=BestFriend($expand=Trips)");
	}

	/// <summary>
	/// Tests that multiple independent expands still work correctly.
	/// </summary>
	[Fact]
	public void Expand_WithMultipleIndependentProperties_ProducesCommaSepa­ratedExpands()
	{
		// Arrange
		var builder = new ODataQueryBuilder<Person>("People", NullLogger.Instance);

		// Act
		var url = builder
			.Expand(p => new { p.BestFriend, p.Trips })
			.BuildUrl();

		// Assert - Should produce comma-separated list
		url.Should().Contain("$expand=");
		url.Should().Contain("BestFriend");
		url.Should().Contain("Trips");
	}

	/// <summary>
	/// Tests that a simple single property expand still works.
	/// </summary>
	[Fact]
	public void Expand_WithSingleProperty_ProducesSimpleExpand()
	{
		// Arrange
		var builder = new ODataQueryBuilder<Person>("People", NullLogger.Instance);

		// Act
		var url = builder
			.Expand(p => p.BestFriend!)
			.BuildUrl();

		// Assert
		url.Should().Be("People?$expand=BestFriend");
	}

	/// <summary>
	/// Tests that deeply nested expands work (3 levels).
	/// Example: p => p.BestFriend!.BestFriend!.Trips
	/// Should produce: $expand=BestFriend($expand=BestFriend($expand=Trips))
	/// </summary>
	[Fact]
	public void Expand_WithDeeplyNestedProperty_ProducesMultiLevelNestedExpand()
	{
		// Arrange
		var builder = new ODataQueryBuilder<Person>("People", NullLogger.Instance);

		// Act
		var url = builder
			.Expand(p => p.BestFriend!.BestFriend!.Trips!)
			.BuildUrl();

		// Assert
		url.Should().Contain("$expand=BestFriend($expand=BestFriend($expand=Trips))");
	}

	/// <summary>
	/// Tests combining nested expands via anonymous type at different levels.
	/// Example: p => new { p.BestFriend, p.BestFriend!.Trips, p.Friends }
	/// Should produce: $expand=BestFriend($expand=Trips),Friends
	/// </summary>
	[Fact]
	public void Expand_WithMixedNestedAndSimpleInAnonymousType_ProducesCorrectSyntax()
	{
		// Arrange
		var builder = new ODataQueryBuilder<Person>("People", NullLogger.Instance);

		// Act
		var url = builder
			.Expand(p => new { p.BestFriend, p.BestFriend!.Trips, p.Friends })
			.BuildUrl();

		// Assert
		url.Should().Contain("$expand=");
		url.Should().Contain("BestFriend($expand=Trips)");
		url.Should().Contain("Friends");
	}

	/// <summary>
	/// Tests that the parent navigation property doesn't need to be explicitly included
	/// when only the nested property is specified.
	/// Example: p => p.BestFriend!.Trips should still work
	/// </summary>
	[Fact]
	public void Expand_WithOnlyNestedProperty_ImpliesParentExpand()
	{
		// Arrange
		var builder = new ODataQueryBuilder<Person>("People", NullLogger.Instance);

		// Act
		var url = builder
			.Expand(p => p.BestFriend!.Friends!)
			.BuildUrl();

		// Assert - Parent should be implied
		url.Should().Contain("$expand=BestFriend($expand=Friends)");
	}

	/// <summary>
	/// Tests that multiple nested paths under the same parent are combined.
	/// Example: p => new { p.BestFriend!.Trips, p.BestFriend!.Friends }
	/// Should produce: $expand=BestFriend($expand=Trips,Friends) or similar valid syntax
	/// </summary>
	[Fact]
	public void Expand_WithMultipleNestedPathsUnderSameParent_CombinesCorrectly()
	{
		// Arrange
		var builder = new ODataQueryBuilder<Person>("People", NullLogger.Instance);

		// Act
		var url = builder
			.Expand(p => new { p.BestFriend!.Trips, p.BestFriend!.Friends })
			.BuildUrl();

		// Assert - Should combine nested expands under same parent
		url.Should().Contain("$expand=BestFriend($expand=");
		url.Should().Contain("Trips");
		url.Should().Contain("Friends");
	}
}
