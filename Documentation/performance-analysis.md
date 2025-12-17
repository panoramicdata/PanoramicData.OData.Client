# Performance Analysis Guide

This document describes the performance analysis process for PanoramicData.OData.Client. Use this guide to identify hotspots, run benchmarks, and implement optimizations.

## Quick Start

```powershell
# Run performance measurement tests
dotnet test --filter "FullyQualifiedName~PerformanceMeasurementTests" -c Release

# Run all benchmarks (full BenchmarkDotNet suite)
.\Run-Benchmarks.ps1

# Run specific benchmarks
.\Run-Benchmarks.ps1 -Filter "*QueryBuilder*"
```

## Performance Analysis Process

### Phase 1: Identify Slow Tests

Find unit tests that take the longest to run:

```powershell
dotnet test --filter "FullyQualifiedName~UnitTests" -c Release `
    --logger "console;verbosity=detailed" 2>&1 | `
    Select-String -Pattern "Passed.*\[(\d+) ms\]" | `
    ForEach-Object { 
        $match = $_ -match '\[(\d+) ms\]'
        if ($matches) { 
            [PSCustomObject]@{Time=[int]$matches[1]; Test=$_.ToString()} 
        } 
    } | Sort-Object Time -Descending | Select-Object -First 20
```

> **Note**: Unit test times include test setup overhead (mocking, etc.), so focus on patterns rather than absolute values.

### Phase 2: Run Performance Measurement Tests

Run the built-in performance measurement tests:

```powershell
dotnet test --filter "FullyQualifiedName~PerformanceMeasurementTests" -c Release
```

These tests measure:
- Simple query construction (baseline)
- Raw filter vs expression filter
- Captured variable overhead
- Complex expression parsing
- Function call reflection overhead
- String method parsing
- OR precedence handling

### Phase 3: Run BenchmarkDotNet (Detailed)

The project includes pre-built benchmarks in `PanoramicData.OData.Client.Test/Benchmarks/`:

| Benchmark Class | What It Measures |
|-----------------|------------------|
| `QueryBuilderBenchmarks` | URL construction, expression parsing |
| `JsonSerializationBenchmarks` | JSON parsing, response deserialization |
| `ConcurrentRequestBenchmarks` | Concurrent request handling |

### Phase 4: Visual Studio Performance Profiler

For detailed hotspot analysis:

1. Open the solution in Visual Studio
2. **Debug ? Performance Profiler** (Alt+F2)
3. Select diagnostics:
   - ? **CPU Usage** - Find hot code paths
   - ? **.NET Object Allocation Tracking** - Find allocation hotspots
4. Click **Start** and run specific benchmarks or tests
5. Analyze the flame graph and hot path

## Current Performance Findings

### Analysis Date: December 2024

Based on performance analysis, the following patterns were identified:

### Identified Hotspots

| Area | Severity | Description |
|------|----------|-------------|
| Expression Compilation | Medium | `Expression.Compile()` is called for captured variables |
| Reflection in Functions | Medium | `GetProperties()` used for function parameter formatting |
| String Allocations | Low | Multiple string operations in URL building |

### 1. Expression Compilation (Medium Severity)

**Location**: `ODataQueryBuilder.ExpressionParsing.cs`

**Issue**: When using LINQ expressions with captured variables, the expression tree must be compiled to extract the value:

```csharp
// This requires expression compilation
var minPrice = 100m;
builder.Filter(p => p.Price > minPrice);

// This is faster - no compilation needed
builder.Filter("Price gt 100");
```

**Current Implementation**:
```csharp
private static object? EvaluateExpression(Expression expression)
{
    var objectMember = Expression.Convert(expression, typeof(object));
    var getterLambda = Expression.Lambda<Func<object>>(objectMember);
    var getter = getterLambda.Compile();  // EXPENSIVE
    return getter();
}
```

**Recommendation**: For performance-critical scenarios, use raw filter strings. Expression caching could be implemented but adds complexity.

### 2. Reflection in Function Calls (Medium Severity)

**Location**: `ODataQueryBuilder.ExpressionParsing.cs` - `FormatFunctionParameters`

**Issue**: Anonymous type parameter formatting uses reflection:

```csharp
// Uses reflection to enumerate properties
builder.Function("Search", new { Term = "widget", Max = 10 });
```

**Current Implementation**:
```csharp
private static string FormatFunctionParameters(object parameters)
{
    var props = parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
    // ...iterates and formats each property
}
```

**Recommendation**: This is acceptable for the flexibility it provides. For hot paths, consider using typed parameter objects.

### 3. JSON Serialization (Already Optimized ?)

**Location**: `ODataClient.cs`

**Status**: `JsonSerializerOptions` are created once and reused across all requests.

```csharp
private readonly JsonSerializerOptions _jsonOptions = new()
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    // ...
};
```

## Optimization Recommendations

### For Library Consumers

1. **Use raw filter strings for hot paths**:
   ```csharp
   // Faster (no expression parsing)
   builder.Filter("Price gt 100 and Rating ge 4");
   
   // Slower (expression tree walking + potential compilation)
   builder.Filter(p => p.Price > 100 && p.Rating >= 4);
   ```

2. **Reuse ODataClient instances** - HttpClient and JsonSerializerOptions are reused internally.

3. **Use pagination wisely** - `GetAllAsync` is convenient but may load large datasets into memory.

### Future Optimization Opportunities

1. **Expression Compilation Caching**
   - Cache compiled expressions using `ConditionalWeakTable`
   - Trade memory for CPU in repeated queries

2. **StringBuilder Pooling**
   - Use `ObjectPool<StringBuilder>` for URL construction
   - Minimal benefit expected (already fast)

3. **Source Generators**
   - Generate optimized serializers at compile time
   - Significant effort, modest gains

## Benchmark Interpretation

### Example Output

```
|                Method |        Mean | Allocated |
|---------------------- |------------:|----------:|
|           SimpleQuery |    45.23 ns |      72 B |
|       QueryWithFilter |   234.12 ns |     312 B |
| QueryWithExprFilter   | 2,456.78 ns |   1,024 B |
```

### What to Look For

| Metric | Good | Concerning | Critical |
|--------|------|------------|----------|
| Mean Time | < 100 ns | 100-1000 ns | > 1 ms |
| Allocations | < 100 B | 100-1000 B | > 10 KB |
| Gen0 Collections | 0 | 0.01-0.1 | > 0.1 |

## Running Full Analysis

To perform a complete performance analysis:

```powershell
# 1. Build in Release mode
dotnet build -c Release

# 2. Run performance measurement tests
dotnet test --filter "FullyQualifiedName~PerformanceMeasurementTests" -c Release

# 3. Find slow unit tests
dotnet test --filter "FullyQualifiedName~UnitTests" -c Release `
    --logger "console;verbosity=detailed" 2>&1 | `
    Tee-Object -FilePath test-timing.txt

# 4. Run full benchmarks (optional, takes longer)
.\Run-Benchmarks.ps1
```

## Adding New Benchmarks

When adding new functionality, consider adding benchmarks:

```csharp
[Benchmark]
public string NewFeatureBenchmark()
{
    var builder = new ODataQueryBuilder<Product>("Products", NullLogger.Instance)
        .NewFeature(...);
    return builder.BuildUrl();
}
```

## References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [.NET Performance Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/performance/)
- [High-Performance .NET](https://docs.microsoft.com/en-us/aspnet/core/performance/performance-best-practices)
