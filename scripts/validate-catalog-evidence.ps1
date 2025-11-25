#Requires -Version 5.1
<#
.SYNOPSIS
    Catalog Evidence Validator - Validates catalog and implementation sync

.DESCRIPTION
    Validates:
    1. Evidence paths in catalog YAML files exist
    2. Project structure follows catalog standards
    3. Detects forbidden patterns

.EXAMPLE
    ./scripts/validate-catalog-evidence.ps1
    ./scripts/validate-catalog-evidence.ps1 -Verbose
    ./scripts/validate-catalog-evidence.ps1 -FixSuggestions

.NOTES
    Catalog Version: v2025.11.25
#>

[CmdletBinding()]
param(
    [switch]$FixSuggestions,
    [string]$CatalogPath = "catalog",
    [string]$SrcPath = "src"
)

$ErrorActionPreference = "Continue"
$script:TotalErrors = 0
$script:TotalWarnings = 0
$script:Results = @{
    EvidenceMissing = @()
    EvidenceFound = @()
    StructureViolations = @()
    StructureOK = @()
}

# ============================================================================
# Helper Functions
# ============================================================================

function Write-Header {
    param([string]$Title)
    Write-Host ""
    Write-Host ("=" * 70) -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor Cyan
    Write-Host ("=" * 70) -ForegroundColor Cyan
}

function Write-OK {
    param([string]$Message)
    Write-Host "  [OK] $Message" -ForegroundColor Green
}

function Write-NG {
    param([string]$Message)
    Write-Host "  [NG] $Message" -ForegroundColor Red
    $script:TotalErrors++
}

function Write-Warn {
    param([string]$Message)
    Write-Host "  [WARN] $Message" -ForegroundColor Yellow
    $script:TotalWarnings++
}

function Write-Detail {
    param([string]$Message)
    if ($VerbosePreference -eq "Continue") {
        Write-Host "  [INFO] $Message" -ForegroundColor Gray
    }
}

# ============================================================================
# Evidence Path Validation
# ============================================================================

function Test-EvidencePaths {
    Write-Header "Evidence Path Validation"

    $yamlFiles = Get-ChildItem -Path $CatalogPath -Recurse -Filter "*.yaml" -ErrorAction SilentlyContinue

    if (-not $yamlFiles) {
        Write-Warn "No YAML files found in $CatalogPath"
        return
    }

    Write-Host "  Scanning $($yamlFiles.Count) YAML files..." -ForegroundColor Gray

    foreach ($file in $yamlFiles) {
        $content = Get-Content $file.FullName -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
        if (-not $content) { continue }

        # Find evidence section
        if ($content -match "evidence:") {
            $lines = $content -split "`n"
            $inEvidence = $false
            $evidencePaths = @()

            foreach ($line in $lines) {
                if ($line -match "^evidence:") {
                    $inEvidence = $true
                    continue
                }

                if ($inEvidence) {
                    # End at next top-level section
                    if ($line -match "^[a-z_]+:" -and $line -notmatch "^\s") {
                        break
                    }

                    # Extract paths starting with "src/"
                    if ($line -match '"(src/[^"]+)"') {
                        $evidencePaths += $matches[1]
                    }
                    elseif ($line -match "'(src/[^']+)'") {
                        $evidencePaths += $matches[1]
                    }
                }
            }

            # Check path existence
            foreach ($path in $evidencePaths) {
                # Skip markers
                if ($path -match "TODO|TBD") {
                    Write-Detail "Skipping: $path (marked as not implemented)"
                    continue
                }

                # Strip annotations like " (Delete() メソッド)" or " (line 14, ...)"
                # Use greedy match to handle nested parentheses like "Delete()"
                $cleanPath = $path -replace '\s+\(.*\)\s*$', ''

                $fullPath = Join-Path (Get-Location) $cleanPath
                if (Test-Path $fullPath) {
                    $script:Results.EvidenceFound += @{
                        Catalog = $file.Name
                        Path = $path
                    }
                    Write-Detail "Found: $path"
                }
                else {
                    $script:Results.EvidenceMissing += @{
                        Catalog = $file.Name
                        Path = $path
                        ExpectedLocation = $fullPath
                    }
                    Write-NG "$($file.Name): Missing evidence path"
                    Write-Host "         $path" -ForegroundColor Red
                }
            }
        }
    }

    # Summary
    Write-Host ""
    Write-Host "  Evidence Summary:" -ForegroundColor White
    Write-Host "    Found: $($script:Results.EvidenceFound.Count)" -ForegroundColor Green
    $missingColor = if ($script:Results.EvidenceMissing.Count -gt 0) { "Red" } else { "Green" }
    Write-Host "    Missing: $($script:Results.EvidenceMissing.Count)" -ForegroundColor $missingColor
}

