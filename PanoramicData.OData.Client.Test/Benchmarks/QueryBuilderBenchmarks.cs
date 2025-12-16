using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.OData.Client.Test.Models;
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
	/// </summary>
	[Benchmark]
	public string QueryWithExpressionFilter()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Price > 100);
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
	/// </summary>
	[Benchmark]
	public string ExpressionFilterComplex()
	{
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => p.Price > 100 && p.Rating >= 3 && p.Name != null);
		return builder.BuildUrl();
	}

	/// <summary>
	/// Benchmark for collection Contains (IN clause).
	/// </summary>
	[Benchmark]
	public string ExpressionFilterWithIn()
	{
		var ids = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
		var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
			.Filter(p => ids.Contains(p.Id));
		return builder.BuildUrl();
	}
}
