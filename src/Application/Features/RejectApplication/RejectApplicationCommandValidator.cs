using FluentValidation;

namespace Application.Features.RejectApplication;

/// <summary>
/// 申請却下コマンドバリデーター
/// </summary>
public sealed class RejectApplicationCommandValidator : AbstractValidator<RejectApplicationCommand>
{
    public RejectApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("申請IDは必須です");

        RuleFor(x => x.ApproverRole)
            .NotEmpty()
            .WithMessage("承認者ロールは必須です");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("却下理由は必須です")
            .MaximumLength(2000)
            .WithMessage("却下理由は2000文字以内で入力してください");
    }
}
