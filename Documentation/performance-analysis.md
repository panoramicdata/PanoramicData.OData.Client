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

## Performance Optimizations Implemented

The following optimizations have been implemented in the codebase:

### 1. Reflection-Based Expression Evaluation (Major)

**Location**: `ODataQueryBuilder.ExpressionParsing.cs` - `TryEvaluateWithReflection`

**Before**: Every captured variable required `Expression.Compile()` which is expensive.

**After**: Simple member access chains are evaluated using reflection, avoiding compilation overhead.

```csharp
// This is now significantly faster:
var minPrice = 100m;
builder.Filter(p => p.Price > minPrice);
```

**Impact**: ~10-50x faster for captured variable expressions.

### 2. FrozenDictionary for Operator Mappings

**Location**: `ODataQueryBuilder.ExpressionParsing.cs` - `OperatorMap`

Uses `FrozenDictionary<ExpressionType, string>` for O(1) operator lookups with optimal memory layout.

### 3. Stack-Based Member Path Building

**Location**: `ODataQueryBuilder.ExpressionParsing.cs` - `GetMemberPath`

**Before**: `List<string>.Insert(0, ...)` which is O(n) per insertion.

**After**: Uses `Stack<string>` for O(1) push operations.

### 4. PropertyInfo Caching

**Location**: `ODataQueryBuilder.ExpressionParsing.cs` - `PropertyCache`

Uses `ConditionalWeakTable<Type, PropertyInfo[]>` to cache PropertyInfo arrays for anonymous types, avoiding repeated reflection in `FormatFunctionParameters`.

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

## Recommendations for Consumers

### Use Raw Filters for Hot Paths

```csharp
// Faster (no expression parsing)
builder.Filter("Price gt 100 and Rating ge 4");

// Slower (expression tree walking)
builder.Filter(p => p.Price > 100 && p.Rating >= 4);
```

### Reuse ODataClient Instances

HttpClient and JsonSerializerOptions are reused internally.

### Pre-allocate Collections

```csharp
// Pre-allocate instead of creating in lambda
var ids = new[] { 1, 2, 3, 4, 5 };
builder.Filter(p => ids.Contains(p.Id));
```

## Current Performance Characteristics

| Operation | Relative Speed | Notes |
|-----------|----------------|-------|
| Simple query | 1x (baseline) | Just entity set name |
| Raw filter | ~2x | String parsing only |
| Expression filter | ~5-10x | Tree walking |
| Captured variable | ~5-15x | Reflection-based (was ~50x with compilation) |
| Function call | ~10-20x | Reflection for parameters |

## Future Optimization Opportunities

1. **Source Generators** - Generate optimized serializers at compile time
2. **ReadOnlySpan<char>** - Use spans for string building in hot paths
3. **Expression Caching** - Cache compiled expressions for repeated patterns

## References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [.NET Performance Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/performance/)
- [High-Performance .NET](https://docs.microsoft.com/en-us/aspnet/core/performance/performance-best-practices)
