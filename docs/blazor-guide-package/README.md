# Blazor Enterprise Architecture Guide - Package

## 📄 ファイル構成

```
blazor-guide-package/
├── BLAZOR_ARCHITECTURE_GUIDE_COMPLETE.md  # 📖 統合版（自動生成）
├── build-complete.sh                       # 🔧 統合版生成スクリプト
├── README.md                               # このファイル
└── docs/                                   # 📁 分割版（章ごとのファイル）
    ├── 00_README.md
    ├── 01_イントロダクション.md
    └── ... (全20ファイル)
```

## 🚀 使い方

### 統合版（推奨）
- **BLAZOR_ARCHITECTURE_GUIDE_COMPLETE.md** を開いてください
- 全文検索、印刷、PDF変換、オフライン閲覧に最適
- 自動生成されるため、常に最新の内容が反映されます

### 分割版
- **docs/** フォルダ内の各章を個別に参照
- 特定の章だけを共有する場合に便利
- **編集はこちらで行ってください**（統合版は自動生成されます）

## 🔧 統合版の生成方法

章別ファイルを編集した後、以下のコマンドで統合版を更新できます：

```bash
# Linux/Mac/Git Bash
cd docs/blazor-guide-package
bash build-complete.sh

# Windows PowerShell
cd docs/blazor-guide-package
powershell.exe -ExecutionPolicy Bypass -File build-complete.ps1
```

### 自動化（推奨）

Git Hooksを使って、コミット前に自動生成することもできます：

```bash
# .git/hooks/pre-commit に以下を追加
#!/bin/bash
cd docs/blazor-guide-package
bash build-complete.sh
git add BLAZOR_ARCHITECTURE_GUIDE_COMPLETE.md
```

---

## ✅ v2.1.2 更新内容

- ✅ 統合版を自動生成化（手動メンテナンス不要）
- ✅ 章番号の不一致を修正（17章→20章）
- ✅ 全リンク切れを修正（90箇所）
- ✅ ビルドスクリプトを追加

**Version**: 2.1.2 (自動生成版)
**最終更新**: 2025年11月2日
