<#
.SYNOPSIS
    workpack を使って claude -p でステートレス実装を実行

.DESCRIPTION
    workpack（5ファイル）を implementation-prompt.md と連結し、
    claude -p でステートレスに実行します。
    出力は unified diff 形式で output.diff に保存されます。

    再現性メタ（reproducibility.yaml）を自動生成し、
    guardrails.yaml の detection.pattern を機械的に全適用します。

.PARAMETER TaskId
    タスクID（例: T001-create-book-entity）

.PARAMETER DryRun
    実際に claude を呼び出さず、プロンプトのみ出力

.PARAMETER Model
    使用するモデル（デフォルト: claude-sonnet-4-20250514）

.PARAMETER Temperature
    温度パラメータ（デフォルト: 0）

.EXAMPLE
    ./scripts/run-implementation.ps1 -TaskId "T001-create-book-entity"

.EXAMPLE
    ./scripts/run-implementation.ps1 -TaskId "T001-create-book-entity" -DryRun
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$TaskId,

    [switch]$DryRun,

    [string]$Model = "claude-sonnet-4-20250514",

    [double]$Temperature = 0
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $ProjectRoot) { $ProjectRoot = (Get-Location).Path }

Write-Host "=== Stateless Implementation Runner ===" -ForegroundColor Cyan
Write-Host "Task ID: $TaskId"
Write-Host "Model: $Model"
Write-Host "Temperature: $Temperature"
Write-Host "Dry Run: $DryRun"
Write-Host ""

# ============================================================
# Phase 0: Validation
# ============================================================
Write-Host "[Phase 0] Validating workpack..." -ForegroundColor Yellow

$WorkpackDir = "$ProjectRoot/workpacks/active/$TaskId"
if (-not (Test-Path $WorkpackDir)) {
    throw "Workpack not found: $WorkpackDir. Run generate-workpack.ps1 first."
}

$requiredFiles = @(
    "task.md",
    "spec.extract.md",
    "policy.yaml",
    "guardrails.yaml",
    "repo.snapshot.md"
)

foreach ($file in $requiredFiles) {
    $filePath = "$WorkpackDir/$file"
    if (Test-Path $filePath) {
        Write-Host "  [OK] $file" -ForegroundColor Green
    } else {
        throw "Missing required file: $filePath"
    }
}

$PromptTemplate = "$ProjectRoot/workpacks/_templates/implementation-prompt.md"
if (-not (Test-Path $PromptTemplate)) {
    throw "Prompt template not found: $PromptTemplate"
}

Write-Host "[Phase 0] Validation complete." -ForegroundColor Green
Write-Host ""

# ============================================================
# Phase 0.5: Generate Reproducibility Meta
# ============================================================
Write-Host "[Phase 0.5] Generating reproducibility meta..." -ForegroundColor Yellow

# Get versions and hashes
$baseCommit = git rev-parse HEAD 2>$null
if (-not $baseCommit) { $baseCommit = "N/A" }

$claudeVersion = "N/A"
try {
    $claudeVersion = (claude --version 2>$null) -join " "
} catch {}

$promptTemplateHash = (Get-FileHash $PromptTemplate -Algorithm SHA256).Hash.Substring(0, 16)

$workpackHashes = @{}
foreach ($file in $requiredFiles) {
    $filePath = "$WorkpackDir/$file"
    $hash = (Get-FileHash $filePath -Algorithm SHA256).Hash.Substring(0, 16)
    $workpackHashes[$file] = $hash
}

$reproducibilityYaml = @"
# Reproducibility Meta for: $TaskId
# Generated at: $(Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")

execution:
  model: "$Model"
  temperature: $Temperature  # Note: CLI may not support --temperature; intended value only
  cli_version: "$claudeVersion"
  prompt_template_hash: "$promptTemplateHash"

repository:
  base_commit: "$baseCommit"
  working_dir_clean: $(if ((git status --porcelain 2>$null | Measure-Object).Count -eq 0) { "true" } else { "false" })

workpack_hashes:
  task.md: "$($workpackHashes['task.md'])"
  spec.extract.md: "$($workpackHashes['spec.extract.md'])"
  policy.yaml: "$($workpackHashes['policy.yaml'])"
  guardrails.yaml: "$($workpackHashes['guardrails.yaml'])"
  repo.snapshot.md: "$($workpackHashes['repo.snapshot.md'])"

