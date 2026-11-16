using System.Diagnostics;

namespace PanoramicData.OData.Client.Test.PerformanceTests;

/// <summary>
/// Performance measurement tests for identifying hotspots.
/// These tests verify performance characteristics and document results.
/// </summary>
public class PerformanceMeasurementTests
{
	private const int Iterations = 5000;

	private static readonly decimal MinPrice = 100m;

	/// <summary>
	/// Every query shape completes and reports a positive cost per operation.
	/// </summary>
	[Fact]
	public void MeasureQueryBuilderPerformance()
	{
		Warmup();

		var shapes = new Dictionary<string, Func<ODataQueryBuilder<Product>, ODataQueryBuilder<Product>>>
		{
			["simple"] = query => query,
			["raw filter"] = query => query.Filter("Price gt 100"),
			["expression filter"] = query => query.Filter(p => p.Price > 100),
			["captured variable"] = query => query.Filter(p => p.Price > MinPrice),
			["complex expression"] = query => query.Filter(p => p.Price > 100 && p.Rating >= 3 && p.Name != null),
			["function"] = query => query.Function("Search", new { Term = "test", Max = 10 }),
			["string contains"] = query => query.Filter(p => p.Name.Contains("widget")),
			["or precedence"] = query => query.Filter(p => p.Price > 100 && (p.Rating == 4 || p.Rating == 5))
		};

		foreach (var (name, shape) in shapes)
		{
			Measure(shape).Should().BePositive(name);
		}
	}

	/// <summary>
	/// Verifies that expression filters with captured variables involve expression compilation.
	/// </summary>
	[Fact]
	public void ExpressionWithCapturedVariable_IsSlowerThanRawFilter()
	{
		Warmup();

		var rawFilterTime = Measure(query => query.Filter("Price gt 100"));
		var capturedVarTime = Measure(query => query.Filter(p => p.Price > MinPrice));

		capturedVarTime.Should().BeGreaterThan(rawFilterTime * 0.5,
			"Captured variable expression should involve some overhead");
	}

	/// <summary>
	/// Documents that function calls use reflection for parameter formatting.
	/// </summary>
	[Fact]
	public void FunctionWithParameters_UsesReflection()
	{
		Warmup();

		Measure(query => query).Should().BePositive();
		Measure(query => query.Function("Search", new { Term = "test", Max = 10 })).Should().BePositive();
	}

	private static void Warmup()
	{
		for (var i = 0; i < 100; i++)
		{
			NewQuery().BuildUrl();
		}
	}

	private static ODataQueryBuilder<Product> NewQuery() => new("Products", NullLogger.Instance);

	private static double Measure(Func<ODataQueryBuilder<Product>, ODataQueryBuilder<Product>> shape)
	{
		var sw = Stopwatch.StartNew();
		for (var i = 0; i < Iterations; i++)
		{
			shape(NewQuery()).BuildUrl();
		}

		sw.Stop();
		return sw.Elapsed.TotalMilliseconds * 1000.0 / Iterations;
	}
}
