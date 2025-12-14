using FluentValidation;

namespace Application.Features.ApproveApplication;

/// <summary>
/// 申請承認コマンドバリデーター
/// </summary>
public sealed class ApproveApplicationCommandValidator : AbstractValidator<ApproveApplicationCommand>
{
    public ApproveApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("申請IDは必須です");

        RuleFor(x => x.ApproverRole)
            .NotEmpty()
            .WithMessage("承認者ロールは必須です");

        RuleFor(x => x.Comment)
            .MaximumLength(2000)
            .WithMessage("コメントは2000文字以内で入力してください");
    }
}
