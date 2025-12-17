<#
.SYNOPSIS
    Publishes the PanoramicData.OData.Client package to NuGet.org.

.DESCRIPTION
    This script performs the following steps:
    1. Checks for uncommitted changes (git porcelain)
    2. Determines the Nerdbank GitVersioning version
    3. Updates CHANGELOG.md [vNext] placeholder with actual version
    4. Commits changelog update (which increments git height)
    5. Recalculates version after commit
    6. Validates nuget-key.txt exists, has content, and is gitignored
    7. Runs unit tests (unless -SkipTests is specified)
    8. Publishes to NuGet.org

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

function Get-NbgvVersion {
    $nbgvOutput = nbgv get-version --format json 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Exit "Failed to get version from nbgv. Ensure Nerdbank.GitVersioning is installed.`n$nbgvOutput"
    }
    return $nbgvOutput | ConvertFrom-Json
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

# Step 2: Check if CHANGELOG.md has [vNext] placeholder with content
Write-Step "Checking CHANGELOG.md for [vNext] placeholder..."

$changelogPath = Join-Path $solutionRoot "CHANGELOG.md"
if (-not (Test-Path $changelogPath)) {
    Write-Error-Exit "CHANGELOG.md not found. Please create it with a [vNext] section."
}

$changelogContent = Get-Content $changelogPath -Raw

# Check if [vNext] exists and has content (not just followed by another ## section or EOF)
$vNextMatch = [regex]::Match($changelogContent, '## \[vNext\]\s*\r?\n(.*?)(?=\r?\n## \[|$)', [System.Text.RegularExpressions.RegexOptions]::Singleline)

if (-not $vNextMatch.Success) {
    Write-Host "   No [vNext] placeholder found - changelog already up to date." -ForegroundColor Yellow
    $needsChangelogUpdate = $false
} else {
    $vNextContent = $vNextMatch.Groups[1].Value.Trim()
    if ([string]::IsNullOrWhiteSpace($vNextContent)) {
        Write-Host "   [vNext] section is empty - nothing to release." -ForegroundColor Yellow
        $needsChangelogUpdate = $false
    } else {
        $needsChangelogUpdate = $true
        Write-Success "Found [vNext] placeholder with content to release."
    }
}

# Step 3: Get current version and update changelog if needed
if ($needsChangelogUpdate) {
    Write-Step "Calculating version for changelog..."
    
    # Get current version info - this will be the version BEFORE the changelog commit
    # After committing, git height increases by 1, so we need to calculate the post-commit version
    $currentVersionInfo = Get-NbgvVersion
    $currentHeight = [int]$currentVersionInfo.GitCommitHeight
    $nextHeight = $currentHeight + 1
    
    # For NuGet, use the predicted NuGet version format
    $nugetVersion = $currentVersionInfo.NuGetPackageVersion -replace "\.${currentHeight}(-|$)", ".$nextHeight`$1"
    
    Write-Host "   Current version: $($currentVersionInfo.NuGetPackageVersion)" -ForegroundColor Gray
    Write-Host "   Predicted post-commit version: $nugetVersion" -ForegroundColor Gray
    
    # Update CHANGELOG.md
    Write-Step "Updating CHANGELOG.md with version $nugetVersion..."
    
    $today = Get-Date -Format "yyyy-MM-dd"
    
    # Replace [vNext] with versioned header, keeping a new empty [vNext] at the top
    $newChangelogContent = $changelogContent -replace '## \[vNext\]', "## [vNext]`n`n## [$nugetVersion] - $today"
    
    Set-Content $changelogPath $newChangelogContent -Encoding UTF8
    Write-Success "CHANGELOG.md updated with version $nugetVersion"
    
    # Commit the changelog update
    Write-Step "Committing changelog update..."
    git add $changelogPath
    git commit -m "Release $nugetVersion - Update CHANGELOG.md"
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Exit "Failed to commit changelog update."
    }
    Write-Success "Changelog committed."
}

# Step 4: Determine final version from Nerdbank GitVersioning
Write-Step "Determining final version from Nerdbank.GitVersioning..."

$versionInfo = Get-NbgvVersion
$version = $versionInfo.NuGetPackageVersion
if (-not $version) {
    Write-Error-Exit "Could not determine NuGet package version from nbgv output."
}
Write-Success "Version: $version"

# Step 5: Validate nuget-key.txt
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

# Step 6: Run unit tests (unless skipped)
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

# Step 7: Build and pack
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

# Step 8: Publish to NuGet.org
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

Write-Host "`nDon't forget to push the changelog commit:" -ForegroundColor Yellow
Write-Host "   git push origin main" -ForegroundColor Yellow

exit 0
