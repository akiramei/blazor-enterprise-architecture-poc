# Repository Snapshot for: {{task_id}}

## Meta

| Key | Value |
|-----|-------|
| Task ID | {{task_id}} |
| Snapshot Date | {{snapshot_at}} |
| Base Commit | {{base_commit}} |

---

## Project Structure Context

```
src/
├── Kernel/                           # DDD基盤
│   ├── Entity.cs
│   ├── ValueObject.cs
│   └── AggregateRoot.cs
│
├── Domain/{{bounded_context}}/       # BC固有ドメイン
│   ├── {{aggregate}}/
│   │   ├── {{entity}}.cs             # ★ 参照/作成対象
│   │   └── {{entity}}Id.cs
│   └── Boundaries/
│       └── {{boundary_service}}.cs
│
├── Shared/
│   ├── Application/                  # ICommand, IQuery, Result<T>
│   └── Infrastructure/               # Behaviors
│
└── Application/
    ├── Features/{{feature}}/         # VSA機能スライス
    │   ├── {{slice}}/                # ★ 作成/変更対象
    │   │   ├── {{slice}}Command.cs
    │   │   ├── {{slice}}CommandHandler.cs
    │   │   ├── {{slice}}CommandValidator.cs
    │   │   └── {{slice}}.razor
    │   └── ...
    │
    └── Infrastructure/{{bounded_context}}/
        └── {{repository}}.cs
```

---

## Related Files

### Existing Files to Reference

#### Entity Definition

```csharp
// File: src/Domain/{{bounded_context}}/{{aggregate}}/{{entity}}.cs
// Purpose: Entity implementation to extend/reference

{{entity_code_snippet}}
```

#### Repository Interface

```csharp
// File: src/Domain/{{bounded_context}}/{{aggregate}}/I{{entity}}Repository.cs
// Purpose: Repository interface to implement

{{repository_interface_snippet}}
```

#### Value Objects

```csharp
// File: src/Domain/{{bounded_context}}/{{aggregate}}/{{value_object}}.cs
// Purpose: Value object definitions

{{value_object_snippet}}
```

---

### Similar Implementations (Pattern Examples)

#### Command Example

```csharp
// File: src/Application/Features/{{reference_feature}}/{{reference_command}}Command.cs
// Purpose: Reference implementation for Command pattern

{{reference_command_snippet}}
```

#### Handler Example

```csharp
// File: src/Application/Features/{{reference_feature}}/{{reference_command}}CommandHandler.cs
// Purpose: Reference implementation for Handler pattern

{{reference_handler_snippet}}
```

#### Validator Example

```csharp
// File: src/Application/Features/{{reference_feature}}/{{reference_command}}CommandValidator.cs
// Purpose: Reference implementation for Validator pattern

{{reference_validator_snippet}}
```

---

### Infrastructure Files

#### DbContext

```csharp
// File: src/Application/Infrastructure/{{bounded_context}}/{{bounded_context}}DbContext.cs
// Purpose: DbContext with DbSet definitions

{{dbcontext_snippet}}
```

#### Repository Implementation

```csharp
// File: src/Application/Infrastructure/{{bounded_context}}/{{entity}}Repository.cs
// Purpose: Repository implementation example

{{repository_implementation_snippet}}
```

---

## DI Registration

```csharp
// File: src/Application/DependencyInjection.cs
// Purpose: Service registration pattern

{{di_registration_snippet}}
```

---

## Namespace Conventions

| Layer | Namespace Pattern |
|-------|-------------------|
| Domain Entity | `{{project_name}}.Domain.{{bounded_context}}.{{aggregate}}` |
| Command/Handler | `{{project_name}}.Application.Features.{{feature}}.{{slice}}` |
| Repository Interface | `{{project_name}}.Domain.{{bounded_context}}.{{aggregate}}` |
| Repository Impl | `{{project_name}}.Application.Infrastructure.{{bounded_context}}` |

---

## Files to Create/Modify

### Create

| Path | Template |
|------|----------|
| {{create_path_1}} | {{template_1}} |
| {{create_path_2}} | {{template_2}} |

### Modify

| Path | Change Description |
|------|-------------------|
| {{modify_path_1}} | {{change_description_1}} |

---

## Notes

{{additional_notes}}

---

**Template Variables**:
- `{{task_id}}`: タスクID
- `{{bounded_context}}`: 境界づけられたコンテキスト名
- `{{aggregate}}`: 集約名
- `{{entity}}`: エンティティ名
- `{{feature}}`: 機能名
- `{{slice}}`: スライス名
- `{{snapshot_at}}`: スナップショット日時
- `{{base_commit}}`: ベースコミットハッシュ
