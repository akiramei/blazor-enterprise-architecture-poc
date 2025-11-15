using FluentValidation;

namespace RejectPurchaseRequest.Application;

public class RejectPurchaseRequestValidator : AbstractValidator<RejectPurchaseRequestCommand>
{
    public RejectPurchaseRequestValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("購買申請IDは必須です");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("却下理由は必須です")
            .MaximumLength(2000).WithMessage("却下理由は2000文字以内で入力してください");
    }
}
