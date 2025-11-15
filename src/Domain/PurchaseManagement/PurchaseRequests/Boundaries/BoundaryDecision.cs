using Shared.Kernel;

namespace Domain.PurchaseManagement.PurchaseRequests.Boundaries;

/// <summary>
/// バウンダリー判定結果
///
/// 【パターン: Type-Safe Boundary Decision】
///
/// 責務:
/// - ドメインルールの判定結果を型安全に表現
/// - 許可/拒否の理由を構造化して提供
/// - UI層が判定結果に基づいて適切にレンダリングできるようにする
///
/// 利点:
/// - bool や string の代わりに明示的な型で判定結果を表現
/// - 拒否理由（BlockingReasons）を構造化して提供
/// - 可観測性: 判定結果をログやメトリクスとして記録しやすい
/// </summary>
public sealed record BoundaryDecision
{
    /// <summary>アクションが許可されているか</summary>
    public bool IsAllowed { get; init; }

    /// <summary>許可されたアクション一覧</summary>
    public IReadOnlyList<ApprovalAction> AllowedActions { get; init; }

    /// <summary>ブロックされた理由（IsAllowed=falseの場合）</summary>
    public IReadOnlyList<DomainError> BlockingReasons { get; init; }

    /// <summary>判定時のコンテキスト情報（デバッグ・可観測性用）</summary>
    public DecisionContext Context { get; init; }

    private BoundaryDecision(
        bool isAllowed,
        IReadOnlyList<ApprovalAction> allowedActions,
        IReadOnlyList<DomainError> blockingReasons,
        DecisionContext context)
    {
        IsAllowed = isAllowed;
        AllowedActions = allowedActions;
        BlockingReasons = blockingReasons;
        Context = context;
    }

    /// <summary>
    /// 許可された判定結果を作成
    /// </summary>
    public static BoundaryDecision Allowed(
        IReadOnlyList<ApprovalAction> actions,
        DecisionContext context) => new(
        isAllowed: true,
        allowedActions: actions,
        blockingReasons: Array.Empty<DomainError>(),
        context: context
    );

    /// <summary>
    /// 拒否された判定結果を作成
    /// </summary>
    public static BoundaryDecision Denied(
        IReadOnlyList<DomainError> reasons,
        DecisionContext context) => new(
        isAllowed: false,
        allowedActions: Array.Empty<ApprovalAction>(),
        blockingReasons: reasons,
        context: context
    );

    /// <summary>
    /// 特定のアクションタイプが許可されているか判定
    /// </summary>
    public bool CanPerform(ApprovalActionType actionType) =>
        IsAllowed && AllowedActions.Any(a => a.Type == actionType && a.IsEnabled);

    /// <summary>
    /// 特定のアクションタイプを取得（存在しない場合はnull）
    /// </summary>
    public ApprovalAction? GetAction(ApprovalActionType actionType) =>
        AllowedActions.FirstOrDefault(a => a.Type == actionType);
}

/// <summary>
/// 判定時のコンテキスト情報
///
/// 可観測性とデバッグのために判定時の状態を記録
/// </summary>
public sealed record DecisionContext
{
    /// <summary>判定対象のユーザーID</summary>
    public Guid UserId { get; init; }

    /// <summary>判定対象のリクエストID</summary>
    public Guid RequestId { get; init; }

    /// <summary>判定時のリクエストステータス</summary>
    public PurchaseRequestStatus RequestStatus { get; init; }

    /// <summary>現在の承認ステップ番号（存在する場合）</summary>
    public int? CurrentStepNumber { get; init; }

    /// <summary>判定を行ったタイムスタンプ</summary>
    public DateTime DecisionTimestamp { get; init; }

    /// <summary>ユーザーが申請者かどうか</summary>
    public bool IsRequester { get; init; }

    /// <summary>ユーザーが現在の承認者かどうか</summary>
    public bool IsCurrentApprover { get; init; }

    public static DecisionContext Create(
        Guid userId,
        PurchaseRequest request,
        bool isRequester,
        bool isCurrentApprover) => new()
    {
        UserId = userId,
        RequestId = request.Id,
        RequestStatus = request.Status,
        CurrentStepNumber = request.CurrentApprovalStep?.StepNumber,
        DecisionTimestamp = DateTime.UtcNow,
        IsRequester = isRequester,
        IsCurrentApprover = isCurrentApprover
    };
}
