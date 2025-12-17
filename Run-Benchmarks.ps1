<#
.SYNOPSIS
    Runs performance benchmarks for PanoramicData.OData.Client.

.DESCRIPTION
    This script runs BenchmarkDotNet benchmarks in Release mode with profiling.
    Results are exported to the BenchmarkDotNet.Artifacts folder.

.PARAMETER Filter
    Filter expression for running specific benchmarks (e.g., "*QueryBuilder*")

.PARAMETER Quick
    Run quick benchmarks (fewer iterations for faster feedback)

.PARAMETER Profile
    Enable detailed CPU profiling (Windows only, requires admin)

.EXAMPLE
    .\Run-Benchmarks.ps1
    
.EXAMPLE
    .\Run-Benchmarks.ps1 -Filter "*QueryBuilder*"

.EXAMPLE
    .\Run-Benchmarks.ps1 -Quick
#>

param(
    [string]$Filter = "*",
    [switch]$Quick,
    [switch]$Profile
)

$ErrorActionPreference = 'Stop'

$solutionRoot = $PSScriptRoot
$testProject = Join-Path $solutionRoot "PanoramicData.OData.Client.Test\PanoramicData.OData.Client.Test.csproj"

Write-Host "========================================" -ForegroundColor Magenta
Write-Host " PanoramicData.OData.Client Benchmarks" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

# Build in Release mode
Write-Host "`n>> Building in Release mode..." -ForegroundColor Yellow
dotnet build $testProject --configuration Release --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n>> Running benchmarks..." -ForegroundColor Yellow

# Construct the benchmark command
$benchmarkArgs = @(
    "run",
    "--project", $testProject,
    "--configuration", "Release",
    "--no-build",
    "--",
    "--filter", $Filter
)

if ($Quick) {
    $benchmarkArgs += "--job", "short"
}

if ($Profile) {
    $benchmarkArgs += "--profiler", "ETW"
}

# Run benchmarks
& dotnet $benchmarkArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nBenchmark run failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host " Benchmarks completed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "`nResults are in: BenchmarkDotNet.Artifacts\" -ForegroundColor Cyan

exit 0