# ============================================================================
# Structure Violation Detection
# ============================================================================

function Test-StructureViolations {
    Write-Header "Structure Violation Detection"

    $violations = @()

    # Rule 1: Features/{Feature}/UI/ subfolder is forbidden
    Write-Host "  Checking: Features/{Feature}/UI/ subfolders..." -ForegroundColor Gray
    $featuresPath = Join-Path $SrcPath "Application/Features"
    if (Test-Path $featuresPath) {
        $uiFolders = Get-ChildItem -Path $featuresPath -Directory -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -eq "UI" }

        foreach ($folder in $uiFolders) {
            $relativePath = $folder.FullName -replace [regex]::Escape((Get-Location).Path + [IO.Path]::DirectorySeparatorChar), ""
            $violations += @{
                Type = "ForbiddenUISubfolder"
                Path = $relativePath
                Rule = "Features/{Feature}/UI/ subfolder is forbidden"
                Fix = "Move .razor files to parent folder and delete UI/ folder"
            }
            Write-NG "Forbidden UI subfolder: $relativePath"
        }
    }

    # Rule 2: Application/Shared/{BC}/ folder detection (warning)
    Write-Host "  Checking: Application/Shared/{BC}/ folders..." -ForegroundColor Gray
    $appSharedPath = Join-Path $SrcPath "Application/Shared"
    if (Test-Path $appSharedPath) {
        $sharedBCFolders = Get-ChildItem -Path $appSharedPath -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -notin @("Components", "Shared") }

        foreach ($folder in $sharedBCFolders) {
            $relativePath = "src/Application/Shared/$($folder.Name)"
            $violations += @{
                Type = "SharedBCFolder"
                Path = $relativePath
                Rule = "Application/Shared/{BC}/ is deprecated (use Application/Infrastructure/{BC}/)"
                Fix = "Move folder to Application/Infrastructure/$($folder.Name)/"
            }
            Write-Warn "Non-standard BC folder: Application/Shared/$($folder.Name)/"
        }
    }

    # Rule 3: Shared project BC contamination
    Write-Host "  Checking: Shared project BC contamination..." -ForegroundColor Gray
    $sharedPath = Join-Path $SrcPath "Shared"
    if (Test-Path $sharedPath) {
        $allowedFolders = @("Kernel", "Domain", "Application", "Infrastructure", "Abstractions")
        $sharedProjectBCFolders = Get-ChildItem -Path $sharedPath -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -notin $allowedFolders -and $_.Name -notmatch "^Infrastructure\." }

        foreach ($folder in $sharedProjectBCFolders) {
            $relativePath = "src/Shared/$($folder.Name)"
            $violations += @{
                Type = "SharedProjectBCContamination"
                Path = $relativePath
                Rule = "Do not place BC folders in Shared project"
                Fix = "Move BC-specific code to Application/Infrastructure/{BC}/ or Domain/{BC}/"
            }
            Write-NG "BC contamination in Shared: Shared/$($folder.Name)/"
        }
    }

    # Rule 4: Kernel location check
    Write-Host "  Checking: Kernel location..." -ForegroundColor Gray
    $kernelInShared = Test-Path (Join-Path $SrcPath "Shared/Kernel")
    $kernelIndependent = Test-Path (Join-Path $SrcPath "Kernel")

    if ($kernelInShared -and -not $kernelIndependent) {
        $violations += @{
            Type = "KernelLocation"
            Path = "src/Shared/Kernel"
            Rule = "Kernel should be at src/Kernel/ (independent)"
            Fix = "Move src/Shared/Kernel/ to src/Kernel/ and update project references"
        }
        Write-Warn "Kernel is in Shared/ (recommended: src/Kernel/)"
    }
    elseif ($kernelIndependent) {
        Write-OK "Kernel is correctly placed at src/Kernel/"
    }

    $script:Results.StructureViolations = $violations

    # Summary
    Write-Host ""
    Write-Host "  Structure Summary:" -ForegroundColor White
    $errorViolations = @($violations | Where-Object { $_.Type -eq "ForbiddenUISubfolder" -or $_.Type -eq "SharedProjectBCContamination" })
    $warningViolations = @($violations | Where-Object { $_.Type -eq "SharedBCFolder" -or $_.Type -eq "KernelLocation" })

    $errorColor = if ($errorViolations.Count -gt 0) { "Red" } else { "Green" }
    $warnColor = if ($warningViolations.Count -gt 0) { "Yellow" } else { "Green" }

    Write-Host "    Violations: $($errorViolations.Count)" -ForegroundColor $errorColor
    Write-Host "    Warnings: $($warningViolations.Count)" -ForegroundColor $warnColor
}

# ============================================================================
# Project Structure Validation
# ============================================================================

