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

## このコマンドの目的

- SPEC の内容を元に、Vertical Slice + パターンカタログ前提の技術計画を作ること
- 選択されたパターンは後続の `/speckit.tasks`, `/speckit.implement` から再利用される
- Boundary セクション（UI がある場合）と Catalog Binding セクションが必須出力

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

### Phase 0.25: Guardrails Extraction (CRITICAL)

**This phase extracts business rules that MUST NOT be violated.**

> **詳細**: `catalog/AI_GUARDRAILS.md` の「Phase 0.25: Guardrails 抽出」を参照

1. SPEC から「〜のみ」「優先」「先着」などの重要ルールを抽出
2. Guardrails セクションとして plan.md に追加
3. 各 Guardrail に ID、FR番号、対象スコープ、違反時の問題を記載

**警告**: Guardrails が0件の場合、spec に前提条件が明示されていない可能性がある。

### Phase 0.5: Catalog Pattern Selection (CATALOG EXTENSION)

**This phase is added by the catalog. DO NOT skip.**

> **Skills ヒント**: パターン選択時に `vsa-pattern-selector` の知識が自動的に適用される
> 可能性があります。Feature Slices、Query Patterns、Domain Patterns の選択基準は
> Skills が提供します。

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

   > **Skills ヒント**: UI がある場合、`vsa-boundary-modeler` の知識が自動的に適用される
   > 可能性があります。Intent 定義、Entity.CanXxx() 設計、BoundaryService の責務分離は
   > Skills が提供します。

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

> **詳細**: `catalog/AI_GUARDRAILS.md` の「属性の強制反映チェック」を参照

**ルール**: spec に明記された属性は data-model から落としてはならない。

特に注意すべき属性: Position, Order, Status, State, Priority, Limit

#### 1.2 Query Semantics の明示（Query バグ対策）

> **詳細**: `catalog/AI_GUARDRAILS.md` の「Query Semantics の明示」を参照

**ルール**: Query の意味（semantic）を plan で明示し、使用すべき Repository メソッドを指定する。

### Phase 1.5: Design-Level COMMON_MISTAKES Check (CRITICAL - AUTO)

**This check is MANDATORY and runs AUTOMATICALLY after Phase 1.**

> **詳細**: `catalog/AI_GUARDRAILS.md` の「Phase 1.5: Self-correction Loop」を参照

> **Skills ヒント**: 実装計画チェック時に `vsa-implementation-guard` の知識が自動的に適用される
> 可能性があります。禁止事項、必須パターンは Skills が提供します。

**要約**:
1. `catalog/COMMON_MISTAKES.md` を読む
2. 計画を各ミスパターンと照合
3. 違反があれば自己修正ループで自動修正（ユーザー許可不要）
4. 修正版を出力
5. 全チェック通過 → tasks へ進む / 自動修正不可 → STOP

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
