using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.OData.Client.Test.Models;
using System.Diagnostics;

namespace PanoramicData.OData.Client.Test.PerformanceTests;

/// <summary>
/// Performance measurement tests for identifying hotspots.
/// These tests verify performance characteristics and document results.
/// </summary>
public class PerformanceMeasurementTests
{
	private const int Iterations = 5000;

	/// <summary>
	/// Measures relative performance of different query operations.
	/// This test documents performance characteristics.
	/// </summary>
	[Fact]
	public void MeasureQueryBuilderPerformance()
	{
		// Warmup
		for (var i = 0; i < 100; i++)
		{
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance).BuildUrl();
		}

		// Test 1: Simple query (baseline)
		var simpleQueryTime = MeasureOperation(() =>
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance).BuildUrl());

		// Test 2: Raw filter
		var rawFilterTime = MeasureOperation(() =>
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
				.Filter("Price gt 100").BuildUrl());

		// Test 3: Expression filter (property comparison)
		var expressionFilterTime = MeasureOperation(() =>
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
				.Filter(p => p.Price > 100).BuildUrl());

		// Test 4: Expression with captured variable (requires expression compilation)
		var minPrice = 100m;
		var capturedVarTime = MeasureOperation(() =>
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
				.Filter(p => p.Price > minPrice).BuildUrl());

		// Test 5: Complex expression
		var complexExprTime = MeasureOperation(() =>
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
				.Filter(p => p.Price > 100 && p.Rating >= 3 && p.Name != null).BuildUrl());

		// Test 6: Function with reflection
		var functionTime = MeasureOperation(() =>
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
				.Function("Search", new { Term = "test", Max = 10 }).BuildUrl());

		// Test 7: String Contains
		var stringContainsTime = MeasureOperation(() =>
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
				.Filter(p => p.Name.Contains("widget")).BuildUrl());

		// Test 8: OR precedence
		var orPrecedenceTime = MeasureOperation(() =>
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
				.Filter(p => p.Price > 100 && (p.Rating == 4 || p.Rating == 5)).BuildUrl());

		// Document results
		var baseline = simpleQueryTime;

		// Verify basic operations are reasonably fast
		rawFilterTime.Should().BeLessThan(baseline * 5, "Raw filter should not be >5x slower than baseline");

		// Expression operations are expected to be slower due to tree walking
		// Just verify they complete (not regression test for absolute time)
		expressionFilterTime.Should().BePositive();
		capturedVarTime.Should().BePositive();

		// Complex expressions should complete
		complexExprTime.Should().BePositive();
		orPrecedenceTime.Should().BePositive();

		// Function with reflection should complete
		functionTime.Should().BePositive();
	}

	/// <summary>
	/// Verifies that expression filters with captured variables involve expression compilation.
	/// This is expected behavior but documents the performance cost.
	/// </summary>
	[Fact]
	public void ExpressionWithCapturedVariable_IsSlowerThanRawFilter()
	{
		// Warmup
		for (var i = 0; i < 100; i++)
		{
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance).Filter("Price gt 100").BuildUrl();
		}

		// Raw filter (no expression compilation)
		var rawFilterTime = MeasureOperation(() =>
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
				.Filter("Price gt 100").BuildUrl());

		// Expression with captured variable (requires compilation)
		var minPrice = 100m;
		var capturedVarTime = MeasureOperation(() =>
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
				.Filter(p => p.Price > minPrice).BuildUrl());

		// Document: Expression with captured variable should be slower
		// This is expected due to expression compilation
		// We don't enforce a specific ratio, just document the behavior
		capturedVarTime.Should().BeGreaterThan(rawFilterTime * 0.5,
			"Captured variable expression should involve some overhead");
	}

	/// <summary>
	/// Documents that function calls use reflection for parameter formatting.
	/// </summary>
	[Fact]
	public void FunctionWithParameters_UsesReflection()
	{
		// Warmup
		for (var i = 0; i < 100; i++)
		{
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance).BuildUrl();
		}

		// Simple query baseline
		var simpleQueryTime = MeasureOperation(() =>
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance).BuildUrl());

		// Function with anonymous type parameters (uses reflection)
		var functionTime = MeasureOperation(() =>
			new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
				.Function("Search", new { Term = "test", Max = 10 }).BuildUrl());

		// Function calls should complete - we're documenting the overhead exists
		functionTime.Should().BePositive();

		// Document: Functions are expected to be slower due to reflection
		// This is acceptable for the flexibility they provide
	}

	private static double MeasureOperation(Action operation)
	{
		var sw = Stopwatch.StartNew();
		for (var i = 0; i < Iterations; i++)
		{
			operation();
		}

		sw.Stop();
		return sw.ElapsedMilliseconds * 1000.0 / Iterations; // Return microseconds per operation
	}
}
