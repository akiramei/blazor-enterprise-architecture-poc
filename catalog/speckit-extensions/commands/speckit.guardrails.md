---
description: Extract guardrails from spec and catalog patterns to prevent AI implementation errors.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

---

## このコマンドの目的

**「曖昧化する前に」ガードレールを固定する**

SPEC と カタログパターン から「絶対に守るべきルール」を抽出し、
AI が実装時にメソッドを誤用することを防止する。

### 背景（Library9ドッグフーディング問題）

| 段階 | 状態 | 問題 |
|-----|------|------|
| Specify | 正しい（DequeueAsync を使うべき） | - |
| Plan | 「後続を繰り上げ」と記述 | **どのAPIか曖昧** |
| Implement | CheckAndPromoteNextAsync を誤用 | **名前から推測して誤選択** |

**解決策**: Specify 直後にガードレールを固定し、Plan 以降で参照させる。

---

## ワークフロー位置

```
specify → 【guardrails】→ plan → tasks → implement
         ↑ここで実行
```

**入力**: `specs/{feature}/{slice}.spec.yaml` + カタログパターン
**出力**: `specs/{feature}/{slice}.guardrails.yaml`

---

## Phase 1: Load Context

### Step 1.1: SPEC ファイル読み込み

```
1. specs/{feature}/{slice}.spec.yaml を読む
2. characteristics セクションからパターンヒントを抽出
3. domain_rules からビジネスルールを抽出
4. FR番号付きの要件を列挙
```

### Step 1.2: カタログパターン特定

```
1. catalog/index.json を読む
2. ai_decision_matrix でカテゴリを特定
3. characteristics に基づいてパターンを選択
4. 該当パターンの YAML ファイルパスを取得
```

### Step 1.3: パターン YAML 読み込み

特定されたパターン YAML から以下のセクションを読み込む:

| セクション | 抽出対象 |
|-----------|---------|
| `ai_guidance.orchestration_rules` | 必須アクション、禁止アクション |
| `ai_guidance.canonical_call_paths` | 正解経路、アンチパターン |
| `ai_guidance.common_mistakes` | 頻出ミス（severity: critical/high） |
| `ai_guidance.must_read_checklist` | 必須確認項目 |

---

## Phase 2: Extract Forbidden Actions

パターン YAML から **禁止事項** を抽出する。

### 抽出元

| ソース | 抽出内容 |
|-------|---------|
| `orchestration_rules[].forbidden_actions` | 禁止アクションリスト |
| `canonical_call_paths[].anti_patterns` | アンチパターン |
| `common_mistakes` (severity: critical/high) | 重大な頻出ミス |

### 出力形式

```yaml
forbidden_actions:
  - id: FA-001
    source: "{pattern_id}.yaml"
    scope: "{Handler/Service名}"
    forbidden: "{禁止アクションの説明}"
    reason: "{禁止理由}"
    severity: critical | high | medium | low
    detection:
      pattern: "{正規表現パターン}"
      exclude_in: ["{除外ファイルパターン}"]
```

### 例

```yaml
forbidden_actions:
  - id: FA-001
    source: "domain-ordered-queue.yaml"
    scope: "CancelReservationCommandHandler"
    forbidden: "reservation.Cancel() を直接呼ぶ"
    reason: "Position繰り上げが行われない"
    severity: critical
    detection:
      pattern: "reservation\\.Cancel\\(\\)"
      exclude_in: ["*QueueService.cs"]
```

---

## Phase 3: Extract Canonical Routes

パターン YAML から **唯一の正解経路** を抽出する。

### 抽出元

| ソース | 抽出内容 |
|-------|---------|
| `orchestration_rules[].required_action` | 必須アクション |
| `canonical_call_paths[].path` | 正解経路（DAG形式） |
| `orchestration_rules[].code_example (✅)` | 正しいコード例 |

### 出力形式

```yaml
canonical_routes:
  - id: CR-001
    operation: "{操作名}"
    source: "{pattern_id}.yaml"
    path:
      - caller: "{呼び出し元クラス}"
        method: "{呼び出すメソッド}"
        params: ["{パラメータ}"]
        note: "{補足説明}"
    anti_patterns:
      - pattern: "{間違った呼び出しパターン}"
        problem: "{問題点}"
```

