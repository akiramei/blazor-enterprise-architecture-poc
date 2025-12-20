<#
.SYNOPSIS
    specs/ から workpack（自己完結型入力パッケージ）を生成

.DESCRIPTION
    Phase A（設計）の成果物から Phase B（実装）用の workpack を生成します。
    spec.yaml, manifest.yaml, guardrails.yaml から自動抽出を行い、
    自己完結型のパッケージを作成します。

    workpack は以下の5ファイルで構成されます：
    - task.md
    - spec.extract.md
    - policy.yaml
    - guardrails.yaml
    - repo.snapshot.md

.PARAMETER TaskId
    タスクID（例: T001-create-book-entity）

.PARAMETER SpecPath
    spec.yaml のパス（拡張子なし、例: specs/loan/LendBook）

.PARAMETER PatternId
    適用するパターンID（例: feature-create-entity）

.PARAMETER Force
    既存の workpack を上書きする

.EXAMPLE
    ./scripts/generate-workpack.ps1 -TaskId "T001-create-book-entity" -SpecPath "specs/loan/LendBook" -PatternId "feature-create-entity"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$TaskId,

    [Parameter(Mandatory=$true)]
    [string]$SpecPath,

    [string]$PatternId = "",

    [switch]$Force
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $ProjectRoot) { $ProjectRoot = (Get-Location).Path }

# パスの正規化
$SpecPath = $SpecPath -replace '\\', '/'
$SpecPath = $SpecPath.TrimEnd('/')

Write-Host "=== Workpack Generator ===" -ForegroundColor Cyan
Write-Host "Task ID: $TaskId"
Write-Host "Spec Path: $SpecPath"
Write-Host "Pattern ID: $(if ($PatternId) { $PatternId } else { '(auto-detect)' })"
Write-Host ""

# ============================================================
# Phase 0: Validation & File Discovery
# ============================================================
Write-Host "[Phase 0] Validating inputs and discovering files..." -ForegroundColor Yellow

# TaskId format check
if ($TaskId -notmatch '^T\d+-[a-z0-9-]+$') {
    Write-Warning "TaskId format warning: Expected T{number}-{description} (e.g., T001-create-book-entity)"
}

# Extract feature and slice from spec path
$pathParts = $SpecPath -split '/'
if ($pathParts.Count -lt 3) {
    throw "Invalid SpecPath format. Expected: specs/{feature}/{slice}"
}
$Feature = $pathParts[-2]
$Slice = $pathParts[-1]

Write-Host "  Feature: $Feature"
Write-Host "  Slice: $Slice"

# Discover files
$SpecYaml = "$ProjectRoot/$SpecPath.spec.yaml"
$GuardrailsYaml = "$ProjectRoot/specs/$Feature/$Slice.guardrails.yaml"
$ManifestYaml = "$ProjectRoot/manifests/$Feature/$Slice.manifest.yaml"
$DecisionsYaml = "$ProjectRoot/specs/$Feature/$Slice.decisions.yaml"
$PlanMd = "$ProjectRoot/specs/$Feature/$Slice.plan.md"

$files = @{
    "Spec" = @{ Path = $SpecYaml; Required = $true; Content = $null }
    "Guardrails" = @{ Path = $GuardrailsYaml; Required = $true; Content = $null }
    "Manifest" = @{ Path = $ManifestYaml; Required = $false; Content = $null }
    "Decisions" = @{ Path = $DecisionsYaml; Required = $false; Content = $null }
    "Plan" = @{ Path = $PlanMd; Required = $false; Content = $null }
}

$missingRequired = @()
foreach ($file in $files.GetEnumerator()) {
    if (Test-Path $file.Value.Path) {
        Write-Host "  [OK] $($file.Key): $($file.Value.Path)" -ForegroundColor Green
        $file.Value.Content = Get-Content $file.Value.Path -Raw
    } else {
        if ($file.Value.Required) {
            Write-Host "  [MISSING] $($file.Key): $($file.Value.Path)" -ForegroundColor Red
            $missingRequired += $file.Key
        } else {
            Write-Host "  [OPTIONAL] $($file.Key): $($file.Value.Path)" -ForegroundColor Yellow
        }
    }
}

