namespace Application.Hubs;

/// <summary>
/// SignalR クライアント用のメソッド定義
/// </summary>
public interface IProductHubClient
{
    /// <summary>
    /// 製品が変更されたことを通知し、他のユーザーの画面を更新する
    /// </summary>
    Task ProductChanged();
}
