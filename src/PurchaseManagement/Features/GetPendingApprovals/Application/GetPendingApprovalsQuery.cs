using MediatR;
using Shared.Application;

namespace GetPendingApprovals.Application;

/// <summary>
/// 承認待ち申請一覧取得クエリ
///
/// 【パターン: CQRS Query】
///
/// 責務:
/// - 現在のユーザーが承認者である承認待ち申請の一覧を取得
/// - 金額の高い順（優先度順）にソート
///
/// ビジネスルール:
/// - 現在のユーザーが承認者として設定されている
/// - かつ、まだ承認していない（IsPending = true）
/// - ステータスが Submitted または InApproval
///
/// 使用シナリオ:
/// - 承認者が自分の承認待ちタスクを確認
/// - ダッシュボードに承認待ち件数を表示
/// - 優先度の高い申請から処理
/// </summary>
public sealed record GetPendingApprovalsQuery : IRequest<Result<List<PendingApprovalDto>>>
{
    /// <summary>
    /// ページ番号（デフォルト: 1）
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// ページサイズ（デフォルト: 20）
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// ソート順（デフォルト: 金額降順）
    /// </summary>
    public string SortBy { get; init; } = "TotalAmount";

    /// <summary>
    /// 昇順ソート（デフォルト: false = 降順）
    /// </summary>
    public bool Ascending { get; init; } = false;
}