### 例

```yaml
canonical_routes:
  - id: CR-001
    operation: "予約キャンセル"
    source: "domain-ordered-queue.yaml"
    path:
      - caller: "CancelReservationCommandHandler"
        method: "IReservationQueueService.DequeueAsync"
        params: ["reservationId", "ct"]
        note: "QueueService経由で呼ぶこと。Entity.Cancel()を直接呼ばない"
    anti_patterns:
      - pattern: "Handler → reservation.Cancel() → CheckAndPromoteNextAsync()"
        problem: "PromotePositions が呼ばれず、Position が繰り上がらない"
```

---

## Phase 4: Extract Spec-Derived Guardrails

SPEC から **ビジネスルールガードレール** を抽出する。

### 抽出キーワード

| キーワード | タイプ | 説明 |
|-----------|-------|------|
| 「〜のみ可能」 | precondition | 前提条件ガードレール |
| 「〜の場合のみ」 | precondition | 前提条件ガードレール |
| 「〜が優先」 | priority | 優先権ガードレール |
| 「〜が先」 | priority | 優先権ガードレール |
| 「〜先着」 | ordering | 順序ガードレール |

### 出力形式

```yaml
spec_derived_guardrails:
  - id: GR-001
    fr_ref: "FR-XXX"
    rule: "{ビジネスルールの説明}"
    scope: "{適用スコープ}"
    violation_impact: "{違反時の影響}"
    enforcement:
      entity_method: "{Entity.CanXxx()メソッド}"
      check_location: "{チェック実行場所}"
```

### 例

```yaml
spec_derived_guardrails:
  - id: GR-001
    fr_ref: "FR-021"
    rule: "Ready 状態の予約者が最優先で貸出権を持つ"
    scope: "LoanBoundaryService"
    violation_impact: "Ready 予約者以外に貸出してしまう"
    enforcement:
      entity_method: "BookCopy.CanBorrow(memberId, readyReserverId)"
      check_location: "LoanBoundaryService.ValidateBorrowAsync"
```

---

## Phase 5: Generate Acceptance Criteria

テスト生成用の **受け入れ条件** を生成する。

### 抽出元

| ソース | 抽出内容 |
|-------|---------|
| `orchestration_rules[].test_case` | テストケース説明 |
| `orchestration_rules[].skip_consequence` | スキップ時の結果（失敗ケース） |
| `orchestration_rules[].test_definition` | Given/When/Then 定義 |

### 出力形式

```yaml
acceptance_criteria:
  - id: AC-001
    gr_ref: "GR-XXX"  # 関連ガードレールID
    criterion: "{受け入れ条件の説明}"
    test_derivation:
      given:
        - "{前提条件1}"
        - "{前提条件2}"
      when: "{実行操作}"
      then:
        - "{期待結果1}"
        - "{期待結果2}"
```

### 例

```yaml
acceptance_criteria:
  - id: AC-001
    source: "domain-ordered-queue.yaml orchestration_rules"
    criterion: "Cancel後、Position=2がPosition=1になること"
    test_derivation:
      given:
        - "Reservation R1 (Position=1, Status=Waiting)"
        - "Reservation R2 (Position=2, Status=Waiting)"
      when: "R1 を DequeueAsync でキャンセル"
      then:
        - "R1.Status == Cancelled"
        - "R1.Position == null"
        - "R2.Position == 1"
        - "R2.Status == Ready"
```

---

## Phase 6: Generate Negative Examples

**やってはいけないコード例** を生成する。

### 抽出元

| ソース | 抽出内容 |
|-------|---------|
| `orchestration_rules[].code_example` (❌マーク) | 禁止コード例 |
| `canonical_call_paths[].anti_patterns` | アンチパターンコード |

### 出力形式

```yaml
negative_examples:
  - id: NE-001
    title: "{例のタイトル}"
    bad_code: |
      // ❌ 禁止
      {禁止コード}
    good_code: |
      // ✅ 正しい
      {正しいコード}
    detection_hint: "{検出ヒント}"
```

