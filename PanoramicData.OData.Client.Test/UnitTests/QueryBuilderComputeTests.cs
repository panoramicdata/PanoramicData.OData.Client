namespace PanoramicData.OData.Client.Test.UnitTests;

/// <summary>
/// Unit tests for $compute query option support.
/// </summary>
public class QueryBuilderComputeTests
{
	private readonly ODataQueryBuilder<Product> _queryBuilder;

	/// <summary>
	/// Initializes a new instance of the test class.
	/// </summary>
	public QueryBuilderComputeTests()
	{
		_queryBuilder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance);
	}

	/// <summary>
	/// Tests that Compute generates correct URL with simple expression.
	/// </summary>
	[Fact]
	public void Compute_SimpleExpression_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Compute("Price mul Quantity as Total")
			.BuildUrl();

		// Assert
		url.Should().Contain("$compute=Price%20mul%20Quantity%20as%20Total");
	}

	/// <summary>
	/// Tests that Compute can be combined with Select.
	/// </summary>
	[Fact]
	public void Compute_WithSelect_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Select("Name,Price,Quantity")
			.Compute("Price mul Quantity as Total")
			.BuildUrl();

		// Assert
		url.Should().Contain("$select=Name,Price,Quantity");
		url.Should().Contain("$compute=Price%20mul%20Quantity%20as%20Total");
	}

	/// <summary>
	/// Tests that multiple Compute expressions are combined.
	/// </summary>
	[Fact]
	public void Compute_MultipleExpressions_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Compute("Price mul Quantity as Total")
			.Compute("Price div 100 as PriceDollars")
			.BuildUrl();

		// Assert
		url.Should().Contain("$compute=Price%20mul%20Quantity%20as%20Total%2CPrice%20div%20100%20as%20PriceDollars");
	}

	/// <summary>
	/// Tests that Compute can be combined with Filter.
	/// </summary>
	[Fact]
	public void Compute_WithFilter_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Compute("Price mul Quantity as Total")
			.Filter("Total gt 100")
			.BuildUrl();

		// Assert
		url.Should().Contain("$compute=");
		url.Should().Contain("$filter=");
	}

	/// <summary>
	/// Tests that Compute can use arithmetic operators.
	/// </summary>
	[Fact]
	public void Compute_ArithmeticExpression_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Compute("(Price sub Discount) mul Quantity as NetTotal")
			.BuildUrl();

		// Assert
		url.Should().Contain("$compute=%28Price%20sub%20Discount%29%20mul%20Quantity%20as%20NetTotal");
	}

	/// <summary>
	/// Tests that Compute with string functions works.
	/// </summary>
	[Fact]
	public void Compute_StringFunction_GeneratesCorrectUrl()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Compute("concat(Name, ' - ', Category) as FullName")
			.BuildUrl();

		// Assert
		url.Should().Contain("$compute=concat%28Name%2C%20%27%20-%20%27%2C%20Category%29%20as%20FullName");
	}

	/// <summary>
	/// Tests that empty Compute is ignored.
	/// </summary>
	[Fact]
	public void Compute_EmptyString_IsIgnored()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Compute("")
			.BuildUrl();

		// Assert
		url.Should().NotContain("$compute");
	}

	/// <summary>
	/// Tests that null Compute is ignored.
	/// </summary>
	[Fact]
	public void Compute_Null_IsIgnored()
	{
		// Arrange & Act
		var url = _queryBuilder
			.Compute(null!)
			.BuildUrl();

		// Assert
		url.Should().NotContain("$compute");
	}
}
