# 10. Infrastructure層の詳細設計

[← 目次に戻る](00_README.md)

---

## 10. Infrastructure層の詳細設計

### 10.1 Repository実装

```csharp
/// <summary>
/// 商品リポジトリ実装
/// </summary>
public sealed class EfProductRepository : IProductRepository
{
    private readonly AppDbContext _context;
    
    public EfProductRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<Product?> GetAsync(ProductId id, CancellationToken ct = default)
    {
        return await _context.Products
            .Include(p => p.Images)  // 集約全体を取得
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }
    
    public async Task<IEnumerable<Product>> GetManyAsync(
        IEnumerable<ProductId> ids, 
        CancellationToken ct = default)
    {
        var guidIds = ids.Select(id => id.Value).ToList();
        
        return await _context.Products
            .Include(p => p.Images)
            .Where(p => guidIds.Contains(p.Id.Value))
            .ToListAsync(ct);
    }
    
    public async Task SaveAsync(Product product, CancellationToken ct = default)
    {
        var existingProduct = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == product.Id, ct);
        
        if (existingProduct == null)
        {
            // 新規追加
            _context.Products.Add(product);
        }
        else
        {
            // 更新
            _context.Entry(existingProduct).CurrentValues.SetValues(product);
            
            // 集約内エンティティ(Images)の同期
            SyncImages(existingProduct, product);
        }
        
        // SaveChangesはTransactionBehaviorで実行される
    }
    
    public async Task DeleteAsync(ProductId id, CancellationToken ct = default)
    {
        var product = await GetAsync(id, ct);
        if (product != null)
        {
            _context.Products.Remove(product);
        }
    }
    
    private void SyncImages(Product existingProduct, Product newProduct)
    {
        // 削除されたImageを削除
        var imagesToDelete = existingProduct.Images
            .Where(ei => !newProduct.Images.Any(ni => ni.Id == ei.Id))
            .ToList();
        
        foreach (var image in imagesToDelete)
        {
            _context.Entry(image).State = EntityState.Deleted;
        }
        
        // 新規または更新されたImageを処理
        foreach (var newImage in newProduct.Images)
        {
            var existingImage = existingProduct.Images.FirstOrDefault(i => i.Id == newImage.Id);
            
            if (existingImage == null)
            {
                // 新規追加
                _context.Entry(newImage).State = EntityState.Added;
            }
            else
            {
                // 更新
                _context.Entry(existingImage).CurrentValues.SetValues(newImage);
            }
        }
    }
}
```

### 10.2 EF Core Configuration

```csharp
/// <summary>
/// Product エンティティ設定
/// </summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        
        // Primary Key
        builder.HasKey(p => p.Id);
        
        // Value Objectのマッピング
        builder.Property(p => p.Id)
            .HasConversion(
                id => id.Value,
                value => ProductId.From(value))
            .ValueGeneratedNever();
        
        builder.OwnsOne(p => p.Price, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("Price")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            
            priceBuilder.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });
        
        // プロパティ設定
        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(p => p.Description)
            .HasMaxLength(2000);
        
        builder.Property(p => p.Stock)
            .IsRequired();
        
        builder.Property(p => p.Status)
            .HasConversion<string>()  // Enumを文字列で保存
            .HasMaxLength(50)
            .IsRequired();
        
        // 集約内エンティティ(ProductImage)
        builder.OwnsMany(p => p.Images, imageBuilder =>
        {
            imageBuilder.ToTable("ProductImages");
            imageBuilder.HasKey(i => i.Id);
            
            imageBuilder.Property(i => i.Url)
                .HasMaxLength(500)
                .IsRequired();
            
            imageBuilder.Property(i => i.DisplayOrder)
                .IsRequired();
            
            imageBuilder.Property(i => i.UploadedAt)
                .IsRequired();
        });
        
        // インデックス
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.Name);
        
        // ドメインイベントは永続化しない
        builder.Ignore(p => p.DomainEvents);
    }
}
```

### 10.3 Read Model DAO(Dapper)

