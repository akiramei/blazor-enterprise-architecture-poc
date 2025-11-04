namespace Shared.Application.Common;

/// <summary>
/// 一括操作の結果
/// </summary>
public sealed record BulkOperationResult
{
    public int SucceededCount { get; init; }
    public int FailedCount { get; init; }
    public int TotalCount => SucceededCount + FailedCount;
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public bool IsAllSucceeded => FailedCount == 0;
    public bool HasAnySucceeded => SucceededCount > 0;

    public static BulkOperationResult AllSucceeded(int count)
    {
        return new BulkOperationResult
        {
            SucceededCount = count,
            FailedCount = 0,
            Errors = Array.Empty<string>()
        };
    }

    public static BulkOperationResult AllFailed(int count, IReadOnlyList<string> errors)
    {
        return new BulkOperationResult
        {
            SucceededCount = 0,
            FailedCount = count,
            Errors = errors
        };
    }

    public static BulkOperationResult PartiallySucceeded(int succeededCount, int failedCount, IReadOnlyList<string> errors)
    {
        return new BulkOperationResult
        {
            SucceededCount = succeededCount,
            FailedCount = failedCount,
            Errors = errors
        };
    }
}
