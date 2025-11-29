using FluentValidation;

namespace Application.Features.ResubmitApplication;

/// <summary>
/// 申請再提出コマンドバリデーター
/// </summary>
public sealed class ResubmitApplicationCommandValidator : AbstractValidator<ResubmitApplicationCommand>
{
    public ResubmitApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("申請IDは必須です");
    }
}