# To reproduce this execution:
# 1. Checkout base_commit: git checkout $baseCommit
# 2. Ensure workpack files have matching hashes
# 3. Run: ./scripts/run-implementation.ps1 -TaskId "$TaskId" -Model "$Model" -Temperature $Temperature
# Note: Temperature is recorded for documentation but may not be passable via CLI
"@

$reproducibilityPath = "$WorkpackDir/reproducibility.yaml"
$reproducibilityYaml | Out-File -FilePath $reproducibilityPath -Encoding utf8
Write-Host "  Saved: reproducibility.yaml" -ForegroundColor Green
Write-Host "  Base commit: $baseCommit" -ForegroundColor Gray
Write-Host ""

# ============================================================
# Phase 1: Build Prompt
# ============================================================
Write-Host "[Phase 1] Building prompt..." -ForegroundColor Yellow

# Read template
$template = Get-Content $PromptTemplate -Raw

# Read workpack files
$taskMd = Get-Content "$WorkpackDir/task.md" -Raw
$specExtractMd = Get-Content "$WorkpackDir/spec.extract.md" -Raw
$policyYaml = Get-Content "$WorkpackDir/policy.yaml" -Raw
$guardrailsYaml = Get-Content "$WorkpackDir/guardrails.yaml" -Raw
$repoSnapshotMd = Get-Content "$WorkpackDir/repo.snapshot.md" -Raw

# Replace placeholders
$prompt = $template
$prompt = $prompt -replace '\{TASK_MD_CONTENT\}', $taskMd
$prompt = $prompt -replace '\{SPEC_EXTRACT_MD_CONTENT\}', $specExtractMd
$prompt = $prompt -replace '\{POLICY_YAML_CONTENT\}', $policyYaml
$prompt = $prompt -replace '\{GUARDRAILS_YAML_CONTENT\}', $guardrailsYaml
$prompt = $prompt -replace '\{REPO_SNAPSHOT_MD_CONTENT\}', $repoSnapshotMd

# Save assembled prompt for debugging
$assembledPromptPath = "$WorkpackDir/assembled-prompt.md"
$prompt | Out-File -FilePath $assembledPromptPath -Encoding utf8

Write-Host "  Prompt assembled: $assembledPromptPath" -ForegroundColor Green
Write-Host "  Prompt size: $($prompt.Length) characters" -ForegroundColor Gray
Write-Host ""

# ============================================================
# Phase 1.5: Parse Guardrails for Validation
# ============================================================
Write-Host "[Phase 1.5] Parsing guardrails for post-execution validation..." -ForegroundColor Yellow

# YAML parser for forbidden_actions AND common_forbidden_actions
# Both sections use the same structure, so we parse them identically
$forbiddenActions = @()

$inForbiddenSection = $false
$inExcludeInMultiline = $false
$currentAction = $null

