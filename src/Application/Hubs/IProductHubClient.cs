namespace Application.Host.Hubs;

/// <summary>
/// SignalR クライアント側のメソッド定義
/// </summary>
public interface IProductHubClient
{
    /// <summary>
    /// 商品が変更されたことを通知（他のユーザーの操作を受信）
    /// </summary>
    Task ProductChanged();
}
