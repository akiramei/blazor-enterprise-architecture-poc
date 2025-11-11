namespace PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;

/// <summary>
/// 承認バウンダリー：承認操作の意図契約
/// UIに依存せず、「誰が」「どの状態で」「何をできるか」を定義
/// </summary>
public interface IApprovalBoundary
{
    /// <summary>
    /// 承認資格をチェック（事前条件）
    /// UIはこの結果に基づいて承認・却下ボタンの活性/非活性を制御する
    /// </summary>
    /// <param name="request">購買申請</param>
    /// <param name="currentUserId">現在のユーザーID</param>
    /// <returns>承認資格判定結果</returns>
    ApprovalEligibility CheckEligibility(PurchaseRequest request, Guid currentUserId);

    /// <summary>
    /// 承認コンテキストを取得
    /// UIが表示すべき情報の完全なスナップショット
    /// </summary>
    /// <param name="request">購買申請</param>
    /// <param name="currentUserId">現在のユーザーID</param>
    /// <returns>承認コンテキスト</returns>
    ApprovalContext GetContext(PurchaseRequest request, Guid currentUserId);
}
