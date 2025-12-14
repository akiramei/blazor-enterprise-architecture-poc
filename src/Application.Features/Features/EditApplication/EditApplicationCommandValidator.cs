using FluentValidation;

namespace Application.Features.EditApplication;

/// <summary>
/// 申請編集コマンドバリデーター
/// </summary>
public sealed class EditApplicationCommandValidator : AbstractValidator<EditApplicationCommand>
{
    public EditApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("申請IDは必須です");

        RuleFor(x => x.Content)
            .MaximumLength(4000)
            .WithMessage("申請内容は4000文字以内で入力してください");
    }
}