if ($missingRequired.Count -gt 0) {
    throw "Missing required files: $($missingRequired -join ', '). Run speckit.specify and speckit.guardrails first."
}

# Check output directory
$OutputDir = "$ProjectRoot/workpacks/active/$TaskId"
if ((Test-Path $OutputDir) -and -not $Force) {
    throw "Workpack already exists: $OutputDir. Use -Force to overwrite."
}

Write-Host "[Phase 0] Validation complete." -ForegroundColor Green
Write-Host ""

# ============================================================
# Helper Functions for YAML Parsing
# ============================================================
function Extract-YamlValue {
    param([string]$Content, [string]$Key)
    if ($Content -match "(?m)^$Key\s*:\s*[`"']?(.+?)[`"']?\s*$") {
        return $Matches[1].Trim()
    }
    return $null
}

function Extract-YamlSection {
    param([string]$Content, [string]$SectionName)
    $lines = $Content -split "`n"
    $inSection = $false
    $sectionContent = @()
    $baseIndent = 0

    foreach ($line in $lines) {
        if ($line -match "^${SectionName}:\s*$") {
            $inSection = $true
            continue
        }

        if ($inSection) {
            if ($line -match "^(\s*)") {
                $currentIndent = $Matches[1].Length
            }

            if ($line -match "^\S" -and $line -notmatch "^\s*#" -and $line -notmatch "^\s*$") {
                # New top-level key, end section
                break
            }

            if ($line -match "^\s+" -or $line -match "^\s*$") {
                $sectionContent += $line
            }
        }
    }

    return ($sectionContent -join "`n").Trim()
}

function Extract-YamlList {
    param([string]$Content, [string]$ListName)
    $section = Extract-YamlSection -Content $Content -SectionName $ListName
    $items = @()
    foreach ($line in ($section -split "`n")) {
        if ($line -match "^\s*-\s*(.+)$") {
            $items += $Matches[1].Trim().Trim('"').Trim("'")
        }
    }
    return $items
}

# ============================================================
# Phase 1: Extract from Spec YAML
# ============================================================
Write-Host "[Phase 1] Extracting from spec.yaml..." -ForegroundColor Yellow

$specContent = $files["Spec"].Content

$specSummary = Extract-YamlValue -Content $specContent -Key "summary"
if (-not $specSummary) {
    $specSummary = "（spec.yaml から抽出してください）"
}

$specActor = Extract-YamlValue -Content $specContent -Key "actor"
if (-not $specActor) { $specActor = "User" }

$boundarySection = Extract-YamlSection -Content $specContent -SectionName "boundary"
$domainRulesSection = Extract-YamlSection -Content $specContent -SectionName "domain_rules"
$scenariosSection = Extract-YamlSection -Content $specContent -SectionName "scenarios"
$characteristicsList = Extract-YamlList -Content $specContent -ListName "characteristics"

Write-Host "  Summary: $(if ($specSummary.Length -gt 50) { $specSummary.Substring(0, 50) + '...' } else { $specSummary })"
Write-Host "  Actor: $specActor"
Write-Host "  Characteristics: $($characteristicsList.Count) items"
Write-Host ""

# ============================================================
# Phase 2: Extract from Manifest YAML (if exists)
# ============================================================
Write-Host "[Phase 2] Extracting from manifest.yaml..." -ForegroundColor Yellow

$manifestPatternId = $PatternId
$manifestPatterns = @()
$generationHints = @()

