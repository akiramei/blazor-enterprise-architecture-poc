<#
.SYNOPSIS
    workpack の output.diff をリポジトリに適用

.DESCRIPTION
    run-implementation.ps1 で生成された unified diff を
    git apply でリポジトリに適用します。

    適用前に base_commit チェックを行い、
    適用後に build/test 結果を verification.md に保存します。

.PARAMETER TaskId
    タスクID（例: T001-create-book-entity）

.PARAMETER DryRun
    実際に適用せず、適用される変更のみ表示

.PARAMETER SkipBaseCommitCheck
    base_commit チェックをスキップ

.PARAMETER SkipVerification
    build/test 検証をスキップ

.PARAMETER MoveToCompleted
    適用成功後、workpack を completed/ に移動

.EXAMPLE
    ./scripts/apply-diff.ps1 -TaskId "T001-create-book-entity" -DryRun

.EXAMPLE
    ./scripts/apply-diff.ps1 -TaskId "T001-create-book-entity" -MoveToCompleted
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$TaskId,

    [switch]$DryRun,

    [switch]$SkipBaseCommitCheck,

    [switch]$SkipVerification,

    [switch]$MoveToCompleted
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $ProjectRoot) { $ProjectRoot = (Get-Location).Path }

Write-Host "=== Diff Applicator ===" -ForegroundColor Cyan
Write-Host "Task ID: $TaskId"
Write-Host "Dry Run: $DryRun"
Write-Host "Skip Base Commit Check: $SkipBaseCommitCheck"
Write-Host "Skip Verification: $SkipVerification"
Write-Host "Move to Completed: $MoveToCompleted"
Write-Host ""

# ============================================================
# Phase 0: Validation
# ============================================================
Write-Host "[Phase 0] Validating..." -ForegroundColor Yellow

$WorkpackDir = "$ProjectRoot/workpacks/active/$TaskId"
if (-not (Test-Path $WorkpackDir)) {
    throw "Workpack not found: $WorkpackDir"
}

$DiffFile = "$WorkpackDir/output.diff"
if (-not (Test-Path $DiffFile)) {
    throw "Diff file not found: $DiffFile. Run run-implementation.ps1 first."
}

# Check for violations
$ViolationsFile = "$WorkpackDir/violations.md"
if (Test-Path $ViolationsFile) {
    Write-Host "  [WARNING] Guardrail violations file exists: $ViolationsFile" -ForegroundColor Yellow
    Write-Host "  Review and fix violations before applying." -ForegroundColor Yellow
    $continue = Read-Host "  Continue anyway? (y/N)"
    if ($continue -ne "y") {
        throw "Aborted due to existing violations."
    }
}

# Check if git is available
$gitCmd = Get-Command "git" -ErrorAction SilentlyContinue
if (-not $gitCmd) {
    throw "git not found. Please install git."
}

# Check if we're in a git repository
$gitRoot = git rev-parse --show-toplevel 2>$null
if (-not $gitRoot) {
    throw "Not in a git repository."
}

Write-Host "  Git root: $gitRoot" -ForegroundColor Green
Write-Host "  Diff file: $DiffFile" -ForegroundColor Green
Write-Host ""

