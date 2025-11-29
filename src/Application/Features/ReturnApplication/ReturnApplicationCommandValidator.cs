using FluentValidation;

namespace Application.Features.ReturnApplication;

/// <summary>
/// 申請差し戻しコマンドバリデーター
/// </summary>
public sealed class ReturnApplicationCommandValidator : AbstractValidator<ReturnApplicationCommand>
{
    public ReturnApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("申請IDは必須です");

        RuleFor(x => x.ApproverRole)
            .NotEmpty()
            .WithMessage("承認者ロールは必須です");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("差し戻し理由は必須です")
            .MaximumLength(2000)
            .WithMessage("差し戻し理由は2000文字以内で入力してください");
    }
}
