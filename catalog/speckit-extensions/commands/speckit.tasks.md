---
description: Generate tasks with embedded catalog references from plan.md
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Purpose

Generate tasks.md with **catalog references embedded in each task**.

This ensures:
- "Executing a task" = "Reading the catalog"
- AI cannot implement without knowing which pattern applies
- Each task has clear constraints from the catalog

---

## Step 0: Unsupported Intents Scan (MANDATORY)

**Before generating tasks, scan `unsupported_intents`.**

1. Read: `catalog/index.json` → `ai_decision_matrix.unsupported_intents`
2. If the user request contains matching keywords (通知/メール/バッチ/PDF等), **STOP** and ask for infra/library prerequisites.
3. Resume tasks generation only after prerequisites are clarified.

---

## タスク分解のルール

タスクを生成する際は、以下のルールに従うこと：

1. **粒度**: 1タスク = 開発者が 30〜90分で完了できる作業を目安に分解する
2. **Slice ごとの分離**: 各 Vertical Slice で「Domain」「Application（Command/Handler/Validator）」「UI（Store/Page）」を分けてタスク化する
3. **スキャフォールド vs ロジック**:
   - カタログのスキャフォールド生成（パターンの雛形生成）は **まとめタスク** にしてよい
   - ドメイン設計やビジネスロジックは Slice ごとにタスクを分ける
4. **依存関係**: タスク間の依存関係を明示し、並列実行可能なタスクを識別する

---

## Step 1: Load Context

1. Read plan.md
2. **Read guardrails.yaml**（存在する場合）
   ```
   Read: specs/{feature}/{slice}.guardrails.yaml

   Extract:
   - canonical_routes → CR-XXX
   - forbidden_actions → FA-XXX
   - spec_derived_guardrails → GR-XXX
   - acceptance_criteria → AC-XXX
   ```
3. Extract **Guardrails** section from plan.md（guardrails.yaml がない場合）
4. Extract **Catalog Binding** section
5. Extract **Query Semantics** section（Query タスクの場合）
6. Identify all Pattern IDs with status `matched` or `auto-applied`

---

## Step 2: Map Tasks to Patterns and Guardrails

> **Skills ヒント**: タスク生成時に以下の Skills が自動適用される可能性があります：
> - `vsa-pattern-selector`: パターン選択の妥当性検証
> - `vsa-boundary-modeler`: Boundary タスクの完全性検証
> - `vsa-implementation-guard`: 制約の自動抽出

For each implementation task, determine:
- Which Pattern ID applies
- The YAML path for that pattern
- Key constraints from `must_read_checklist`
- **Which Guardrails apply**（重要）
- **Which FR numbers are related**

**Mapping rules:**

| Task Type | Pattern ID | Key Constraints |
|-----------|-----------|-----------------|
| Handler実装 | transaction-behavior | SaveChangesAsync禁止 |
| Validator実装 | validation-behavior | 形式検証のみ、DBアクセス禁止 |
| Entity実装 | boundary-pattern | CanXxx()でBoundaryDecisionを返す |
| BoundaryService実装 | boundary-pattern | 委譲のみ、if文禁止 |
| UI実装 | boundary-pattern | ロジックを持たない、結果表示のみ |
| StateMachine実装 | domain-state-machine | Dictionary<State, List<State>>で遷移定義 |
| ValidationService実装 | domain-validation-service | 複数エンティティにまたがる検証、**validation_contract 必須** |
| QueryHandler実装 | query-service-pattern | **Query Semantics に従う** |

### Guardrail-to-Task Mapping（必須）

**ルール**: すべての Guardrail（CR/FA/GR/AC）は最低1つのタスクに紐付ける。

#### guardrails.yaml が存在する場合

```markdown
### Guardrail → Task Mapping

#### Canonical Routes (正解経路)
| ID | Operation | Target Task |
|----|-----------|-------------|
| CR-001 | 予約キャンセル | CancelReservationCommandHandler 実装 |

#### Forbidden Actions (禁止事項)
| ID | Forbidden | Target Task |
|----|-----------|-------------|
| FA-001 | reservation.Cancel() 直接呼び出し | CancelReservationCommandHandler 実装 |
| FA-002 | CheckAndPromoteNextAsync で代用 | CancelReservationCommandHandler 実装 |

#### Spec-Derived Guardrails
| ID | FR Ref | Target Task |
|----|--------|-------------|
| GR-001 | FR-021 | LoanBoundaryService 実装 |
| GR-002 | FR-017 | ReservationValidationService 実装 |

#### Acceptance Criteria
| ID | Criterion | Target Task |
|----|-----------|-------------|
| AC-001 | Ready予約者優先 | LoanBoundaryService 実装 |
| AC-002 | Position繰り上げ | CancelReservationCommandHandler 実装 |
```

