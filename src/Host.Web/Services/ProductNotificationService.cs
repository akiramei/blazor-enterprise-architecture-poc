using Microsoft.AspNetCore.SignalR;
using Shared.Application.Interfaces;
using Application.Hubs;

namespace Application.Services;

/// <summary>
/// SignalR繧剃ｽｿ逕ｨ縺励◆蝠・刀螟画峩騾夂衍繧ｵ繝ｼ繝薙せ
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
        _logger.LogInformation("蝠・刀螟画峩繧貞・繧ｯ繝ｩ繧､繧｢繝ｳ繝医↓騾夂衍");
        await _hubContext.Clients.All.ProductChanged();
    }
}
