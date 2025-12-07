---
description: Execute the implementation planning workflow with catalog integration.
handoffs:
  - label: Create Tasks
    agent: speckit.tasks
    prompt: Break the plan into tasks
    send: true
  - label: Create Checklist
    agent: speckit.checklist
    prompt: Create a checklist for the following domain...
scripts:
  sh: scripts/bash/setup-plan.sh --json
  ps: scripts/powershell/setup-plan.ps1 -Json
agent_scripts:
  sh: scripts/bash/update-agent-context.sh __AGENT__
  ps: scripts/powershell/update-agent-context.ps1 -AgentType __AGENT__
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Outline

1. **Setup**: Run `{SCRIPT}` from repo root and parse JSON for FEATURE_SPEC, IMPL_PLAN, SPECS_DIR, BRANCH.

2. **Load context**: Read FEATURE_SPEC and `/memory/constitution.md`. Load IMPL_PLAN template.

3. **Execute plan workflow**: Follow the structure in IMPL_PLAN template to:
   - Fill Technical Context (mark unknowns as "NEEDS CLARIFICATION")
   - Fill Constitution Check section from constitution
   - Evaluate gates (ERROR if violations unjustified)
   - Phase 0: Generate research.md (resolve all NEEDS CLARIFICATION)
   - **Phase 0.5: Catalog Pattern Selection** (CATALOG EXTENSION - see below)
   - Phase 1: Generate data-model.md, contracts/, quickstart.md
   - **Phase 1.5: Design-Level COMMON_MISTAKES Check** (CATALOG EXTENSION - see below)
   - Re-evaluate Constitution Check post-design

4. **Stop and report**: Command ends after Phase 1.5 (before tasks).

## Phases

### Phase 0: Outline & Research

(Standard spec-kit phase - unchanged)

1. Extract unknowns from Technical Context
2. Generate and dispatch research agents
3. Consolidate findings in research.md

### Phase 0.25: Guardrails Extraction (CRITICAL - NEW)

**This phase extracts business rules that MUST NOT be violated.**

背景: 図書館ドッグフーディングで FR-017, FR-021 などの重要ルールが
実装まで伝播せず、仕様違反が発生した。Guardrails で強制伝播する。

1. **Read feature spec and identify Guardrails**:

   Guardrail の条件:
   - 「〜のみ可能」「〜の場合のみ」という前提条件
   - 「〜が優先」「〜が先」という優先権ルール
   - 違反するとビジネス上の問題が発生するルール

2. **Extract and format Guardrails**:

   ```markdown
   ## Guardrails（絶対遵守）

   以下のルールは実装で必ず満たすこと。違反は仕様違反とみなす。

   | ID | FR | ルール | 対象スコープ | 違反時の問題 |
   |----|----|----|------|-------------|
   | GR-001 | FR-021 | Ready 状態の予約者が最優先で貸出権を持つ | LoanBoundaryService | Ready 予約者以外に貸出してしまう |
   | GR-002 | FR-017 | Available なコピーがある間は予約不可 | ReservationValidationService | 不要な予約を受け付けてしまう |
   | GR-003 | FR-018 | 予約は先着順（Position で管理） | Reservation Entity | 順番が管理できない |
   ```

3. **Guardrail identification checklist**:

   spec を読む際に以下をチェック：
   - [ ] 「〜のみ」「〜だけ」「〜の場合のみ」という文言があるか？
   - [ ] 「優先」「先着」「順番」という文言があるか？
   - [ ] 複数の条件を満たす必要がある操作があるか？
   - [ ] 状態に依存する操作可否判定があるか？

4. **Output**: plan.md に Guardrails セクションを追加

**警告**: Guardrails が0件の場合、spec に前提条件が明示されていない可能性がある。
その場合は research フェーズで確認すること。

### Phase 0.5: Catalog Pattern Selection (CATALOG EXTENSION)

**This phase is added by the catalog. DO NOT skip.**

1. **Read catalog index**:
   ```
   Read: catalog/index.json
   Read: catalog/DECISION_FLOWCHART.md
   Read: catalog/CHARACTERISTICS_CATALOG.md
   ```

2. **Extract characteristics from feature spec**:
   - Operation type: `op:mutates-state`, `op:read-only`
   - Cross-cutting: `xcut:auth`, `xcut:audit`, `xcut:validation`
   - Structure: `struct:single-aggregate`, `struct:state-machine`

3. **Match patterns from catalog**:
   - Use `ai_decision_matrix` in index.json to find pattern IDs
   - For each matched pattern, read: `catalog/patterns/{pattern-id}.yaml`
   - Note `ai_guidance.common_mistakes` from each pattern

