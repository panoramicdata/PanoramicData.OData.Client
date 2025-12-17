namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for OR operator precedence in filter expressions.
/// Verifies that OR expressions are properly wrapped in parentheses when combined with AND.
/// See: https://github.com/panoramicdata/PanoramicData.OData.Client/issues/2
/// </summary>
public class QueryBuilderOrPrecedenceTests
{
	#region Test Model

	/// <summary>
	/// Test entity with properties needed for OR precedence tests.
	/// </summary>
	public class TestEntity
	{
		/// <summary>Gets or sets the ID.</summary>
		public int Id { get; set; }

		/// <summary>Gets or sets the status.</summary>
		public string Status { get; set; } = string.Empty;

		/// <summary>Gets or sets the name.</summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>Gets or sets the category.</summary>
		public string Category { get; set; } = string.Empty;

		/// <summary>Gets or sets the tag.</summary>
		public string Tag { get; set; } = string.Empty;

		/// <summary>Gets or sets the priority.</summary>
		public int Priority { get; set; }

		/// <summary>Gets or sets whether the entity is active.</summary>
		public bool IsActive { get; set; }
	}

	#endregion

	#region Basic OR Precedence Tests

	/// <summary>
	/// Tests that mixed AND/OR expressions preserve parentheses around OR group.
	/// Should generate filter with parentheses around the OR clause.
	/// </summary>
	[Fact]
	public void Filter_WithMixedAndOr_PreservesParentheses()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<TestEntity>("TestEntities", NullLogger.Instance)
			.Filter(e => e.Status == "Active" && (e.Name.Contains("foo") || e.Name.Contains("bar")))
			.BuildUrl();

		// Assert - The URL should contain parentheses around the OR group
		// Decoded: Status eq 'Active' and (contains(Name,'foo') or contains(Name,'bar'))
		// The OR group must be wrapped in parentheses to preserve precedence
		var decoded = Uri.UnescapeDataString(url);

		// Verify the structure: AND with (OR group)
		decoded.Should().Contain("Status eq 'Active'");
		decoded.Should().Contain("contains(Name,'foo')");
		decoded.Should().Contain("contains(Name,'bar')");

