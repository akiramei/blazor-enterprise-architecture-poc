# Catalog Validation Script
# カタログの整合性を検証する

Write-Host ""
Write-Host "Catalog Validation" -ForegroundColor Magenta
Write-Host "=" * 80
Write-Host ""

$errors = @()
$warnings = @()

# catalog/index.json を読み込む
if (-not (Test-Path "./catalog/index.json")) {
    Write-Error "catalog/index.json が見つかりません"
    exit 1
}

$catalogIndex = Get-Content "./catalog/index.json" -Raw | ConvertFrom-Json
Write-Host "✓ catalog/index.json を読み込みました" -ForegroundColor Green

# バージョンチェック
if (-not $catalogIndex.version) {
    $errors += "catalog/index.json に version フィールドがありません"
}
else {
    Write-Host "  バージョン: $($catalogIndex.version)" -ForegroundColor Gray
}

# パターンファイルの存在チェック
Write-Host ""
Write-Host "パターンファイルを検証中..." -ForegroundColor Cyan

foreach ($pattern in $catalogIndex.patterns) {
    $filePath = "./catalog/$($pattern.file)"

    if (-not (Test-Path $filePath)) {
        $errors += "パターンファイルが見つかりません: $filePath"
        Write-Host "  ✗ $($pattern.id)" -ForegroundColor Red
        continue
    }

    # YAMLファイルを読み込む（簡易チェック）
    $content = Get-Content $filePath -Raw

    # 必須フィールドのチェック
    $requiredFields = @("id:", "version:", "name:", "category:", "intent:")
    $missingFields = @()

    foreach ($field in $requiredFields) {
        if ($content -notmatch $field) {
            $missingFields += $field.TrimEnd(':')
        }
    }

    if ($missingFields.Count -gt 0) {
        $errors += "パターン $($pattern.id) に必須フィールドが不足: $($missingFields -join ', ')"
        Write-Host "  ✗ $($pattern.id) (必須フィールド不足)" -ForegroundColor Red
        continue
    }

    # バージョンの一致チェック
    if ($content -match "version:\s*([0-9.]+)") {
        $fileVersion = $matches[1]
        if ($fileVersion -ne $pattern.version) {
            $warnings += "パターン $($pattern.id) のバージョンが不一致 (index: $($pattern.version), file: $fileVersion)"
        }
    }

    Write-Host "  ✓ $($pattern.id)" -ForegroundColor Green
}

# 推奨組み合わせの検証
Write-Host ""
Write-Host "推奨組み合わせを検証中..." -ForegroundColor Cyan

if ($catalogIndex.recommended_combinations) {
    foreach ($combo in $catalogIndex.recommended_combinations) {
        Write-Host "  → $($combo.name)" -ForegroundColor Gray

        foreach ($patternId in $combo.patterns) {
            $pattern = $catalogIndex.patterns | Where-Object { $_.id -eq $patternId }

            if (-not $pattern) {
                $errors += "推奨組み合わせ '$($combo.name)' に存在しないパターン: $patternId"
            }
        }
    }
}

# 結果表示
Write-Host ""
Write-Host "=" * 80

if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "エラー:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
}

if ($warnings.Count -gt 0) {
    Write-Host ""
    Write-Host "警告:" -ForegroundColor Yellow
    $warnings | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}

Write-Host ""

if ($errors.Count -eq 0) {
    Write-Host "✓ カタログは有効です" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "✗ カタログに問題があります" -ForegroundColor Red
    exit 1
}
