using FluentValidation;

namespace Application.Features.LendBook;

/// <summary>
/// 貸出登録コマンドのバリデーター
/// 
/// 【カタログパターン: validation-behavior】
/// - FluentValidation を使用
/// - 入力の形式チェックのみ（ビジネスルールはHandler）
/// - ValidationBehavior で自動実行 → 失敗時は Result.Fail に変換
/// 
/// 【検証内容】
/// - バーコードが空でないこと
/// - バーコードの形式が正しいこと（オプション）
/// </summary>
public sealed class LendBookCommandValidator : AbstractValidator<LendBookCommand>
{
    public LendBookCommandValidator()
    {
        RuleFor(x => x.MemberBarcode)
            .NotEmpty()
            .WithMessage("会員バーコードは必須です");

        RuleFor(x => x.BookCopyBarcode)
            .NotEmpty()
            .WithMessage("蔵書バーコードは必須です");
    }
}
