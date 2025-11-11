namespace PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;

/// <summary>
/// フィルタリングバウンダリーサービス：ドメインサービスとしてフィルタリングロジックを提供
/// </summary>
public class FilteringBoundaryService : IFilteringBoundary
{
    // ソート可能なフィールド定義
    private static readonly string[] AllowedSortFields =
    {
        "RequestNumber",
        "Title",
        "Status",
        "TotalAmount",
        "CreatedAt"
    };

    /// <summary>
    /// 利用可能なフィルターオプションを取得
    /// </summary>
    public FilterOptions GetFilterOptions()
    {
        var statusOptions = new List<StatusFilterOption>
        {
            StatusFilterOption.All() // 「すべて」オプション
        };

        // 全てのステータスをフィルターオプションとして追加
        var statuses = Enum.GetValues<PurchaseRequestStatus>();
        int order = 1;
        foreach (var status in statuses)
        {
            statusOptions.Add(StatusFilterOption.FromStatus(status, order++));
        }

        return new FilterOptions
        {
            StatusOptions = statusOptions.ToArray(),
            AllowedSortFields = AllowedSortFields
        };
    }

    /// <summary>
    /// ステータス表示情報を取得
    /// 注意: StatusDisplayInfo は ApprovalContext.cs で既に定義されている
    /// </summary>
    public StatusDisplayInfo GetStatusDisplay(PurchaseRequestStatus status)
    {
        return StatusDisplayInfo.FromStatus(status);
    }

    /// <summary>
    /// 利用可能なソートオプションを取得
    /// </summary>
    public SortOptions GetSortOptions()
    {
        return new SortOptions
        {
            AvailableFields = new[]
            {
                new SortOption
                {
                    FieldName = "RequestNumber",
                    DisplayName = "申請番号",
                    AllowAscending = true,
                    AllowDescending = true,
                    IsDefault = false
                },
                new SortOption
                {
                    FieldName = "Title",
                    DisplayName = "タイトル",
                    AllowAscending = true,
                    AllowDescending = true,
                    IsDefault = false
                },
                new SortOption
                {
                    FieldName = "Status",
                    DisplayName = "ステータス",
                    AllowAscending = true,
                    AllowDescending = true,
                    IsDefault = false
                },
                new SortOption
                {
                    FieldName = "TotalAmount",
                    DisplayName = "合計金額",
                    AllowAscending = true,
                    AllowDescending = true,
                    IsDefault = false
                },
                new SortOption
                {
                    FieldName = "CreatedAt",
                    DisplayName = "作成日時",
                    AllowAscending = true,
                    AllowDescending = true,
                    IsDefault = true // デフォルトは作成日時
                }
            }
        };
    }

    /// <summary>
    /// ステータスでフィルター可能か判定
    /// </summary>
    public bool IsFilterableStatus(PurchaseRequestStatus status)
    {
        // 全てのステータスがフィルター可能
        return Enum.IsDefined(typeof(PurchaseRequestStatus), status);
    }

    /// <summary>
    /// フィールド名でソート可能か判定
    /// </summary>
    public bool IsSortableField(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            return false;

        return AllowedSortFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase);
    }
}
