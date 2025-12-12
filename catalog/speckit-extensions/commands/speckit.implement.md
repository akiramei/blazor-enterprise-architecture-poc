---
description: Execute the implementation workflow with mandatory catalog reading.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Outline

1. **Phase 0 - Catalog Reading**: Read plan.md, quote from catalog YAMLs, describe "How it affects"
2. **Phase 1 - Implementation Plan**: List files with responsibilities, patterns, mistakes to avoid
3. **Phase 2 - Implementation**: Execute implementation following the plan
4. **Phase 3 - Verification**: Check implementation against quoted checklist items

---

## Phase 0: Mandatory Catalog Reading (CRITICAL)

**This phase is MANDATORY. Do NOT skip.**

> **Skills ヒント**: 実装時に `vsa-implementation-guard` の知識が自動的に適用される可能性があります。
> SaveChangesAsync 禁止、Result<T> パターン強制などの禁止事項は Skills が提供します。

### Step 0.0: Unsupported Intents Scan (FIRST)

**MUST scan unsupported_intents BEFORE quoting patterns.**

1. Read: `catalog/index.json` → `ai_decision_matrix.unsupported_intents`
2. If the request contains matching keywords (通知/メール/バッチ/PDF等), **STOP** and ask for infra/library prerequisites.
3. Continue Phase 0 only after prerequisites are clarified.

### Why This Phase Exists

```
【Problem】
AI can write "working code" without reading catalog YAMLs.
However, this bypasses best practices and pitfall avoidance documented in the catalog.

【Solution】
Require explicit quotes from catalog YAMLs to prove they were read.
Quotes force actual reading, not just checking a box.
```

### Step 0.1: Identify Required Patterns

Read **tasks.md** first (if exists), then plan.md:

**From tasks.md (preferred):**
```markdown
## Task 1: Book Entity実装

**Pattern:** boundary-pattern
**YAML:** `catalog/patterns/boundary-pattern.yaml`

**Catalog Constraints:**
> - Entity.CanXxx() は BoundaryDecision を返す
```

**From plan.md (fallback):**
```markdown
| Pattern ID | Status |
|------------|--------|
| boundary-pattern | matched |
| validation-behavior | auto-applied |
```

**Priority:**
1. If tasks.md exists with Catalog Constraints → use those (already quoted)
2. If only plan.md exists → read YAMLs and quote yourself

### Step 0.2: Read and Quote Each Pattern

For each pattern with status `matched` or `auto-applied`:

1. **Read the YAML file**: `catalog/patterns/{pattern-id}.yaml`
2. **Find `must_read_checklist`** (if exists) or `ai_guidance.common_mistakes`
3. **Quote the key points** in implementation notes

**Required format:**

```markdown
## Implementation Notes - Catalog Constraints

### From boundary-pattern.yaml

**Checklist:**
> - Entity.CanXxx() は BoundaryDecision を返す
> - BoundaryService は委譲のみ、if文は書かない
> - Handler は CanXxx() の結果を必ずチェック

**How it affects this feature:**
- Book.CanBorrow(), Copy.CanLend() を実装する必要がある
- LoanBoundaryService は Book/Copy の CanXxx() に委譲するだけ
- LendBookCommandHandler で CanLend() をチェックしてから貸出処理

### From transaction-behavior.yaml

**Checklist:**
> - Handler 内で SaveChangesAsync を呼ばない
> - TransactionBehavior に任せる

**How it affects this feature:**
- LendBookCommandHandler で SaveChangesAsync を呼ばない
- Loan エンティティの追加後、そのまま return する

### From validation-behavior.yaml

**Checklist:**
> - Validator は DI 登録する（AddValidatorsFromAssembly）
> - DBアクセスを伴う検証は Validator に書かない

**How it affects this feature:**
- LendBookCommandValidator は形式検証のみ（MemberId, CopyId の空チェック）
- 「会員が存在するか」「蔵書が貸出可能か」は Handler で確認
```

### Step 0.3: Read COMMON_MISTAKES.md

**Always read**: `catalog/COMMON_MISTAKES.md`

Quote at least 3 relevant items:

