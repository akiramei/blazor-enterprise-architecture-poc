# Pattern Catalog Summary

**バージョン: v2025.11.19**
**パターン総数: 20**
**最終更新: 2025-11-19**

---

## 📊 カタログ統計

| カテゴリ | パターン数 | 安定版 | Beta | 説明 |
|---------|----------|-------|------|------|
| **Pipeline Behaviors** | 7 | 6 | 1 | 横断的関心事（自動実行） |
| **Feature Slices** | 8 | 8 | 0 | 垂直スライス（完全な機能） |
| **Domain Patterns** | 2 | 2 | 0 | ドメイン層パターン |
| **Query Patterns** | 1 | 1 | 0 | データ取得パターン |
| **Command Patterns** | 1 | 1 | 0 | データ変更パターン |
| **Layer Elements** | 2 | 2 | 0 | レイヤー要素 |
| **UI Patterns** | 1 | 1 | 0 | UI実装パターン |
| **合計** | **20** | **19** | **1** | |

---

## 🚀 Phase別実装状況

### ✅ Phase 1: CRUD完成
- `feature-update-entity` - エンティティ更新（楽観的排他制御）
- `feature-delete-entity` - エンティティ削除（論理削除 or 物理削除）

### ✅ Phase 2: データ連携
- `feature-import-csv` - CSV一括インポート（部分成功/失敗追跡）
- `feature-file-upload` - ファイルアップロード（Azure Blob/S3対応）
- `feature-export-csv` - CSV形式エクスポート（UTF-8 BOM）

### ✅ Phase 3: 監査・通知
- `audit-log-behavior` - 監査ログ自動記録（Pipeline Behavior）
- `realtime-notification-pattern` - SignalRリアルタイム通知

### ✅ Phase 4: 承認ワークフロー
- `feature-approval-workflow` - マルチステップ承認（稟議・申請）
- `domain-state-machine` - ステートマシン（状態遷移管理）
- `domain-approval-history` - 承認履歴記録・追跡

---

## 🎯 カテゴリ別パターン一覧

### Pipeline Behaviors（横断的関心事） - 7パターン

実行順序（order_hint）に従って自動実行。

| ID | 順序 | 目的 | 安定性 |
|---|-----|------|-------|
| `metrics-behavior` | 50 | ビジネスメトリクス・パフォーマンスメトリクス自動収集 | stable |
| `validation-behavior` | 100 | FluentValidation による入力検証を自動実行 | stable |
| `authorization-behavior` | 200 | ロールベース認可チェックを自動実行 | stable |
| `idempotency-behavior` | 300 | Command の冪等性を保証し、重複実行を防止 | **beta** |
| `transaction-behavior` | 400 | Command を単一トランザクションで実行 | stable |
| `audit-log-behavior` | 550 | Command実行の監査ログを自動記録 | stable |
| `logging-behavior` | 600 | すべての Command/Query のログ出力 | stable |

### Feature Slices（垂直スライス） - 8パターン

完全な機能実装（Application + UI + API）。

| ID | 目的 | Phase |
|---|------|-------|
| `feature-create-entity` | エンティティ作成の完全な垂直スライス | 基本 |
| `feature-search-entity` | エンティティ検索・フィルタリング・ページング | 基本 |
| `feature-export-csv` | CSV形式でデータをエクスポート | Phase 2 |
| `feature-update-entity` | エンティティ更新（楽観的排他制御） | Phase 1 |
| `feature-delete-entity` | エンティティ削除（論理削除 or 物理削除） | Phase 1 |
| `feature-import-csv` | CSVファイルからデータを一括インポート | Phase 2 |
| `feature-file-upload` | ファイルアップロード（添付ファイル） | Phase 2 |
| `feature-approval-workflow` | 承認ワークフロー（マルチステップ承認） | Phase 4 |

### Domain Patterns（ドメインパターン） - 2パターン

| ID | 目的 | Phase |
|---|------|-------|
| `domain-state-machine` | ステートマシンによる状態遷移管理 | Phase 4 |
| `domain-approval-history` | 承認履歴を記録・追跡 | Phase 4 |

### Query/Command Patterns - 2パターン

| ID | 目的 |
|---|------|
| `query-get-list` | 全件取得クエリ（キャッシュ対応） |
| `command-create` | 新規エンティティ作成コマンド |

### Layer Elements（レイヤー要素） - 2パターン

| ID | 目的 | レイヤー |
|---|------|---------|
| `layer-store` | UI層の状態管理とI/O | UI |
| `layer-pageactions` | UI手順のオーケストレーション | UI |

### UI Patterns（UI実装パターン） - 1パターン

| ID | 目的 |
|---|------|
| `realtime-notification-pattern` | SignalRリアルタイム通知でUI自動更新 |

---

## 📈 推奨される組み合わせ

### 標準的な CQRS + Behaviors

ほとんどの業務アプリで推奨される基本構成。

```
- metrics-behavior
- validation-behavior
- authorization-behavior
- transaction-behavior
- logging-behavior
```

### 高信頼性構成（冪等性あり）

支払い処理など、重複実行が致命的な処理を含む場合。

```
- metrics-behavior
- validation-behavior
- authorization-behavior
- idempotency-behavior
- transaction-behavior
- logging-behavior
```

### 承認ワークフロー構成

稟議・申請システムを構築する場合。

```
Behaviors:
- validation-behavior
- authorization-behavior
- transaction-behavior
- audit-log-behavior
- logging-behavior

Features:
- feature-approval-workflow

Domain:
- domain-state-machine
- domain-approval-history

UI:
- realtime-notification-pattern
```

---

## 🎓 適用可能なシステム

このカタログのパターンは以下のシステムで活用できます：

### エンタープライズシステム
- 会計システム
- 人事システム
- 給与システム
- 勤怠管理システム
- 経費精算システム

### 業務管理システム
- 購買・在庫・販売管理システム
- 稟議・申請システム
- 営業支援システム
- プロジェクト管理システム

### マスタ管理
- 商品マスタ
- 顧客マスタ
- 取引先マスタ
- 社員マスタ

---

## 📖 詳細ドキュメント

- **カタログ全体**: [catalog/README.md](catalog/README.md)
- **AI使用ガイド**: [catalog/AI_USAGE_GUIDE.md](catalog/AI_USAGE_GUIDE.md)
- **パターン選択ガイド**: [catalog/PATTERN_SELECTION_GUIDE.md](catalog/PATTERN_SELECTION_GUIDE.md)
- **意思決定フローチャート**: [catalog/DECISION_FLOWCHART.md](catalog/DECISION_FLOWCHART.md)

---

## 🔗 クイックリンク

### カタログファイル
- [catalog/index.json](catalog/index.json) - パターンカタログの索引（マスターファイル）
- [patterns.manifest.json](patterns.manifest.json) - プロジェクトで使用するパターンの宣言

### 主要パターンYAML
- [validation-behavior.yaml](catalog/patterns/validation-behavior.yaml)
- [transaction-behavior.yaml](catalog/patterns/transaction-behavior.yaml)
- [feature-approval-workflow.yaml](catalog/features/feature-approval-workflow.yaml)
- [domain-state-machine.yaml](catalog/patterns/domain-state-machine.yaml)

---

**最終更新: 2025-11-19**
**次回更新予定: 新パターン追加時**
