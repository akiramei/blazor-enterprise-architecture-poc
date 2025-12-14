using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.LendBook;

/// <summary>
/// 貸出登録Command
/// 
/// 【SPEC準拠】
/// - boundary.input: member_id (barcode), book_copy_id (barcode)
/// - boundary.output.success: 貸出ID（Guid）
/// 
/// 【カタログパターン: feature-create-entity】
/// - ICommand<Result<Guid>> を継承
/// - 戻り値は作成されたエンティティのID
/// </summary>
public sealed class LendBookCommand : ICommand<Result<Guid>>
{
    /// <summary>
    /// 会員バーコード
    /// </summary>
    public string MemberBarcode { get; init; } = string.Empty;

    /// <summary>
    /// 蔵書バーコード
    /// </summary>
    public string BookCopyBarcode { get; init; } = string.Empty;
}
