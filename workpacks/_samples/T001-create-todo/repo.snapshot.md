# Repository Snapshot for: T001-create-todo

## Meta

| Key | Value |
|-----|-------|
| Task ID | T001-create-todo |
| Snapshot Date | 2025-01-01T00:00:00Z |
| Base Commit | (sample - no actual commit) |

---

## Project Structure Context

```
src/
├── Kernel/                           # DDD基盤
│   ├── Entity.cs
│   ├── ValueObject.cs
│   └── AggregateRoot.cs
│
├── Domain/Todo/                      # ★ 作成対象
│   ├── Todo.cs                       # Todo エンティティ
│   └── TodoId.cs                     # 型付きID
│
├── Shared/
│   ├── Application/                  # ICommand, IQuery, Result<T>
│   │   ├── ICommand.cs
│   │   └── Result.cs
│   └── Infrastructure/               # Behaviors
│       ├── ValidationBehavior.cs
│       └── TransactionBehavior.cs
│
└── Application/
    ├── Features/CreateTodo/          # ★ 作成対象
    │   ├── CreateTodoCommand.cs
    │   ├── CreateTodoCommandHandler.cs
    │   └── CreateTodoCommandValidator.cs
    │
    └── Infrastructure/Todo/
        └── TodoRepository.cs
```

---

## Related Files

### Similar Implementations (Pattern Examples)

以下は既存の実装例です。このパターンを参考にしてください。

#### Command Example

```csharp
// File: src/Application/Features/CreateProduct/CreateProductCommand.cs
// Purpose: Reference implementation for Command pattern

namespace Application.Features.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price
) : ICommand<Result<Guid>>;
```

#### Handler Example

```csharp
// File: src/Application/Features/CreateProduct/CreateProductCommandHandler.cs
// Purpose: Reference implementation for Handler pattern

namespace Application.Features.CreateProduct;

public sealed class CreateProductCommandHandler
    : ICommandHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _repository;

    public CreateProductCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = Product.Create(
            request.Name,
            request.Description,
            request.Price);

        await _repository.AddAsync(product, cancellationToken);

        // SaveChangesAsync は呼ばない！TransactionBehavior が自動実行
        return Result.Success(product.Id.Value);
    }
}
```

#### Validator Example

```csharp
// File: src/Application/Features/CreateProduct/CreateProductCommandValidator.cs
// Purpose: Reference implementation for Validator pattern

namespace Application.Features.CreateProduct;

public sealed class CreateProductCommandValidator
    : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("名前は必須です")
            .MaximumLength(100).WithMessage("名前は100文字以下です");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("価格は0より大きくしてください");
    }
}
```

---

## Namespace Conventions

| Layer | Namespace Pattern |
|-------|-------------------|
| Domain Entity | `Domain.Todo` |
| Command/Handler | `Application.Features.CreateTodo` |
| Repository Interface | `Domain.Todo` |
| Repository Impl | `Application.Infrastructure.Todo` |

---

## Files to Create/Modify

### Create

| Path | Template |
|------|----------|
| src/Domain/Todo/Todo.cs | Entity with Create factory |
| src/Domain/Todo/TodoId.cs | Typed ID |
| src/Domain/Todo/ITodoRepository.cs | Repository interface |
| src/Application/Features/CreateTodo/CreateTodoCommand.cs | ICommand<Result<Guid>> |
| src/Application/Features/CreateTodo/CreateTodoCommandHandler.cs | Handler |
| src/Application/Features/CreateTodo/CreateTodoCommandValidator.cs | FluentValidation |

### Modify

| Path | Change Description |
|------|-------------------|
| src/Application/DependencyInjection.cs | Add ITodoRepository registration |

---

## Notes

- このサンプルは workpack チュートリアル用です
- 実際のプロジェクトでは、既存のコードを repo.snapshot.md に含めてください
- `generate-workpack.ps1` は自動的に関連コードを抽出します
