using FluentValidation;

namespace ApprovePurchaseRequest.Application;

public class ApprovePurchaseRequestValidator : AbstractValidator<ApprovePurchaseRequestCommand>
{
    public ApprovePurchaseRequestValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("購買申請IDは必須です");

        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("承認コメントは必須です")
            .MaximumLength(1000).WithMessage("承認コメントは1000文字以内で入力してください");
    }
}
