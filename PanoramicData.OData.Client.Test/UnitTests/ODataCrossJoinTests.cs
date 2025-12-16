#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Tests for cross-join query builder.
/// </summary>
public class ODataCrossJoinTests
{
	[Fact]
	public void CrossJoin_BasicQuery_ShouldBuildCorrectUrl()
	{
		// Arrange
		var logger = NullLogger.Instance;
		var builder = new ODataCrossJoinBuilder(["Products", "Categories"], logger);

		// Act
		var url = builder.BuildUrl();

		// Assert
		url.Should().Be("$crossjoin(Products,Categories)");
	}

	[Fact]
	public void CrossJoin_WithFilter_ShouldIncludeFilterParameter()
	{
		// Arrange
		var logger = NullLogger.Instance;
		var builder = new ODataCrossJoinBuilder(["Products", "Categories"], logger)
			.Filter("Products/CategoryId eq Categories/Id");

		// Act
		var url = builder.BuildUrl();

		// Assert
		url.Should().Contain("$crossjoin(Products,Categories)");
		url.Should().Contain("$filter=");
	}

	[Fact]
	public void CrossJoin_WithSelect_ShouldIncludeSelectParameter()
	{
		// Arrange
		var logger = NullLogger.Instance;
		var builder = new ODataCrossJoinBuilder(["Products", "Categories"], logger)
			.Select("Products/Name,Categories/Name");

		// Act
		var url = builder.BuildUrl();

		// Assert
		url.Should().Contain("$select=Products/Name,Categories/Name");
	}

	[Fact]
	public void CrossJoin_WithOrderBy_ShouldIncludeOrderByParameter()
	{
		// Arrange
		var logger = NullLogger.Instance;
		var builder = new ODataCrossJoinBuilder(["Products", "Categories"], logger)
			.OrderBy("Products/Name desc");

		// Act
		var url = builder.BuildUrl();

		// Assert
		url.Should().Contain("$orderby=Products/Name desc");
	}

	[Fact]
	public void CrossJoin_WithPaging_ShouldIncludeSkipAndTop()
	{
		// Arrange
		var logger = NullLogger.Instance;
		var builder = new ODataCrossJoinBuilder(["Products", "Categories"], logger)
			.Skip(10)
			.Top(20);

		// Act
		var url = builder.BuildUrl();

		// Assert
		url.Should().Contain("$skip=10");
		url.Should().Contain("$top=20");
	}

	[Fact]
	public void CrossJoin_WithCount_ShouldIncludeCountParameter()
	{
		// Arrange
		var logger = NullLogger.Instance;
		var builder = new ODataCrossJoinBuilder(["Products", "Categories"], logger)
			.Count();

		// Act
		var url = builder.BuildUrl();

		// Assert
		url.Should().Contain("$count=true");
	}

	[Fact]
	public void CrossJoin_MultipleEntitySets_ShouldIncludeAll()
	{
		// Arrange
		var logger = NullLogger.Instance;
		var builder = new ODataCrossJoinBuilder(["Products", "Categories", "Suppliers"], logger);

		// Act
		var url = builder.BuildUrl();

		// Assert
		url.Should().Be("$crossjoin(Products,Categories,Suppliers)");
	}

	[Fact]
	public async Task CrossJoin_LessThanTwoEntitySets_ShouldThrow()
	{
		// Arrange
		var logger = NullLogger.Instance;

		// Act & Assert
		var act = () => new ODataCrossJoinBuilder(["Products"], logger);
		act.Should().ThrowExactly<ArgumentException>();
	}

	[Fact]
	public void CrossJoin_AllOptions_ShouldBuildCompleteUrl()
	{
		// Arrange
		var logger = NullLogger.Instance;
		var builder = new ODataCrossJoinBuilder(["Products", "Categories"], logger)
			.Filter("Products/CategoryId eq Categories/Id")
			.Select("Products/Name,Categories/Name")
			.OrderBy("Products/Name")
			.Skip(10)
			.Top(20)
			.Count();

		// Act
		var url = builder.BuildUrl();

		// Assert
		url.Should().Contain("$crossjoin(Products,Categories)");
		url.Should().Contain("$filter=");
		url.Should().Contain("$select=");
		url.Should().Contain("$orderby=");
		url.Should().Contain("$skip=10");
		url.Should().Contain("$top=20");
		url.Should().Contain("$count=true");
	}
}
#pragma warning restore CS1591
