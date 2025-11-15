namespace GetPendingApprovals.Application;

/// <summary>
/// 承認待ち申請DTO
///
/// 【パターン: Read Model DTO】
///
/// 責務:
/// - 承認者の承認待ちリストに表示する情報
/// - Dapperによる高速読み取り用
///
/// 表示項目:
/// - 購買申請の基本情報（ID、申請番号、タイトル、金額）
/// - 申請者情報
/// - 現在の承認ステップ情報
/// - 優先度（金額が高いほど優先）
/// </summary>
public sealed record PendingApprovalDto
{
    /// <summary>
    /// 購買申請ID
    /// </summary>
    public Guid PurchaseRequestId { get; init; }

    /// <summary>
    /// 申請番号
    /// </summary>
    public string RequestNumber { get; init; } = string.Empty;

    /// <summary>
    /// タイトル
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// 総額
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// 申請者ID
    /// </summary>
    public Guid RequesterId { get; init; }

    /// <summary>
    /// 申請者名
    /// </summary>
    public string RequesterName { get; init; } = string.Empty;

    /// <summary>
    /// 申請日時
    /// </summary>
    public DateTime SubmittedAt { get; init; }

    /// <summary>
    /// 現在の承認ステップ番号（1, 2, 3）
    /// </summary>
    public int CurrentStepNumber { get; init; }

    /// <summary>
    /// 承認ステップの総数
    /// </summary>
    public int TotalSteps { get; init; }

    /// <summary>
    /// 申請からの経過日数
    /// </summary>
    public int DaysSinceSubmitted => (DateTime.UtcNow - SubmittedAt).Days;
}
