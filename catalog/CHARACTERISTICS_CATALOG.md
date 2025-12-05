# CHARACTERISTICS_CATALOG.md - Characteristics語彙定義

> **目的**: SPECファイルで使用する `characteristics` タグの正式定義。
> AIとSPEC作成者が共通の語彙を使用し、パターン自動選択の基盤とする。

---

## 概要

`characteristics` は SPEC の特性を表すタグで、以下の目的で使用します：

1. **パターン自動選択**: characteristics → 対応パターンを自動マッピング
2. **Manifest生成**: catalog_binding.from_catalog の根拠として記録
3. **意図の明確化**: SPECの性質を構造化された語彙で表現

---

## Prefix一覧

### op: (Operation - 操作特性)

操作の性質を表すタグ。Command/Query の選択に影響。

| タグ | 説明 | 対応パターン |
|------|------|-------------|
| `op:mutates-state` | 状態を変更する操作 | feature-create-entity, feature-update-entity, feature-delete-entity |
| `op:read-only` | 読み取り専用操作 | query-get-list, query-get-by-id, query-get-by-period |
| `op:single-transaction` | 単一トランザクションで完結 | transaction-behavior |
| `op:batch` | バッチ処理（複数レコード一括操作） | feature-import-csv, feature-export-csv |
| `op:async` | 非同期処理（即時応答不要） | outbox-pattern |
| `op:idempotent` | 冪等性が必要（再実行しても同じ結果） | idempotency-behavior |

### xcut: (Cross-cutting - 横断的関心事)

Pipeline Behavior で自動適用される非機能要件。

| タグ | 説明 | 対応パターン | 実行順序 |
|------|------|-------------|:--------:|
| `xcut:validation` | 入力検証が必要 | validation-behavior | 100 |
| `xcut:auth` | 認証/認可が必要 | authorization-behavior | 200 |
| `xcut:idempotent` | 冪等性保証が必要 | idempotency-behavior | 300 |
| `xcut:cache` | キャッシュ対象 | caching-behavior | 350 |
| `xcut:transaction` | トランザクション管理 | transaction-behavior | 400 |
| `xcut:audit` | 監査ログが必要 | audit-log-behavior | 550 |
| `xcut:logging` | リクエストログが必要 | logging-behavior | 600 |
| `xcut:metrics` | メトリクス収集 | metrics-behavior | 50 |

### struct: (Structure - 構造特性)

ドメインモデルの構造に関するタグ。

| タグ | 説明 | 対応パターン |
|------|------|-------------|
| `struct:single-aggregate` | 単一集約のみ操作 | feature-create-entity（基本形） |
| `struct:multi-aggregate` | 複数集約を跨ぐ操作 | domain-validation-service |
| `struct:event-driven` | イベント駆動アーキテクチャ | outbox-pattern, realtime-notification-pattern |
| `struct:state-machine` | 状態遷移を持つ | domain-state-machine |
| `struct:typed-id` | 型安全なIDを使用 | domain-typed-id |
| `struct:timeslot` | 時間枠を扱う | domain-timeslot |

### domain: (Domain - ドメイン特性)

業務ドメインを表すタグ。推奨パターンの選択に影響。

| タグ | 説明 | 推奨パターン |
|------|------|-------------|
| `domain:library-core` | 図書館コア業務（貸出、返却） | audit-log-behavior, domain-validation-service |
| `domain:reservation` | 予約・スケジュール系 | domain-timeslot, domain-validation-service |
| `domain:financial` | 金融・決済系 | idempotency-behavior, audit-log-behavior |
| `domain:inventory` | 在庫・引当系 | concurrency-control, domain-validation-service |
| `domain:workflow` | 承認・ワークフロー系 | feature-approval-workflow, domain-state-machine |
| `domain:master` | マスタ・カタログ系 | caching-behavior |
| `domain:reporting` | 集計・レポート系 | aggregation-reporting, complex-query-service |

### layer: (Layer - レイヤー特性)

生成対象レイヤーを表すタグ。

| タグ | 説明 | 生成対象 |
|------|------|---------|
| `layer:application` | Application層のみ | Command, Handler, Validator |
| `layer:domain` | Domain層のみ | Entity, ValueObject, Repository Interface |
| `layer:ui` | UI層を含む | Store, PageActions, Razor |
| `layer:api` | API層を含む | Controller, Endpoint |
| `layer:full-slice` | 全レイヤー | 垂直スライス全体 |

---

## 自動マッピング規則

### 基本ルール

以下の組み合わせは自動的にパターンを推奨：

| characteristics組み合わせ | 推奨パターン | 信頼度 |
|-------------------------|-------------|:------:|
| `op:mutates-state` + `struct:single-aggregate` | feature-create-entity | 0.95 |
| `op:mutates-state` + `layer:full-slice` | feature-create/update/delete-entity | 0.90 |
| `op:read-only` + `layer:full-slice` | feature-search-entity | 0.90 |
| `op:read-only` + `layer:application` | query-get-list, query-get-by-id | 0.85 |
| `xcut:audit` | audit-log-behavior | 1.00 |
| `xcut:auth` | authorization-behavior | 1.00 |
| `xcut:cache` | caching-behavior | 1.00 |
| `struct:multi-aggregate` | domain-validation-service | 0.90 |
| `struct:state-machine` | domain-state-machine | 0.95 |
| `domain:workflow` | feature-approval-workflow | 0.90 |

### 暗黙的に適用されるパターン

以下のパターンは `op:mutates-state` で自動的に適用：

| パターン | 条件 | 理由 |
|---------|------|------|
| validation-behavior | 常に適用 | 入力検証は必須 |
| transaction-behavior | 常に適用 | トランザクション管理は必須 |
| logging-behavior | 常に適用 | ログは必須 |

### 競合ルール

以下の組み合わせは競合する（同時に指定不可）：

| タグA | タグB | 理由 |
|------|------|------|
| `op:mutates-state` | `op:read-only` | 操作は変更か読み取りのどちらか |
| `op:batch` | `struct:single-aggregate` | バッチは通常複数レコード |

---

## SPECでの使用例

```yaml
# specs/loan/LendBook.spec.yaml
characteristics:
  - op:mutates-state           # 状態を変更（貸出レコード作成）
  - op:single-transaction      # 単一トランザクション
  - xcut:auth                  # 認証必要（職員のみ）
  - xcut:audit                 # 監査ログ必要
  - struct:single-aggregate    # Loan集約のみ操作
  - domain:library-core        # 図書館コア業務
```

**マッピング結果**:
- `op:mutates-state` + `struct:single-aggregate` → `feature-create-entity`
- `xcut:auth` → `authorization-behavior`
- `xcut:audit` → `audit-log-behavior`
- 暗黙適用 → `validation-behavior`, `transaction-behavior`, `logging-behavior`

---

## パターン側での使用（applicability）

```yaml
# catalog/features/feature-create-entity.yaml
applicability:
  required_characteristics:
    - op:mutates-state
  recommended_characteristics:
    - struct:single-aggregate
    - op:single-transaction
  conflicts_with:
    - op:read-only
  auto_include_behaviors:
    - validation-behavior
    - transaction-behavior
```

---

## 語彙の拡張

新しいcharacteristicsタグを追加する場合：

1. このファイルに定義を追加
2. 対応するパターンYAMLに `applicability` を追加
3. `catalog/index.json` の `ai_decision_matrix` を更新（任意）

---

## バージョン

- **v1.0.0** (2025-12-05): 初版リリース
  - 4カテゴリ（op, xcut, struct, domain）+ layer を定義
  - 自動マッピング規則を定義
  - 競合ルールを定義
