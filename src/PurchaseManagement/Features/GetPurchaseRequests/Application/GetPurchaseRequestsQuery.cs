using Shared.Application;
using Shared.Application.Interfaces;

namespace GetPurchaseRequests.Application;

/// <summary>
/// 購買申請一覧取得クエリ
///
/// 【パターン: CQRS Query】
///
/// 責務:
/// - 購買申請の一覧を取得するためのクエリ定義
/// - フィルタリング・ソート・ページング対応
///
/// AI実装時の注意:
/// - IQuery<TResponse>を使用
/// - Dapperによる直接SQL実行でパフォーマンス最適化
/// </summary>
public sealed record GetPurchaseRequestsQuery : IQuery<Result<List<PurchaseRequestListItemDto>>>
{
    /// <summary>
    /// ステータスでフィルタ (null = すべて)
    /// </summary>
    public int? Status { get; init; }

    /// <summary>
    /// 申請者IDでフィルタ (null = すべて)
    /// </summary>
    public Guid? RequesterId { get; init; }

    /// <summary>
    /// ページ番号 (1から開始)
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// ページサイズ
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// ソートフィールド
    /// </summary>
    public string SortBy { get; init; } = "CreatedAt";

    /// <summary>
    /// ソート順序 (true = 昇順, false = 降順)
    /// </summary>
    public bool Ascending { get; init; } = false;
}

/// <summary>
/// 購買申請一覧項目DTO
/// </summary>
public sealed record PurchaseRequestListItemDto
{
    public required Guid Id { get; init; }
    public required string RequestNumber { get; init; }
    public required Guid RequesterId { get; init; }
    public required string RequesterName { get; init; }
    public required string Title { get; init; }
    public required int Status { get; init; }
    public required string StatusName { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string Currency { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? RejectedAt { get; init; }
    public int ApprovalStepCount { get; init; }
    public int ApprovedStepCount { get; init; }
}