if ($files["Manifest"].Content) {
    $manifestContent = $files["Manifest"].Content

    # Extract primary pattern from catalog_binding
    $fromCatalogSection = Extract-YamlSection -Content $manifestContent -SectionName "from_catalog"
    if ($fromCatalogSection -match "id:\s*[`"']?([^`"'\n]+)[`"']?") {
        if (-not $manifestPatternId) {
            $manifestPatternId = $Matches[1]
        }
    }

    # Extract all pattern IDs
    $patternMatches = [regex]::Matches($manifestContent, "id:\s*[`"']?([a-z-]+)[`"']?")
    foreach ($match in $patternMatches) {
        $manifestPatterns += $match.Groups[1].Value
    }

    # Extract generation hints
    $notesSection = Extract-YamlSection -Content $manifestContent -SectionName "notes"
    if ($notesSection) {
        $generationHints = $notesSection -split "`n" | Where-Object { $_ -match "^\s*-\s*" } | ForEach-Object { $_ -replace "^\s*-\s*", "" }
    }

    Write-Host "  Primary pattern: $manifestPatternId"
    Write-Host "  All patterns: $($manifestPatterns.Count) items"
    Write-Host "  Generation hints: $($generationHints.Count) items"
} else {
    Write-Host "  [SKIP] manifest.yaml not found, using defaults"
    if (-not $manifestPatternId) {
        $manifestPatternId = "feature-create-entity"
    }
}

Write-Host ""

# ============================================================
# Phase 3: Create Directory Structure
# ============================================================
Write-Host "[Phase 3] Creating workpack directory..." -ForegroundColor Yellow

if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

Write-Host "  Created: $OutputDir" -ForegroundColor Green
Write-Host ""

# ============================================================
# Phase 4: Generate task.md
# ============================================================
Write-Host "[Phase 4] Generating task.md..." -ForegroundColor Yellow

$TaskMd = @"
# Task: $TaskId

## Meta

