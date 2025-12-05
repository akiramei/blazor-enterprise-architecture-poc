namespace Domain.LibraryManagement.Boundaries;

/// <summary>
/// Boundary判定結果
///
/// 【パターン: Boundary Pattern】
///
/// 責務:
/// - 操作可否の結果を保持
/// - 不可の場合は理由を提供
///
/// 設計:
/// - 不変オブジェクト（record）として実装
/// - ファクトリメソッドで生成
/// - 暗黙のbool変換をサポート
/// </summary>
public sealed record BoundaryDecision
{
    /// <summary>操作が許可されているか</summary>
    public bool IsAllowed { get; }

    /// <summary>拒否の理由（IsAllowed=falseの場合）</summary>
    public string? Reason { get; }

    private BoundaryDecision(bool isAllowed, string? reason = null)
    {
        IsAllowed = isAllowed;
        Reason = reason;
    }

    /// <summary>操作を許可</summary>
    public static BoundaryDecision Allow() => new(true);

    /// <summary>操作を拒否（理由付き）</summary>
    public static BoundaryDecision Deny(string reason) => new(false, reason);

    /// <summary>暗黙のbool変換</summary>
    public static implicit operator bool(BoundaryDecision decision) => decision.IsAllowed;
}
