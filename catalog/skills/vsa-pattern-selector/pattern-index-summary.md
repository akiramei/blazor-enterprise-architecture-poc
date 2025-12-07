# Pattern Index Summary - パターン選択ロジック要約

このファイルは `catalog/LLM_PATTERN_INDEX.md` と `catalog/index.json` の要点をまとめたものです。

---

## クイックリファレンス

### 「〇〇を作って」系

| 要求 | パターン |
|------|---------|
| 〇〇を作成する機能 | feature-create-entity |
| 〇〇を検索する画面 | feature-search-entity |
| 〇〇を編集できるように | feature-update-entity |
| 〇〇を削除する | feature-delete-entity |
| CSV インポート | feature-import-csv |
| CSV エクスポート | feature-export-csv |
| 承認ワークフロー | feature-approval-workflow |

### 「〇〇を取得」系

| 要求 | パターン |
|------|---------|
| 一覧を取得 | query-get-list |
| ID で詳細を取得 | query-get-by-id |
| 期間で取得 | query-get-by-period |
| 空き検索・NOT EXISTS | complex-query-service |

### 「〇〇をチェック」系

| 要求 | パターン |
|------|---------|
| 〇〇できるか判定 | boundary-pattern |
| 状態遷移の管理 | domain-state-machine |
| 重複チェック・在庫確認 | domain-validation-service |
| 順番管理・キュー | domain-ordered-queue |

---

## ai_decision_matrix の主要ルール

### user_intent_to_pattern

| Intent | パターンカテゴリ | 信頼度 |
|--------|-----------------|:------:|
| 新機能追加 | feature-slice | 0.95 |
| 重複チェック | domain-validation-service | 0.90 |
| 空き検索 | complex-query-service | 0.90 |
| 操作可否 | boundary-pattern | 0.85 |
| 状態遷移 | domain-state-machine | 0.85 |

### domain_hints（ドメイン別追加パターン）

| ドメイン | 追加推奨パターン |
|---------|-----------------|
| 会議室予約 | domain-timeslot, domain-typed-id, concurrency-control |
| 在庫管理 | domain-validation-service, concurrency-control |
| 承認系 | domain-state-machine, feature-approval-workflow |
| 図書館 | domain-ordered-queue, audit-log-behavior |

---

## パターン組み合わせの典型例

### CRUD + 検証

```
feature-create-entity
  + validation-behavior（自動適用）
  + transaction-behavior（自動適用）
  + domain-validation-service（重複チェックがある場合）
```

### 予約システム

```
feature-create-entity
  + domain-timeslot（時間枠）
  + complex-query-service（空き検索）
  + concurrency-control（ダブルブッキング防止）
```

### 状態管理あり

```
feature-update-entity
  + domain-state-machine（状態遷移）
  + boundary-pattern（操作可否）
```

### 図書館（ドッグフーディング対応）

```
feature-create-entity (貸出/予約)
  + domain-ordered-queue（予約順番・Position）★FR-018
  + domain-validation-service（全コピー貸出中のみ予約可能）★FR-017
  + boundary-pattern（Ready 予約者の優先権）★FR-021
  + audit-log-behavior（履歴）
```

---

## Pipeline Behaviors 実行順序

| 順序 | Behavior | 自動適用 |
|:---:|----------|:--------:|
| 50 | metrics-behavior | - |
| 100 | validation-behavior | ✅ |
| 200 | authorization-behavior | - |
| 300 | idempotency-behavior | - |
| 350 | caching-behavior | - |
| 400 | transaction-behavior | ✅ |
| 550 | audit-log-behavior | - |
| 600 | logging-behavior | - |

**✅ 自動適用**: 明示的に除外しない限り、すべての Command に適用される。

---

## 警告フレーズ

以下のフレーズが出たら、特定のパターンを必ず検討：

| フレーズ | 必須パターン |
|---------|-------------|
| 「〜のみ可能」「〜の場合のみ」 | domain-validation-service |
| 「順番」「キュー」「Position」 | domain-ordered-queue |
| 「優先権」「Ready 状態の人」 | boundary-pattern (advanced) |
| 「同時」「ダブルブッキング」 | concurrency-control |
| 「監査」「履歴」「コンプライアンス」 | audit-log-behavior |

---

## Kernel（DDD 基盤）

すべてのパターンの前提。最初に理解すべき。

| パターン | 用途 |
|---------|------|
| result-pattern | エラーハンドリング（Result<T>） |
| value-object | 値オブジェクト基底クラス |
| entity-base | エンティティ基底クラス |

---

## 参照ファイル

| ファイル | 内容 |
|---------|------|
| `catalog/index.json` | ai_decision_matrix、パターン定義 |
| `catalog/LLM_PATTERN_INDEX.md` | パターン選択早見表 |
| `catalog/DECISION_FLOWCHART.md` | 詳細な選択アルゴリズム |
| `catalog/features/*.yaml` | Feature Slice パターン |
| `catalog/patterns/*.yaml` | Domain/Query/Behavior パターン |
