<#
.SYNOPSIS
    Runs performance benchmarks.

.DESCRIPTION
    This script runs BenchmarkDotNet benchmarks in Release mode.
    
    To run benchmarks, use BenchmarkDotNet's console runner or create a separate
    benchmark project. This script provides instructions for running benchmarks.

.PARAMETER Filter
    Filter expression for running specific benchmarks (e.g., "*QueryBuilder*")

.EXAMPLE
    .\Run-Benchmarks.ps1
    
.EXAMPLE
    .\Run-Benchmarks.ps1 -Filter "*QueryBuilder*"
#>

param(
    [string]$Filter = "*"
)

$ErrorActionPreference = 'Stop'

$solutionRoot = $PSScriptRoot
$testProject = Join-Path $solutionRoot "PanoramicData.OData.Client.Test\PanoramicData.OData.Client.Test.csproj"

Write-Host "========================================" -ForegroundColor Magenta
Write-Host " Performance Benchmark Instructions" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

Write-Host @"

To run benchmarks, you have several options:

1. Using BenchmarkDotNet programmatically (recommended):
   
   Create a simple console app or use LINQPad with:
   
   ```csharp
   using BenchmarkDotNet.Running;
   using PanoramicData.OData.Client.Test.Benchmarks;
   
   BenchmarkRunner.Run<QueryBuilderBenchmarks>();
   BenchmarkRunner.Run<JsonSerializationBenchmarks>();
   BenchmarkRunner.Run<ConcurrentRequestBenchmarks>();
   ```

2. Using dotnet CLI with BenchmarkDotNet.Tool:
   
   dotnet tool install --global BenchmarkDotNet.Tool
   dotnet benchmark $testProject --filter $Filter

3. The benchmark classes are:
   - QueryBuilderBenchmarks: Tests query URL construction performance
   - JsonSerializationBenchmarks: Tests JSON parsing performance
   - ConcurrentRequestBenchmarks: Tests concurrent request handling

Note: Benchmarks require Release mode compilation for accurate results.

"@ -ForegroundColor Cyan

# Build in Release mode
Write-Host "`n>> Building in Release mode..." -ForegroundColor Yellow
dotnet build $testProject --configuration Release --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n>> Build successful! Benchmarks ready to run." -ForegroundColor Green

exit 0
