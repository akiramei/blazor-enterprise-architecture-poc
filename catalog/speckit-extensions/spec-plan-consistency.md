# Spec/Plan Consistency Rules (SSOT Principle)

> **目的**: Spec と Plan の整合性を保ち、仕様から実装への追跡可能性を確保する
>
> **バージョン**: v1.0.0
> **作成日**: 2025-12-09

---

## Core Principle: Spec is the Single Source of Truth

Spec.md は仕様の唯一の真実の源泉（SSOT）である。Plan.md は Spec を「どう実現するか」を決めるが、「何を実現するか」を変更してはならない。

```
Spec.md (WHAT)  ──→  Plan.md (HOW)  ──→  Tasks.md (WHO/WHEN)
     ↑                    │
     └────────────────────┘
       決定は Spec に反映
```

---

## Rule 1: Plan Addition Requires Spec Reflection (PLAN-SSOT-001)

### ルール

Plan で追加された制約・デフォルト値は、Spec の Assumptions セクションに反映されなければならない。

### 適用例

| Plan での決定 | 必要なアクション |
|---------------|-----------------|
| 予約上限: 3件をデフォルト | Spec.Assumptions に「予約上限は3件」を追記 |
| Ready 有効期限: 24時間 | Spec.Assumptions に「Ready状態は24時間で期限切れ」を追記 |
| ページサイズ: 20件 | Spec.Assumptions に「一覧のデフォルト件数は20件」を追記 |

### 違反検出ロジック

```
SCAN plan.md FOR:
  - "〜をデフォルトとする"
  - "specに明記なし"
  - "〜と仮定する"
  - numeric constraints (e.g., "3件", "24時間", "20件")

FOR EACH constraint C:
  IF C NOT IN spec.md.Assumptions
  THEN WARNING: "Plan constraint '{C}' is not reflected in Spec Assumptions"
```

### 修正ガイダンス

1. Plan の "Unknowns Resolved" セクションを確認
2. 各決定を Spec.Assumptions に追記
3. 追記後、Plan から Spec への参照を追加

```markdown
# spec.md - Assumptions に追記
## Assumptions
- Maximum active reservations per member is 3 (default limit)
- Ready reservation expires after 24 hours if not picked up
```

---

## Rule 2: Spec Attribute Preservation (PLAN-SSOT-002)

### ルール

Spec に定義された属性・Enum値は Plan から欠落してはならない。

### 適用例

| Spec の定義 | Plan に必要な内容 |
|-------------|------------------|
| `ReservationStatus: Waiting, Ready, Completed, Cancelled` | 4つ全ての値を Plan に記載 |
| `Reservation.Position: int (required)` | Position を data-model に含める |
| `Loan.DueDate: DateTime (required)` | DueDate を data-model に含める |

### 違反検出ロジック

```
EXTRACT spec.entities[E].status_values AS spec_values
EXTRACT plan.entities[E].status_values AS plan_values

FOR EACH value V IN spec_values:
  IF V NOT IN plan_values
  THEN ERROR: "Enum value '{V}' from Spec.{E} is missing in Plan"

EXTRACT spec.entities[E].attributes WHERE required = true AS spec_attrs
EXTRACT plan.data_model.entities[E].attributes AS plan_attrs

FOR EACH attr A IN spec_attrs:
  IF A NOT IN plan_attrs
  THEN ERROR: "Required attribute '{A}' from Spec.{E} is missing in Plan data-model"
```

### 修正ガイダンス

1. Spec の Key Entities セクションを確認
2. 全ての status/enum 値を Plan にコピー
3. required 属性が data-model に含まれているか確認

---

## Rule 3: Decision Documentation (PLAN-SSOT-003)

### ルール

曖昧な仕様に対する決定は「Unknowns Resolved」セクションに記録し、その決定は Spec の Assumptions に反映される。

### フォーマット

```markdown
## Unknowns Resolved

| 項目 | Spec の状態 | 決定 | 理由 | Spec 反映 |
|------|------------|------|------|:---------:|
| 予約上限 | 明記なし | 3件 | 図書館業界の標準的な慣行 | Y |
| Ready 有効期限 | 明記なし | 24時間 | 一般的な取り置き期間 | Y |
```

### 違反検出ロジック

```
SCAN plan.md "Unknowns Resolved" section
FOR EACH decision D:
  IF D.spec_reflected != "Y"
  THEN WARNING: "Decision '{D.item}' is not reflected in Spec"

  IF D.value NOT IN spec.md.Assumptions
  THEN ERROR: "Decision '{D.item}={D.value}' marked as reflected but not found in Spec.Assumptions"
```

### 修正ガイダンス

1. 全ての Unknowns Resolved の決定を確認
2. "Spec 反映" 列を追加し、反映状況を追跡
3. 未反映の決定は Spec.Assumptions に追記後、"Y" にマーク

---

## Consistency Check Workflow

### /speckit.plan 実行時

```
1. Spec を読み込む
2. Plan を生成
3. **Consistency Check**:
   a. Plan の制約を抽出
   b. Spec.Assumptions と照合
   c. 不一致があれば WARNING
4. Plan 出力
```

### /speckit.analyze 実行時

```
1. Spec, Plan, Tasks を読み込む
2. **PLAN-SSOT-001 Check**: Plan 制約 → Spec 反映確認
3. **PLAN-SSOT-002 Check**: Spec 属性 → Plan 保持確認
4. **PLAN-SSOT-003 Check**: Unknowns Resolved → Spec 反映確認
5. Consistency Issues をレポート
6. Remediation Suggestions を提示
```

---

## Integration with spec-kit Commands

### speckit.plan

Phase 1.75 として追加:

```markdown
### Phase 1.75: Spec/Plan Consistency Check

1. Scan Plan for constraints/defaults not in Spec
2. Verify all Spec Enum values are preserved
3. Output warnings if inconsistencies found
```

### speckit.analyze

Consistency Detection セクションを追加:

```markdown
### Consistency Issues (Spec/Plan)

| ID | Severity | Issue | Location | Recommendation |
|----|----------|-------|----------|----------------|
```

---

## Examples

### Good Example

```markdown
# spec.md
## Assumptions
- Maximum active reservations per member is 3

# plan.md
## Unknowns Resolved
| 項目 | Spec の状態 | 決定 | Spec 反映 |
|------|------------|------|:---------:|
| 予約上限 | Assumptions に追記済み | 3件 | Y |
```

### Bad Example (Violation)

```markdown
# spec.md
## Assumptions
(予約上限の記載なし)

# plan.md
## Unknowns Resolved
- 予約上限: specに明記なし → 3件をデフォルトとする

# Analysis Result
WARNING: Plan constraint '予約上限3件' is not reflected in Spec Assumptions
```

---

## Related Documents

- `constitution-additions.md` - Constitution への統合
- `decision-guide.md` - 曖昧さ解消ガイド
- `SPEC_KIT_GUARDRAILS_DESIGN.md` - Guardrails 設計
