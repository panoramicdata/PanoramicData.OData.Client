using System.Diagnostics;

namespace PanoramicData.OData.Client.Test.Benchmarks;

/// <summary>
/// Simple performance tests that can be run to quickly identify hotspots.
/// Use this for quick feedback before running full BenchmarkDotNet suite.
/// </summary>
public static class QuickPerformanceTest
{
	/// <summary>
	/// Runs quick performance tests and prints results.
	/// </summary>
	public static void Run(int iterations = 10000)
	{
		Console.WriteLine($"Running {iterations} iterations per test...\n");

		// Warmup
		for (var i = 0; i < 100; i++)
		{
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance).BuildUrl();
		}

		var results = new List<(string Name, double MicrosecondsPerOp)>();

		// Test 1: Simple query (no expression)
		var sw = Stopwatch.StartNew();
		for (var i = 0; i < iterations; i++)
		{
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance).BuildUrl();
		}

		sw.Stop();
		results.Add(("SimpleQuery", sw.ElapsedMilliseconds * 1000.0 / iterations));

		// Test 2: Raw filter (no expression)
		sw.Restart();
		for (var i = 0; i < iterations; i++)
		{
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance).Filter("Price gt 100").BuildUrl();
		}

		sw.Stop();
		results.Add(("RawFilter", sw.ElapsedMilliseconds * 1000.0 / iterations));

		// Test 3: Expression filter (requires compilation)
		sw.Restart();
		for (var i = 0; i < iterations; i++)
		{
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance).Filter(p => p.Price > 100).BuildUrl();
		}

		sw.Stop();
		results.Add(("ExpressionFilter", sw.ElapsedMilliseconds * 1000.0 / iterations));

		// Test 4: Expression with captured variable
		var minPrice = 100m;
		sw.Restart();
		for (var i = 0; i < iterations; i++)
		{
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance).Filter(p => p.Price > minPrice).BuildUrl();
		}

		sw.Stop();
		results.Add(("CapturedVariable", sw.ElapsedMilliseconds * 1000.0 / iterations));

		// Test 5: Expression with Contains (IN clause)
		var ids = new[] { 1, 2, 3, 4, 5 };
		sw.Restart();
		for (var i = 0; i < iterations; i++)
		{
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance).Filter(p => ids.Contains(p.Id)).BuildUrl();
		}

		sw.Stop();
		results.Add(("ContainsExpression", sw.ElapsedMilliseconds * 1000.0 / iterations));

		// Test 6: Complex expression
		sw.Restart();
		for (var i = 0; i < iterations; i++)
		{
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance).Filter(p => p.Price > 100 && p.Rating >= 3 && p.Name != null).BuildUrl();
		}

		sw.Stop();
		results.Add(("ComplexExpression", sw.ElapsedMilliseconds * 1000.0 / iterations));

		// Test 7: Function with reflection
		sw.Restart();
		for (var i = 0; i < iterations; i++)
		{
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance).Function("Search", new { Term = "test", Max = 10 }).BuildUrl();
		}

		sw.Stop();
		results.Add(("FunctionWithReflection", sw.ElapsedMilliseconds * 1000.0 / iterations));

		// Test 8: OR precedence expression
		sw.Restart();
		for (var i = 0; i < iterations; i++)
		{
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
				.Filter(p => p.Price > 100 && (p.Rating == 4 || p.Rating == 5)).BuildUrl();
		}

		sw.Stop();
		results.Add(("OrPrecedenceExpr", sw.ElapsedMilliseconds * 1000.0 / iterations));

		// Print results
		Console.WriteLine("Performance Results:");
		Console.WriteLine("".PadRight(60, '-'));
		Console.WriteLine($"{"Test",-25} {"µs/op",-10} {"Relative",-10}");
		Console.WriteLine("".PadRight(60, '-'));

		var baseline = results[0].MicrosecondsPerOp;
		foreach (var (name, microseconds) in results.OrderBy(r => r.MicrosecondsPerOp))
		{
			var relative = microseconds / baseline;
			Console.WriteLine($"{name,-25} {microseconds,8:F2} {relative,8:F1}x");
		}

		Console.WriteLine("".PadRight(60, '-'));
		Console.WriteLine("\nHotspot Analysis:");

		// Find hotspots (>10x baseline)
		var hotspots = results.Where(r => r.MicrosecondsPerOp / baseline > 10).ToList();
		if (hotspots.Count > 0)
		{
			Console.WriteLine("??  These operations are significantly slower than baseline:");
			foreach (var (name, microseconds) in hotspots.OrderByDescending(r => r.MicrosecondsPerOp))
			{
				Console.WriteLine($"   - {name}: {microseconds / baseline:F1}x slower");
			}
		}
		else
		{
			Console.WriteLine("? No significant hotspots detected (all < 10x baseline)");
		}
	}
}