---

## Phase 7: Generate Verification Rules

**静的検査ルール** と **テスト生成ヒント** を生成する。

### 静的検査ルール

```yaml
verification_rules:
  static_checks:
    - id: SC-001
      type: "grep_prohibition"
      pattern: "{正規表現パターン}"
      exclude_paths: ["{除外パス}"]
      message: "{エラーメッセージ}"
```

### テスト生成ヒント

```yaml
verification_rules:
  test_generation_hints:
    - guardrail_id: "GR-XXX"
      test_type: "boundary_decision | orchestration | state_transition"
      template_ref: "spec-test-template.yaml#{section}"
```

---

## Phase 8: Output Generation

### Step 8.1: guardrails.yaml 生成

テンプレート: `catalog/scaffolds/guardrails-template.yaml`

出力先: `specs/{feature}/{slice}.guardrails.yaml`

### Step 8.2: 検証チェック

| チェック | 条件 | 結果 |
|---------|------|------|
| canonical_routes | 最低1件 | ❌ ERROR: 空の場合 |
| forbidden_actions | 0件以上 | ⚠️ WARNING: 空の場合 |
| spec_derived_guardrails | FR番号付き | ⚠️ WARNING: FR番号なし |
| acceptance_criteria | GR/FR紐付け | ⚠️ WARNING: 紐付けなし |

### Step 8.3: 出力サマリー

```
## Guardrails Generation Summary

- Feature: {Feature}
- Slice: {Slice}
- Output: specs/{feature}/{slice}.guardrails.yaml

### Extracted Guardrails

| Category | Count | Status |
|----------|-------|--------|
| Forbidden Actions | {N} | ✅ / ⚠️ |
| Canonical Routes | {N} | ✅ / ❌ |
| Spec-Derived GR | {N} | ✅ / ⚠️ |
| Acceptance Criteria | {N} | ✅ / ⚠️ |
| Negative Examples | {N} | ✅ / ⚠️ |
| Static Checks | {N} | ✅ / ⚠️ |

### Referenced Patterns

- {pattern_id_1}
- {pattern_id_2}
```

---

## Key Rules

### MUST

- **ALWAYS extract canonical routes** - 正解経路がないと AI が迷う
- **ALWAYS include FR references** - FR 番号がないと追跡できない
- **ALWAYS generate negative examples** - AI は悪例から学ぶ
- **ALWAYS validate output** - 検証チェックを実行

### NEVER

- **NEVER skip forbidden_actions extraction** - 禁止事項は必須
- **NEVER leave canonical_routes empty** - 空の場合は ERROR
- **NEVER omit detection patterns** - 静的検査が機能しなくなる

### WARNING Conditions

- `forbidden_actions` が空 → パターンに禁止事項がない可能性。確認必要。
- `spec_derived_guardrails` に FR 番号がない → 追跡が困難。確認必要。
- `acceptance_criteria` が空 → テスト生成ができない。確認必要。

### ERROR Conditions

- `canonical_routes` が空 → **パターンが不完全**。実行中止。
- パターン YAML が見つからない → **カタログ不整合**。実行中止。

---

## 次のフェーズへの連携

生成された `guardrails.yaml` は以下のフェーズで参照される:

| フェーズ | 参照方法 |
|---------|---------|
| `/speckit.plan` | Guardrails セクションに引用 |
| `/speckit.tasks` | タスクに Guardrail ID 紐付け |
| `/speckit.implement` | canonical_routes を正解経路として提示 |
| テスト生成 | acceptance_criteria から xUnit テスト生成 |

---

## 参照ファイル

| ファイル | 役割 |
|---------|------|
| `catalog/scaffolds/guardrails-template.yaml` | 出力テンプレート |
| `catalog/patterns/domain-ordered-queue.yaml` | オーケストレーションパターン |
| `catalog/patterns/domain-orchestrator.yaml` | Orchestratorパターン |
| `catalog/scaffolds/spec-test-template.yaml` | テスト生成連携 |
| `catalog/COMMON_MISTAKES.md` | 頻出ミス参照 |
