namespace ProductCatalog.Application.Common.Interfaces;

/// <summary>
/// 商品変更通知サービス（リアルタイム更新通知）
/// </summary>
public interface IProductNotificationService
{
    /// <summary>
    /// 商品が変更されたことを全クライアントに通知
    /// </summary>
    Task NotifyProductChangedAsync(CancellationToken cancellationToken = default);
}
