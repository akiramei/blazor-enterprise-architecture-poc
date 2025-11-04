namespace Shared.Application.Common;

/// <summary>
/// ページング結果
///
/// 【パターン: ページング結果】
///
/// 使用シナリオ:
/// - 検索結果をページング表示する場合
/// - 総件数とページ情報が必要な場合
/// - UIでページャーを表示する場合
///
/// 実装ガイド:
/// - データ本体（Items）と付加情報（総件数、ページ情報）を含む
/// - HasNextPage, HasPreviousPageで簡単にページャーを実装可能
/// - イミュータブル（record）
///
/// AI実装時の注意:
/// - TotalCountは検索結果の総件数（全ページ合計）
/// - PageSizeは1ページあたりの件数
/// - CurrentPageは現在のページ番号（1始まり）
/// - TotalPagesは総ページ数（計算で求める）
/// </summary>
/// <typeparam name="T">アイテムの型</typeparam>
public sealed record PagedResult<T>
{
    /// <summary>
    /// データ本体
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>
    /// 総件数（全ページ合計）
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// 現在のページ番号（1始まり）
    /// </summary>
    public int CurrentPage { get; init; }

    /// <summary>
    /// 1ページあたりの件数
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// 総ページ数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// 前のページが存在するか
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// 次のページが存在するか
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// ファクトリメソッド
    /// </summary>
    public static PagedResult<T> Create(
        IReadOnlyList<T> items,
        int totalCount,
        int currentPage,
        int pageSize)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = currentPage,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 空の結果
    /// </summary>
    public static PagedResult<T> Empty(int currentPage, int pageSize)
    {
        return new PagedResult<T>
        {
            Items = Array.Empty<T>(),
            TotalCount = 0,
            CurrentPage = currentPage,
            PageSize = pageSize
        };
    }
}
