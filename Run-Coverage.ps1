<#
.SYNOPSIS
    Runs tests with code coverage and generates a report.

.DESCRIPTION
    This script runs all unit tests with code coverage enabled,
    generates a coverage report, and optionally opens the HTML report.

.PARAMETER OpenReport
    Opens the HTML coverage report in the default browser after generation.

.PARAMETER Filter
    Filter expression for running specific tests (e.g., "FullyQualifiedName~UnitTests")

.EXAMPLE
    .\Run-Coverage.ps1
    
.EXAMPLE
    .\Run-Coverage.ps1 -OpenReport
    
.EXAMPLE
    .\Run-Coverage.ps1 -Filter "FullyQualifiedName~QueryBuilder"
#>

param(
    [switch]$OpenReport,
    [string]$Filter
)

$ErrorActionPreference = 'Stop'

$solutionRoot = $PSScriptRoot
$testProject = Join-Path $solutionRoot "PanoramicData.OData.Client.Test\PanoramicData.OData.Client.Test.csproj"
$coverageDir = Join-Path $solutionRoot "coverage"
$reportDir = Join-Path $coverageDir "report"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Code Coverage Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Clean previous coverage results
if (Test-Path $coverageDir) {
    Write-Host "`n>> Cleaning previous coverage results..." -ForegroundColor Yellow
    Remove-Item $coverageDir -Recurse -Force
}
New-Item -ItemType Directory -Path $coverageDir -Force | Out-Null

# Build test command
$testArgs = @(
    "test",
    $testProject,
    "--configuration", "Release",
    "--collect:""XPlat Code Coverage""",
    "--results-directory", $coverageDir,
    "--settings", (Join-Path $solutionRoot "PanoramicData.OData.Client.Test\coverlet.runsettings.json"),
    "-p:CollectCoverage=true",
    "-p:CoverletOutputFormat=cobertura",
    "-p:CoverletOutput=$coverageDir/"
)

if ($Filter) {
    $testArgs += "--filter"
    $testArgs += $Filter
}

# Run tests with coverage
Write-Host "`n>> Running tests with coverage..." -ForegroundColor Yellow
& dotnet @testArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed!" -ForegroundColor Red
    exit 1
}

# Find the coverage file
$coverageFile = Get-ChildItem -Path $coverageDir -Filter "coverage.cobertura.xml" -Recurse | Select-Object -First 1

if (-not $coverageFile) {
    # Try the default location from collect
    $coverageFile = Get-ChildItem -Path $coverageDir -Filter "*.xml" -Recurse | Where-Object { $_.Name -like "*coverage*" } | Select-Object -First 1
}

if (-not $coverageFile) {
    Write-Host "No coverage file found!" -ForegroundColor Red
    exit 1
}

Write-Host "`n>> Coverage file: $($coverageFile.FullName)" -ForegroundColor Green

# Check if reportgenerator is installed
$reportGenerator = Get-Command reportgenerator -ErrorAction SilentlyContinue
if (-not $reportGenerator) {
    Write-Host "`n>> Installing ReportGenerator..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-reportgenerator-globaltool
}

# Generate HTML report
Write-Host "`n>> Generating HTML report..." -ForegroundColor Yellow
reportgenerator `
    -reports:$($coverageFile.FullName) `
    -targetdir:$reportDir `
    -reporttypes:"Html;Badges;MarkdownSummary" `
    -title:"PanoramicData.OData.Client Coverage"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Report generation failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host " Coverage report generated!" -ForegroundColor Green
Write-Host " Report: $reportDir\index.html" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Display summary
$summaryFile = Join-Path $reportDir "Summary.md"
if (Test-Path $summaryFile) {
    Write-Host "`n>> Coverage Summary:" -ForegroundColor Cyan
    Get-Content $summaryFile | Write-Host
}

# Open report if requested
if ($OpenReport) {
    $indexFile = Join-Path $reportDir "index.html"
    if (Test-Path $indexFile) {
        Start-Process $indexFile
    }
}

exit 0
