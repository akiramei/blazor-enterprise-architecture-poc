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
   - Re-evaluate Constitution Check post-design

4. **Stop and report**: Command ends after Phase 2 planning.

## Phases

### Phase 0: Outline & Research

(Standard spec-kit phase - unchanged)

1. Extract unknowns from Technical Context
2. Generate and dispatch research agents
3. Consolidate findings in research.md

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

   | Requirement | Pattern ID | Status | Notes |
   |-------------|-----------|--------|-------|
   | 機能作成 | feature-create-entity | matched | |
   | 入力検証 | validation-behavior | auto-applied | |

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

2. Generate API contracts from functional requirements
   - Use MediatR patterns from catalog (not REST endpoints)

3. Agent context update

## Key Rules

- Use absolute paths
- ERROR on gate failures or unresolved clarifications
- **NEVER skip Catalog Binding phase** - Constitution requires it
- **NEVER implement patterns that exist in catalog** - use templates
- If Boundary section is empty for UI features → ERROR
