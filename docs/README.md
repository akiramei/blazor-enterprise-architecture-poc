# ドキュメント構成ガイド

このフォルダには、プロジェクトのアーキテクチャドキュメントが体系的に整理されています。

---

## 📂 フォルダ構造

```
docs/
├── README.md                      # このファイル（ドキュメント構成ガイド）
│
├── blazor-guide-package/          # 🎯 メインドキュメント
│   ├── BLAZOR_ARCHITECTURE_GUIDE_COMPLETE.md  # 完全版（全章統合）
│   └── docs/                      # 章別ドキュメント（00-19章）
│       ├── 00_README.md           # 目次と推奨される読み方
│       ├── 01-19_*.md             # 各章（アーキテクチャ、パターン、実装ガイド）
│       └── CHANGELOG.md           # ドキュメント変更履歴
│
├── architecture/                  # 📐 アーキテクチャ設計文書
│   ├── cross-cutting-concerns.md  # 横断的関心事の詳細設計
│   ├── STATE-MANAGEMENT-LAYERS.md # 状態管理の2層モデル
│   ├── OUTBOX_PATTERN.md          # Outboxパターンの実装
│   ├── SHARED-VS-KERNEL-DISTINCTION.md  # SharedとKernelの使い分け
│   └── UI-POLICY-PUSH-DESIGNER-BENEFITS.md  # UI設計ポリシー
│
└── patterns/                      # 📋 設計パターン集
    ├── REST-API-DESIGN-GUIDE.md   # REST API設計ガイド
    ├── API-CLIENT-CONTRACT.md     # APIクライアント契約
    ├── 01_APPROVAL_WORKFLOW_PATTERN.md  # 承認ワークフローパターン
    ├── 02_AGGREGATION_REPORTING_PATTERN.md  # 集計レポートパターン
    ├── BUSINESS_PATTERNS_ROADMAP.md  # ビジネスパターンロードマップ
    └── README.md                  # パターン集の概要
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
1. **[architecture/cross-cutting-concerns.md](architecture/cross-cutting-concerns.md)** - 横断的関心事
2. **[architecture/STATE-MANAGEMENT-LAYERS.md](architecture/STATE-MANAGEMENT-LAYERS.md)** - 状態管理
3. **[03_アーキテクチャ概要](blazor-guide-package/docs/03_アーキテクチャ概要.md)** - 設計原則

---

## 📚 各フォルダの役割

### 1. blazor-guide-package/
**役割**: プロジェクトのメインドキュメント（完全なアーキテクチャガイド）

**対象読者**:
- 開発者全員（必読）
- 3層アーキテクチャ経験者
- Blazor初心者

**内容**:
- アーキテクチャ設計原則（モノリシックVSA）
- 各層の詳細設計（UI/Application/Domain/Infrastructure）
- 実装パターンカタログ
- テスト戦略
- ベストプラクティス

**更新ポリシー**:
- ✅ プロジェクトの標準的な実装パターンを追加した場合は更新
- ✅ アーキテクチャ変更時は必ず更新
- ❌ 実験的な機能は記載しない

---

### 2. architecture/
**役割**: アーキテクチャ設計の詳細仕様書

**対象読者**:
- アーキテクト
- 上級開発者
- コードレビュアー

**内容**:
- 横断的関心事の実装詳細
- 状態管理の設計パターン
- Outboxパターンの実装
- SharedとKernelの使い分け
- UI設計ポリシー

**更新ポリシー**:
- ✅ アーキテクチャの基本方針変更時に更新
- ✅ 新しい横断的関心事を追加した場合に更新
- ❌ 実装詳細のみの変更では更新不要

**参照関係**:
- `cross-cutting-concerns.md` は Blazorガイド20章として参照される
- Blazorガイドから分離して管理（長大なため）

---

### 3. patterns/
**役割**: プロジェクト横断の設計パターン集

**対象読者**:
- APIを公開する場合の開発者
- フロントエンド/バックエンド連携担当者
- ビジネスロジック設計者

**内容**:
- REST API設計ガイドライン
- APIクライアント契約パターン
- 承認ワークフローパターン
- 集計レポートパターン
- ビジネスパターンロードマップ

**更新ポリシー**:
- ✅ REST API公開時に参照・更新
- ✅ 新しいビジネスパターン実装時に追加
- ❌ 使用しない場合は更新不要

---

## 🚫 ドキュメント管理の3つのルール

### ✅ ルール1: 永続的な設計文書のみ追加する

**追加OK:**
- アーキテクチャ原則、実装パターン、設計ガイドライン
- チーム全体の共通知識、プロジェクト標準

**追加NG:**
- 一時的な作業記録（移行計画・進捗記録） → コミットメッセージで管理
- 個人的なメモ、調査メモ → ローカル環境で管理
- TODOリスト → GitHubのIssueまたはコード内コメント

### ✅ ルール2: 適切なフォルダに分類する

**分類先:**
- `blazor-guide-package/`: 実装ガイド（開発者向け）
- `architecture/`: アーキテクチャ設計仕様（アーキテクト向け）
- `patterns/`: 設計パターン集（横断的パターン）

**チェックリスト:**
- [ ] このドキュメントは永続的に必要か？
- [ ] 既存のドキュメントに統合できないか？
- [ ] 適切なフォルダに分類できるか？
- [ ] 複数の開発者が参照するか？

### ✅ ルール3: 変更時は関連ドキュメントも更新する

**更新タイミング:**
- **Blazorガイド**: アーキテクチャ変更時は必須、新パターン追加時は推奨
- **アーキテクチャ文書**: 基本方針変更時は必須
- **パターン集**: 該当機能の実装時に更新

**削除タイミング:**
- 一時的な作業記録が完了した時 → 削除
- 他のドキュメントに統合された時 → 削除
- 該当機能が削除された時 → 削除（ただしアーキテクチャガイドは履歴として保持）

---

## 📌 よくある質問

### Q1: 実装中のメモはどこに置くべきか？
**A**: docs/には置かない。ローカル環境またはコード内コメントで管理。

### Q2: 新しいアーキテクチャパターンを追加したい
**A**: まず`blazor-guide-package/docs/05_パターンカタログ一覧.md`に追加。必要に応じて新しい章を作成。

### Q3: このフォルダ構造を変更したい
**A**: まず`docs/README.md`（このファイル）の変更をレビューし、合意を得てから実施。

### Q4: Blazorガイドとarchitecture/の使い分けは？
**A**:
- **Blazorガイド**: 開発者向けの実装ガイド（How）
- **architecture/**: アーキテクトが定めた設計原則（Why/What）

---

## 📖 参考リンク

- **プロジェクトメインREADME**: [/README.md](../README.md)
- **Blazorガイド目次**: [blazor-guide-package/docs/00_README.md](blazor-guide-package/docs/00_README.md)
- **横断的関心事**: [architecture/cross-cutting-concerns.md](architecture/cross-cutting-concerns.md)
- **状態管理**: [architecture/STATE-MANAGEMENT-LAYERS.md](architecture/STATE-MANAGEMENT-LAYERS.md)

---

## 🏗️ 現在のアーキテクチャ概要

このプロジェクトは **モノリシックVSA (Vertical Slice Architecture)** を採用しています。

### プロジェクト構造

```
src/
├── Application/              # 単一Blazorプロジェクト
│   ├── Application.csproj    # すべての機能を含む
│   ├── Program.cs
│   ├── Core/                 # Commands, Queries, Behaviors
│   ├── Features/             # 19機能 (UIを含む垂直スライス)
│   ├── Infrastructure/
│   ├── Shared/
│   └── ...
├── Domain/                   # 分離されたドメインプロジェクト
│   ├── ProductCatalog/
│   └── PurchaseManagement/
└── Shared/                   # 共通ライブラリ
    ├── Kernel/
    ├── Domain/
    ├── Application/
    ├── Infrastructure/
    └── ...
```

### 設計原則

- **YAGNI (You Aren't Gonna Need It)**: シンプルから始め、必要に応じて複雑化
- **単一プロジェクト**: Domainのみ分離、それ以外は単一Applicationプロジェクト
- **垂直スライス**: 機能ごとにUI + Application + Infrastructure を含む
- **モノリシック**: 将来的にマイクロサービス化が必要になった場合のみ分割

---

**最終更新**: 2025-11-16
**バージョン**: 2.0.0 (モノリシック統合完了)
