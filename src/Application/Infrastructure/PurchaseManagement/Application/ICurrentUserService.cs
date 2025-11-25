namespace PurchaseManagement.Shared.Application;

/// <summary>
/// 現在のユーザー情報取得サービスインターフェース
///
/// 【マルチテナント対応】
/// - TenantId プロパティを追加してマルチテナント分離を実現
/// - UserId を Guid (non-nullable) に変更して認証必須化
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// 現在のユーザーID（認証済みユーザーは必須）
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// 現在のテナントID（マルチテナント環境では必須）
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// ユーザー名
    /// </summary>
    string? UserName { get; }
}
