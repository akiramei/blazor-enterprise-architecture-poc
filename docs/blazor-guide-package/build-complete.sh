#!/bin/bash
# Blazor Architecture Guide - 統合版自動生成スクリプト

set -e

echo "=== Blazor Architecture Guide 統合版生成 ==="

# 出力ファイル
OUTPUT_FILE="BLAZOR_ARCHITECTURE_GUIDE_COMPLETE.md"
DOCS_DIR="docs"

# 章ファイルのリスト（00_README.mdは除外）
CHAPTERS=(
    "01_イントロダクション.md"
    "02_このプロジェクトについて.md"
    "03_アーキテクチャ概要.md"
    "04_採用技術とパターン.md"
    "05_パターンカタログ一覧.md"
    "06_全体アーキテクチャ図.md"
    "07_レイヤー構成と責務.md"
    "08_具体例_商品管理機能.md"
    "09_UI層の詳細設計.md"
    "10_Application層の詳細設計.md"
    "11_Domain層の詳細設計.md"
    "12_Infrastructure層の詳細設計.md"
    "13_信頼性パターン.md"
    "14_パフォーマンス最適化.md"
    "15_テスト戦略.md"
    "16_ベストプラクティス.md"
    "17_まとめ.md"
    "18_3層アーキテクチャからの移行ガイド.md"
    "19_AIへの実装ガイド.md"
)

# ヘッダー作成
CURRENT_DATE=$(date '+%Y年%m月%d日 %H:%M:%S')
cat > "$OUTPUT_FILE" << EOF
# Blazor Enterprise Architecture Guide - 完全版

**Version**: 2.1.2 (自動生成版)
**生成日**: $CURRENT_DATE
**章数**: ${#CHAPTERS[@]}章

---

## 📋 目次

EOF

# 目次を生成
echo "目次を生成中..."
for chapter in "${CHAPTERS[@]}"; do
    chapter_file="$DOCS_DIR/$chapter"
    if [ -f "$chapter_file" ]; then
        # 最初の # 行を取得
        title=$(grep -m 1 '^#\s' "$chapter_file" | sed 's/^#\s*//')
        if [ -n "$title" ]; then
            echo "- $title" >> "$OUTPUT_FILE"
        fi
    fi
done

echo "" >> "$OUTPUT_FILE"
echo "---" >> "$OUTPUT_FILE"
echo "" >> "$OUTPUT_FILE"

# 各章を結合
echo "章を結合中..."
for chapter in "${CHAPTERS[@]}"; do
    chapter_file="$DOCS_DIR/$chapter"
    if [ -f "$chapter_file" ]; then
        echo "処理中: $chapter"

        # 章の区切りを追加
        echo "" >> "$OUTPUT_FILE"
        echo "" >> "$OUTPUT_FILE"
        echo "---" >> "$OUTPUT_FILE"
        echo "" >> "$OUTPUT_FILE"

        # 「← 目次に戻る」リンクを除去して追加
        sed '/\[←.*目次に戻る\](00_README\.md)/d' "$chapter_file" >> "$OUTPUT_FILE"
        echo "" >> "$OUTPUT_FILE"
    else
        echo "警告: $chapter が見つかりません"
    fi
done

# 完了メッセージ
FILE_SIZE=$(du -h "$OUTPUT_FILE" | cut -f1)
echo ""
echo "=== 完了 ==="
echo "出力: $OUTPUT_FILE"
echo "ファイルサイズ: $FILE_SIZE"
echo ""
echo "統合版が正常に生成されました！"
