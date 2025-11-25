# ドキュメント構成ガイド

このフォルダには、プロジェクトのアーキテクチャドキュメントが体系的に整理されています。

> **AIエージェントへ**: 実装時は `catalog/` を優先参照してください。
> このフォルダは人間の開発者向けの解説・補足資料です。

---

## 📂 フォルダ構造

```
docs/
├── README.md                      # このファイル（ドキュメント構成ガイド）
│
├── blazor-guide-package/          # 人間向けアーキテクチャガイド
│   ├── BLAZOR_ARCHITECTURE_GUIDE_COMPLETE.md  # 完全版（全章統合・自動生成）
│   ├── build-complete.ps1         # 統合版生成スクリプト（PowerShell）
│   ├── build-complete.sh          # 統合版生成スクリプト（Bash）
│   ├── README.md                  # パッケージ説明
│   └── docs/                      # 章別ドキュメント（00-19章）
│       ├── 00_README.md           # 目次と推奨される読み方
│       ├── 01-19_*.md             # 各章
│       └── CHANGELOG.md           # ドキュメント変更履歴
│
├── architecture/                  # アーキテクチャ設計文書
│   ├── UI-POLICY-PUSH-DESIGNER-BENEFITS.md  # UI設計ポリシー
│   └── decisions/                 # ADR（Architecture Decision Records）
│       └── ADR-001-ef-core-mapping-strategy.md
│
└── patterns/                      # 外部向けパターン
    └── API-CLIENT-CONTRACT.md     # APIクライアント契約書
```

---

## 🎯 ドキュメントの役割分担

