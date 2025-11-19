#Requires -Version 7.0
<#
.SYNOPSIS
    Catalogと実装コードの同期を検証するスクリプト

.DESCRIPTION
    catalog/ フォルダ内のYAMLファイルとindex.jsonを検証し、
    実際のプロジェクト構造との整合性を確認します。

.EXAMPLE
    .\validate-catalog-sync.ps1
#>

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $PSScriptRoot
$catalogRoot = Join-Path $scriptRoot "catalog"

Write-Host "=== Catalog Sync Validation Report ===" -ForegroundColor Cyan
Write-Host ""

# Validation results
$results = @{
    FilePathValidation = @()
    TemplateValidation = @()
    VersionConsistency = @()
    Warnings = @()
    Errors = @()
}

# Counters
$totalChecks = 0
$passedChecks = 0
$warningCount = 0
$errorCount = 0

#region File Path Validation

Write-Host "1. File Path Validation" -ForegroundColor Yellow

# Load index.json
$indexPath = Join-Path $catalogRoot "index.json"
if (-not (Test-Path $indexPath)) {
    $results.Errors += "index.json not found: $indexPath"
    $errorCount++
} else {
    $index = Get-Content $indexPath -Raw -Encoding UTF8 | ConvertFrom-Json -AsHashtable

    # Check each pattern's YAML file
    foreach ($pattern in $index['patterns']) {
        $yamlPath = Join-Path $catalogRoot $pattern['file']
        $totalChecks++

        if (Test-Path $yamlPath) {
            $passedChecks++
            $results.FilePathValidation += "OK $($pattern['id']): YAML exists"

            # Load YAML and check evidence fields
            $yamlContent = Get-Content $yamlPath -Raw -Encoding UTF8

            # Extract implementation.file_path
            if ($yamlContent -match 'file_path:\s*"([^"]+)"') {
                $filePath = $matches[1]
                # Skip paths with template variables
                if ($filePath -notmatch '\{.*\}') {
                    $fullPath = Join-Path $scriptRoot $filePath
                    $totalChecks++
                    if (Test-Path $fullPath) {
                        $passedChecks++
                        $results.FilePathValidation += "  OK implementation exists: $filePath"
                    } else {
                        $errorCount++
                        $results.Errors += "  ERROR implementation NOT FOUND: $filePath"
                    }
                }
            }

            # Check evidence fields
            if ($yamlContent -match 'evidence:\s*\n((?:\s{2}[^\n]+\n?)*)') {
                $evidenceBlock = $matches[1]
                $evidenceLines = $evidenceBlock -split "`n" | Where-Object { $_ -match ':\s*"([^"]+)"' }

                foreach ($line in $evidenceLines) {
                    if ($line -match ':\s*"([^"]+)"') {
                        $evidencePath = $matches[1]
                        # Skip "unimplemented" strings
                        if ($evidencePath -notmatch 'implemented|future' -and $evidencePath -match '^src/') {
                            $fullEvidencePath = Join-Path $scriptRoot $evidencePath
                            $totalChecks++
                            if (Test-Path $fullEvidencePath) {
                                $passedChecks++
                                $results.FilePathValidation += "  OK evidence exists: $evidencePath"
                            } else {
                                $warningCount++
                                $results.Warnings += "  WARN evidence NOT FOUND: $evidencePath"
                            }
                        }
                    }
                }
            }
        } else {
            $errorCount++
            $results.Errors += "ERROR $($pattern['id']): YAML NOT FOUND at $yamlPath"
        }
    }
}

Write-Host "  Checks completed: $passedChecks / $totalChecks" -ForegroundColor Green
Write-Host ""

#endregion

#region Version Consistency Validation

Write-Host "2. Version Consistency Validation" -ForegroundColor Yellow

$versionChecks = 0
$versionPassed = 0

if ($index) {
    foreach ($pattern in $index['patterns']) {
        $yamlPath = Join-Path $catalogRoot $pattern['file']

        if (Test-Path $yamlPath) {
            $yamlContent = Get-Content $yamlPath -Raw -Encoding UTF8

            # Extract version from YAML
            if ($yamlContent -match '^version:\s*(.+)$') {
                $yamlVersion = $matches[1].Trim()
                $indexVersion = $pattern['version']

                $versionChecks++
                $totalChecks++

                if ($yamlVersion -eq $indexVersion) {
                    $versionPassed++
                    $passedChecks++
                    $results.VersionConsistency += "OK $($pattern['id']): v$yamlVersion"
                } else {
                    $warningCount++
                    $results.Warnings += "WARN $($pattern['id']): version mismatch (YAML: $yamlVersion, index: $indexVersion)"
                }
            }
        }
    }
}

Write-Host "  Checks completed: $versionPassed / $versionChecks" -ForegroundColor Green
Write-Host ""

#endregion

#region Template Variable Validation

Write-Host "3. Template Variable Validation" -ForegroundColor Yellow

