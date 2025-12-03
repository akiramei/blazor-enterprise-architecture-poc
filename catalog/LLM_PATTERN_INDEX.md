# LLM_PATTERN_INDEX.md - パターン選択早見表

> **このファイルの目的**: AIエージェントがパターンを効率的に選択するためのMarkdown版インデックスです。
> `catalog/index.json` の内容をAIが読みやすい形式でまとめています。

---

## 1. パターン選択フローチャート

```
ユーザーの要求を分析
    ↓
【STEP 1】カテゴリを特定

├─「〇〇機能を作って」「〇〇画面を追加」
│   → Feature Slices（垂直スライス）
│
├─「すべてのCommandに〇〇」「システム全体で〇〇」
│   → Pipeline Behaviors（横断的関心事）
│
├─「一覧を取得」「検索」「レポート」
│   → Query Patterns
│
├─「状態遷移」「操作可否」「型安全ID」
│   → Domain Patterns
│
├─「同時実行制御」「トランザクション保証」
│   → Infrastructure Patterns
│
└─「UI状態管理」「リアルタイム更新」
    → UI Patterns

【STEP 2】具体的なパターンを選択（下記テーブル参照）

【STEP 3】パターンYAMLを読む
    → catalog/features/*.yaml または catalog/patterns/*.yaml
```

**迷った場合**: `feature-slice` をデフォルトで選択

---

## 2. Feature Slices（垂直スライス）

完全な機能を縦に実装するパターン。**最も頻繁に使用**。

| パターンID | 用途 | トリガーフレーズ | YAMLファイル |
|-----------|------|-----------------|-------------|
| feature-create-entity | エンティティ作成 | 「〇〇を作成」「新規登録」「追加」 | features/feature-create-entity.yaml |
| feature-search-entity | 検索・一覧表示 | 「〇〇を検索」「一覧画面」「フィルタ」 | features/feature-search-entity.yaml |
| feature-update-entity | エンティティ更新 | 「〇〇を編集」「更新」「変更」 | features/feature-update-entity.yaml |
| feature-delete-entity | エンティティ削除 | 「〇〇を削除」「消す」「除去」 | features/feature-delete-entity.yaml |
| feature-import-csv | CSVインポート | 「CSVを取り込む」「一括登録」「インポート」 | features/feature-import-csv.yaml |
| feature-export-csv | CSVエクスポート | 「CSVに出力」「ダウンロード」「エクスポート」 | features/feature-export-csv.yaml |
| feature-file-upload | ファイルアップロード | 「ファイルを添付」「画像アップロード」 | features/feature-file-upload.yaml |
| feature-approval-workflow | 承認ワークフロー | 「承認」「稟議」「ワークフロー」「申請」 | features/feature-approval-workflow.yaml |
| feature-authentication | 認証機能 | 「ログイン」「認証」「2FA」 | features/feature-authentication.yaml |

---

## 3. Pipeline Behaviors（横断的関心事）

すべてのCommand/Queryに自動適用される処理。**実行順序がある**。

| 順序 | パターンID | 用途 | トリガーフレーズ | YAMLファイル |
|:---:|-----------|------|-----------------|-------------|
| 50 | metrics-behavior | メトリクス収集 | 「パフォーマンス監視」「メトリクス」 | patterns/metrics-behavior.yaml |
| 100 | validation-behavior | 入力検証 | 「バリデーション」「入力チェック」 | patterns/validation-behavior.yaml |
| 200 | authorization-behavior | 認可チェック | 「権限」「ロール」「認可」 | patterns/authorization-behavior.yaml |
| 300 | idempotency-behavior | 冪等性保証（beta） | 「重複防止」「冪等」「二重実行」 | patterns/idempotency-behavior.yaml |
| 350 | caching-behavior | キャッシュ | 「キャッシュ」「高速化」 | patterns/caching-behavior.yaml |
| 400 | transaction-behavior | トランザクション | 「トランザクション」「ロールバック」 | patterns/transaction-behavior.yaml |
| 550 | audit-log-behavior | 監査ログ | 「監査」「履歴」「証跡」「コンプライアンス」 | patterns/audit-log-behavior.yaml |
| 600 | logging-behavior | リクエストログ | 「ログ」「デバッグ」「トラブルシュート」 | patterns/logging-behavior.yaml |

### 推奨構成

**標準的な業務アプリ:**
```
metrics-behavior → validation-behavior → authorization-behavior
→ transaction-behavior → logging-behavior
```

**高信頼性（決済等）:**
```
上記 + idempotency-behavior
```

---

## 4. Query Patterns（データ取得）

CQRS の Query 側パターン。

| パターンID | 用途 | トリガーフレーズ | YAMLファイル |
|-----------|------|-----------------|-------------|
| query-get-list | 全件/一覧取得 | 「一覧を取得」「リスト」 | patterns/query-get-list.yaml |
| query-get-by-id | ID指定取得 | 「IDで取得」「詳細表示」 | patterns/query-get-by-id.yaml |
| query-get-by-period | 期間指定取得 | 「今日/今週/期間」「日付範囲」「カレンダー」 | patterns/query-get-by-period.yaml |
| complex-query-service | 複合条件クエリ | 「空き検索」「NOT EXISTS」「複数テーブル結合」「未割当」 | patterns/complex-query-service.yaml |
| query-service-pattern | クエリサービス基盤 | 「Query Handler整理」「AsNoTracking」 | patterns/query-service-pattern.yaml |
| aggregation-reporting | 集計・レポート | 「ダッシュボード」「集計」「ランキング」「KPI」 | patterns/aggregation-reporting.yaml |

