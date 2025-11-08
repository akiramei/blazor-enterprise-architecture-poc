using FluentValidation;

namespace SubmitPurchaseRequest.Application;

public class SubmitPurchaseRequestValidator : AbstractValidator<SubmitPurchaseRequestCommand>
{
    public SubmitPurchaseRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("タイトルは必須です")
            .MaximumLength(200).WithMessage("タイトルは200文字以内で入力してください");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("説明は2000文字以内で入力してください");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("明細は最低1件必要です")
            .Must(items => items.Count <= 100).WithMessage("明細は100件までです");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.ProductName).NotEmpty().MaximumLength(200);
            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0).WithMessage("単価は0円より大きい金額を指定してください");
            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("数量は1以上を指定してください");
        });
    }
}
