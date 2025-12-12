using Domain.PurchaseManagement.PurchaseRequests;

namespace Domain.PurchaseManagement.Boundaries;

/// <summary>
/// Intent コンテキスト：現在の状態で実行可能な業務意図（Intent）の一覧
///
/// 【パターン: Intent-Command分離】
///
/// UIはこのコンテキストを使って:
/// - どのボタンを表示するか（AvailableIntents）
/// - ボタンのラベル・色・アイコン（IntentMetadata）
/// - 確認メッセージの表示（RequiresConfirmation）
///
/// を決定できる。UIは「承認」「却下」といった技術的なコマンド名を知る必要がない。
/// </summary>
public sealed record IntentContext
{
    /// <summary>現在の申請（DTO版では不要のためnullable）</summary>
    public PurchaseRequest? Request { get; init; }

    /// <summary>現在実行可能なIntent一覧</summary>
    public required AvailableIntent[] AvailableIntents { get; init; }

    /// <summary>現在のユーザーID</summary>
    public required Guid CurrentUserId { get; init; }

    /// <summary>実行可能なIntentが存在するか</summary>
    public bool HasAvailableIntents => AvailableIntents.Length > 0;

    /// <summary>
    /// 指定されたIntentが実行可能か判定
    /// </summary>
    public bool CanExecute(ApprovalIntent intent)
    {
        return AvailableIntents.Any(i => i.Intent == intent && i.IsEnabled);
    }

    /// <summary>
    /// 指定されたIntentのメタデータを取得
    /// </summary>
    public AvailableIntent? GetIntent(ApprovalIntent intent)
    {
        return AvailableIntents.FirstOrDefault(i => i.Intent == intent);
    }
}

/// <summary>
/// 実行可能なIntent（メタデータ + 有効性判定）
/// </summary>
public sealed record AvailableIntent
{
    /// <summary>Intent種別</summary>
    public required ApprovalIntent Intent { get; init; }

    /// <summary>有効化されているか（権限チェック済み）</summary>
    public required bool IsEnabled { get; init; }

    /// <summary>無効化されている理由（デバッグ用）</summary>
    public string? DisabledReason { get; init; }

    /// <summary>メタデータ（表示情報）</summary>
    public required IntentMetadata Metadata { get; init; }

    /// <summary>
    /// 有効なIntentを生成
    /// </summary>
    public static AvailableIntent Enabled(ApprovalIntent intent)
    {
        return new AvailableIntent
        {
            Intent = intent,
            IsEnabled = true,
            DisabledReason = null,
            Metadata = IntentMetadata.FromIntent(intent)
        };
    }

    /// <summary>
    /// 無効なIntentを生成（権限なし）
    /// </summary>
    public static AvailableIntent Disabled(ApprovalIntent intent, string reason)
    {
        return new AvailableIntent
        {
            Intent = intent,
            IsEnabled = false,
            DisabledReason = reason,
            Metadata = IntentMetadata.FromIntent(intent)
        };
    }
}
