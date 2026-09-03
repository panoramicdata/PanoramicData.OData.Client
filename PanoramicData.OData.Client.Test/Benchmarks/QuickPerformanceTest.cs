using System.Diagnostics;

namespace PanoramicData.OData.Client.Test.Benchmarks;

/// <summary>
/// Simple performance tests that can be run to quickly identify hotspots.
/// Use this for quick feedback before running full BenchmarkDotNet suite.
/// </summary>
public static class QuickPerformanceTest
{
	/// <summary>
	/// A scenario is significant when it costs this many times the simple-query baseline.
	/// </summary>
	private const double HotspotThreshold = 10;

	private const decimal MinPrice = 100m;

	private static readonly int[] Ids = [1, 2, 3, 4, 5];

	/// <summary>
	/// The query shapes measured, in the order they are run. The first is the baseline every
	/// other result is reported relative to.
	/// </summary>
	private static readonly (string Name, Action Operation)[] Scenarios =
	[
		("SimpleQuery", static () =>
			Builder().BuildUrl()),

		("RawFilter", static () =>
			Builder().Filter("Price gt 100").BuildUrl()),

		("ExpressionFilter", static () =>
			Builder().Filter(p => p.Price > 100).BuildUrl()),

		("CapturedVariable", static () =>
			Builder().Filter(p => p.Price > MinPrice).BuildUrl()),

		("ContainsExpression", static () =>
			Builder().Filter(p => Ids.Contains(p.Id)).BuildUrl()),

		("ComplexExpression", static () =>
			Builder().Filter(p => p.Price > 100 && p.Rating >= 3 && p.Name != null).BuildUrl()),

		("FunctionWithReflection", static () =>
			Builder().Function("Search", new { Term = "test", Max = 10 }).BuildUrl()),

		("OrPrecedenceExpr", static () =>
			Builder().Filter(p => p.Price > 100 && (p.Rating == 4 || p.Rating == 5)).BuildUrl()),
	];

	/// <summary>
	/// Runs quick performance tests and prints results.
	/// </summary>
	public static void Run(int iterations = 10000)
	{
		Console.WriteLine($"Running {iterations} iterations per test...\n");

		Warmup();

		var results = Scenarios
			.Select(scenario => (scenario.Name, MicrosecondsPerOp: Measure(scenario.Operation, iterations)))
			.ToList();

		PrintResults(results);
		PrintHotspots(results);
	}

	private static ODataQueryBuilder<Product> Builder()
		=> new("Products", NullLogger.Instance);

	private static void Warmup()
	{
		for (var i = 0; i < 100; i++)
		{
			Builder().BuildUrl();
		}
	}

	private static double Measure(Action operation, int iterations)
	{
		var stopwatch = Stopwatch.StartNew();
		for (var i = 0; i < iterations; i++)
		{
			operation();
		}

		stopwatch.Stop();

		return stopwatch.ElapsedMilliseconds * 1000.0 / iterations;
	}

	private static void PrintResults(List<(string Name, double MicrosecondsPerOp)> results)
	{
		var baseline = results[0].MicrosecondsPerOp;

		Console.WriteLine("Performance Results:");
		Console.WriteLine("".PadRight(60, '-'));
		Console.WriteLine($"{"Test",-25} {"us/op",-10} {"Relative",-10}");
		Console.WriteLine("".PadRight(60, '-'));

		foreach (var (name, microseconds) in results.OrderBy(r => r.MicrosecondsPerOp))
		{
			Console.WriteLine($"{name,-25} {microseconds,8:F2} {microseconds / baseline,8:F1}x");
		}

		Console.WriteLine("".PadRight(60, '-'));
	}

	private static void PrintHotspots(List<(string Name, double MicrosecondsPerOp)> results)
	{
		var baseline = results[0].MicrosecondsPerOp;
		var hotspots = results
			.Where(r => r.MicrosecondsPerOp / baseline > HotspotThreshold)
			.OrderByDescending(r => r.MicrosecondsPerOp)
			.ToList();

		Console.WriteLine("\nHotspot Analysis:");

		if (hotspots.Count == 0)
		{
			Console.WriteLine($"OK: no significant hotspots detected (all < {HotspotThreshold:F0}x baseline)");
			return;
		}

		Console.WriteLine("WARNING: these operations are significantly slower than baseline:");
		foreach (var (name, microseconds) in hotspots)
		{
			Console.WriteLine($"   - {name}: {microseconds / baseline:F1}x slower");
		}
	}
}