foreach ($line in ($guardrailsYaml -split "`n")) {
    $trimmed = $line.Trim()

    # Match both forbidden_actions: and common_forbidden_actions:
    if ($trimmed -eq "forbidden_actions:" -or $trimmed -eq "common_forbidden_actions:") {
        $inForbiddenSection = $true
        $inExcludeInMultiline = $false
        continue
    }

    if ($inForbiddenSection) {
        # New top-level section starts (not a sub-key)
        if ($trimmed -match "^[a-z_]+:" -and $trimmed -notmatch "^(id|scope|forbidden|reason|severity|detection|pattern|exclude_in):") {
            # Save last action before switching
            if ($currentAction) {
                $forbiddenActions += $currentAction
                $currentAction = $null
            }
            $inForbiddenSection = $false
            $inExcludeInMultiline = $false
            continue
        }

        # New action item
        if ($trimmed -match "^- id:\s*[`"']?([^`"']+)[`"']?") {
            if ($currentAction) {
                $forbiddenActions += $currentAction
            }
            $currentAction = @{
                Id = $Matches[1]
                Scope = "*"
                Pattern = $null
                ExcludeIn = @()
                Description = ""
            }
            $inExcludeInMultiline = $false
        }

        if ($currentAction) {
            if ($trimmed -match "^scope:\s*[`"']?([^`"']+)[`"']?") {
                $currentAction.Scope = $Matches[1]
                $inExcludeInMultiline = $false
            }
            if ($trimmed -match "^forbidden:\s*[`"']?(.+)[`"']?$") {
                $currentAction.Description = $Matches[1]
                $inExcludeInMultiline = $false
            }
            if ($trimmed -match "^pattern:\s*[`"']?(.+)[`"']?$") {
                $currentAction.Pattern = $Matches[1]
                $inExcludeInMultiline = $false
            }
            # Inline exclude_in: ["a", "b"]
            if ($trimmed -match "^exclude_in:\s*\[(.+)\]") {
                $excludes = $Matches[1] -split "," | ForEach-Object { $_.Trim().Trim('"').Trim("'") }
                $currentAction.ExcludeIn = $excludes
                $inExcludeInMultiline = $false
            }
            # Multi-line exclude_in start
            elseif ($trimmed -match "^exclude_in:\s*$") {
                $currentAction.ExcludeIn = @()
                $inExcludeInMultiline = $true
            }
            # Multi-line exclude_in items (- "*.cs" or - *.cs)
            elseif ($inExcludeInMultiline -and $trimmed -match "^-\s*[`"']?([^`"']+)[`"']?$") {
                $currentAction.ExcludeIn += $Matches[1].Trim()
            }
            # End multi-line on other keys
            elseif ($inExcludeInMultiline -and $trimmed -match "^[a-z_]+:") {
                $inExcludeInMultiline = $false
            }
        }
    }
}

# Add last action
if ($currentAction) {
    $forbiddenActions += $currentAction
}

$validPatternCount = ($forbiddenActions | Where-Object { $_.Pattern }).Count
Write-Host "  Loaded $($forbiddenActions.Count) forbidden actions ($validPatternCount with detection patterns)" -ForegroundColor Green
Write-Host ""

# ============================================================
# Phase 2: Execute (or Dry Run)
# ============================================================
if ($DryRun) {
    Write-Host "[Phase 2] DRY RUN - Skipping claude execution" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Assembled prompt saved to: $assembledPromptPath" -ForegroundColor Cyan
    Write-Host "Reproducibility meta saved to: $reproducibilityPath" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "To execute manually:" -ForegroundColor Yellow
    Write-Host "  claude -p `"$assembledPromptPath`" --model $Model"
    Write-Host ""
    exit 0
}

Write-Host "[Phase 2] Executing claude -p..." -ForegroundColor Yellow
Write-Host "  This may take a few minutes..." -ForegroundColor Gray

# Check if claude CLI is available
$claudeCmd = Get-Command "claude" -ErrorAction SilentlyContinue
if (-not $claudeCmd) {
    throw "claude CLI not found. Please install Claude Code CLI."
}

# Execute claude
$outputPath = "$WorkpackDir/output.diff"
$startTime = Get-Date

try {
    # Note: claude -p reads from stdin or file
    # We'll pipe the prompt to claude
    # Temperature 0 is used for maximum determinism/reproducibility
    $claudeArgs = @("-p", "--model", $Model, "--output-format", "text")

    # Note: claude CLI may not support --temperature yet
    # If not supported, this will be ignored or cause an error
    # Uncomment when/if supported:
    # if ($Temperature -ne $null) {
    #     $claudeArgs += @("--temperature", $Temperature.ToString())
    # }

    Write-Host "  Command: claude $($claudeArgs -join ' ')" -ForegroundColor Gray
    $prompt | claude @claudeArgs 2>&1 | Out-File -FilePath $outputPath -Encoding utf8

    $endTime = Get-Date
    $duration = $endTime - $startTime

    Write-Host "  Execution complete in $($duration.TotalSeconds.ToString('F1')) seconds" -ForegroundColor Green

    # Update reproducibility with execution info
    $executionInfo = @"

execution_result:
  duration_seconds: $($duration.TotalSeconds.ToString('F1'))
  executed_at: "$(Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")"
  output_file: "output.diff"
"@
    Add-Content -Path $reproducibilityPath -Value $executionInfo
}
catch {
    Write-Host "  Error during execution: $_" -ForegroundColor Red
    throw
}

Write-Host ""

# ============================================================
# Phase 3: Validate Output (Guardrails-Driven)
# ============================================================
Write-Host "[Phase 3] Validating output against guardrails..." -ForegroundColor Yellow

if (-not (Test-Path $outputPath)) {
    throw "Output file not created: $outputPath"
}