```csharp
/// <summary>
/// 商品読み取り専用DAO
/// </summary>
public interface IProductReadDao
{
    Task<IEnumerable<ProductDto>> GetProductListAsync(
        int page, 
        int pageSize, 
        CancellationToken ct = default);
    
    Task<int> GetTotalCountAsync(CancellationToken ct = default);
    
    Task<ProductDetailDto?> GetProductDetailAsync(
        Guid id, 
        CancellationToken ct = default);
}

public sealed class DapperProductReadDao : IProductReadDao
{
    private readonly IDbConnection _connection;
    
    public DapperProductReadDao(IDbConnection connection)
    {
        _connection = connection;
    }
    
    public async Task<IEnumerable<ProductDto>> GetProductListAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        // SQL Viewまたは最適化されたクエリ
        const string sql = @"
            SELECT 
                p.Id,
                p.Name,
                p.Price,
                p.Currency,
                p.Stock,
                p.Status,
                (SELECT COUNT(*) FROM ProductImages WHERE ProductId = p.Id) as ImageCount
            FROM Products p
            WHERE p.Status != 'Deleted'
            ORDER BY p.Name
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        
        return await _connection.QueryAsync<ProductDto>(
            sql,
            new { Offset = (page - 1) * pageSize, PageSize = pageSize });
    }
    
    public async Task<int> GetTotalCountAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(*) FROM Products WHERE Status != 'Deleted'";
        return await _connection.ExecuteScalarAsync<int>(sql);
    }
    
    public async Task<ProductDetailDto?> GetProductDetailAsync(
        Guid id,
        CancellationToken ct = default)
    {
        const string sql = @"
            SELECT 
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.Currency,
                p.Stock,
                p.Status,
                i.Id as ImageId,
                i.Url as ImageUrl,
                i.DisplayOrder as ImageDisplayOrder
            FROM Products p
            LEFT JOIN ProductImages i ON p.Id = i.ProductId
            WHERE p.Id = @Id";
        
        var productDict = new Dictionary<Guid, ProductDetailDto>();
        
        await _connection.QueryAsync<ProductDetailDto, ProductImageDto, ProductDetailDto>(
            sql,
            (product, image) =>
            {
                if (!productDict.TryGetValue(product.Id, out var productEntry))
                {
                    productEntry = product;
                    productEntry.Images = new List<ProductImageDto>();
                    productDict.Add(product.Id, productEntry);
                }
                
                if (image != null)
                {
                    ((List<ProductImageDto>)productEntry.Images).Add(image);
                }
                
                return productEntry;
            },
            new { Id = id },
            splitOn: "ImageId");
        
        return productDict.Values.FirstOrDefault();
    }
}

/// <summary>
/// 商品リストDTO
/// </summary>
public sealed record ProductDto(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    int Stock,
    string Status,
    int ImageCount
)
{
    public string DisplayPrice => $"Â¥{Price:N0}";
    public bool IsLowStock => Stock < 10;
}

/// <summary>
/// 商品詳細DTO
/// </summary>
public sealed class ProductDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "JPY";
    public int Stock { get; init; }
    public string Status { get; init; } = string.Empty;
    public IEnumerable<ProductImageDto> Images { get; set; } = Enumerable.Empty<ProductImageDto>();
}

public sealed record ProductImageDto(
    Guid ImageId,
    string ImageUrl,
    int ImageDisplayOrder
);
```

### 10.4 Outbox Pattern実装

