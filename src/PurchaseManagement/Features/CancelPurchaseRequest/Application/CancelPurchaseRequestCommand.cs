using MediatR;
using Shared.Application;

namespace CancelPurchaseRequest.Application;

/// <summary>
/// 購買申請キャンセルコマンド
///
/// 【パターン: CQRS Command】
///
/// 責務:
/// - 申請者による購買申請のキャンセル
/// - ドメインイベント (PurchaseRequestCancelledEvent) 発行
///
/// ビジネスルール:
/// - 申請者のみキャンセル可能
/// - 下書き、承認済み、却下済み、既キャンセルはキャンセル不可
/// - 申請中 (Submitted / InApproval) のみキャンセル可能
///
/// 使用シナリオ:
/// - 申請者が誤って申請した場合
/// - 申請後に購入不要になった場合
/// - 承認前に内容を修正したい場合（キャンセル→再申請）
/// </summary>
public sealed record CancelPurchaseRequestCommand : IRequest<Result>
{
    /// <summary>
    /// 購買申請ID
    /// </summary>
    public Guid PurchaseRequestId { get; init; }

    /// <summary>
    /// キャンセル理由（任意）
    /// </summary>
    public string? Reason { get; init; }
}
