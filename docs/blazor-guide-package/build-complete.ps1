# Blazor Architecture Guide - 統合版自動生成スクリプト
# 章別ファイルから BLAZOR_ARCHITECTURE_GUIDE_COMPLETE.md を生成

$ErrorActionPreference = "Stop"
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "=== Blazor Architecture Guide 統合版生成 ===" -ForegroundColor Cyan

# 出力ファイルパス
$outputFile = Join-Path $PSScriptRoot "BLAZOR_ARCHITECTURE_GUIDE_COMPLETE.md"
$docsDir = Join-Path $PSScriptRoot "docs"

# 章ファイルのリスト（00_README.mdは除外）
$chapters = @(
    "01_イントロダクション.md",
    "02_このプロジェクトについて.md",
    "03_アーキテクチャ概要.md",
    "04_採用技術とパターン.md",
    "05_パターンカタログ一覧.md",
    "06_全体アーキテクチャ図.md",
    "07_レイヤー構成と責務.md",
    "08_具体例_商品管理機能.md",
    "09_UI層の詳細設計.md",
    "10_Application層の詳細設計.md",
    "11_Domain層の詳細設計.md",
    "12_Infrastructure層の詳細設計.md",
    "13_信頼性パターン.md",
    "14_パフォーマンス最適化.md",
    "15_テスト戦略.md",
    "16_ベストプラクティス.md",
    "17_まとめ.md",
    "18_3層アーキテクチャからの移行ガイド.md",
    "19_AIへの実装ガイド.md"
)

# ヘッダー作成
$header = @"
# Blazor Enterprise Architecture Guide - 完全版

**Version**: 2.1.2 (自動生成版)
**生成日**: $(Get-Date -Format "yyyy年MM月dd日 HH:mm:ss")
**章数**: $($chapters.Count)章

---

## 📋 目次

"@

# 目次を生成
$toc = @()
foreach ($chapter in $chapters) {
    $chapterPath = Join-Path $docsDir $chapter
    if (Test-Path $chapterPath) {
        # 最初の # 行を取得してタイトルとする
        $title = Get-Content $chapterPath -Encoding UTF8 |
                 Where-Object { $_ -match '^#\s+(.+)' } |
                 Select-Object -First 1
        if ($title -match '^#\s+(.+)') {
            $titleText = $matches[1]
            $toc += "- $titleText"
        }
    }
}

$header += ($toc -join "`n")
$header += "`n`n---`n`n"

Write-Host "ヘッダーと目次を生成しました" -ForegroundColor Green

# 統合版を生成
$content = @($header)

foreach ($chapter in $chapters) {
    $chapterPath = Join-Path $docsDir $chapter

    if (Test-Path $chapterPath) {
        Write-Host "処理中: $chapter" -ForegroundColor Yellow

        # ファイル内容を読み込み
        $chapterContent = Get-Content $chapterPath -Encoding UTF8 -Raw

        # 「← 目次に戻る」リンクを除去
        $chapterContent = $chapterContent -replace '\[←\s*目次に戻る\]\(00_README\.md\)', ''

        # 章の区切りを追加
        $content += "`n`n---`n`n"
        $content += $chapterContent
        $content += "`n"
    } else {
        Write-Host "警告: $chapter が見つかりません" -ForegroundColor Red
    }
}

# ファイルに書き出し（UTF-8 BOM付き）
$utf8WithBom = New-Object System.Text.UTF8Encoding $true
[System.IO.File]::WriteAllText($outputFile, ($content -join ""), $utf8WithBom)

Write-Host ""
Write-Host "=== 完了 ===" -ForegroundColor Cyan
Write-Host "出力: $outputFile" -ForegroundColor Green
$fileSize = [math]::Round((Get-Item $outputFile).Length / 1KB, 1)
Write-Host "ファイルサイズ: $fileSize KB" -ForegroundColor Green
Write-Host ""
Write-Host "統合版が正常に生成されました" -ForegroundColor Green
