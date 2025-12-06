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
2. Extract **Catalog Binding** section
3. Identify all Pattern IDs with status `matched` or `auto-applied`

---

## Step 2: Map Tasks to Patterns

For each implementation task, determine:
- Which Pattern ID applies
- The YAML path for that pattern
- Key constraints from `must_read_checklist`

**Mapping rules:**

| Task Type | Pattern ID | Key Constraints |
|-----------|-----------|-----------------|
| Handler実装 | transaction-behavior | SaveChangesAsync禁止 |
| Validator実装 | validation-behavior | 形式検証のみ、DBアクセス禁止 |
| Entity実装 | boundary-pattern | CanXxx()でBoundaryDecisionを返す |
| BoundaryService実装 | boundary-pattern | 委譲のみ、if文禁止 |
| UI実装 | boundary-pattern | ロジックを持たない、結果表示のみ |
| StateMachine実装 | domain-state-machine | Dictionary<State, List<State>>で遷移定義 |
| ValidationService実装 | domain-validation-service | 複数エンティティにまたがる検証 |

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

**Pattern:** {pattern-id}
**YAML:** `catalog/patterns/{pattern-id}.yaml`

**Catalog Constraints:**
> - {must_read_checklist item 1}
> - {must_read_checklist item 2}

**Acceptance Criteria:**
- [ ] {constraint 1 が守られている}
- [ ] {constraint 2 が守られている}
```

---

## Key Rules

- **NEVER create a task without Pattern reference** (if pattern applies)
- **ALWAYS include Catalog Constraints** from must_read_checklist
- **Acceptance Criteria must verify constraint compliance**
- Tasks without applicable pattern: mark as "Pattern: (none - creative)"

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
