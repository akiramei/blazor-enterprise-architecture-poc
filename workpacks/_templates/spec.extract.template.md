# Spec Extract for: {{task_id}}

## Source

| Document | Path |
|----------|------|
| Spec | specs/{{feature_name}}/{{slice_name}}.spec.yaml |
| Plan | specs/{{feature_name}}/{{slice_name}}.plan.md |
| Decisions | specs/{{feature_name}}/{{slice_name}}.decisions.yaml |
| Manifest | manifests/{{feature_name}}/{{slice_name}}.manifest.yaml |

---

## Extracted Context

### Summary

{{spec_summary}}

### Actor

{{actor_name}}

---

## Relevant Functional Requirements

| FR-ID | Description | Related to this task |
|-------|-------------|---------------------|
| {{fr_id}} | {{fr_description}} | {{relevance}} |

---

## Boundary Design

### Input

| Name | Source | Description |
|------|--------|-------------|
| {{input_name}} | {{input_source}} | {{input_description}} |

### Output

#### Success

- {{success_message}}

#### Errors

| Code | Message |
|------|---------|
| {{error_code}} | {{error_message}} |

---

## Entity Design

```yaml
# From spec.yaml data_model section
{{entity_yaml}}
```

---

## Domain Rules

| Rule ID | Rule | Entity Method | Notes |
|---------|------|---------------|-------|
| {{dr_id}} | {{domain_rule}} | {{entity_method}} | {{rule_notes}} |

---

## Scenarios

### Happy Path

1. {{happy_path_step_1}}
2. {{happy_path_step_2}}
3. {{happy_path_step_3}}

### Exception Paths

| ID | Condition | Expected |
|----|-----------|----------|
| {{ex_id}} | {{exception_condition}} | {{expected_behavior}} |

---

## Catalog Pattern Reference

### Pattern

| Key | Value |
|-----|-------|
| Pattern ID | {{pattern_id}} |
| YAML | catalog/patterns/{{pattern_id}}.yaml |

### Key Constraints (from must_read_checklist)

> {{constraint_1}}

> {{constraint_2}}

> {{constraint_3}}

### Common Mistakes to Avoid

> {{common_mistake_1}}

> {{common_mistake_2}}

---

## Skill Knowledge Embedding

### From vsa-boundary-modeler

- 業務ロジックはEntityが持つ
- BoundaryServiceは委譲のみ
- Entity.CanXxx()はBoundaryDecisionを返す

### From vsa-implementation-guard

- Handler内でSaveChangesAsync()を呼ばない
- Result<T>パターンを使用
- ICommand<Result<T>>を使用（IRequest<T>直接使用禁止）

---

**Template Variables**:
- `{{task_id}}`: タスクID
- `{{feature_name}}`: 機能名
- `{{slice_name}}`: スライス名
- `{{spec_summary}}`: 仕様の要約
- `{{pattern_id}}`: 適用パターン
