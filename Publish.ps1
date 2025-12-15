<#
.SYNOPSIS
    Publishes the PanoramicData.OData.Client package to NuGet.org.

.DESCRIPTION
    This script performs the following steps:
    1. Checks for uncommitted changes (git porcelain)
    2. Determines the Nerdbank GitVersioning version
    3. Validates nuget-key.txt exists, has content, and is gitignored
    4. Runs unit tests (unless -SkipTests is specified)
    5. Publishes to NuGet.org

.PARAMETER SkipTests
    Skip running unit tests before publishing.

.EXAMPLE
    .\Publish.ps1
    
.EXAMPLE
    .\Publish.ps1 -SkipTests
#>

param(
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host "`n>> $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "   $Message" -ForegroundColor Green
}

function Write-Error-Exit {
    param([string]$Message)
    Write-Host "   ERROR: $Message" -ForegroundColor Red
    exit 1
}

# Get script directory (solution root)
$solutionRoot = $PSScriptRoot
Set-Location $solutionRoot

Write-Host "========================================" -ForegroundColor Magenta
Write-Host " PanoramicData.OData.Client Publisher" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

# Step 1: Check for uncommitted changes
Write-Step "Checking for uncommitted changes..."

$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Error-Exit "Working directory is not clean. Please commit or stash changes before publishing.`n$gitStatus"
}
Write-Success "Working directory is clean."

# Step 2: Determine Nerdbank GitVersioning version
Write-Step "Determining version from Nerdbank.GitVersioning..."

$nbgvOutput = nbgv get-version --format json 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error-Exit "Failed to get version from nbgv. Ensure Nerdbank.GitVersioning is installed.`n$nbgvOutput"
}

$versionInfo = $nbgvOutput | ConvertFrom-Json
$version = $versionInfo.NuGetPackageVersion
if (-not $version) {
    Write-Error-Exit "Could not determine NuGet package version from nbgv output."
}
Write-Success "Version: $version"

# Step 3: Validate nuget-key.txt
Write-Step "Validating nuget-key.txt..."

$nugetKeyPath = Join-Path $solutionRoot "nuget-key.txt"

# Check file exists
if (-not (Test-Path $nugetKeyPath)) {
    Write-Error-Exit "nuget-key.txt not found at: $nugetKeyPath"
}

# Check file has content
$nugetKey = (Get-Content $nugetKeyPath -Raw).Trim()
if ([string]::IsNullOrWhiteSpace($nugetKey)) {
    Write-Error-Exit "nuget-key.txt is empty."
}
Write-Success "nuget-key.txt exists and has content."

# Check file is gitignored
$gitCheckIgnore = git check-ignore $nugetKeyPath 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error-Exit "nuget-key.txt is NOT gitignored. Add it to .gitignore before publishing."
}
Write-Success "nuget-key.txt is properly gitignored."

# Step 4: Run unit tests (unless skipped)
if ($SkipTests) {
    Write-Step "Skipping unit tests (-SkipTests specified)."
} else {
    Write-Step "Running unit tests..."
    
    $testResult = dotnet test "$solutionRoot\PanoramicData.OData.Client.Test\PanoramicData.OData.Client.Test.csproj" --configuration Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Exit "Unit tests failed."
    }
    Write-Success "All tests passed."
}

# Step 5: Build and pack
Write-Step "Building and packing..."

$projectPath = Join-Path $solutionRoot "PanoramicData.OData.Client\PanoramicData.OData.Client.csproj"
$outputPath = Join-Path $solutionRoot "artifacts"

# Clean artifacts directory
if (Test-Path $outputPath) {
    Remove-Item $outputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $outputPath | Out-Null

$packResult = dotnet pack $projectPath --configuration Release --output $outputPath
if ($LASTEXITCODE -ne 0) {
    Write-Error-Exit "Failed to pack the project."
}
Write-Success "Package created successfully."

# Step 6: Publish to NuGet.org
Write-Step "Publishing to NuGet.org..."

$packagePath = Join-Path $outputPath "PanoramicData.OData.Client.$version.nupkg"
if (-not (Test-Path $packagePath)) {
    # Try to find any matching package
    $packages = Get-ChildItem $outputPath -Filter "*.nupkg"
    if ($packages.Count -eq 0) {
        Write-Error-Exit "No .nupkg file found in artifacts directory."
    }
    $packagePath = $packages[0].FullName
    Write-Host "   Using package: $($packages[0].Name)" -ForegroundColor Yellow
}

$pushResult = dotnet nuget push $packagePath --api-key $nugetKey --source https://api.nuget.org/v3/index.json --skip-duplicate
if ($LASTEXITCODE -ne 0) {
    Write-Error-Exit "Failed to publish to NuGet.org."
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host " Successfully published version $version" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

exit 0
