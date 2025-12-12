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
        RuleFor(x => x.MemberBarcode)
            .NotEmpty()
            .WithMessage("会員バーコードは必須です");

        RuleFor(x => x.BookCopyBarcode)
            .NotEmpty()
            .WithMessage("蔵書バーコードは必須です");
    }
}
