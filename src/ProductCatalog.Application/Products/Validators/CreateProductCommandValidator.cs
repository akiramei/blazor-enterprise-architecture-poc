using FluentValidation;
using ProductCatalog.Application.Products.Commands;

namespace ProductCatalog.Application.Products.Validators;

/// <summary>
/// CreateProductCommand のバリデーター
/// </summary>
public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("商品名は必須です")
            .MaximumLength(200)
            .WithMessage("商品名は200文字以内で入力してください");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("説明は2000文字以内で入力してください");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("価格は0より大きい値を入力してください");

        RuleFor(x => x.InitialStock)
            .GreaterThanOrEqualTo(0)
            .WithMessage("在庫数は0以上の値を入力してください");
    }
}