function Test-ProjectStructure {
    Write-Header "Project Structure Validation"

    $expectedProjects = @(
        @{ Pattern = "*.Kernel.csproj"; Location = "src/Kernel"; Required = $false; Description = "DDD Foundation (Kernel)" },
        @{ Pattern = "*.Application.csproj"; Location = "src/Application"; Required = $true; Description = "Web Host (Application)" },
        @{ Pattern = "*.Domain.*.csproj"; Location = "src/Domain"; Required = $true; Description = "BC Domain" },
        @{ Pattern = "*.Shared.Application.csproj"; Location = "src/Shared/Application"; Required = $false; Description = "Shared Application Layer" },
        @{ Pattern = "*.Shared.Infrastructure.csproj"; Location = "src/Shared/Infrastructure"; Required = $false; Description = "Shared Infrastructure Layer" }
    )

    Write-Host "  Checking project structure..." -ForegroundColor Gray

    foreach ($proj in $expectedProjects) {
        $found = Get-ChildItem -Path $SrcPath -Recurse -Filter $proj.Pattern -ErrorAction SilentlyContinue

        if ($found) {
            Write-OK "$($proj.Description): $($found.Name)"
            $script:Results.StructureOK += $proj.Description
        }
        elseif ($proj.Required) {
            Write-NG "Missing required project: $($proj.Pattern)"
        }
        else {
            Write-Detail "Optional project not found: $($proj.Pattern)"
        }
    }
}

# ============================================================================
# Fix Suggestions
# ============================================================================

function Write-FixSuggestions {
    if (-not $FixSuggestions) { return }

    Write-Header "Fix Suggestions"

    if ($script:Results.EvidenceMissing.Count -gt 0) {
        Write-Host ""
        Write-Host "  Evidence Path Fixes:" -ForegroundColor Yellow
        foreach ($item in $script:Results.EvidenceMissing) {
            Write-Host "    - $($item.Catalog):" -ForegroundColor White
            Write-Host "      Missing: $($item.Path)" -ForegroundColor Gray

            # Search for similar files
            $fileName = Split-Path $item.Path -Leaf
            $similar = Get-ChildItem -Path $SrcPath -Recurse -Filter $fileName -ErrorAction SilentlyContinue | Select-Object -First 3
            if ($similar) {
                Write-Host "      Possible matches:" -ForegroundColor Cyan
                foreach ($s in $similar) {
                    $relativePath = $s.FullName -replace [regex]::Escape((Get-Location).Path + [IO.Path]::DirectorySeparatorChar), ""
                    Write-Host "        - $relativePath" -ForegroundColor Cyan
                }
            }
        }
    }

    if ($script:Results.StructureViolations.Count -gt 0) {
        Write-Host ""
        Write-Host "  Structure Violation Fixes:" -ForegroundColor Yellow
        foreach ($item in $script:Results.StructureViolations) {
            Write-Host "    - $($item.Path):" -ForegroundColor White
            Write-Host "      Rule: $($item.Rule)" -ForegroundColor Gray
            Write-Host "      Fix: $($item.Fix)" -ForegroundColor Cyan
        }
    }
}

# ============================================================================
# Report Output
# ============================================================================

function Write-Report {
    Write-Header "Validation Report"

    $totalIssues = $script:TotalErrors + $script:TotalWarnings

    Write-Host ""
    $totalColor = if ($totalIssues -gt 0) { "Yellow" } else { "Green" }
    $errorColor = if ($script:TotalErrors -gt 0) { "Red" } else { "Green" }
    $warnColor = if ($script:TotalWarnings -gt 0) { "Yellow" } else { "Green" }

    Write-Host "  Total Issues: $totalIssues" -ForegroundColor $totalColor
    Write-Host "    Errors: $script:TotalErrors" -ForegroundColor $errorColor
    Write-Host "    Warnings: $script:TotalWarnings" -ForegroundColor $warnColor
    Write-Host ""

    if ($totalIssues -eq 0) {
        Write-Host "  All validations passed!" -ForegroundColor Green
    }
    else {
        Write-Host "  Run with -FixSuggestions for detailed fix recommendations" -ForegroundColor Cyan
    }

    Write-Host ""
    Write-Host ("=" * 70) -ForegroundColor Cyan

    # Exit code
    if ($script:TotalErrors -gt 0) {
        exit 1
    }
}

# ============================================================================
# Main Execution
# ============================================================================

Write-Host ""
Write-Host "Catalog Evidence Validator v1.0.0" -ForegroundColor Magenta
Write-Host "Catalog: $CatalogPath | Source: $SrcPath" -ForegroundColor Gray

Test-EvidencePaths
Test-StructureViolations
Test-ProjectStructure
Write-FixSuggestions
Write-Report
