namespace Domain.PurchaseManagement.Boundaries;

/// <summary>
/// 承認業務における意図（Intent）
///
/// 【パターン: Intent-Command分離】
///
/// 開発者視点のコマンド名ではなく、業務視点の意図を表現。
/// UIはこのIntentを扱い、ドメイン層がIntentを適切なコマンドにマッピングする。
///
/// 設計上の利点:
/// - UI層が技術詳細（どのコマンドか）から解放される
/// - 業務用語で画面設計が可能
/// - 多言語対応が容易（Intentのラベルを表示）
/// - 業務フロー変更時の影響範囲が明確
/// </summary>
public enum ApprovalIntent
{
    /// <summary>
    /// 1次承認を実施する
    /// ステータス: PendingFirstApproval → PendingSecondApproval
    /// </summary>
    PerformFirstApproval,

    /// <summary>
    /// 2次承認を実施する
    /// ステータス: PendingSecondApproval → PendingFinalApproval
    /// </summary>
    PerformSecondApproval,

    /// <summary>
    /// 最終承認を実施する（申請完了）
    /// ステータス: PendingFinalApproval → Approved
    /// </summary>
    PerformFinalApproval,

    /// <summary>
    /// 修正のため差し戻す
    /// 申請者に戻して再提出を求める
    /// ステータス: Pending* → Submitted
    /// </summary>
    SendBackForRevision,

    /// <summary>
    /// 申請を却下する（終了）
    /// 申請を完全に拒否し、終了状態にする
    /// ステータス: Pending* → Rejected
    /// </summary>
    RejectPermanently
}

/// <summary>
/// Intent のメタデータ（表示名、説明、重要度など）
/// </summary>
public record IntentMetadata
{
    /// <summary>Intent 種別</summary>
    public required ApprovalIntent Intent { get; init; }

    /// <summary>表示ラベル（UI表示用）</summary>
    public required string Label { get; init; }

    /// <summary>詳細説明</summary>
    public required string Description { get; init; }

    /// <summary>ボタンのスタイルクラス（Bootstrap）</summary>
    public required string ButtonClass { get; init; }

    /// <summary>アイコン（Bootstrap Icons）</summary>
    public required string Icon { get; init; }

    /// <summary>確認メッセージが必要か</summary>
    public bool RequiresConfirmation { get; init; }

    /// <summary>確認メッセージテンプレート</summary>
    public string? ConfirmationMessage { get; init; }

    /// <summary>
    /// Intentからメタデータを生成
    /// </summary>
    public static IntentMetadata FromIntent(ApprovalIntent intent)
    {
        return intent switch
        {
            ApprovalIntent.PerformFirstApproval => new IntentMetadata
            {
                Intent = intent,
                Label = "1次承認",
                Description = "1次承認を実施します",
                ButtonClass = "btn-success",
                Icon = "bi-check-circle",
                RequiresConfirmation = true,
                ConfirmationMessage = "1次承認を実施してもよろしいですか？"
            },
            ApprovalIntent.PerformSecondApproval => new IntentMetadata
            {
                Intent = intent,
                Label = "2次承認",
                Description = "2次承認を実施します",
                ButtonClass = "btn-success",
                Icon = "bi-check-circle-fill",
                RequiresConfirmation = true,
                ConfirmationMessage = "2次承認を実施してもよろしいですか？"
            },
            ApprovalIntent.PerformFinalApproval => new IntentMetadata
            {
                Intent = intent,
                Label = "最終承認",
                Description = "最終承認を実施し、申請を完了します",
                ButtonClass = "btn-primary",
                Icon = "bi-shield-check",
                RequiresConfirmation = true,
                ConfirmationMessage = "最終承認を実施してもよろしいですか？この操作により申請が完了します。"
            },
            ApprovalIntent.SendBackForRevision => new IntentMetadata
            {
                Intent = intent,
                Label = "差し戻し",
                Description = "申請者に修正を依頼します",
                ButtonClass = "btn-warning",
                Icon = "bi-arrow-return-left",
                RequiresConfirmation = true,
                ConfirmationMessage = "申請を差し戻してもよろしいですか？申請者は再提出が可能です。"
            },
            ApprovalIntent.RejectPermanently => new IntentMetadata
            {
                Intent = intent,
                Label = "却下",
                Description = "申請を却下し、終了します",
                ButtonClass = "btn-danger",
                Icon = "bi-x-circle",
                RequiresConfirmation = true,
                ConfirmationMessage = "申請を却下してもよろしいですか？この操作は取り消せません。"
            },
            _ => throw new ArgumentOutOfRangeException(nameof(intent), intent, "未知のIntent")
        };
    }
}