# ============================================================
# Phase 0.5: Base Commit Check
# ============================================================
if (-not $SkipBaseCommitCheck) {
    Write-Host "[Phase 0.5] Checking base commit..." -ForegroundColor Yellow

    $ReproducibilityFile = "$WorkpackDir/reproducibility.yaml"
    if (Test-Path $ReproducibilityFile) {
        $reproContent = Get-Content $ReproducibilityFile -Raw

        # Extract base_commit from reproducibility.yaml
        if ($reproContent -match "base_commit:\s*[`"']?([a-f0-9]+)[`"']?") {
            $expectedCommit = $Matches[1]
            $currentCommit = git rev-parse HEAD 2>$null

            if ($expectedCommit -eq "N/A") {
                Write-Host "  [SKIP] base_commit was N/A (not in git repo during generation)" -ForegroundColor Yellow
            } elseif ($currentCommit -eq $expectedCommit) {
                Write-Host "  [OK] HEAD matches base_commit: $expectedCommit" -ForegroundColor Green
            } elseif ($currentCommit.StartsWith($expectedCommit) -or $expectedCommit.StartsWith($currentCommit)) {
                Write-Host "  [OK] HEAD matches base_commit (prefix match)" -ForegroundColor Green
            } else {
                Write-Host "  [WARNING] HEAD does not match base_commit!" -ForegroundColor Yellow
                Write-Host "    Expected: $expectedCommit" -ForegroundColor Yellow
                Write-Host "    Current:  $currentCommit" -ForegroundColor Yellow
                Write-Host ""
                Write-Host "  The diff was generated against a different commit." -ForegroundColor Yellow
                Write-Host "  This may cause conflicts or unexpected behavior." -ForegroundColor Yellow
                Write-Host ""
                $continue = Read-Host "  Continue anyway? (y/N)"
                if ($continue -ne "y") {
                    throw "Aborted due to base commit mismatch. Use -SkipBaseCommitCheck to override."
                }
            }
        } else {
            Write-Host "  [SKIP] Could not parse base_commit from reproducibility.yaml" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  [SKIP] reproducibility.yaml not found" -ForegroundColor Yellow
    }

    Write-Host ""
}

# ============================================================
# Phase 1: Analyze Diff
# ============================================================
Write-Host "[Phase 1] Analyzing diff..." -ForegroundColor Yellow

$diffContent = Get-Content $DiffFile -Raw

# Extract file paths from diff
$fileMatches = [regex]::Matches($diffContent, '^\+\+\+ b/(.+)$', [System.Text.RegularExpressions.RegexOptions]::Multiline)
$affectedFiles = $fileMatches | ForEach-Object { $_.Groups[1].Value }

if ($affectedFiles.Count -eq 0) {
    Write-Host "  [WARNING] No files detected in diff. Checking format..." -ForegroundColor Yellow

    # Check if it's a different format
    if ($diffContent -match 'diff --git') {
        Write-Host "  Detected git diff format" -ForegroundColor Gray
    } elseif ($diffContent -match '^---\s') {
        Write-Host "  Detected unified diff format" -ForegroundColor Gray
    } else {
        Write-Host "  [ERROR] Unrecognized diff format" -ForegroundColor Red
        Write-Host "  First 500 characters:" -ForegroundColor Gray
        Write-Host $diffContent.Substring(0, [Math]::Min(500, $diffContent.Length))
        throw "Invalid diff format"
    }
} else {
    Write-Host "  Files to be modified:" -ForegroundColor Green
    foreach ($file in $affectedFiles) {
        $fullPath = Join-Path $gitRoot $file
        if (Test-Path $fullPath) {
            Write-Host "    [MODIFY] $file" -ForegroundColor Yellow
        } else {
            Write-Host "    [CREATE] $file" -ForegroundColor Green
        }
    }
}

Write-Host ""

# ============================================================
# Phase 2: Dry Run Check
# ============================================================
Write-Host "[Phase 2] Checking if diff can be applied..." -ForegroundColor Yellow

Push-Location $gitRoot
try {
    # Try to apply with --check (dry run)
    $checkResult = git apply --check $DiffFile 2>&1
    $checkExitCode = $LASTEXITCODE

    if ($checkExitCode -eq 0) {
        Write-Host "  [OK] Diff can be applied cleanly" -ForegroundColor Green
    } else {
        Write-Host "  [ERROR] Diff cannot be applied:" -ForegroundColor Red
        Write-Host $checkResult -ForegroundColor Red
        throw "Diff cannot be applied. Please fix conflicts manually."
    }
}
finally {
    Pop-Location
}

Write-Host ""

# ============================================================
# Phase 3: Apply Diff (or show what would be applied)
# ============================================================
if ($DryRun) {
    Write-Host "[Phase 3] DRY RUN - Showing what would be applied..." -ForegroundColor Yellow

    Push-Location $gitRoot
    try {
        # Show stats
        $statResult = git apply --stat $DiffFile 2>&1
        Write-Host $statResult
    }
    finally {
        Pop-Location
    }

    Write-Host ""
    Write-Host "To apply for real, run without -DryRun flag" -ForegroundColor Cyan
    exit 0
}

