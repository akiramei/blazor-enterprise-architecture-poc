namespace Domain.PurchaseManagement.PurchaseRequests;

/// <summary>
/// 購買申請のステータス
/// </summary>
public enum PurchaseRequestStatus
{
    /// <summary>下書き（未提出）</summary>
    Draft = 0,

    /// <summary>申請中（提出済み、承認待ち）</summary>
    Submitted = 1,

    /// <summary>1次承認待ち</summary>
    PendingFirstApproval = 2,

    /// <summary>2次承認待ち</summary>
    PendingSecondApproval = 3,

    /// <summary>最終承認待ち</summary>
    PendingFinalApproval = 4,

    /// <summary>承認済み</summary>
    Approved = 5,

    /// <summary>却下</summary>
    Rejected = 6,

    /// <summary>キャンセル</summary>
    Cancelled = 7
}
