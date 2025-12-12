using Domain.PurchaseManagement.PurchaseRequests;

namespace Domain.PurchaseManagement.Boundaries;

/// <summary>
/// 承認バウンダリー：承認操作の意図契約
/// UIに依存せず、「誰が」「どの状態で」「何をできるか」を定義
///
/// 【Intent-Command分離パターン】
/// UIは業務意図（Intent）のみを扱い、どのコマンドを呼ぶかはバウンダリーが決定する
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

    /// <summary>
    /// 実行可能なIntent一覧を取得（Intent-Command分離パターン）
    /// UIは技術的なコマンド名を知らず、業務意図（Intent）だけを扱う
    /// </summary>
    /// <param name="request">購買申請</param>
    /// <param name="currentUserId">現在のユーザーID</param>
    /// <returns>Intentコンテキスト（実行可能なIntent一覧）</returns>
    IntentContext GetIntentContext(PurchaseRequest request, Guid currentUserId);

    /// <summary>
    /// 指定されたIntentが実行可能か判定
    /// </summary>
    /// <param name="request">購買申請</param>
    /// <param name="intent">実行したいIntent</param>
    /// <param name="currentUserId">現在のユーザーID</param>
    /// <returns>実行可能ならtrue</returns>
    bool CanExecuteIntent(PurchaseRequest request, ApprovalIntent intent, Guid currentUserId);

    /// <summary>
    /// IntentをMediatRコマンドに変換（Intent→Command マッピング）
    /// ドメイン層がIntentを技術実装（Command）に変換する
    /// </summary>
    /// <param name="intent">業務意図</param>
    /// <param name="requestId">申請ID</param>
    /// <param name="userId">実行ユーザーID</param>
    /// <param name="comment">コメント</param>
    /// <param name="idempotencyKey">冪等性キー（UI層が生成・管理）</param>
    /// <returns>MediatRコマンド（IRequest）</returns>
    object CreateCommandFromIntent(ApprovalIntent intent, Guid requestId, Guid userId, string? comment, string idempotencyKey);
}