$output = Get-Content $outputPath -Raw

# Check if output looks like a diff
if ($output -match '^---\s+a/' -or $output -match '^diff --git') {
    Write-Host "  [OK] Output appears to be a valid diff" -ForegroundColor Green
} else {
    Write-Host "  [WARNING] Output may not be a valid diff format" -ForegroundColor Yellow
    Write-Host "  First 200 characters:" -ForegroundColor Gray
    Write-Host "  $($output.Substring(0, [Math]::Min(200, $output.Length)))" -ForegroundColor Gray
}

# Helper: Convert wildcard to regex
# Design decision: SUFFIX MATCH (no ^ anchor, with $ anchor)
#   - "*Tests.cs"      -> ".*Tests\.cs$"     (matches: path/to/FooTests.cs)
#   - "*Behavior.cs"   -> ".*Behavior\.cs$"  (matches: src/Behaviors/LogBehavior.cs)
#   - "*.cs"           -> ".*\.cs$"          (matches: any .cs file)
#
# If full path matching is needed in future (e.g., "src/Tests/*"),
# consider adding ^ anchor or a separate function.
function Convert-WildcardToRegex {
    param([string]$Wildcard)
    # Escape regex special chars except * and ?
    $escaped = [regex]::Escape($Wildcard)
    # Convert wildcard patterns
    $escaped = $escaped -replace '\\\*', '.*'
    $escaped = $escaped -replace '\\\?', '.'
    # Suffix match: end anchor only (no start anchor)
    return "$escaped$"
}

# Helper: Check if file path matches any exclude pattern
function Test-ExcludeMatch {
    param([string]$FilePath, [string[]]$ExcludePatterns)
    foreach ($pattern in $ExcludePatterns) {
        if (-not $pattern) { continue }
        try {
            $regex = Convert-WildcardToRegex $pattern
            if ($FilePath -match $regex) {
                return $true
            }
        }
        catch {
            # Invalid pattern, skip
        }
    }
    return $false
}

# Split diff into per-file chunks
# Each chunk: { FilePath = "path/to/file.cs", AddedLines = "only +lines..." }
$diffChunks = @()
$currentChunk = $null
$currentAddedLines = @()

foreach ($line in ($output -split "`n")) {
    # New file starts with +++ b/path/to/file
    if ($line -match '^\+\+\+ b/(.+)$') {
        # Save previous chunk
        if ($currentChunk) {
            $currentChunk.AddedLines = $currentAddedLines -join "`n"
            $diffChunks += $currentChunk
        }
        $filePath = $Matches[1].Trim()
        # Skip deleted files (+++ b/dev/null)
        if ($filePath -eq "/dev/null" -or $filePath -eq "dev/null") {
            $currentChunk = $null
            $currentAddedLines = @()
        }
        else {
            $currentChunk = @{
                FilePath = $filePath
                AddedLines = ""
            }
            $currentAddedLines = @()
        }
    }
    # Collect only added lines (starts with + but not ++ or +++)
    elseif ($currentChunk -and $line -match '^\+[^+]') {
        $currentAddedLines += $line
    }
}
# Save last chunk
if ($currentChunk) {
    $currentChunk.AddedLines = $currentAddedLines -join "`n"
    $diffChunks += $currentChunk
}

Write-Host "  Parsed $($diffChunks.Count) file chunk(s) from diff" -ForegroundColor Gray

# Fallback: if no chunks parsed (malformed diff), check entire output
$useFallback = ($diffChunks.Count -eq 0)
if ($useFallback) {
    Write-Host "  [WARNING] No file chunks detected, using fallback (full output check)" -ForegroundColor Yellow
}

# Apply ALL forbidden_actions patterns from guardrails.yaml
$violations = @()
$checkedCount = 0

