# Pattern Scaffolder - PoC
# パターンマニフェストに基づいてコードを生成・配線するツール

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("apply", "validate", "list")]
    [string]$Command = "validate",

    [Parameter(Mandatory=$false)]
    [string]$ManifestPath = ".\patterns.manifest.json"
)

# カラー出力
function Write-Success { param($Message) Write-Host "✓ $Message" -ForegroundColor Green }
function Write-Error { param($Message) Write-Host "✗ $Message" -ForegroundColor Red }
function Write-Info { param($Message) Write-Host "ℹ $Message" -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host "⚠ $Message" -ForegroundColor Yellow }

# マニフェストを読み込む
function Read-Manifest {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        Write-Error "マニフェストファイルが見つかりません: $Path"
        exit 1
    }

    try {
        $manifest = Get-Content $Path -Raw | ConvertFrom-Json
        Write-Success "マニフェストを読み込みました: $Path"
        return $manifest
    }
    catch {
        Write-Error "マニフェストの読み込みに失敗しました: $_"
        exit 1
    }
}

# カタログインデックスを取得
function Get-CatalogIndex {
    param([string]$CatalogUrl)

    Write-Info "カタログインデックスを取得中: $CatalogUrl"

    # github: プレフィックスを実際のURLに変換
    if ($CatalogUrl -match "^github:(.+)/(.+)/(.+)@(.+)$") {
        $org = $matches[1]
        $repo = $matches[2]
        $path = $matches[3]
        $tag = $matches[4]
        $CatalogUrl = "https://raw.githubusercontent.com/$org/$repo/$tag/$path"
    }

    try {
        $response = Invoke-RestMethod -Uri $CatalogUrl -Method Get
        Write-Success "カタログインデックスを取得しました"
        return $response
    }
    catch {
        Write-Error "カタログインデックスの取得に失敗しました: $_"
        exit 1
    }
}

# パターン定義を取得
function Get-PatternDefinition {
    param(
        [string]$CatalogUrl,
        [string]$PatternFile
    )

    # ベースURLを取得
    $baseUrl = $CatalogUrl -replace "/[^/]+$", ""
    $patternUrl = "$baseUrl/$PatternFile"

    Write-Info "パターン定義を取得中: $patternUrl"

    try {
        $response = Invoke-RestMethod -Uri $patternUrl -Method Get
        return $response
    }
    catch {
        Write-Error "パターン定義の取得に失敗しました: $_"
        return $null
    }
}

# マニフェストを検証
function Validate-Manifest {
    param($Manifest, $CatalogIndex)

    Write-Info "マニフェストを検証中..."

    $errors = @()
    $warnings = @()

    # 選択されたパターンがカタログに存在するか確認
    foreach ($selected in $Manifest.selected_patterns) {
        $pattern = $CatalogIndex.patterns | Where-Object { $_.id -eq $selected.id }

        if (-not $pattern) {
            $errors += "パターン '$($selected.id)' がカタログに存在しません"
            continue
        }

        # バージョンチェック
        if ($pattern.version -ne $selected.version) {
            $warnings += "パターン '$($selected.id)' のバージョンが異なります (カタログ: $($pattern.version), マニフェスト: $($selected.version))"
        }

        # 安定性チェック
        if ($pattern.stability -eq "beta") {
            $warnings += "パターン '$($selected.id)' はベータ版です"
        }
        elseif ($pattern.stability -eq "alpha") {
            $warnings += "パターン '$($selected.id)' はアルファ版です（本番環境では非推奨）"
        }
    }

    # 実行順序の検証（Behaviors）
    if ($Manifest.assembly_order) {
        $behaviorPatterns = $Manifest.selected_patterns | Where-Object {
            $pattern = $CatalogIndex.patterns | Where-Object { $_.id -eq $_.id }
            $pattern.category -eq "pipeline-behavior"
        }

        $orderedIds = $Manifest.assembly_order
        $selectedBehaviorIds = $behaviorPatterns | ForEach-Object { $_.id }

        foreach ($behaviorId in $selectedBehaviorIds) {
            if ($behaviorId -notin $orderedIds) {
                $errors += "Behavior '$behaviorId' が assembly_order に含まれていません"
            }
        }
    }

    # 結果表示
    if ($errors.Count -gt 0) {
        Write-Error "検証エラー:"
        $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    }

    if ($warnings.Count -gt 0) {
        Write-Warning "警告:"
        $warnings | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
    }

    if ($errors.Count -eq 0) {
        Write-Success "マニフェストは有効です"
        return $true
    }
    else {
        return $false
    }
}

