using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.ReserveBook;

/// <summary>
/// 予約登録コマンド
///
/// 【SPEC準拠】
/// - actor: Member（会員）
/// - boundary.input: member_barcode, book_copy_barcode（バーコード入力）
///
/// 【カタログパターン: feature-create-entity】
/// - ICommand<Result<Guid>> を実装
/// - SaveChangesAsync は Handler で呼ばない
/// </summary>
public sealed record ReserveBookCommand : ICommand<Result<Guid>>
{
    /// <summary>
    /// 会員バーコード
    /// </summary>
    public required string MemberBarcode { get; init; } = string.Empty;

    /// <summary>
    /// 予約対象の蔵書バーコード
    /// </summary>
    public required string BookCopyBarcode { get; init; } = string.Empty;
}
