using FluentValidation;

namespace Application.Features.ReserveBook;

/// <summary>
/// 予約登録コマンドバリデーター
///
/// 【カタログパターン: validation-behavior】
/// - FluentValidation による入力検証
/// - ValidationBehavior が自動実行
/// </summary>
public sealed class ReserveBookCommandValidator : AbstractValidator<ReserveBookCommand>
{
    public ReserveBookCommandValidator()
    {
        RuleFor(x => x.MemberId)
            .NotEmpty()
            .WithMessage("会員IDは必須です");

        RuleFor(x => x.BookCopyId)
            .NotEmpty()
            .WithMessage("蔵書IDは必須です");
    }
}