$templateChecks = 0
$templatePassed = 0

$expectedVariables = @(
    # Core template variables
    '{Entity}', '{entity}', '{BoundedContext}',
    # Entity properties
    '{Id}', '{Name}',
    # Search/Filter parameters
    '{NameFilter}', '{MinPrice}', '{MaxPrice}', '{Status}',
    '{Page}', '{PageSize}', '{OrderBy}', '{IsDescending}',
    # Operation-specific
    '{Operation}',
    # Versioning
    '{Current}', '{Latest}', '{Version}',
    # Bulk operation results
    '{SucceededCount}', '{FailedCount}', '{TotalCount}',
    # Request/Command context
    '{RequestType}', '{RequestName}', '{CommandType}', '{CommandName}',
    # Authorization
    '{Policy}', '{UserName}', '{Roles}',
    # Idempotency
    '{IdempotencyKey}',
    # Logging/Tracing
    '{RequestId}', '{CorrelationId}', '{ElapsedMs}',
    # Events
    '{EventType}', '{OutboxMessageId}'
)

Get-ChildItem -Path $catalogRoot -Filter "*.yaml" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Raw -Encoding UTF8

    # Check template variables
    if ($content -match 'template:\s*\|') {
        $templateChecks++
        $totalChecks++

        # Check for invalid variable formats
        $pattern = '\{[A-Za-z_]+\}'
        $invalidVars = [regex]::Matches($content, $pattern) |
                       Where-Object { $_.Value -notin $expectedVariables } |
                       Select-Object -ExpandProperty Value -Unique

        if ($invalidVars.Count -eq 0) {
            $templatePassed++
            $passedChecks++
            $results.TemplateValidation += "OK $($_.Name): consistent template variables"
        } else {
            $warningCount++
            $varList = $invalidVars -join ', '
            $results.Warnings += "WARN $($_.Name): unexpected variables: $varList"
        }
    }
}

Write-Host "  Checks completed: $templatePassed / $templateChecks" -ForegroundColor Green
Write-Host ""

#endregion

#region Final Report

Write-Host "=== Validation Summary ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "File Path Validation: " -NoNewline
if ($results.FilePathValidation.Count -gt 0) {
    Write-Host "PASS ($($results.FilePathValidation.Count) checks)" -ForegroundColor Green
} else {
    Write-Host "N/A" -ForegroundColor Gray
}

Write-Host "Version Consistency: " -NoNewline
if ($versionPassed -eq $versionChecks) {
    Write-Host "PASS ($versionPassed/$versionChecks)" -ForegroundColor Green
} elseif ($versionPassed -gt 0) {
    Write-Host "WARNING ($versionPassed/$versionChecks)" -ForegroundColor Yellow
} else {
    Write-Host "FAIL" -ForegroundColor Red
}

Write-Host "Template Validation: " -NoNewline
if ($templatePassed -eq $templateChecks) {
    Write-Host "PASS ($templatePassed/$templateChecks)" -ForegroundColor Green
} elseif ($templatePassed -gt 0) {
    Write-Host "WARNING ($templatePassed/$templateChecks)" -ForegroundColor Yellow
} else {
    Write-Host "N/A" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Total Checks: $totalChecks" -ForegroundColor Cyan
Write-Host "  PASS: $passedChecks" -ForegroundColor Green
Write-Host "  WARNING: $warningCount" -ForegroundColor Yellow
Write-Host "  ERROR: $errorCount" -ForegroundColor Red
Write-Host ""

# Display warnings and errors
if ($results.Warnings.Count -gt 0) {
    Write-Host "Warnings ($($results.Warnings.Count)):" -ForegroundColor Yellow
    $results.Warnings | ForEach-Object { Write-Host "  $_" }
    Write-Host ""
}

if ($results.Errors.Count -gt 0) {
    Write-Host "Errors ($($results.Errors.Count)):" -ForegroundColor Red
    $results.Errors | ForEach-Object { Write-Host "  $_" }
    Write-Host ""
}

# Overall rating
Write-Host "Overall Rating: " -NoNewline
if ($errorCount -eq 0 -and $warningCount -eq 0) {
    Write-Host "EXCELLENT" -ForegroundColor Green
    Write-Host "  The catalog is fully synchronized." -ForegroundColor Green
} elseif ($errorCount -eq 0) {
    Write-Host "GOOD (with warnings)" -ForegroundColor Yellow
    Write-Host "  The catalog is generally synchronized with some warnings." -ForegroundColor Yellow
} else {
    Write-Host "NEEDS ATTENTION" -ForegroundColor Red
    Write-Host "  The catalog has errors that need to be fixed." -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Validation Complete ===" -ForegroundColor Cyan

# Exit code
if ($errorCount -gt 0) {
    exit 1
} elseif ($warningCount -gt 0) {
    exit 0  # Warnings only count as success
} else {
    exit 0
}

#endregion
