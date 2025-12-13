namespace Shared.Kernel;

/// <summary>
/// ドメインエラー: ビジネスルール違反やドメイン制約違反を表現
/// DomainExceptionとは異なり、Result<T>パターンで使用する
/// </summary>
public sealed record DomainError
{
    /// <summary>
    /// エラーコード（機械可読）
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// エラーメッセージ（人間可読）
    /// </summary>
    public string Message { get; init; }

    public DomainError(string code, string message)
    {
        Code = code;
        Message = message;
    }
}
