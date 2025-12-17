using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System.Diagnostics.CodeAnalysis;

namespace PanoramicData.OData.Client.Test.Benchmarks;

/// <summary>
/// Performance benchmarks for ODataQueryBuilder URL construction.
/// Measures the speed of query building operations.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods")]
public class QueryBuilderBenchmarks
{
	// Pre-allocated array for IN clause benchmark
	private static readonly int[] _preAllocatedIds = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

	/// <summary>
	/// Benchmark for simple query with no options.
	/// </summary>
	[Benchmark(Baseline = true)]
	public string SimpleQuery()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance);
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for query with single filter.
	/// </summary>
	[Benchmark]
	public string QueryWithFilter()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("Price gt 100");
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for query with expression filter.
	/// This shows the cost of expression compilation.
	/// </summary>
	[Benchmark]
	public string QueryWithExpressionFilter()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Price > 100);
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark comparing expression with captured variable vs constant.
	/// Captured variables require expression compilation.
	/// </summary>
	[Benchmark]
	public string ExpressionWithCapturedVariable()
	{
		var minPrice = 100m;
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Price > minPrice);
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for query with multiple filters.
	/// </summary>
	[Benchmark]
	public string QueryWithMultipleFilters()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("Price gt 100")
			.Filter("Rating ge 3")
			.Filter("Name ne null");
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for query with select.
	/// </summary>
	[Benchmark]
	public string QueryWithSelect()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Select("ID,Name,Price,Rating");
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for query with orderby.
	/// </summary>
	[Benchmark]
	public string QueryWithOrderBy()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.OrderBy("Price desc")
			.OrderBy("Name");
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for query with paging.
	/// </summary>
	[Benchmark]
	public string QueryWithPaging()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Skip(100)
			.Top(25)
			.Count();
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for complex query with all options.
	/// </summary>
	[Benchmark]
	public string ComplexQuery()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter("Price gt 100")
			.Filter("Rating ge 3")
			.Select("ID,Name,Price,Rating")
			.Expand("Category")
			.OrderBy("Price desc")
			.OrderBy("Name")
			.Skip(20)
			.Top(10)
			.Count()
			.WithHeader("Prefer", "return=representation");
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for query with key.
	/// </summary>
	[Benchmark]
	public string QueryWithKey()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Key(12345);
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for query with string key.
	/// </summary>
	[Benchmark]
	public string QueryWithStringKey()
	{
		var builder = new ODataQueryBuilder<Person>("People", NullLogger.Instance)
			.Key("russellwhyte");
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for query with function call.
	/// This tests reflection-based parameter formatting.
	/// </summary>
	[Benchmark]
	public string QueryWithFunction()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Function("SearchProducts", new { SearchTerm = "widget", MaxResults = 10 });
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for query with $apply.
	/// </summary>
	[Benchmark]
	public string QueryWithApply()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Apply("groupby((Name), aggregate(Price with sum as TotalPrice))");
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for expression filter with string contains.
	/// Tests string method parsing.
	/// </summary>
	[Benchmark]
	public string ExpressionFilterWithContains()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Name.Contains("widget"));
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for expression filter with complex condition.
	/// Tests multiple binary expressions.
	/// </summary>
	[Benchmark]
	public string ExpressionFilterComplex()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Price > 100 && p.Rating >= 3 && p.Name != null);
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for collection Contains (IN clause) with pre-allocated array.
	/// Tests expression compilation with collection.
	/// </summary>
	[Benchmark]
	public string ExpressionFilterWithInPreAllocated()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => _preAllocatedIds.Contains(p.Id));
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for collection Contains (IN clause) with new array each time.
	/// Shows overhead of array allocation + expression compilation.
	/// </summary>
	[Benchmark]
	public string ExpressionFilterWithInNewArray()
	{
		var ids = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => ids.Contains(p.Id));
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for OR precedence expression (tests new parentheses logic).
	/// </summary>
	[Benchmark]
	public string ExpressionFilterWithOrPrecedence()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Price > 100 && (p.Rating == 4 || p.Rating == 5));
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for expression with Select.
	/// Tests member expression parsing.
	/// </summary>
	[Benchmark]
	public string ExpressionSelect()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Select(p => new { p.Id, p.Name, p.Price });
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for expression with OrderBy.
	/// </summary>
	[Benchmark]
	public string ExpressionOrderBy()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.OrderBy(p => p.Price, descending: true)
			.OrderBy(p => p.Name);
		return builder.BuildUrl();
	}
}