# パターンを一覧表示
function List-Patterns {
    param($Manifest, $CatalogIndex)

    Write-Host ""
    Write-Host "選択されたパターン:" -ForegroundColor Cyan
    Write-Host "=" * 80

    foreach ($selected in $Manifest.selected_patterns) {
        $pattern = $CatalogIndex.patterns | Where-Object { $_.id -eq $selected.id }

        if ($pattern) {
            $statusIcon = if ($selected.config.enabled) { "✓" } else { "✗" }
            $stabilityColor = switch ($pattern.stability) {
                "stable" { "Green" }
                "beta" { "Yellow" }
                "alpha" { "Red" }
                default { "White" }
            }

            Write-Host ""
            Write-Host "  $statusIcon " -NoNewline
            Write-Host "$($pattern.name) " -ForegroundColor White -NoNewline
            Write-Host "[$($pattern.stability)]" -ForegroundColor $stabilityColor
            Write-Host "     ID: $($pattern.id)" -ForegroundColor Gray
            Write-Host "     バージョン: $($selected.version)" -ForegroundColor Gray
            Write-Host "     モード: $($selected.mode)" -ForegroundColor Gray
            Write-Host "     $($pattern.intent)" -ForegroundColor Gray
        }
    }

    Write-Host ""
    Write-Host "=" * 80
    Write-Host ""
    Write-Host "実行順序 (Behaviors):" -ForegroundColor Cyan
    if ($Manifest.assembly_order) {
        $Manifest.assembly_order | ForEach-Object {
            Write-Host "  → $_"
        }
    }
    else {
        Write-Host "  (未指定)"
    }
    Write-Host ""
}

# パターンを適用（PoC版 - 実際の生成は未実装）
function Apply-Patterns {
    param($Manifest, $CatalogIndex)

    Write-Info "パターンを適用中..."

    foreach ($selected in $Manifest.selected_patterns) {
        if (-not $selected.config.enabled) {
            Write-Host "  ⊘ $($selected.id) (無効)" -ForegroundColor Gray
            continue
        }

        $pattern = $CatalogIndex.patterns | Where-Object { $_.id -eq $selected.id }

        if ($selected.mode -eq "package") {
            Write-Info "  [package] $($pattern.name)"
            # TODO: NuGet パッケージの追加
        }
        elseif ($selected.mode -eq "copy") {
            Write-Info "  [copy] $($pattern.name)"
            # TODO: テンプレートのコピー
        }
    }

    Write-Warning "PoC版のため、実際のコード生成は未実装です"
    Write-Info "次のステップ:"
    Write-Host "  1. NuGet パッケージの追加"
    Write-Host "  2. テンプレートのコピー"
    Write-Host "  3. DI 配線の自動生成"
}

# メイン処理
Write-Host ""
Write-Host "Pattern Scaffolder - PoC" -ForegroundColor Magenta
Write-Host "=" * 80
Write-Host ""

$manifest = Read-Manifest -Path $ManifestPath
$catalogIndex = Get-CatalogIndex -CatalogUrl $manifest.catalog_index

switch ($Command) {
    "validate" {
        $isValid = Validate-Manifest -Manifest $manifest -CatalogIndex $catalogIndex
        exit $(if ($isValid) { 0 } else { 1 })
    }
    "list" {
        List-Patterns -Manifest $manifest -CatalogIndex $catalogIndex
    }
    "apply" {
        $isValid = Validate-Manifest -Manifest $manifest -CatalogIndex $catalogIndex
        if ($isValid) {
            Apply-Patterns -Manifest $manifest -CatalogIndex $catalogIndex
        }
        else {
            Write-Error "マニフェストが無効なため、適用を中止しました"
            exit 1
        }
    }
}

Write-Host ""
