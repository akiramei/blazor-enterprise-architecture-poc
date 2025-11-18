using Microsoft.AspNetCore.SignalR;

namespace Application.Hubs;

/// <summary>
/// 蝠・刀邂｡逅・畑 SignalR Hub
/// </summary>
public sealed class ProductHub : Hub<IProductHubClient>
{
    private readonly ILogger<ProductHub> _logger;

    public ProductHub(ILogger<ProductHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("繧ｯ繝ｩ繧､繧｢繝ｳ繝域磁邯・ {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("クライアント切断: {ConnectionId}, 切断理由: {Exception}",
            Context.ConnectionId,
            exception?.Message ?? "なし");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 縺吶∋縺ｦ縺ｮ繧ｯ繝ｩ繧､繧｢繝ｳ繝医↓蝠・刀螟画峩繧帝夂衍
    /// </summary>
    public async Task NotifyProductChanged()
    {
        _logger.LogInformation("蝠・刀螟画峩繧貞・繧ｯ繝ｩ繧､繧｢繝ｳ繝医↓騾夂衍: {ConnectionId}", Context.ConnectionId);
        await Clients.All.ProductChanged();
    }
}
