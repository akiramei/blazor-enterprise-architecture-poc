using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.ReserveBook;

/// <summary>
/// 予約登録コマンド
///
/// 【SPEC準拠】
/// - actor: Member（会員）
/// - boundary.input: book_id（予約対象の本のID）
///
/// 【カタログパターン: feature-create-entity】
/// - ICommand<Result<Guid>> を実装
/// - SaveChangesAsync は Handler で呼ばない
/// </summary>
public sealed record ReserveBookCommand : ICommand<Result<Guid>>
{
    /// <summary>
    /// 会員ID
    /// </summary>
    public required Guid MemberId { get; init; }

    /// <summary>
    /// 予約対象の蔵書ID
    /// </summary>
    public required Guid BookCopyId { get; init; }
}