Write-Host "[Phase 3] Applying diff..." -ForegroundColor Yellow

Push-Location $gitRoot
try {
    # Apply the diff
    $applyResult = git apply $DiffFile 2>&1
    $applyExitCode = $LASTEXITCODE

    if ($applyExitCode -eq 0) {
        Write-Host "  [OK] Diff applied successfully" -ForegroundColor Green
    } else {
        Write-Host "  [ERROR] Failed to apply diff:" -ForegroundColor Red
        Write-Host $applyResult -ForegroundColor Red
        throw "Failed to apply diff"
    }
}
finally {
    Pop-Location
}

Write-Host ""

# ============================================================
# Phase 4: Post-Apply Verification
# ============================================================
Write-Host "[Phase 4] Post-apply verification..." -ForegroundColor Yellow

$verificationResults = @{
    git_status = $null
    build = $null
    test = $null
    format = $null
    timestamp = Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ"
}

# Git status
Push-Location $gitRoot
try {
    $statusResult = git status --short 2>&1
    $verificationResults.git_status = @{
        success = $true
        files = ($statusResult -split "`n" | Where-Object { $_ })
    }

    if ($statusResult) {
        Write-Host "  Changed files:" -ForegroundColor Green
        Write-Host $statusResult
    } else {
        Write-Host "  [WARNING] No changes detected in git status" -ForegroundColor Yellow
    }
}
finally {
    Pop-Location
}

Write-Host ""

# Build and test verification (if not skipped)
if (-not $SkipVerification) {
    Write-Host "[Phase 4.5] Running build and test verification..." -ForegroundColor Yellow

    # Build
    Write-Host "  Running: dotnet build..." -ForegroundColor Gray
    Push-Location $gitRoot
    try {
        $buildOutput = dotnet build --no-restore 2>&1
        $buildExitCode = $LASTEXITCODE

        $verificationResults.build = @{
            success = ($buildExitCode -eq 0)
            exit_code = $buildExitCode
            output = ($buildOutput | Select-Object -Last 20) -join "`n"
        }

        if ($buildExitCode -eq 0) {
            Write-Host "  [OK] Build succeeded" -ForegroundColor Green
        } else {
            Write-Host "  [FAIL] Build failed (exit code: $buildExitCode)" -ForegroundColor Red
            Write-Host ($buildOutput | Select-Object -Last 10)
        }
    }
    catch {
        $verificationResults.build = @{
            success = $false
            error = $_.Exception.Message
        }
        Write-Host "  [ERROR] Build error: $_" -ForegroundColor Red
    }
    finally {
        Pop-Location
    }

    # Test (only if build succeeded)
    if ($verificationResults.build.success) {
        Write-Host "  Running: dotnet test..." -ForegroundColor Gray
        Push-Location $gitRoot
        try {
            $testOutput = dotnet test --no-build 2>&1
            $testExitCode = $LASTEXITCODE

            $verificationResults.test = @{
                success = ($testExitCode -eq 0)
                exit_code = $testExitCode
                output = ($testOutput | Select-Object -Last 20) -join "`n"
            }

            if ($testExitCode -eq 0) {
                Write-Host "  [OK] Tests passed" -ForegroundColor Green
            } else {
                Write-Host "  [FAIL] Tests failed (exit code: $testExitCode)" -ForegroundColor Red
                Write-Host ($testOutput | Select-Object -Last 10)
            }
        }
        catch {
            $verificationResults.test = @{
                success = $false
                error = $_.Exception.Message
            }
            Write-Host "  [ERROR] Test error: $_" -ForegroundColor Red
        }
        finally {
            Pop-Location
        }
    } else {
        Write-Host "  [SKIP] Tests skipped due to build failure" -ForegroundColor Yellow
        $verificationResults.test = @{
            success = $false
            skipped = $true
            reason = "Build failed"
        }
    }

    Write-Host ""
}

