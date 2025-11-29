using FluentValidation;

namespace Application.Features.CreateApplication;

/// <summary>
/// 申請作成コマンドバリデーター
///
/// 【パターン: FluentValidation】
///
/// 責務:
/// - コマンドの入力値検証
/// - ValidationBehaviorが自動実行
/// </summary>
public sealed class CreateApplicationCommandValidator : AbstractValidator<CreateApplicationCommand>
{
    public CreateApplicationCommandValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("有効な申請タイプを指定してください");

        // Contentは下書き段階では空でもOK（提出時にチェック）
        RuleFor(x => x.Content)
            .MaximumLength(4000)
            .WithMessage("申請内容は4000文字以内で入力してください");
    }
}