```csharp
/// <summary>
/// Outboxメッセージ
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    
    public static OutboxMessage Create<TEvent>(TEvent @event) where TEvent : DomainEvent
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = typeof(TEvent).AssemblyQualifiedName!,
            PayloadJson = JsonSerializer.Serialize(@event),
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };
    }
    
    public TEvent GetEvent<TEvent>() where TEvent : DomainEvent
    {
        return JsonSerializer.Deserialize<TEvent>(PayloadJson)!;
    }
}

/// <summary>
/// Outbox Dispatcher(バックグラウンドサービス)
/// </summary>
public sealed class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);
    
    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Dispatcher started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }
            
            await Task.Delay(_interval, stoppingToken);
        }
        
        _logger.LogInformation("Outbox Dispatcher stopped");
    }
    
    private async Task ProcessOutboxMessagesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ProductsHub>>();
        
        // 未配信メッセージを取得
        var messages = await context.OutboxMessages
            .Where(m => m.PublishedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .Take(100)
            .ToListAsync(ct);
        
        foreach (var message in messages)
        {
            try
            {
                // イベントタイプに応じた処理
                await PublishEventAsync(message, hubContext, ct);
                
                // 配信成功
                message.PublishedAt = DateTime.UtcNow;
                _logger.LogInformation(
                    "Outbox message published: {EventType} [Id: {Id}]",
                    message.EventType,
                    message.Id);
            }
            catch (Exception ex)
            {
                // リトライカウント増加
                message.RetryCount++;
                message.ErrorMessage = ex.Message;
                
                _logger.LogWarning(ex,
                    "Failed to publish outbox message: {EventType} [Id: {Id}] Retry: {RetryCount}",
                    message.EventType,
                    message.Id,
                    message.RetryCount);
            }
        }
        
        await context.SaveChangesAsync(ct);
    }
    
    private async Task PublishEventAsync(
        OutboxMessage message,
        IHubContext<ProductsHub> hubContext,
        CancellationToken ct)
    {
        // SignalRで配信
        if (message.EventType.Contains(nameof(ProductDeletedEvent)))
        {
            var @event = message.GetEvent<ProductDeletedEvent>();
            await hubContext.Clients.All.SendAsync(
                "ProductDeleted",
                @event.ProductId.Value,
                ct);
        }
        else if (message.EventType.Contains(nameof(ProductPriceChangedEvent)))
        {
            var @event = message.GetEvent<ProductPriceChangedEvent>();
            await hubContext.Clients.All.SendAsync(
                "ProductPriceChanged",
                @event.ProductId.Value,
                @event.NewPrice.Amount,
                ct);
        }
        
        // 外部Message Broker(RabbitMQ等)への配信も可能
        // await _messageBroker.PublishAsync(message);
    }
}
```

### 10.5 SignalR Hub

```csharp
/// <summary>
/// 商品管理用SignalR Hub
/// </summary>
public sealed class ProductsHub : Hub
{
    private readonly ILogger<ProductsHub> _logger;
    
    public ProductsHub(ILogger<ProductsHub> logger)
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
        _logger.LogInformation(
            "Client disconnected: {ConnectionId} {Exception}",
            Context.ConnectionId,
            exception?.Message);
        
        await base.OnDisconnectedAsync(exception);
    }
    
    /// <summary>
    /// 特定のグループに参加(例: 商品カテゴリ別)
    /// </summary>
    public async Task JoinProductGroup(string category)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"category_{category}");
        _logger.LogInformation(
            "Client {ConnectionId} joined group: category_{Category}",
            Context.ConnectionId,
            category);
    }
    
    /// <summary>
    /// グループから退出
    /// </summary>
    public async Task LeaveProductGroup(string category)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"category_{category}");
    }
}

// Blazor Component側での使用例
@code {
    private HubConnection? _hubConnection;
    
    protected override async Task OnInitializedAsync()
    {
        // SignalR接続
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/hubs/products"))
            .WithAutomaticReconnect()
            .Build();
        
        // イベントハンドラー登録
        _hubConnection.On<Guid>("ProductDeleted", async (productId) =>
        {
            // 削除された商品をUIから除外
            await Actions.LoadAsync();
        });
        
        _hubConnection.On<Guid, decimal>("ProductPriceChanged", async (productId, newPrice) =>
        {
            // 価格変更を反映
            await Actions.LoadAsync();
        });
        
        await _hubConnection.StartAsync();
        
        // Store購読
        Store.OnChangeAsync += HandleStateChanged;
        await Actions.LoadAsync();
    }
    
    public async ValueTask DisposeAsync()
    {
        Store.OnChangeAsync -= HandleStateChanged;
        
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
```

---
