# speckit.workpack - Workpack Generation Command

## Overview

specs/ から workpack（自己完結型入力パッケージ）を生成するコマンド。

**目的**: Phase A（設計）から Phase B（実装）への橋渡し

## Syntax

```
/speckit.workpack <task-id> --spec <spec-path> [options]
```

## Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `<task-id>` | Yes | タスクID（例: T001-create-book-entity） |
| `--spec <path>` | Yes | spec.yaml のパス（例: specs/loan/LendBook） |
| `--output <dir>` | No | 出力先（デフォルト: workpacks/active/{task-id}/） |

## Example

```bash
/speckit.workpack T001-create-book-entity --spec specs/loan/LendBook
```

---

## Workflow

### Phase 0: Input Validation

```
1. Check: task-id format (T{number}-{description})
2. Check: spec path exists
   - specs/{feature}/{slice}.spec.yaml
   - specs/{feature}/{slice}.guardrails.yaml
   - specs/{feature}/{slice}.decisions.yaml (optional)
   - manifests/{feature}/{slice}.manifest.yaml
3. Check: workpacks/active/{task-id}/ does not exist
```

### Phase 1: Create Workpack Directory

```
workpacks/active/{task-id}/
├── task.md
├── spec.extract.md
├── policy.yaml
├── guardrails.yaml
└── repo.snapshot.md
```

### Phase 2: Generate task.md

**Input**: plan.md + tasks.yaml + task-id
**Template**: workpacks/_templates/task.template.md

```markdown
# Task: {task-id}

## Meta
- ID: {task-id}
- Feature: {from spec path}
- Slice: {from spec path}
- Pattern: {from manifest.yaml}

## Objective
{from tasks.yaml or plan.md}

## Acceptance Criteria
{from tasks.yaml}

## Expected Output
{from plan.md file structure section}
```

### Phase 3: Generate spec.extract.md

**Input**: spec.yaml + manifest.yaml
**Template**: workpacks/_templates/spec.extract.template.md

**Extraction Rules**:
1. **Relevant FRs only**: このタスクに関連するFRのみ抽出
2. **Boundary section**: UI タスクの場合は必須
3. **Domain rules**: Entity.CanXxx() に関連するルール
4. **Pattern constraints**: catalog/patterns/{pattern-id}.yaml の must_read_checklist

```yaml
# Extract from manifest.yaml
catalog_binding:
  from_catalog:
    - id: feature-create-entity
      # → catalog/patterns/feature-create-entity.yaml を読む
      # → must_read_checklist を抽出
```

### Phase 4: Generate policy.yaml

**Input**: decisions.yaml + manifest.yaml + command-spec.yaml
**Template**: workpacks/_templates/policy.template.yaml

**Generation Rules**:
1. **core_values**: decisions.yaml の離散化された値
2. **rules**: command-spec.yaml の state_transitions, preconditions
3. **patterns**: manifest.yaml の catalog_binding
4. **generation_notes**: COMMON_MISTAKES.md からの抜粋

### Phase 5: Generate guardrails.yaml

**Input**: specs/{feature}/{slice}.guardrails.yaml
**Template**: workpacks/_templates/guardrails.template.yaml

**Extraction Rules**:
1. **canonical_routes**: タスクに関連する経路のみ
2. **forbidden_actions**: 共通 + タスク固有
3. **negative_examples**: 共通 + タスク固有

**Common Guardrails (Always Include)**:
```yaml
forbidden_actions:
  - id: FA-COMMON-001
    forbidden: "SaveChangesAsync() in Handler"
  - id: FA-COMMON-002
    forbidden: "Business logic in BoundaryService"
  - id: FA-COMMON-003
    forbidden: "throw exceptions for errors"
```

### Phase 6: Generate repo.snapshot.md

**Input**: Codebase scan
**Template**: workpacks/_templates/repo.snapshot.template.md

**Extraction Rules**:
1. **Related entities**: Domain/{BC}/{Aggregate}/ から
2. **Reference implementations**: 同じパターンの既存実装
3. **Namespace patterns**: 既存ファイルから推測
4. **DI registration**: DependencyInjection.cs から

**Scan Targets**:
```
src/Domain/{BC}/{Aggregate}/          # Entity, ValueObject
src/Application/Features/{Feature}/   # 既存 Command/Handler
src/Application/Infrastructure/{BC}/  # Repository
```

---

## Output Verification

生成後の検証チェック:

```
[ ] task.md: Objective is not empty
[ ] task.md: At least 1 acceptance criterion
[ ] spec.extract.md: At least 1 FR extracted
[ ] spec.extract.md: Pattern constraints included
[ ] policy.yaml: core_values section populated
[ ] policy.yaml: patterns section populated
[ ] guardrails.yaml: canonical_routes is not empty
[ ] guardrails.yaml: forbidden_actions includes common rules
[ ] repo.snapshot.md: At least 1 reference implementation
```

---

## Integration with Existing Speckit

### Relationship to Other Commands

```
speckit.specify    → spec.yaml
speckit.guardrails → guardrails.yaml
speckit.plan       → plan.md + manifest.yaml + tasks.yaml
speckit.workpack   → workpacks/active/{task-id}/  ★ This command
speckit.implement  → (uses workpack as input)
```

### When to Use

```
✅ After speckit.plan is complete
✅ Before speckit.implement
✅ When transitioning from Phase A to Phase B
```

---

## Error Handling

| Error | Resolution |
|-------|------------|
| Spec not found | Verify spec path exists |
| Guardrails missing | Run speckit.guardrails first |
| Manifest missing | Run speckit.plan first |
| Workpack exists | Use --force to overwrite |

---

## Related Files

- `workpacks/_templates/` - テンプレートファイル
- `catalog/COMMON_MISTAKES.md` - 共通禁止事項
- `catalog/skills/vsa-implementation-guard/` - 実装ガード知識
