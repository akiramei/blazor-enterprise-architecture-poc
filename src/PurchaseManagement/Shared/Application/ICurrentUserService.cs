namespace PurchaseManagement.Shared.Application;

/// <summary>
/// 現在のユーザー情報取得サービスインターフェース
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
}