| Key | Value |
|-----|-------|
| ID | $TaskId |
| Feature | $Feature |
| Slice | $Slice |
| Parent Plan | specs/$Feature/$Slice.plan.md |
| Pattern | $manifestPatternId |
| Status | pending |
| Created | $(Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ") |

## Objective

$specSummary

## Scope

### In Scope

- $manifestPatternId パターンに基づく実装
- $Slice 機能の Command/Handler/Validator

### Out of Scope

- 他の機能スライスへの変更
- UI コンポーネント（別タスクで実装）

## Acceptance Criteria

- [ ] AC-001: Command クラスが ICommand<Result<T>> を実装している
- [ ] AC-002: Handler が SaveChangesAsync を呼んでいない
- [ ] AC-003: Validator が FluentValidation で実装されている
- [ ] AC-004: ビルドが成功する
- [ ] AC-005: 既存テストが通る

## Dependencies

| Type | Task ID | Description |
|------|---------|-------------|
| Depends on | - | - |
| Blocks | - | - |

## Expected Output

### Files to Create

| Path | Purpose |
|------|---------|
| src/Application/Features/$Feature/$Slice/${Slice}Command.cs | Command 定義 |
| src/Application/Features/$Feature/$Slice/${Slice}CommandHandler.cs | Handler 実装 |
| src/Application/Features/$Feature/$Slice/${Slice}CommandValidator.cs | Validator 実装 |

### Files to Modify

| Path | Change Type |
|------|-------------|
| src/Application/DependencyInjection.cs | サービス登録追加 |

## Guardrail References

| ID | Rule | Severity |
|----|------|----------|
| FA-COMMON-001 | SaveChangesAsync in Handler 禁止 | critical |
| FA-COMMON-002 | BoundaryService に業務ロジック禁止 | high |
| FA-COMMON-003 | throw Exception 禁止 | high |

## Notes

Generated by generate-workpack.ps1 from specs/$Feature/$Slice.spec.yaml
"@

$TaskMd | Out-File -FilePath "$OutputDir/task.md" -Encoding utf8
Write-Host "  Created: task.md" -ForegroundColor Green

# ============================================================
# Phase 5: Generate spec.extract.md
# ============================================================
Write-Host "[Phase 5] Generating spec.extract.md..." -ForegroundColor Yellow

# Read pattern constraints if pattern file exists
$patternConstraints = @(
    "ICommand<Result<T>> を使用する",
    "SaveChangesAsync は Handler 内で呼ばない",
    "Result<T> パターンでエラーを返す"
)

$patternFile = "$ProjectRoot/catalog/patterns/$manifestPatternId.yaml"
if (Test-Path $patternFile) {
    $patternContent = Get-Content $patternFile -Raw
    $mustReadChecklist = Extract-YamlSection -Content $patternContent -SectionName "must_read_checklist"
    if ($mustReadChecklist) {
        $patternConstraints = $mustReadChecklist -split "`n" | Where-Object { $_ -match "^\s*-\s*" } | ForEach-Object { $_ -replace "^\s*-\s*", "" }
    }
}

$SpecExtractMd = @"
# Spec Extract for: $TaskId

## Source

| Document | Path |
|----------|------|
| Spec | specs/$Feature/$Slice.spec.yaml |
| Plan | specs/$Feature/$Slice.plan.md |
| Decisions | specs/$Feature/$Slice.decisions.yaml |
| Manifest | manifests/$Feature/$Slice.manifest.yaml |

---

## Extracted Context

### Summary

$specSummary

### Actor

$specActor

---

## Boundary Design

$boundarySection

---

## Domain Rules

$domainRulesSection

---

## Scenarios

$scenariosSection

---

## Characteristics

$(if ($characteristicsList.Count -gt 0) { $characteristicsList | ForEach-Object { "- $_" } | Out-String } else { "- (none specified)" })

---

## Catalog Pattern Reference

### Pattern

| Key | Value |
|-----|-------|
| Pattern ID | $manifestPatternId |
| YAML | catalog/patterns/$manifestPatternId.yaml |

### Key Constraints (from must_read_checklist)

$($patternConstraints | ForEach-Object { "> $_`n" } | Out-String)

---

## Generation Hints (from manifest.yaml)

$(if ($generationHints.Count -gt 0) { $generationHints | ForEach-Object { "- $_" } | Out-String } else { "- SaveChangesAsync は Handler 内で呼ばない`n- Result<T> パターンを使用`n- ICommand<Result<T>> を使用" })

---

## Skill Knowledge Embedding

### From vsa-boundary-modeler

- 業務ロジックは Entity が持つ
- BoundaryService は委譲のみ
- Entity.CanXxx() は BoundaryDecision を返す

### From vsa-implementation-guard

- Handler 内で SaveChangesAsync() を呼ばない
- Result<T> パターンを使用
- ICommand<Result<T>> を使用（IRequest<T> 直接使用禁止）
"@

$SpecExtractMd | Out-File -FilePath "$OutputDir/spec.extract.md" -Encoding utf8
Write-Host "  Created: spec.extract.md" -ForegroundColor Green

# ============================================================
# Phase 6: Generate policy.yaml
# ============================================================
Write-Host "[Phase 6] Generating policy.yaml..." -ForegroundColor Yellow

# Extract decisions if exists
$coreValues = "  # No decisions.yaml found"
if ($files["Decisions"].Content) {
    $coreValues = $files["Decisions"].Content -replace "(?m)^", "  # "
}

$PolicyYaml = @"
# Policy for: $TaskId
# Generated from: decisions.yaml + manifest.yaml

meta:
  task_id: "$TaskId"
  feature: "$Feature"
  slice: "$Slice"
  generated_from:
    spec: "specs/$Feature/$Slice.spec.yaml"
    decisions: "specs/$Feature/$Slice.decisions.yaml"
    manifest: "manifests/$Feature/$Slice.manifest.yaml"
  generated_at: "$(Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")"

# ============================================================
# Layer 1: Core Discretization (from decisions.yaml)
# ============================================================
core_values:
$coreValues

# ============================================================
# Pattern Bindings (from manifest.yaml catalog_binding)
# ============================================================
patterns:
  primary:
    id: "$manifestPatternId"
    yaml: "catalog/patterns/$manifestPatternId.yaml"
    constraints:
$($patternConstraints | ForEach-Object { "      - `"$_`"" } | Out-String)
  supporting:
$(if ($manifestPatterns.Count -gt 1) {
    $manifestPatterns | Where-Object { $_ -ne $manifestPatternId } | ForEach-Object { "    - id: `"$_`"`n      yaml: `"catalog/patterns/$_.yaml`"" } | Out-String
} else {
    "    # No supporting patterns"
})