#### guardrails.yaml がない場合（Legacy）

```markdown
| Guardrail | 対象タスク |
|-----------|-----------|
| GR-001 (FR-021) | LoanBoundaryService 実装 |
| GR-002 (FR-017) | ReservationValidationService 実装 |
| GR-003 (FR-018) | Reservation Entity 実装 |
```

**エラー条件**:
- Guardrail（CR/FA/GR/AC）が1つもタスクに紐付いていない → **ERROR**
- タスクが正解経路（CR）に従わない構造 → **WARNING**

### UI-IR-to-Task Mapping（UIがある場合は必須）

**ルール**: UI-IR が存在する場合、UI 実装タスクは UI-IR を参照すること。

1. **UI-IR 存在チェック**:
   - `specs/{feature}/{slice}.ui-ir.yaml` が存在するか確認
   - plan.md に「UI-IR Summary」セクションがあるか確認

2. **UI タスクへの UI-IR 参照追加**:
   ```markdown
   ## Task N: {Screen}Page 実装

   **UI-IR Reference:** `specs/{feature}/{slice}.ui-ir.yaml`
   **Pattern:** boundary-pattern

   **From UI-IR:**
   > - Primary Action: {action_name} (confirmation: {level})
   > - Maturity Level: {level}
   > - is_destructive: {true/false}
   > - disabled_when: Entity.CanXxx() → UI の disabled 属性
   ```

3. **UI-IR がない場合の対応**:
   - **WARNING**: plan.md の Phase 1.4 が実行されたか確認
   - Boundary セクションがあるのに UI-IR がない → **ERROR**
   - `/speckit.plan` を再実行して UI-IR を生成

---

## Step 3: Generate tasks.md

**Required format:**

```markdown
# Tasks

## Overview

| Task | Pattern | Status |
|------|---------|--------|
| Book Entity実装 | boundary-pattern | [ ] |
| LendBookCommandHandler | transaction-behavior | [ ] |
| LendBookCommandValidator | validation-behavior | [ ] |

---

## Task 1: Book Entity実装

**Pattern:** boundary-pattern
**YAML:** `catalog/patterns/boundary-pattern.yaml`

**Catalog Constraints:**
> - Entity.CanXxx() は BoundaryDecision を返す
> - BoundaryServiceに業務ロジックを書かない（委譲のみ）

**Acceptance Criteria:**
- [ ] CanBorrow() メソッドが BoundaryDecision を返す
- [ ] CanReturn() メソッドが BoundaryDecision を返す
- [ ] 業務ルールがEntityに集約されている

---

## Task 2: LendBookCommandHandler

**Pattern:** transaction-behavior
**YAML:** `catalog/patterns/transaction-behavior.yaml`

**Catalog Constraints:**
> - Handler内でSaveChangesAsyncを呼ばない
> - TransactionBehaviorに任せる

**Acceptance Criteria:**
- [ ] SaveChangesAsync() を呼んでいない
- [ ] Result<T> を返している
- [ ] Boundary.CanXxx() をチェックしてから処理

---

## Task 3: LendBookCommandValidator

**Pattern:** validation-behavior
**YAML:** `catalog/patterns/validation-behavior.yaml`

**Catalog Constraints:**
> - 形式検証のみ（DBアクセスしない）
> - AddValidatorsFromAssembly() でDI登録

**Acceptance Criteria:**
- [ ] DBアクセスをしていない
- [ ] NotEmpty, MaxLength などの形式検証のみ
```

---

## Task Template

Each task MUST include:

### 基本テンプレート

```markdown
## Task N: {TaskName}

**Guardrails:** {FA-XXX, CR-XXX, GR-XXX, AC-XXX} ← 関連するものをすべて記載
**関連 FR:** FR-XXX, FR-YYY

**Pattern:** {pattern-id}
**YAML:** `catalog/patterns/{pattern-id}.yaml`

**Catalog Constraints:**
> - {must_read_checklist item 1}
> - {must_read_checklist item 2}

**Acceptance Criteria:**
- [ ] {constraint 1 が守られている}
- [ ] {constraint 2 が守られている}
- [ ] **GR-XXX を満たしている** ← Guardrail がある場合は必須
```

### guardrails.yaml がある場合の拡張テンプレート

