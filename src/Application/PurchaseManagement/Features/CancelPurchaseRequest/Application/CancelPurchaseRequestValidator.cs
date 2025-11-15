using FluentValidation;

namespace CancelPurchaseRequest.Application;

/// <summary>
/// CancelPurchaseRequestCommand バリデーター
///
/// 【パターン: FluentValidation】
///
/// 検証ルール:
/// - PurchaseRequestId: 必須、Empty不可
/// - Reason: 任意（最大1000文字）
/// </summary>
public sealed class CancelPurchaseRequestValidator : AbstractValidator<CancelPurchaseRequestCommand>
{
    public CancelPurchaseRequestValidator()
    {
        RuleFor(x => x.PurchaseRequestId)
            .NotEmpty()
            .WithMessage("購買申請IDは必須です");

        RuleFor(x => x.Reason)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.Reason))
            .WithMessage("キャンセル理由は1000文字以内で入力してください");
    }
}