# ============================================================
# Non-Creative Bindings (from manifest.yaml)
# ============================================================
non_creative:
  command_structure:
    provider: "$manifestPatternId"
    notes: "Command + Handler + Validator の構造"

  validation_pipeline:
    provider: "validation-behavior"
    notes: "FluentValidation による自動検証"

  transaction_management:
    provider: "transaction-behavior"
    notes: "SaveChangesAsync は Handler 内で呼ばない"

  result_pattern:
    provider: "result-pattern"
    notes: "すべての戻り値は Result<T>"

# ============================================================
# Generation Notes (CRITICAL)
# ============================================================
generation_notes:
  must_do:
    - "ICommand<Result<T>> を使用する"
    - "Result<T> パターンでエラーを返す"
    - "サービスのライフタイムは Scoped"

  must_not_do:
    - "Handler 内で SaveChangesAsync() を呼ばない"
    - "BoundaryService に業務ロジック（if文）を書かない"
    - "例外を throw してエラーを伝播しない"

# ============================================================
# Characteristics (from spec.yaml)
# ============================================================
characteristics:
$(if ($characteristicsList.Count -gt 0) { $characteristicsList | ForEach-Object { "  - `"$_`"" } | Out-String } else { "  - `"op:mutates-state`"`n  - `"xcut:validation`"`n  - `"xcut:transaction`"" })
"@

$PolicyYaml | Out-File -FilePath "$OutputDir/policy.yaml" -Encoding utf8
Write-Host "  Created: policy.yaml" -ForegroundColor Green

# ============================================================
# Phase 7: Copy and Enhance Guardrails
# ============================================================
Write-Host "[Phase 7] Generating guardrails.yaml..." -ForegroundColor Yellow

# Start with the original guardrails content
$guardrailsContent = $files["Guardrails"].Content

# Add meta header
$GuardrailsYamlContent = @"
# Guardrails for: $TaskId
# Copied from: specs/$Feature/$Slice.guardrails.yaml
# Enhanced at: $(Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")

meta:
  task_id: "$TaskId"
  feature: "$Feature"
  slice: "$Slice"
  source: "specs/$Feature/$Slice.guardrails.yaml"

# --- Original Content Below ---

$guardrailsContent

# --- Common Guardrails (Always Applied) ---

common_forbidden_actions:
  - id: "FA-COMMON-001"
    scope: "*Handler.cs"
    forbidden: "SaveChangesAsync() を Handler 内で呼ぶ"
    reason: "TransactionBehavior が自動実行する"
    severity: "critical"
    detection:
      pattern: "\\.SaveChangesAsync\\("
      exclude_in: ["*Behavior.cs", "*DbContext.cs"]

  - id: "FA-COMMON-002"
    scope: "*BoundaryService.cs"
    forbidden: "業務ロジック（if文での状態判定）を書く"
    reason: "Entity.CanXxx() に委譲すべき"
    severity: "high"
    detection:
      pattern: "if\\s*\\([^)]*Status|State"

  - id: "FA-COMMON-003"
    scope: "*.cs"
    forbidden: "throw new で例外を投げてエラーを伝播"
    reason: "Result<T> パターンを使用すべき"
    severity: "high"
    detection:
      pattern: "throw new \\w+Exception"
      exclude_in: ["*Tests.cs"]
"@

$GuardrailsYamlContent | Out-File -FilePath "$OutputDir/guardrails.yaml" -Encoding utf8
Write-Host "  Created: guardrails.yaml" -ForegroundColor Green

# ============================================================
# Phase 8: Generate repo.snapshot.md
# ============================================================
Write-Host "[Phase 8] Generating repo.snapshot.md..." -ForegroundColor Yellow

# Find reference implementations
$referenceHandlers = @()
$handlersPath = "$ProjectRoot/src/Application/Features"
if (Test-Path $handlersPath) {
    $handlers = Get-ChildItem -Path $handlersPath -Recurse -Filter "*Handler.cs" | Select-Object -First 2
    foreach ($handler in $handlers) {
        $referenceHandlers += @{
            Path = $handler.FullName.Replace($ProjectRoot, "").TrimStart("/\")
            Content = (Get-Content $handler.FullName -Raw | Select-String -Pattern "public .* Handle\(" -Context 0,15 | Out-String)
        }
    }
}

$baseCommit = git rev-parse HEAD 2>$null
if (-not $baseCommit) { $baseCommit = "N/A" }

$RepoSnapshotMd = @"
# Repository Snapshot for: $TaskId

## Meta

| Key | Value |
|-----|-------|
| Task ID | $TaskId |
| Snapshot Date | $(Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ") |
| Base Commit | $baseCommit |

---

## Project Structure Context

``````
src/
├── Kernel/                           # DDD基盤
│   ├── Entity.cs
│   ├── ValueObject.cs
│   └── AggregateRoot.cs
│
├── Domain/                           # BC固有ドメイン
│   └── {BoundedContext}/
│       └── {Aggregate}/
│
├── Shared/
│   ├── Application/                  # ICommand, IQuery, Result<T>
│   └── Infrastructure/               # Behaviors
│
└── Application/
    ├── Features/$Feature/            # VSA機能スライス
    │   └── $Slice/                   # ★ 作成/変更対象
    │
    └── Infrastructure/               # Repository
``````

---

## Reference Implementations

$(if ($referenceHandlers.Count -gt 0) {
    $referenceHandlers | ForEach-Object {
@"
### $($_.Path)

``````csharp
$($_.Content.Trim())
``````

"@
    } | Out-String
} else {
@"
### No existing handlers found

Please refer to catalog/patterns/$manifestPatternId.yaml for implementation patterns.
"@
})

---

## Namespace Conventions

| Layer | Namespace Pattern |
|-------|-------------------|
| Domain Entity | VSASample.Domain.{BC}.{Aggregate} |
| Command/Handler | VSASample.Application.Features.$Feature.$Slice |
| Repository Interface | VSASample.Domain.{BC}.{Aggregate} |
| Repository Impl | VSASample.Application.Infrastructure.{BC} |

---

## Files to Create

| Path | Template |
|------|----------|
| src/Application/Features/$Feature/$Slice/${Slice}Command.cs | Command |
| src/Application/Features/$Feature/$Slice/${Slice}CommandHandler.cs | Handler |
| src/Application/Features/$Feature/$Slice/${Slice}CommandValidator.cs | Validator |

---

## Important Notes

- Base commit: $baseCommit
- Pattern: $manifestPatternId
- This snapshot was auto-generated. Review and update if needed.
"@

$RepoSnapshotMd | Out-File -FilePath "$OutputDir/repo.snapshot.md" -Encoding utf8
Write-Host "  Created: repo.snapshot.md" -ForegroundColor Green
Write-Host ""

# ============================================================
# Summary
# ============================================================
Write-Host "=== Workpack Generation Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output Directory: $OutputDir" -ForegroundColor Green
Write-Host ""
Write-Host "Generated Files:" -ForegroundColor Yellow
Get-ChildItem $OutputDir | ForEach-Object { Write-Host "  - $($_.Name)" }
Write-Host ""

# Extraction summary
Write-Host "Extraction Summary:" -ForegroundColor Yellow
Write-Host "  Spec summary: $(if ($specSummary) { 'Extracted' } else { 'Placeholder' })"
Write-Host "  Boundary section: $(if ($boundarySection) { 'Extracted' } else { 'Empty' })"
Write-Host "  Domain rules: $(if ($domainRulesSection) { 'Extracted' } else { 'Empty' })"
Write-Host "  Pattern ID: $manifestPatternId"
Write-Host "  Pattern constraints: $($patternConstraints.Count) items"
Write-Host "  Characteristics: $($characteristicsList.Count) items"
Write-Host "  Reference handlers: $($referenceHandlers.Count) found"
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Review generated files, especially spec.extract.md and repo.snapshot.md"
Write-Host "  2. Fill in any remaining placeholders"
Write-Host "  3. Run: ./scripts/run-implementation.ps1 -TaskId $TaskId"