		// The key assertion: OR should be wrapped in parentheses
		decoded.Should().Contain("(contains(Name,'foo') or contains(Name,'bar'))");
	}

	/// <summary>
	/// Tests that nested OR in multiple AND conditions preserves parentheses.
	/// The OR should NOT break out of the AND group.
	/// </summary>
	[Fact]
	public void Filter_WithNestedOrInAnd_ReturnsCorrectResults()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<TestEntity>("TestEntities", NullLogger.Instance)
			.Filter(e => e.Id == 1 && e.Category == "A" && (e.Tag == "X" || e.Tag == "Y"))
			.BuildUrl();

		// Assert
		var decoded = Uri.UnescapeDataString(url);

		// Verify the structure
		decoded.Should().Contain("Id eq 1");
		decoded.Should().Contain("Category eq 'A'");
		decoded.Should().Contain("Tag eq 'X'");
		decoded.Should().Contain("Tag eq 'Y'");

		// The OR group must be wrapped in parentheses
		decoded.Should().Contain("(Tag eq 'X' or Tag eq 'Y')");
	}

	#endregion

	#region Simple OR (No AND) Tests

	/// <summary>
	/// Tests that simple OR without AND works correctly.
	/// </summary>
	[Fact]
	public void Filter_WithSimpleOr_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<TestEntity>("TestEntities", NullLogger.Instance)
			.Filter(e => e.Status == "Active" || e.Status == "Pending")
			.BuildUrl();

		// Assert
		var decoded = Uri.UnescapeDataString(url);
		decoded.Should().Contain("Status eq 'Active'");
		decoded.Should().Contain("Status eq 'Pending'");
	}

	/// <summary>
	/// Tests that multiple ORs work correctly.
	/// </summary>
	[Fact]
	public void Filter_WithMultipleOrs_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<TestEntity>("TestEntities", NullLogger.Instance)
			.Filter(e => e.Status == "Active" || e.Status == "Pending" || e.Status == "Draft")
			.BuildUrl();

		// Assert
		var decoded = Uri.UnescapeDataString(url);
		decoded.Should().Contain("Status eq 'Active'");
		decoded.Should().Contain("Status eq 'Pending'");
		decoded.Should().Contain("Status eq 'Draft'");
		decoded.Should().Contain(" or ");
	}

	#endregion

	#region Complex Nested Boolean Tests

	/// <summary>
	/// Tests OR on the left side of AND.
	/// (A OR B) AND C should preserve parentheses around (A OR B).
	/// </summary>
	[Fact]
	public void Filter_WithOrOnLeftOfAnd_PreservesParentheses()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<TestEntity>("TestEntities", NullLogger.Instance)
			.Filter(e => (e.Status == "Active" || e.Status == "Pending") && e.IsActive)
			.BuildUrl();

		// Assert
		var decoded = Uri.UnescapeDataString(url);

		// The OR must be wrapped in parentheses
		decoded.Should().Contain("(Status eq 'Active' or Status eq 'Pending')");
		decoded.Should().Contain("IsActive");
	}

	/// <summary>
	/// Tests multiple nested boolean groups.
	/// (A OR B) AND (C OR D) should preserve both OR groups.
	/// </summary>
	[Fact]
	public void Filter_WithMultipleOrGroups_PreservesAllParentheses()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<TestEntity>("TestEntities", NullLogger.Instance)
			.Filter(e => (e.Status == "Active" || e.Status == "Pending") && (e.Category == "A" || e.Category == "B"))
			.BuildUrl();

		// Assert
		var decoded = Uri.UnescapeDataString(url);

		// Both OR groups must be wrapped in parentheses
		decoded.Should().Contain("(Status eq 'Active' or Status eq 'Pending')");
		decoded.Should().Contain("(Category eq 'A' or Category eq 'B')");
	}

	/// <summary>
	/// Tests deeply nested boolean expressions.
	/// A AND (B OR (C AND D)) should preserve correct structure.
	/// </summary>
	[Fact]
	public void Filter_WithDeeplyNestedBoolean_PreservesStructure()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<TestEntity>("TestEntities", NullLogger.Instance)
			.Filter(e => e.IsActive && (e.Priority > 5 || (e.Status == "Active" && e.Category == "A")))
			.BuildUrl();

		// Assert
		var decoded = Uri.UnescapeDataString(url);

		decoded.Should().Contain("IsActive");
		decoded.Should().Contain("Priority gt 5");
		decoded.Should().Contain("Status eq 'Active'");
		decoded.Should().Contain("Category eq 'A'");
	}

	#endregion

	#region Operator Precedence Edge Cases

	/// <summary>
	/// Tests that AND has higher precedence than OR in OData.
	/// With explicit parentheses (A OR B) AND C, the OR must be grouped.
	/// </summary>
	[Fact]
	public void Filter_WithExplicitParentheses_OverridesDefaultPrecedence()
	{
		// Without explicit parentheses, this would be: Active OR (Pending AND IsActive)
		// With explicit parentheses, this should be: (Active OR Pending) AND IsActive
		var url = new ODataQueryBuilder<TestEntity>("TestEntities", NullLogger.Instance)
			.Filter(e => (e.Status == "Active" || e.Status == "Pending") && e.IsActive)
			.BuildUrl();

		var decoded = Uri.UnescapeDataString(url);

		// The parentheses around the OR are essential for correct interpretation
		decoded.Should().Contain("(Status eq 'Active' or Status eq 'Pending')");
	}

	/// <summary>
	/// Tests string method combined with OR in AND context.
	/// </summary>
	[Fact]
	public void Filter_WithStringMethodsAndOrInAnd_PreservesParentheses()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<TestEntity>("TestEntities", NullLogger.Instance)
			.Filter(e => e.IsActive && (e.Name.StartsWith("Test") || e.Name.EndsWith("Demo")))
			.BuildUrl();

		// Assert
		var decoded = Uri.UnescapeDataString(url);

		decoded.Should().Contain("IsActive");
		decoded.Should().Contain("startswith(Name,'Test')");
		decoded.Should().Contain("endswith(Name,'Demo')");

		// OR group must be wrapped
		decoded.Should().Contain("(startswith(Name,'Test') or endswith(Name,'Demo'))");
	}

	/// <summary>
	/// Tests numeric comparisons with OR in AND context.
	/// </summary>
	[Fact]
	public void Filter_WithNumericComparisonsAndOrInAnd_PreservesParentheses()
	{
		// Arrange & Act
		var url = new ODataQueryBuilder<TestEntity>("TestEntities", NullLogger.Instance)
			.Filter(e => e.IsActive && (e.Priority < 3 || e.Priority > 7))
			.BuildUrl();

		// Assert
		var decoded = Uri.UnescapeDataString(url);

		decoded.Should().Contain("IsActive");
		decoded.Should().Contain("Priority lt 3");
		decoded.Should().Contain("Priority gt 7");

		// OR group must be wrapped
		decoded.Should().Contain("(Priority lt 3 or Priority gt 7)");
	}

	#endregion
}
