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

## Step 1: Load Context

1. Read plan.md
2. Extract **Guardrails** section（必須）
3. Extract **Catalog Binding** section
4. Extract **Query Semantics** section（Query タスクの場合）
5. Identify all Pattern IDs with status `matched` or `auto-applied`

---

## Step 2: Map Tasks to Patterns and Guardrails

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

**ルール**: すべての Guardrail は最低1つのタスクに紐付ける。

```markdown
| Guardrail | 対象タスク |
|-----------|-----------|
| GR-001 (FR-021) | LoanBoundaryService 実装 |
| GR-002 (FR-017) | ReservationValidationService 実装 |
| GR-003 (FR-018) | Reservation Entity 実装 |
```

**警告**: Guardrail が紐付いていないタスクがある場合、エラーとする。

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

```markdown
## Task N: {TaskName}

**Guardrails:** {GR-XXX (FR-YYY)} ← 関連する Guardrail があれば必須
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
