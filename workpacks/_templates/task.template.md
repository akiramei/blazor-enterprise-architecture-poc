# Task: {{task_id}}

## Meta

| Key | Value |
|-----|-------|
| ID | {{task_id}} |
| Feature | {{feature_name}} |
| Slice | {{slice_name}} |
| Parent Plan | specs/{{feature_name}}/{{slice_name}}.plan.md |
| Pattern | {{pattern_id}} |
| Status | pending |
| Created | {{created_at}} |

## Objective

{{objective_description}}

## Scope

### In Scope

- {{in_scope_item_1}}
- {{in_scope_item_2}}

### Out of Scope

- {{out_of_scope_item_1}}
- {{out_of_scope_item_2}}

## Acceptance Criteria

- [ ] AC-001: {{acceptance_criterion_1}}
- [ ] AC-002: {{acceptance_criterion_2}}
- [ ] AC-003: {{acceptance_criterion_3}}

## Dependencies

| Type | Task ID | Description |
|------|---------|-------------|
| Depends on | {{depends_on_task_id}} | {{dependency_description}} |
| Blocks | {{blocks_task_id}} | {{blocker_description}} |

## Expected Output

### Files to Create

| Path | Purpose |
|------|---------|
| {{file_path_1}} | {{file_purpose_1}} |
| {{file_path_2}} | {{file_purpose_2}} |

### Files to Modify

| Path | Change Type |
|------|-------------|
| {{modify_path_1}} | {{change_type_1}} |

## Guardrail References

このタスクで遵守すべきガードレール（guardrails.yaml から抽出）:

| ID | Rule | Severity |
|----|------|----------|
| {{guardrail_id}} | {{guardrail_rule}} | {{severity}} |

## Notes

{{additional_notes}}

---

**Template Variables**:
- `{{task_id}}`: タスクID（例: T001-create-book-entity）
- `{{feature_name}}`: 機能名（例: loan）
- `{{slice_name}}`: スライス名（例: LendBook）
- `{{pattern_id}}`: 適用パターン（例: feature-create-entity）
- `{{created_at}}`: 作成日時（ISO 8601形式）
