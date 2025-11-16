namespace Shared.Domain.Identity;

/// <summary>
/// 二要素認証のリカバリーコード
///
/// 用途:
/// - TOTP認証アプリにアクセスできない場合の緊急ログイン手段
/// - 各コードは1回のみ使用可能
/// - セキュリティのため、コードはハッシュ化して保存
///
/// セキュリティ設計:
/// - 生成時にBCryptでハッシュ化
/// - 使用済みフラグで再利用を防止
/// - ユーザーごとに通常10個程度生成
/// </summary>
public sealed class TwoFactorRecoveryCode
{
    /// <summary>
    /// リカバリーコードID
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// ユーザーID
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// ハッシュ化されたコード
    /// BCrypt.Net.BCrypt.HashPasswordで生成
    /// </summary>
    public string CodeHash { get; private set; } = string.Empty;

    /// <summary>
    /// 使用済みフラグ
    /// </summary>
    public bool IsUsed { get; private set; }

    /// <summary>
    /// 使用日時（UTC）
    /// </summary>
    public DateTime? UsedAt { get; private set; }

    /// <summary>
    /// 作成日時（UTC）
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    // EF Core用のパラメータレスコンストラクタ
    private TwoFactorRecoveryCode()
    {
    }

    /// <summary>
    /// リカバリーコードを作成（ファクトリメソッド）
    /// </summary>
    /// <param name="userId">ユーザーID</param>
    /// <param name="plainCode">平文のリカバリーコード</param>
    /// <returns>新しいリカバリーコードエンティティ</returns>
    public static TwoFactorRecoveryCode Create(Guid userId, string plainCode)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(plainCode))
            throw new ArgumentException("Recovery code cannot be empty", nameof(plainCode));

        return new TwoFactorRecoveryCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CodeHash = BCrypt.Net.BCrypt.HashPassword(plainCode),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// コードを検証
    /// </summary>
    /// <param name="plainCode">検証する平文コード</param>
    /// <returns>コードが一致する場合true</returns>
    public bool Verify(string plainCode)
    {
        if (string.IsNullOrWhiteSpace(plainCode))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(plainCode, CodeHash);
        }
        catch
        {
            // ハッシュ検証エラーの場合はfalse
            return false;
        }
    }

    /// <summary>
    /// 使用済みとしてマーク
    /// </summary>
    public void MarkAsUsed()
    {
        if (IsUsed)
            throw new InvalidOperationException("Recovery code is already used");

        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }
}
