using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace PanoramicData.OData.Client.Test.Benchmarks;

/// <summary>
/// Entry point for running benchmarks with profiling support.
/// Run this with: dotnet run -c Release --project PanoramicData.OData.Client.Test
/// </summary>
public static class BenchmarkProgram
{
	/// <summary>
	/// Runs all benchmarks with memory and allocation profiling.
	/// </summary>
	public static void RunAll()
	{
		var config = GetConfig();

		Console.WriteLine("Running QueryBuilder Benchmarks...");
		BenchmarkRunner.Run<QueryBuilderBenchmarks>(config);

		Console.WriteLine("Running JSON Serialization Benchmarks...");
		BenchmarkRunner.Run<JsonSerializationBenchmarks>(config);
	}

	/// <summary>
	/// Runs QueryBuilder benchmarks only.
	/// </summary>
	public static void RunQueryBuilderBenchmarks() => BenchmarkRunner.Run<QueryBuilderBenchmarks>(GetConfig());

	/// <summary>
	/// Runs JSON Serialization benchmarks only.
	/// </summary>
	public static void RunJsonBenchmarks() => BenchmarkRunner.Run<JsonSerializationBenchmarks>(GetConfig());

	/// <summary>
	/// Creates a configuration with memory diagnostics and profiling.
	/// </summary>
	private static ManualConfig GetConfig() => ManualConfig.Create(DefaultConfig.Instance)
			// Memory allocation tracking
			.AddDiagnoser(MemoryDiagnoser.Default)
			// Thread contention diagnostics (useful for async code)
			.AddDiagnoser(ThreadingDiagnoser.Default)
			// Export results in multiple formats
			.AddExporter(MarkdownExporter.GitHub)
			.AddExporter(HtmlExporter.Default)
			// Use short runs for quick feedback during development
			.AddJob(Job.ShortRun
				.WithWarmupCount(3)
				.WithIterationCount(5));

	/// <summary>
	/// Creates a detailed profiling configuration with ETW events (Windows only).
	/// </summary>
	public static ManualConfig GetDetailedProfilingConfig()
	{
		var config = ManualConfig.Create(DefaultConfig.Instance)
			.AddDiagnoser(MemoryDiagnoser.Default)
			.AddDiagnoser(ThreadingDiagnoser.Default)
			.AddExporter(MarkdownExporter.GitHub);

		// Add ETW profiler for Windows (shows CPU hotspots)
		if (OperatingSystem.IsWindows())
		{
			config.AddDiagnoser(new EventPipeProfiler(
				EventPipeProfile.CpuSampling));
		}

		return config;
	}
}
