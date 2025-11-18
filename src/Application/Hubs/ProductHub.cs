using Microsoft.AspNetCore.SignalR;

namespace Application.Hubs;

/// <summary>
/// SignalR Hub for product update notifications
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
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}, Reason: {Exception}",
            Context.ConnectionId,
            exception?.Message ?? "Normal disconnection");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Notifies all connected clients about product changes
    /// </summary>
    public async Task NotifyProductChanged()
    {
        _logger.LogInformation("Broadcasting product change notification to all clients: {ConnectionId}", Context.ConnectionId);
        await Clients.All.ProductChanged();
    }
}