# ============================================================
# Phase 5: Save Verification Report
# ============================================================
Write-Host "[Phase 5] Saving verification report..." -ForegroundColor Yellow

$overallSuccess = $true
if ($verificationResults.build -and -not $verificationResults.build.success) {
    $overallSuccess = $false
}
if ($verificationResults.test -and -not $verificationResults.test.success -and -not $verificationResults.test.skipped) {
    $overallSuccess = $false
}

$verificationReport = @"
# Verification Report for: $TaskId
# Generated: $($verificationResults.timestamp)

## Overall Status: $(if ($overallSuccess) { "PASS" } else { "FAIL" })

## Git Status

Changed files:
$(if ($verificationResults.git_status.files) { $verificationResults.git_status.files | ForEach-Object { "- $_" } | Out-String } else { "- (none)" })

## Build

$(if ($SkipVerification) {
"Status: SKIPPED"
} elseif ($verificationResults.build.success) {
"Status: PASS"
} else {
@"
Status: FAIL
Exit Code: $($verificationResults.build.exit_code)

Output (last 20 lines):
``````
$($verificationResults.build.output)
``````
"@
})

## Test

$(if ($SkipVerification) {
"Status: SKIPPED"
} elseif ($verificationResults.test.skipped) {
"Status: SKIPPED (build failed)"
} elseif ($verificationResults.test.success) {
"Status: PASS"
} else {
@"
Status: FAIL
Exit Code: $($verificationResults.test.exit_code)

Output (last 20 lines):
``````
$($verificationResults.test.output)
``````
"@
})

## Reproducibility Reference

See: reproducibility.yaml
"@

$verificationPath = "$WorkpackDir/verification.md"
$verificationReport | Out-File -FilePath $verificationPath -Encoding utf8
Write-Host "  Saved: $verificationPath" -ForegroundColor Green
Write-Host ""

# ============================================================
# Phase 6: Move to Completed (optional)
# ============================================================
if ($MoveToCompleted) {
    if (-not $overallSuccess) {
        Write-Host "[Phase 6] Skipping move to completed due to verification failures" -ForegroundColor Yellow
        Write-Host "  Fix issues and re-run verification, or use manual move" -ForegroundColor Yellow
    } else {
        Write-Host "[Phase 6] Moving workpack to completed..." -ForegroundColor Yellow

        $CompletedDir = "$ProjectRoot/workpacks/completed"
        if (-not (Test-Path $CompletedDir)) {
            New-Item -ItemType Directory -Path $CompletedDir -Force | Out-Null
        }

        $TargetDir = "$CompletedDir/$TaskId"
        if (Test-Path $TargetDir) {
            Remove-Item -Recurse -Force $TargetDir
        }

        Move-Item -Path $WorkpackDir -Destination $TargetDir
        Write-Host "  Moved to: $TargetDir" -ForegroundColor Green
    }
    Write-Host ""
}

# ============================================================
# Summary
# ============================================================
Write-Host "=== Apply Complete ===" -ForegroundColor Cyan
Write-Host ""

if ($overallSuccess) {
    Write-Host "Changes have been applied and verified successfully." -ForegroundColor Green
} else {
    Write-Host "Changes have been applied but verification failed." -ForegroundColor Yellow
    Write-Host "Review verification.md for details." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Verification report: $verificationPath" -ForegroundColor Gray
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
if ($overallSuccess) {
    Write-Host "  1. Review the changes: git diff"
    Write-Host "  2. Commit: git add . && git commit -m 'feat: $TaskId'"
} else {
    Write-Host "  1. Review verification.md for failure details"
    Write-Host "  2. Fix issues manually or regenerate workpack"
    Write-Host "  3. Re-run: ./scripts/apply-diff.ps1 -TaskId $TaskId"
}
Write-Host ""

if (-not $MoveToCompleted -and $overallSuccess) {
    Write-Host "Tip: Run with -MoveToCompleted to archive the workpack" -ForegroundColor Gray
}