4. **Generate Catalog Binding section** in plan.md:

   ```markdown
   ## Catalog Binding

   | Requirement | Pattern ID | Status | must_read_checklist 引用（実装時に記入） |
   |-------------|-----------|--------|----------------------------------------|
   | 機能作成 | feature-create-entity | matched | （実装時に引用を記入） |
   | 入力検証 | validation-behavior | auto-applied | （実装時に引用を記入） |
   | 状態遷移 | domain-state-machine | matched | （実装時に引用を記入） |

   ### 引用記入ルール（実装フェーズで使用）

   実装開始前に、各パターンの `must_read_checklist` から引用を記入すること。
   引用することで「読んだつもり」を防止する。

   **引用例:**
   ```
   | validation-behavior | auto-applied | > Handler内でSaveChangesAsyncを呼ばない |
   ```

   ### Unmatched Requirements
   - {requirement}: {reason why no pattern matches}

   ### Creative Areas
   - Domain Model: {entities to design}
   - Validation Logic: {rules to implement}
   - UI Layout: {screens to design}
   ```

5. **Boundary modeling** (if UI is involved):
   - Read: `catalog/patterns/boundary-pattern.yaml`
   - List all Intents (user intentions)
   - Design Entity.CanXxx() methods for each Intent

   ```markdown
   ## Boundary

   ### Intents
   | Intent | Entity.CanXxx() | Decision Logic |
   |--------|-----------------|----------------|

   ### Conversation Script
   | User Intent | System Response |
   |-------------|-----------------|
   ```

**Output**: Plan with Catalog Binding, Boundary sections complete

### Phase 1: Design & Contracts

(Standard spec-kit phase with catalog awareness)

1. Extract entities from feature spec → data-model.md
   - Include **Entity.CanXxx() methods from Boundary section**
   - **属性の強制反映チェック（CRITICAL - NEW）**

2. Generate API contracts from functional requirements
   - Use MediatR patterns from catalog (not REST endpoints)
   - **Query Semantics の明示（CRITICAL - NEW）**

3. Agent context update

#### 1.1 属性の強制反映チェック（FR-018 対策）

背景: 図書館ドッグフーディングで `Position` 属性が spec に明記されていたにもかかわらず、
data-model から欠落した。

**ルール**: spec に明記された属性は data-model から落としてはならない。

**チェック手順**:

1. spec から全エンティティの属性を抽出
2. data-model.md と照合
3. 欠落があればエラー

```markdown
## Attribute Enforcement Check

| Entity | Attribute | Spec | data-model | Status |
|--------|-----------|:----:|:----------:|:------:|
| Reservation | Position | ✅ | ✅ | OK |
| Reservation | Status | ✅ | ✅ | OK |
| Loan | DueDate | ✅ | ❌ | **ERROR** |

**欠落属性**: Loan.DueDate が data-model にありません。追加してください。
```

**特に注意すべき属性**:
- Position, Order, Sequence（順序管理）
- Status, State（状態管理）
- Priority, Rank（優先度）
- Limit, Max, Min（上限/下限）

#### 1.2 Query Semantics の明示（Query バグ対策）

背景: 図書館ドッグフーディングで `GetLoansQuery` が一般一覧と Overdue の
両方の意味を持ち、実装で混同された（コピペバグ）。

**ルール**: Query の意味（semantic）を plan で明示し、使用すべき Repository メソッドを指定する。

```markdown
## Query Semantics

| Query | Semantic | Repository Method | 禁止 |
|-------|----------|-------------------|------|
| GetLoansQuery | 全 Loan 一覧 / Active 一覧 | GetAllLoansAsync, GetActiveLoansByMemberIdAsync | ❌ GetOverdueLoansAsync |
| GetOverdueLoansQuery | Overdue 専用 | GetOverdueLoansAsync | ❌ GetActiveLoansByMemberIdAsync |
| GetReservationsQuery | 予約一覧 | GetReservationsByMemberIdAsync | - |

**注意**: Query と Repository メソッドの対応を間違えないこと。
```

**チェック項目**:
- [ ] 同じ Repository メソッドを異なる Query で使いまわしていないか？
- [ ] Query の semantic が明確に区別されているか？
- [ ] 「〜専用」の Query が他の用途に使われていないか？

### Phase 1.5: Design-Level COMMON_MISTAKES Check (CRITICAL - AUTO)

**This check is MANDATORY and runs AUTOMATICALLY after Phase 1.**

Do NOT wait for user instruction. Execute this phase immediately after Phase 1.

**Why automatic?**
- Human review is error-prone and inconsistent
- AI-generated plan should be AI-validated
- "Let machines do what machines can do" (spec-kit philosophy)
- Catching violations here prevents them from propagating to all tasks

---

#### Step 1: Read COMMON_MISTAKES.md

```
Read: catalog/COMMON_MISTAKES.md
```

#### Step 2: Check plan against each mistake

