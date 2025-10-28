namespace ProductCatalog.Application.Common;

/// <summary>
/// 一括処理結果
///
/// 【パターン: 一括処理結果】
///
/// 使用シナリオ:
/// - 一括削除、一括更新などの結果を返す場合
/// - 成功した件数と失敗した件数を分けて返したい場合
/// - 失敗した項目のエラーメッセージを詳細に返したい場合
///
/// 実装ガイド:
/// - SucceededCount: 成功した件数
/// - FailedCount: 失敗した件数
/// - Errors: 失敗した項目のエラーメッセージリスト
/// - イミュータブル（record）
///
/// AI実装時の注意:
/// - 一括処理でも個別にエラーハンドリングする
/// - エラーメッセージには識別子を含める（どの項目で失敗したか分かるように）
/// - UI側で成功/失敗を適切に表示できるようにする
/// </summary>
public sealed record BulkOperationResult
{
    /// <summary>
    /// 成功した件数
    /// </summary>
    public int SucceededCount { get; init; }

    /// <summary>
    /// 失敗した件数
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// 総件数
    /// </summary>
    public int TotalCount => SucceededCount + FailedCount;

    /// <summary>
    /// エラーメッセージリスト
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// すべて成功したか
    /// </summary>
    public bool IsAllSucceeded => FailedCount == 0;

    /// <summary>
    /// 一部でも成功したか
    /// </summary>
    public bool HasAnySucceeded => SucceededCount > 0;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public BulkOperationResult(int succeededCount, int failedCount, IReadOnlyList<string> errors)
    {
        SucceededCount = succeededCount;
        FailedCount = failedCount;
        Errors = errors;
    }

    /// <summary>
    /// ファクトリメソッド: 全成功
    /// </summary>
    public static BulkOperationResult AllSucceeded(int count)
    {
        return new BulkOperationResult(count, 0, Array.Empty<string>());
    }

    /// <summary>
    /// ファクトリメソッド: 全失敗
    /// </summary>
    public static BulkOperationResult AllFailed(int count, IReadOnlyList<string> errors)
    {
        return new BulkOperationResult(0, count, errors);
    }
}