---

## 5. Domain Patterns（ドメインモデル）

業務ロジックを表現するパターン。

| パターンID | 用途 | トリガーフレーズ | YAMLファイル |
|-----------|------|-----------------|-------------|
| boundary-pattern | 操作可否判定 | 「〇〇できるか」「権限チェック」「CanXxx」 | patterns/boundary-pattern.yaml |
| domain-state-machine | 状態遷移 | 「ステータス」「状態遷移」「ワークフロー状態」 | patterns/domain-state-machine.yaml |
| domain-validation-service | 業務ルール検証 | 「重複チェック」「在庫確認」「残高確認」「引当」 | patterns/domain-validation-service.yaml |
| domain-typed-id | 型安全ID | 「強い型付け」「ProductId」「OrderId」 | patterns/domain-typed-id.yaml |
| domain-timeslot | 時間枠管理 | 「予約時間」「タイムスロット」「重複判定」 | patterns/domain-timeslot.yaml |
| domain-approval-history | 承認履歴 | 「承認履歴」「誰が承認」「承認追跡」 | patterns/domain-approval-history.yaml |

---

## 6. Infrastructure Patterns（インフラ層）

データ整合性・永続化のパターン。

| パターンID | 用途 | トリガーフレーズ | YAMLファイル |
|-----------|------|-----------------|-------------|
| concurrency-control | 同時実行制御 | 「楽観ロック」「悲観ロック」「ダブルブッキング防止」 | patterns/concurrency-control.yaml |
| outbox-pattern | イベント配信保証 | 「結果整合性」「マイクロサービス」「イベント駆動」 | patterns/outbox-pattern.yaml |

---

## 7. UI Patterns（Blazor UI）

フロントエンド実装パターン。

| パターンID | 用途 | トリガーフレーズ | YAMLファイル |
|-----------|------|-----------------|-------------|
| layer-store | 状態管理（Flux） | 「Store」「状態管理」「セッション」 | patterns/layer-store.yaml |
| realtime-notification-pattern | リアルタイム更新 | 「SignalR」「リアルタイム」「自動更新」 | patterns/realtime-notification-pattern.yaml |
| undo-redo-pattern | 操作取り消し | 「Undo」「Redo」「操作履歴」 | patterns/undo-redo-pattern.yaml |

---

## 8. Kernel（DDD基盤）

すべてのパターンの前提となる基盤。**最初に理解すべき**。

| パターンID | 用途 | YAMLファイル |
|-----------|------|-------------|
| result-pattern | エラーハンドリング（Result<T>） | kernel/result-pattern.yaml |
| value-object | 値オブジェクト基底クラス | kernel/value-object.yaml |
| entity-base | エンティティ基底クラス | kernel/entity-base.yaml |

---

## 9. 条件付き必読パターン

特定の条件下で **必ず読むべき** パターン。

| 条件 | 必読パターン | 理由 |
|------|-------------|------|
| UIがある | boundary-pattern | AIの学習バイアスでBoundaryが忘却されやすい |
| 状態遷移がある | domain-state-machine | 遷移制約の明確化 |
| 重複チェックがある | domain-validation-service | 複数エンティティをまたぐ検証 |
| 予約/ダブルブッキング | concurrency-control | 同時実行制御が必須 |
| 監査証跡が必要 | audit-log-behavior | コンプライアンス対応 |
| パフォーマンス要件 | metrics-behavior, caching-behavior | 最適化のため |

---

## 10. ドメイン別推奨パターン

### 図書館・貸出管理
- audit-log-behavior（貸出・返却履歴）
- concurrency-control（同時貸出防止）
- domain-timeslot（貸出期間）

### 金融・決済
- audit-log-behavior（決済履歴）
- idempotency-behavior（重複決済防止）
- concurrency-control（残高更新）

### 予約システム
- domain-timeslot（予約時間枠）
- complex-query-service（空き検索）
- concurrency-control（ダブルブッキング防止）

### 承認ワークフロー
- feature-approval-workflow（承認機能）
- domain-state-machine（承認状態遷移）
- domain-approval-history（承認履歴）

---

## 11. パターンYAMLの読み方

各パターンYAMLには以下のセクションがあります：

```yaml
id: パターンID
name: パターン名
intent: 目的・意図

ai_selection_hints:
  trigger_phrases: []     # ユーザーが言いそうなフレーズ
  typical_requests: []    # よくある要求例

implementation:
  file_path: "生成ファイルパス"
  template: |             # コードテンプレート
    ...

ai_guidance:
  when_to_use: []         # 使用すべき条件
  common_mistakes:        # 頻出ミス
    - mistake: "..."
      solution: "..."

evidence:
  implementation_file: "実装例ファイルパス"

dependencies:
  requires: []            # 依存パターン
  conflicts: []           # 競合パターン
```

---

## カタログバージョン

- **バージョン**: v2025.11.27.3
- **最終更新**: 2025-11-27
