# Spec Extract for: T001-create-todo

## Source

| Document | Path |
|----------|------|
| Spec | specs/_samples/CreateTodo.spec.yaml |
| Guardrails | specs/_samples/CreateTodo.guardrails.yaml |
| Manifest | specs/_samples/CreateTodo.manifest.yaml |

---

## Extracted Context

### Summary

TODO アイテムを作成する機能。
ユーザーがタイトル（必須）と期限日（任意）を入力して新しい TODO を登録する。

### Actor

ユーザー

---

## Relevant Functional Requirements

| FR-ID | Description | Related to this task |
|-------|-------------|---------------------|
| FR-001 | TODOを作成できる | 直接関連 |
| FR-002 | タイトルは1-100文字 | バリデーションで実装 |
| FR-003 | 期限日は今日以降 | バリデーションで実装 |

---

## Boundary Design

### Input

| Name | Source | Description |
|------|--------|-------------|
| Title | ユーザー入力 | TODOのタイトル（1-100文字） |
| DueDate | ユーザー入力 | 期限日（任意、今日以降） |

### Output

#### Success

- 作成された TODO の ID (Guid)

#### Errors

| Code | Message |
|------|---------|
| VALIDATION_ERROR | タイトルが空または100文字超過 |
| VALIDATION_ERROR | 期限日が過去の日付 |

---

## Entity Design

```csharp
public class Todo : Entity<TodoId>
{
    public string Title { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Todo() { } // EF Core用

    public static Todo Create(string title, DateOnly? dueDate)
    {
        return new Todo
        {
            Id = TodoId.New(),
            Title = title,
            DueDate = dueDate,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

---

## Domain Rules

| Rule ID | Rule | Entity Method | Notes |
|---------|------|---------------|-------|
| DR-001 | タイトルは1文字以上100文字以下 | Validator | FluentValidation で実装 |
| DR-002 | 期限日は今日以降（指定する場合） | Validator | FluentValidation で実装 |

---

## Scenarios

### Happy Path

1. ユーザーがタイトル「買い物」で TODO を作成
2. バリデーション通過
3. Todo エンティティが生成される
4. リポジトリに追加
5. TransactionBehavior が SaveChangesAsync を実行
6. 作成された ID が返される

### Exception Paths

| ID | Condition | Expected |
|----|-----------|----------|
| EX-001 | タイトルが空 | VALIDATION_ERROR |
| EX-002 | タイトルが101文字以上 | VALIDATION_ERROR |
| EX-003 | 期限日が昨日以前 | VALIDATION_ERROR |

---

## Catalog Pattern Reference

### Pattern

| Key | Value |
|-----|-------|
| Pattern ID | feature-create-entity |
| YAML | catalog/patterns/feature-create-entity.yaml |

### Key Constraints (from must_read_checklist)

> Handler 内で SaveChangesAsync() を呼ばない

> Result<T> パターンでエラーを返す

> ICommand<Result<T>> を使用する

### Common Mistakes to Avoid

> ❌ Handler で _dbContext.SaveChangesAsync() を呼ぶ

> ❌ throw new ValidationException() で例外を投げる

---

## Skill Knowledge Embedding

### From vsa-boundary-modeler

- 業務ロジックは Entity が持つ
- BoundaryService は委譲のみ
- Entity.CanXxx() は BoundaryDecision を返す

### From vsa-implementation-guard

- Handler 内で SaveChangesAsync() を呼ばない
- Result<T> パターンを使用
- ICommand<Result<T>> を使用（IRequest<T> 直接使用禁止）