```markdown
### From COMMON_MISTAKES.md
> - Handler内でSaveChangesAsync()を呼ばない（TransactionBehaviorが自動実行）
> - BoundaryServiceに業務ロジック（if文）を書かない
> - Value Objectの比較はインスタンス同士で行う
```

---

## Phase 1: Implementation Plan

After Phase 0 constraints are documented, create an implementation plan.

### Step 1.1: List Files to Create/Modify

Based on catalog constraints and plan.md, list all files:

```markdown
## Implementation Plan

### Files to Create

| File | Responsibility | Patterns Applied | COMMON_MISTAKES to Avoid |
|------|---------------|------------------|-------------------------|
| Book.cs | 蔵書エンティティ + CanBorrow() | boundary-pattern | BoundaryServiceに業務ロジックを書かない |
| LendBookCommand.cs | 貸出コマンド定義 | - | - |
| LendBookCommandHandler.cs | 貸出処理オーケストレーション | transaction-behavior | SaveChangesAsyncを呼ばない |
| LendBookCommandValidator.cs | 形式検証 | validation-behavior | DBアクセスをValidatorで行わない |
| BookBoundaryService.cs | Entity.CanXxx()への委譲 | boundary-pattern | if文で業務ロジックを書かない |

### Files to Modify

| File | Change | Reason |
|------|--------|--------|
| Program.cs | DI登録追加 | BoundaryService, Validator |
```

### Step 1.2: Verify Plan Against Constraints

Before proceeding, check:

```
□ Each file has clear responsibility
□ Patterns are correctly assigned to files
□ COMMON_MISTAKES are identified per file
□ No pattern constraint is violated in the plan
```

---

## Phase 2: Implementation

After Phase 1 plan is documented, proceed with implementation.

### During Implementation

**For each file, check its assigned constraints:**
- Handler files → transaction-behavior constraints
- Validator files → validation-behavior constraints
- Boundary files → boundary-pattern constraints
- UI files → DTO naming, API signature (COMMON_MISTAKES)

---

## Phase 3: Verification

After implementation, verify against catalog checklist:

```markdown
## Post-Implementation Verification

| Quote Item | Verified? | Evidence |
|------------|:---------:|----------|
| SaveChangesAsync not in Handler | ✅ | Line 45-50 of Handler.cs |
| Entity.CanXxx() returns BoundaryDecision | ✅ | Book.cs:120 |
| BoundaryService delegates to Entity | ✅ | BookBoundaryService.cs:35 |
```

---

## Key Rules

- **NEVER skip Phase 0** - quotes prove catalog was read
- **NEVER write "I read the file"** without actual quotes
- **ALWAYS document quotes** before starting implementation
- If unable to find `must_read_checklist`, quote from `ai_guidance.common_mistakes`
- Quotes should be specific and actionable, not generic statements

---

## Error Conditions

| Condition | Action |
|-----------|--------|
| Phase 0 skipped | ERROR - must read and quote catalogs |
| No quotes documented | ERROR - must provide specific quotes |
| Implementation contradicts quotes | ERROR - fix implementation |
| Pattern YAML not found | WARN - document and continue |

---

## Example Workflow

```
Phase 0: Catalog Reading
------------------------
1. Read plan.md → Catalog Binding shows:
   - boundary-pattern (matched)
   - validation-behavior (auto-applied)
   - transaction-behavior (auto-applied)

2. Read catalog/patterns/boundary-pattern.yaml
   → Quote must_read_checklist items
   → Write "How it affects this feature"

3. Read catalog/patterns/validation-behavior.yaml
   → Quote must_read_checklist items
   → Write "How it affects this feature"

4. Read catalog/patterns/transaction-behavior.yaml
   → Quote must_read_checklist items
   → Write "How it affects this feature"

5. Read catalog/COMMON_MISTAKES.md
   → Quote 3+ relevant items

Phase 1: Implementation Plan
----------------------------
6. List files to create/modify
   → For each file: responsibility, patterns, mistakes to avoid

7. Verify plan against constraints

Phase 2: Implementation
-----------------------
8. Implement each file, checking assigned constraints

Phase 3: Verification
---------------------
9. Post-implementation verification against quotes
   → Evidence for each constraint
```
