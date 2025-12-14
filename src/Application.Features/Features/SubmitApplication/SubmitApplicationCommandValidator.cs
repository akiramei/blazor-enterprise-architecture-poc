using FluentValidation;

namespace Application.Features.SubmitApplication;

/// <summary>
/// 申請提出コマンドバリデーター
/// </summary>
public sealed class SubmitApplicationCommandValidator : AbstractValidator<SubmitApplicationCommand>
{
    public SubmitApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("申請IDは必須です");
    }
}
