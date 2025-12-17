<#
.SYNOPSIS
    Adds a new entry to the CHANGELOG.md file under the [vNext] section.

.DESCRIPTION
    This script adds changelog entries to the appropriate category (Added, Changed, 
    Deprecated, Removed, Fixed, Security) under the [vNext] placeholder section.
    
    The [vNext] placeholder will be replaced with the actual version number during
    the publish process.

.PARAMETER Category
    The changelog category. Must be one of: Added, Changed, Deprecated, Removed, Fixed, Security

.PARAMETER Message
    The changelog entry message describing the change.

.EXAMPLE
    .\Add-ChangelogEntry.ps1 -Category Added -Message "New feature for batch processing"

.EXAMPLE
    .\Add-ChangelogEntry.ps1 -Category Fixed -Message "Resolved null reference in query builder"

.EXAMPLE
    .\Add-ChangelogEntry.ps1 Added "Support for OData V4.01 features"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('Added', 'Changed', 'Deprecated', 'Removed', 'Fixed', 'Security')]
    [string]$Category,

    [Parameter(Mandatory = $true, Position = 1)]
    [string]$Message
)

$ErrorActionPreference = 'Stop'

$changelogPath = Join-Path $PSScriptRoot 'CHANGELOG.md'

if (-not (Test-Path $changelogPath)) {
    Write-Error "CHANGELOG.md not found at: $changelogPath"
    exit 1
}

$content = Get-Content $changelogPath -Raw
$lines = Get-Content $changelogPath

# Find the [vNext] section
$vNextIndex = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^\s*##\s*\[vNext\]') {
        $vNextIndex = $i
        break
    }
}

if ($vNextIndex -eq -1) {
    Write-Error "[vNext] section not found in CHANGELOG.md. Please ensure the changelog has a [vNext] placeholder."
    exit 1
}

# Find or create the category section under [vNext]
$categoryHeader = "### $Category"
$categoryIndex = -1
$nextSectionIndex = $lines.Count

# Look for the category header between [vNext] and the next version section
for ($i = $vNextIndex + 1; $i -lt $lines.Count; $i++) {
    # Stop if we hit another version section
    if ($lines[$i] -match '^\s*##\s*\[[\d\.]+') {
        $nextSectionIndex = $i
        break
    }
    
    if ($lines[$i] -eq $categoryHeader) {
        $categoryIndex = $i
    }
}

# Format the entry
$entry = "- $Message"

if ($categoryIndex -ne -1) {
    # Category exists - find the end of the category's entries
    $insertIndex = $categoryIndex + 1
    
    # Skip past existing entries in this category
    while ($insertIndex -lt $nextSectionIndex -and 
           $lines[$insertIndex] -match '^\s*-' -or 
           [string]::IsNullOrWhiteSpace($lines[$insertIndex])) {
        if ($lines[$insertIndex] -match '^\s*-') {
            $insertIndex++
        }
        elseif ([string]::IsNullOrWhiteSpace($lines[$insertIndex])) {
            # Check if next line is a new category or version
            if ($insertIndex + 1 -lt $lines.Count -and $lines[$insertIndex + 1] -match '^###\s') {
                break
            }
            $insertIndex++
        }
        else {
            break
        }
    }
    
    # Insert after the last entry in the category
    $newLines = @()
    $newLines += $lines[0..($categoryIndex)]
    $newLines += $entry
    if ($categoryIndex + 1 -lt $lines.Count) {
        $newLines += $lines[($categoryIndex + 1)..($lines.Count - 1)]
    }
    $lines = $newLines
}
else {
    # Category doesn't exist - need to add it
    # Find where to insert (after [vNext] header and any existing categories, before next version)
    $insertIndex = $vNextIndex + 1
    
    # Skip any blank lines after [vNext]
    while ($insertIndex -lt $nextSectionIndex -and [string]::IsNullOrWhiteSpace($lines[$insertIndex])) {
        $insertIndex++
    }
    
    # Define category order
    $categoryOrder = @('Added', 'Changed', 'Deprecated', 'Removed', 'Fixed', 'Security')
    $targetCategoryIndex = [Array]::IndexOf($categoryOrder, $Category)
    
    # Find the right position based on category order
    $foundPosition = $false
    for ($i = $insertIndex; $i -lt $nextSectionIndex; $i++) {
        if ($lines[$i] -match '^###\s+(\w+)') {
            $existingCategory = $Matches[1]
            $existingCategoryIndex = [Array]::IndexOf($categoryOrder, $existingCategory)
            
            if ($existingCategoryIndex -gt $targetCategoryIndex) {
                # Insert before this category
                $insertIndex = $i
                $foundPosition = $true
                break
            }
        }
    }
    
    if (-not $foundPosition) {
        # Insert at the end of the [vNext] section (before next version or end of file)
        $insertIndex = $nextSectionIndex
    }
    
    # Insert the new category with entry
    $newLines = @()
    if ($insertIndex -gt 0) {
        $newLines += $lines[0..($insertIndex - 1)]
    }
    
    # Add blank line before if previous line isn't blank
    if ($insertIndex -gt 0 -and -not [string]::IsNullOrWhiteSpace($lines[$insertIndex - 1])) {
        $newLines += ''
    }
    
    $newLines += $categoryHeader
    $newLines += $entry
    
    if ($insertIndex -lt $lines.Count) {
        # Add blank line after if next line isn't blank
        if (-not [string]::IsNullOrWhiteSpace($lines[$insertIndex])) {
            $newLines += ''
        }
        $newLines += $lines[$insertIndex..($lines.Count - 1)]
    }
    
    $lines = $newLines
}

# Write back to file
$lines | Set-Content $changelogPath -Encoding UTF8

Write-Host "Added to CHANGELOG.md [$Category]: $Message" -ForegroundColor Green