foreach ($action in $forbiddenActions) {
    if (-not $action.Pattern) {
        continue
    }

    $checkedCount++

    try {
        if ($useFallback) {
            # Fallback: check entire output (less precise, but better than nothing)
            if ($output -match $action.Pattern) {
                $violations += @{
                    Id = $action.Id
                    Description = $action.Description
                    Pattern = $action.Pattern
                    FilePath = "(fallback: unknown file)"
                }
            }
        }
        else {
            # Normal: check each file chunk separately (added lines only)
            foreach ($chunk in $diffChunks) {
                # Check if pattern matches in added lines only
                if ($chunk.AddedLines -match $action.Pattern) {
                    # Check if this file is excluded
                    $excluded = Test-ExcludeMatch -FilePath $chunk.FilePath -ExcludePatterns $action.ExcludeIn

                    if (-not $excluded) {
                        $violations += @{
                            Id = $action.Id
                            Description = $action.Description
                            Pattern = $action.Pattern
                            FilePath = $chunk.FilePath
                        }
                    }
                }
            }
        }
    }
    catch {
        Write-Host "  [WARNING] Invalid regex pattern in $($action.Id): $($action.Pattern)" -ForegroundColor Yellow
    }
}

Write-Host "  Checked $checkedCount detection patterns" -ForegroundColor Gray

if ($violations.Count -gt 0) {
    Write-Host "  [VIOLATION] $($violations.Count) guardrail violation(s) detected:" -ForegroundColor Red
    foreach ($v in $violations) {
        Write-Host "    [$($v.Id)] $($v.Description)" -ForegroundColor Red
        Write-Host "      File: $($v.FilePath)" -ForegroundColor Gray
        Write-Host "      Pattern: $($v.Pattern)" -ForegroundColor Gray
    }

    # Save violations report
    $violationsReport = @"
# Guardrail Violations Report
# Task: $TaskId
# Generated: $(Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")

## Violations Found: $($violations.Count)

$($violations | ForEach-Object {
@"

### $($_.Id)
- **File**: ``$($_.FilePath)``
- **Description**: $($_.Description)
- **Pattern**: ``$($_.Pattern)``
"@
})

## Action Required
Review output.diff and fix violations before applying.
"@
    $violationsPath = "$WorkpackDir/violations.md"
    $violationsReport | Out-File -FilePath $violationsPath -Encoding utf8
    Write-Host ""
    Write-Host "  Violations report saved to: $violationsPath" -ForegroundColor Yellow
} else {
    Write-Host "  [OK] No guardrail violations detected" -ForegroundColor Green

    # Remove old violations file if exists
    $violationsPath = "$WorkpackDir/violations.md"
    if (Test-Path $violationsPath) {
        Remove-Item $violationsPath
    }
}

# Append guardrail validation results to reproducibility.yaml
$guardrailInfo = @"

guardrail_validation:
  validated_at: "$(Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")"
  patterns_checked: $checkedCount
  file_chunks_parsed: $($diffChunks.Count)
  fallback_mode: $($useFallback.ToString().ToLower())
  violations_count: $($violations.Count)
  violations_detected: $($violations.Count -gt 0)
$(if ($violations.Count -gt 0) {
  "  violation_ids:"
  $violations | ForEach-Object { "    - $($_.Id)" }
})
"@
Add-Content -Path $reproducibilityPath -Value $guardrailInfo
Write-Host "  Appended guardrail results to: reproducibility.yaml" -ForegroundColor Gray

Write-Host ""

# ============================================================
# Summary
# ============================================================
Write-Host "=== Execution Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output saved to: $outputPath" -ForegroundColor Green
Write-Host "Reproducibility meta: $reproducibilityPath" -ForegroundColor Green
Write-Host ""

if ($violations.Count -gt 0) {
    Write-Host "[WARNING] Fix guardrail violations before applying!" -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Review the generated diff: $outputPath"
if ($violations.Count -gt 0) {
    Write-Host "  2. Fix violations listed in: $violationsPath" -ForegroundColor Yellow
    Write-Host "  3. Re-run this script after fixing workpack"
} else {
    Write-Host "  2. If satisfied, apply: ./scripts/apply-diff.ps1 -TaskId $TaskId"
    Write-Host "  3. If not, modify workpack and re-run this script"
}

# ============================================================
# Exit Code (for CI/automation)
# ============================================================
# Exit 1 if: violations detected OR diff format invalid
$exitCode = 0

if ($violations.Count -gt 0) {
    Write-Host ""
    Write-Host "[EXIT 1] Guardrail violations detected" -ForegroundColor Red
    $exitCode = 1
}

if ($diffChunks.Count -eq 0 -and -not $useFallback) {
    # No chunks and not in fallback = likely invalid diff
    Write-Host ""
    Write-Host "[EXIT 1] Invalid diff format (no file chunks detected)" -ForegroundColor Red
    $exitCode = 1
}

exit $exitCode
