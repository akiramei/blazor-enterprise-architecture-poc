namespace PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;

/// <summary>
/// フィルタリングバウンダリー：一覧表示の契約
/// UIに依存せず、「どのフィルターが利用可能か」「どう表示すべきか」を定義
/// </summary>
public interface IFilteringBoundary
{
    /// <summary>
    /// 利用可能なフィルターオプションを取得
    /// UIはこの結果に基づいてフィルターUIを構築する
    /// </summary>
    /// <returns>フィルターオプション</returns>
    FilterOptions GetFilterOptions();

    /// <summary>
    /// ステータス表示情報を取得
    /// UIはこの結果に基づいてバッジの色・アイコンを決定する
    /// </summary>
    /// <param name="status">ステータス</param>
    /// <returns>ステータス表示情報</returns>
    StatusDisplayInfo GetStatusDisplay(PurchaseRequestStatus status);

    /// <summary>
    /// 利用可能なソートオプションを取得
    /// UIはこの結果に基づいてソートUIを構築する
    /// </summary>
    /// <returns>ソートオプション</returns>
    SortOptions GetSortOptions();

    /// <summary>
    /// ステータスでフィルター可能か判定
    /// </summary>
    /// <param name="status">ステータス</param>
    /// <returns>フィルター可能ならtrue</returns>
    bool IsFilterableStatus(PurchaseRequestStatus status);

    /// <summary>
    /// フィールド名でソート可能か判定
    /// </summary>
    /// <param name="fieldName">フィールド名</param>
    /// <returns>ソート可能ならtrue</returns>
    bool IsSortableField(string fieldName);
}