```markdown
## Task N: CancelReservationCommandHandler 実装

**Guardrails:** FA-001, FA-002, CR-001, GR-003, AC-002
**関連 FR:** FR-018

**Pattern:** transaction-behavior
**YAML:** `catalog/patterns/transaction-behavior.yaml`

### Canonical Route (CR-001)

> CancelReservationCommandHandler
>   → IReservationQueueService.DequeueAsync(reservationId, ct)
>     → Reservation.Cancel()
>     → PromotePositions(後続)
>     → PromoteNext(新しい先頭)

### Forbidden Actions

> - ❌ **FA-001**: reservation.Cancel() を直接呼ぶ
>   - 理由: Position繰り上げが行われない
> - ❌ **FA-002**: CheckAndPromoteNextAsync() で代用する
>   - 理由: Position再インデックスが行われない

### Catalog Constraints

> - Handler内でSaveChangesAsyncを呼ばない
> - Result<T> を返す

### Acceptance Criteria

- [ ] **CR-001** の正解経路に従っている（DequeueAsync を呼んでいる）
- [ ] **FA-001** を違反していない（Entity.Cancel() 直接呼び出しなし）
- [ ] **FA-002** を違反していない（CheckAndPromoteNextAsync 使用なし）
- [ ] **AC-002** のテストが通る（Position繰り上げ確認）
- [ ] SaveChangesAsync() を呼んでいない
- [ ] Result<T> を返している
```

### ValidationService タスクの追加要素

ValidationService 実装タスクには **validation_contract** を必ず含める。

```markdown
## Task N: ReservationValidationService 実装

**Guardrails:** GR-002 (FR-017)
**関連 FR:** FR-017

**Pattern:** domain-validation-service
**YAML:** `catalog/patterns/domain-validation-service.yaml`

**Validation Contract:**
```yaml
service: ReservationValidationService
method: ValidateCanReserveAsync
requires:
  - IBookRepository.GetByIdAsync(bookId) - 書籍存在確認
  - IBookCopyRepository.GetAvailableCopiesByBookIdAsync(bookId) - ★必須: 利用可能コピー確認
  - IMemberRepository.GetByIdAsync(memberId) - 会員存在・状態確認
  - IReservationRepository.GetActiveReservationsByMemberIdAsync(memberId) - 予約上限確認
ensures:
  - "書籍が存在すること"
  - "★ Available なコピーが0件であること (FR-017)"
  - "会員がアクティブであること"
  - "予約上限に達していないこと"
```

**Acceptance Criteria:**
- [ ] requires に列挙された全リポジトリメソッドを呼び出している
- [ ] ensures に列挙された全条件をチェックしている
- [ ] **GR-002 を満たしている（Available コピーがある場合は予約不可）**
```

### QueryHandler タスクの追加要素

QueryHandler 実装タスクには **Query Semantics** を必ず含める。

```markdown
## Task N: GetLoansQueryHandler 実装

**関連 FR:** （該当があれば）

**Pattern:** query-service-pattern
**YAML:** `catalog/patterns/query-service-pattern.yaml`

**Query Semantic:** 全 Loan 一覧 / Active 一覧

**使用すべき Repository メソッド:**
- GetAllLoansAsync（管理者用）
- GetActiveLoansByMemberIdAsync（会員用）

**使用禁止:**
- ❌ GetOverdueLoansAsync（これは GetOverdueLoansQuery 用）

**Acceptance Criteria:**
- [ ] MemberId が null → GetAllLoansAsync を呼んでいる
- [ ] MemberId が指定 → GetActiveLoansByMemberIdAsync を呼んでいる
- [ ] GetOverdueLoansAsync を呼んでいない
```

---

## Key Rules

- **NEVER create a task without Pattern reference** (if pattern applies)
- **ALWAYS include Catalog Constraints** from must_read_checklist
- **Acceptance Criteria must verify constraint compliance**
- Tasks without applicable pattern: mark as "Pattern: (none - creative)"
- **ALWAYS map Guardrails to tasks** - すべての Guardrail は最低1つのタスクに紐付ける
- **ALWAYS include FR references** - 関連する FR 番号をタスクに記載
- **ValidationService タスクには validation_contract 必須**
- **QueryHandler タスクには Query Semantics 必須**
- **Guardrail 違反は仕様違反** - Acceptance Criteria に Guardrail チェックを含める

---

## Pattern-less Tasks

Some tasks don't have a catalog pattern (creative areas):

```markdown
## Task N: UI Layout Design

**Pattern:** (none - creative)
**Reference:** plan.md > Creative Areas > UI Layout

**Acceptance Criteria:**
- [ ] {UI requirements from spec}
```

---

## Output

Generate `tasks.md` in the specs directory with:
1. Overview table (all tasks with patterns)
2. Individual task sections with catalog constraints
3. Acceptance criteria derived from constraints