Review the plan and check for these common violations:

| Check Item | What to Look For |
|------------|------------------|
| SaveChangesAsync in Handler | Handler設計がSaveChangesAsyncを呼ぶ前提になっていないか |
| Singleton/Scoped混在 | サービス登録がSingletonになっていないか |
| BoundaryServiceに業務ロジック | BoundaryService設計にif文（業務判定）が含まれていないか |
| Entity.CanXxx()の欠如 | Boundary設計でEntity.CanXxx()が定義されているか |
| Result<T>不使用 | 例外をthrowする設計になっていないか |
| Feature sliceの逸脱 | 共通サービスに機能固有ロジックが含まれていないか |

#### Step 3: Self-Correction Loop (IMPORTANT)

```
WHILE violations exist:
    1. Identify the violation
    2. Determine the correction based on catalog patterns
    3. Apply the correction to the plan
    4. Re-check the corrected plan

UNTIL all checks pass
```

**This loop runs automatically. Do NOT ask user for permission to fix.**

#### Step 4: Output corrected plan

If corrections were made, output the **corrected version** of the plan:

```markdown
## Design-Level Check (COMMON_MISTAKES)

### Check Results

| Check Item | Initial | After Correction |
|------------|:-------:|:----------------:|
| SaveChangesAsync | ❌ | ✅ |
| Singleton/Scoped | ✅ | ✅ |
| BoundaryService | ❌ | ✅ |
| Entity.CanXxx() | ✅ | ✅ |
| Result<T> | ✅ | ✅ |
| Feature slice | ✅ | ✅ |

### Violations Found & Corrected

| Violation | Location | Correction Applied |
|-----------|----------|-------------------|
| Handler calls SaveChangesAsync | LendBookCommandHandler設計 | TransactionBehaviorに任せる設計に修正 |
| BoundaryService has business logic | BookBoundaryService設計 | Entity.CanBorrow()に委譲する設計に修正 |

### Corrected Plan Sections

(Output only the sections that were modified)

#### Before:
```
LendBookCommandHandler:
  - Call repository.AddAsync()
  - Call dbContext.SaveChangesAsync()  ← violation
```

#### After:
```
LendBookCommandHandler:
  - Call repository.AddAsync()
  - Return Result.Success()  ← TransactionBehavior handles SaveChanges
```
```

#### Step 5: Final gate check

```
IF all checks pass (after self-correction):
  → Output: "✅ Design-Level Check PASSED. Ready for /speckit.tasks"
  → Proceed to tasks

IF unable to auto-correct (requires spec clarification):
  → Output: "❌ Design-Level Check FAILED. Manual intervention required."
  → List unresolvable issues
  → STOP - Do NOT proceed to tasks
```

**Output**: Corrected plan with Design-Level Check section complete

---

## Key Rules

- Use absolute paths
- ERROR on gate failures or unresolved clarifications
- **NEVER skip Guardrails Extraction phase** - 重要ルールの伝播漏れを防ぐ
- **NEVER skip Catalog Binding phase** - Constitution requires it
- **NEVER skip Design-Level Check phase** - Runs AUTOMATICALLY after Phase 1
- **NEVER ask user permission to fix violations** - Self-correct automatically
- **NEVER implement patterns that exist in catalog** - use templates
- **NEVER drop attributes from spec** - 属性の強制反映チェックを実行
- **ALWAYS define Query Semantics** - Query と Repository の対応を明示
- If Boundary section is empty for UI features → ERROR
- If Design-Level Check cannot auto-correct → ERROR (do not proceed to tasks)
- If Attribute Enforcement Check fails → ERROR (do not proceed)
- If Guardrails section is empty → WARNING (confirm with spec)
- **ALWAYS output corrected plan** if violations were found and fixed

---

## Task Generation Guidelines (for speckit.tasks handoff)

When handing off to `speckit.tasks`, ensure:

1. **Catalog Binding is complete** - Pattern IDs are assigned to requirements
2. **Each task maps to a Pattern** - Use the mapping below

### Task-to-Pattern Mapping

| Task Type | Pattern ID |
|-----------|-----------|
| Handler実装 | transaction-behavior |
| Validator実装 | validation-behavior |
| Entity実装（CanXxx） | boundary-pattern |
| BoundaryService実装 | boundary-pattern |
| UI実装 | boundary-pattern |
| StateMachine実装 | domain-state-machine |
| ValidationService実装 | domain-validation-service |

### Task Format Requirement

Each task in tasks.md MUST include:
- **Pattern:** {pattern-id}
- **YAML:** catalog/patterns/{pattern-id}.yaml
- **Catalog Constraints:** quoted from must_read_checklist
- **Acceptance Criteria:** derived from constraints

See: `catalog/speckit-extensions/commands/speckit.tasks.md` for details
