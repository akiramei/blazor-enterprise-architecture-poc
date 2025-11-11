namespace PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;

/// <summary>
/// フィルターオプション：利用可能なフィルター定義
/// </summary>
public record FilterOptions
{
    /// <summary>
    /// ステータスフィルターオプション
    /// </summary>
    public StatusFilterOption[] StatusOptions { get; init; } = Array.Empty<StatusFilterOption>();

    /// <summary>
    /// ソート可能なフィールド
    /// </summary>
    public string[] AllowedSortFields { get; init; } = Array.Empty<string>();
}

/// <summary>
/// ステータスフィルターオプション
/// </summary>
public record StatusFilterOption
{
    /// <summary>
    /// ステータス値（null = すべて）
    /// </summary>
    public PurchaseRequestStatus? Value { get; init; }

    /// <summary>
    /// 表示ラベル
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// 説明（ツールチップなど）
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 表示順序
    /// </summary>
    public int DisplayOrder { get; init; }

    /// <summary>
    /// 「すべて」オプションを作成
    /// </summary>
    public static StatusFilterOption All()
    {
        return new StatusFilterOption
        {
            Value = null,
            Label = "すべてのステータス",
            Description = "全てのステータスを表示",
            DisplayOrder = 0
        };
    }

    /// <summary>
    /// ステータスからオプションを作成
    /// </summary>
    public static StatusFilterOption FromStatus(PurchaseRequestStatus status, int displayOrder)
    {
        return new StatusFilterOption
        {
            Value = status,
            Label = GetStatusLabel(status),
            Description = GetStatusDescription(status),
            DisplayOrder = displayOrder
        };
    }

    private static string GetStatusLabel(PurchaseRequestStatus status)
    {
        return status switch
        {
            PurchaseRequestStatus.Draft => "下書き",
            PurchaseRequestStatus.Submitted => "提出済み",
            PurchaseRequestStatus.PendingFirstApproval => "1次承認待ち",
            PurchaseRequestStatus.PendingSecondApproval => "2次承認待ち",
            PurchaseRequestStatus.PendingFinalApproval => "3次承認待ち",
            PurchaseRequestStatus.Approved => "承認済み",
            PurchaseRequestStatus.Rejected => "却下",
            PurchaseRequestStatus.Cancelled => "キャンセル",
            _ => "不明"
        };
    }

    private static string GetStatusDescription(PurchaseRequestStatus status)
    {
        return status switch
        {
            PurchaseRequestStatus.Draft => "まだ提出されていない申請",
            PurchaseRequestStatus.Submitted => "提出済みで承認待ち",
            PurchaseRequestStatus.PendingFirstApproval => "第1承認者の承認待ち",
            PurchaseRequestStatus.PendingSecondApproval => "第2承認者の承認待ち",
            PurchaseRequestStatus.PendingFinalApproval => "最終承認者の承認待ち",
            PurchaseRequestStatus.Approved => "全ての承認が完了",
            PurchaseRequestStatus.Rejected => "承認者により却下",
            PurchaseRequestStatus.Cancelled => "申請者によりキャンセル",
            _ => ""
        };
    }
}

/// <summary>
/// ソートオプション
/// </summary>
public record SortOptions
{
    /// <summary>
    /// ソート可能なフィールド
    /// </summary>
    public SortOption[] AvailableFields { get; init; } = Array.Empty<SortOption>();
}

/// <summary>
/// ソートオプション（個別フィールド）
/// </summary>
public record SortOption
{
    /// <summary>
    /// フィールド名（データベースカラム名）
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>
    /// 表示名
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// 昇順ソート可能か
    /// </summary>
    public bool AllowAscending { get; init; } = true;

    /// <summary>
    /// 降順ソート可能か
    /// </summary>
    public bool AllowDescending { get; init; } = true;

    /// <summary>
    /// デフォルトの並び順か
    /// </summary>
    public bool IsDefault { get; init; }
}
