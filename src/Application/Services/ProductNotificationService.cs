using Microsoft.AspNetCore.SignalR;
using Shared.Application.Interfaces;
using Application.Host.Hubs;

namespace Application.Host.Services;

/// <summary>
/// SignalRを使用した商品変更通知サービス
/// </summary>
public sealed class ProductNotificationService : IProductNotificationService
{
    private readonly IHubContext<ProductHub, IProductHubClient> _hubContext;
    private readonly ILogger<ProductNotificationService> _logger;

    public ProductNotificationService(
        IHubContext<ProductHub, IProductHubClient> hubContext,
        ILogger<ProductNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyProductChangedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("商品変更を全クライアントに通知");
        await _hubContext.Clients.All.ProductChanged();
    }
}
