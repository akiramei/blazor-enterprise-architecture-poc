using FluentValidation;
using ProductCatalog.Application.Products.Commands;

namespace ProductCatalog.Application.Products.Validators;

/// <summary>
/// DeleteProductCommand のバリデーター
/// </summary>
public sealed class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("商品IDは必須です");
    }
}
