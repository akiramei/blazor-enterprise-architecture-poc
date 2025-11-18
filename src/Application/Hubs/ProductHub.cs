using Microsoft.AspNetCore.SignalR;

namespace Application.Hubs;

/// <summary>
/// 製品通知用 SignalR Hub
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
        _logger.LogInformation("クライアントが接続しました: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("クライアントが切断: {ConnectionId}, 理由: {Exception}",
            Context.ConnectionId,
            exception?.Message ?? "正常切断");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// すべてのクライアントに製品変更を通知
    /// </summary>
    public async Task NotifyProductChanged()
    {
        _logger.LogInformation("製品変更を全クライアントに送信: {ConnectionId}", Context.ConnectionId);
        await Clients.All.ProductChanged();
    }
}