| フォルダ | 対象読者 | 目的 |
|---------|---------|------|
| **catalog/** | AIエージェント | パターン定義、実装テンプレート、意思決定ルール |
| **docs/** | 人間の開発者 | 設計思想の理解、学習、背景説明 |

### AIエージェントが参照すべき場所

```
catalog/
├── index.json              # パターン索引・意思決定マトリクス
├── AI_USAGE_GUIDE.md       # 実装ルール
├── COMMON_MISTAKES.md      # 頻出ミス
├── patterns/*.yaml         # パターン定義
├── features/*.yaml         # 機能スライス定義
└── scaffolds/*.yaml        # プロジェクト構造定義
```

### 人間の開発者が読むべき場所

```
docs/
├── blazor-guide-package/   # アーキテクチャ全体の理解
├── architecture/           # 設計判断の背景
└── patterns/               # 外部連携の契約
```

---

## 🎯 読者別ガイド

### 初めて読む方
1. **[blazor-guide-package/docs/00_README.md](blazor-guide-package/docs/00_README.md)** ← まずはここから
2. 読者別の推奨パスに従って学習

### 3層アーキテクチャ経験者（WPF/WinForms）
1. **[18_3層アーキテクチャからの移行ガイド](blazor-guide-package/docs/18_3層アーキテクチャからの移行ガイド.md)** ← 最短パス
2. 既知の概念から新しいパターンへ段階的に学習

### アーキテクチャ設計者
1. **[03_アーキテクチャ概要](blazor-guide-package/docs/03_アーキテクチャ概要.md)** - 設計原則
2. **[architecture/decisions/](architecture/decisions/)** - ADR（設計判断記録）

---

## 📚 各フォルダの役割

### 1. blazor-guide-package/

**役割**: プロジェクトのメインドキュメント（人間向けアーキテクチャガイド）

**対象読者**:
- 開発者全員（設計思想の理解）
- 3層アーキテクチャ経験者
- Blazor初心者

**内容**:
- アーキテクチャ設計原則（VSA）
- 各層の詳細設計（UI/Application/Domain/Infrastructure）
- パターンの概要説明
- テスト戦略
- ベストプラクティス

**更新ポリシー**:
- ✅ 設計思想の変更時は更新
- ✅ 人間向け解説の改善時は更新
- ❌ AIが参照する実装詳細は `catalog/` に記載

---

### 2. architecture/

**役割**: 設計判断の記録と背景説明

**対象読者**:
- アーキテクト
- 上級開発者
- コードレビュアー

**内容**:
- `UI-POLICY-PUSH-DESIGNER-BENEFITS.md` - UI設計ポリシーの背景
- `decisions/` - ADR（Architecture Decision Records）

**更新ポリシー**:
- ✅ 新しい設計判断時にADRを追加
- ❌ 実装詳細は `catalog/patterns/` に記載

---

### 3. patterns/

**役割**: 外部連携向けの契約書

**対象読者**:
- APIを利用する外部開発者
- フロントエンド/バックエンド連携担当者

**内容**:
- `API-CLIENT-CONTRACT.md` - REST APIクライアント契約

**更新ポリシー**:
- ✅ API公開時に参照・更新
- ❌ 内部実装パターンは `catalog/` に記載

---

## 🚫 ドキュメント管理ポリシー

### ✅ 追加してよいドキュメント

1. **永続的な設計文書**
   - 設計思想の説明
   - ADR（設計判断記録）
   - 外部向け契約書

2. **人間の学習に役立つもの**
   - チュートリアル
   - 概念の解説
   - 比較表

### ❌ 追加してはいけないドキュメント

1. **一時的な作業記録**
   - 移行計画・進捗記録 → コミットメッセージで管理
   - 作業サマリー → CHANGELOG.mdに統合
   - 改善計画 → GitHubのIssue/Project管理

2. **AIが参照すべき実装詳細**
   - パターン定義 → `catalog/patterns/`
   - 実装テンプレート → `catalog/` 内のYAML
   - 意思決定ルール → `catalog/index.json`

3. **バージョン管理不要なもの**
   - 自動生成されるドキュメント（例外: BLAZOR_ARCHITECTURE_GUIDE_COMPLETE.md）
   - ビルド成果物

---

## 🔄 catalogとdocsの使い分け

| 内容 | 配置場所 | 理由 |
|-----|---------|------|
| パターンの定義・テンプレート | `catalog/` | AIが正確に参照 |
| パターンの解説・背景 | `docs/` | 人間が理解を深める |
| 実装ルール・禁止事項 | `catalog/` | AIが厳守 |
| 設計思想・Why | `docs/` | 人間が判断に活用 |
| 意思決定マトリクス | `catalog/index.json` | AIが自動選択 |
| ADR | `docs/architecture/decisions/` | 人間が背景を理解 |

---

## 🏗️ 現在のアーキテクチャ概要

このプロジェクトは **Vertical Slice Architecture (VSA)** を採用しています。

### プロジェクト構造

```
src/
├── Application/              # 単一Blazorプロジェクト
│   ├── Features/             # 機能スライス（垂直統合）
│   ├── Infrastructure/       # BC固有インフラ
│   └── Components/           # Blazor共通コンポーネント
│
├── Domain/                   # ドメインプロジェクト（分離）
│   └── {BoundedContext}/     # BC別ドメインモデル
│
└── Shared/                   # 共通ライブラリ
    ├── Kernel/               # DDD基盤
    ├── Application/          # ICommand, IQuery, Result<T>
    └── Infrastructure/       # Pipeline Behaviors
```

### 設計原則

- **Vertical Slice Architecture**: 機能単位で垂直統合
- **Catalog-Driven Development**: パターンカタログに基づく実装
- **UI同列配置**: `Features/{Feature}/` に .cs と .razor を並べる

---

## 📖 参考リンク

- **プロジェクトメインREADME**: [/README.md](../README.md)
- **カタログ（AI向け）**: [/catalog/](../catalog/)
- **Blazorガイド目次**: [blazor-guide-package/docs/00_README.md](blazor-guide-package/docs/00_README.md)

---

**最終更新**: 2025-11-26
**バージョン**: 3.0.0 (catalog統合対応)
