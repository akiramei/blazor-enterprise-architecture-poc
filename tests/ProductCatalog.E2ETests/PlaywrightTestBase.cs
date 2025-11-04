using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Playwright;
using Shared.Application.Common;
using Shared.Application.Interfaces;
using Shared.Infrastructure.Behaviors;
using Shared.Infrastructure.Platform.Persistence;
using ProductCatalog.Shared.Infrastructure.Persistence;
using ProductCatalog.Shared.Infrastructure.Persistence.Behaviors;
using ProductCatalog.Shared.Application;
using ProductCatalog.Shared.Application.DTOs;
using Shared.Domain.Identity;
using Shared.Domain.AuditLogs;
using ProductCatalog.Shared.Domain.Products;
using Shared.Infrastructure.Idempotency;
using Shared.Infrastructure.Persistence;
using ProductCatalog.Shared.Infrastructure.Persistence.Repositories;
using Shared.Infrastructure.Services;
using ProductCatalog.Host.Components;
using ProductCatalog.Host.Services;
using ProductCatalog.Host.Hubs;
using ProductCatalog.Shared.UI.Actions;
using ProductCatalog.Shared.UI.Store;
using GetProducts.Application;

namespace ProductCatalog.E2ETests;

/// <summary>
/// テスト用のProductReadRepository（InMemory database用）
/// AppDbContextから直接クエリして IProductReadRepository として提供
/// </summary>
internal class TestAuditLogRepository : IAuditLogRepository
{
    public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        // No-op for tests
        return Task.CompletedTask;
    }

    public Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No-op for tests
        return Task.FromResult<AuditLog?>(null);
    }

    public Task<IEnumerable<AuditLog>> GetByEntityAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        // No-op for tests
        return Task.FromResult<IEnumerable<AuditLog>>(Array.Empty<AuditLog>());
    }

    public Task<IEnumerable<AuditLog>> GetByUserAsync(
        Guid userId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        // No-op for tests
        return Task.FromResult<IEnumerable<AuditLog>>(Array.Empty<AuditLog>());
    }

    public Task<IEnumerable<AuditLog>> GetByDateRangeAsync(
        DateTime startUtc,
        DateTime endUtc,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        // No-op for tests
        return Task.FromResult<IEnumerable<AuditLog>>(Array.Empty<AuditLog>());
    }
}

internal class TestProductReadRepository : IProductReadRepository
{
    private readonly ProductCatalogDbContext _context;

    public TestProductReadRepository(ProductCatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _context.Products.ToListAsync(cancellationToken);
        return products.Select(p => new ProductDto(
            p.Id.Value,
            p.Name,
            p.Description,
            p.Price.Amount,
            p.Price.Currency,
            p.Stock,
            // DisplayPriceを計算
            p.Price.Currency == "JPY" ? $"¥{p.Price.Amount:N0}" :
            p.Price.Currency == "USD" ? $"${p.Price.Amount:N2}" :
            $"{p.Price.Amount:N2} {p.Price.Currency}",
            p.Status.ToString(),
            (int)p.Version
        )).ToList();
    }

    public async Task<ProductDetailDto?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => EF.Property<ProductId>(p, "_id") == new ProductId(productId), cancellationToken);

        if (product == null) return null;

        return new ProductDetailDto
        {
            Id = product.Id.Value,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price.Amount,
            Stock = product.Stock,
            Status = product.Status.ToString(),
            IsDeleted = product.IsDeleted,
            Version = product.Version,
            Images = product.Images
                .Select(img => new ProductImageDto
                {
                    Id = img.Id.Value,
                    Url = img.Url,
                    DisplayOrder = img.DisplayOrder
                })
                .ToList()
        };
    }

    public async Task<PagedResult<ProductDto>> SearchAsync(
        string? nameFilter,
        decimal? minPrice,
        decimal? maxPrice,
        ProductStatus? status,
        int page,
        int pageSize,
        string orderBy,
        bool isDescending,
        CancellationToken cancellationToken = default)
    {
        var products = await _context.Products.ToListAsync(cancellationToken);

        // フィルタリング
        var filtered = products.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            filtered = filtered.Where(p => p.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (minPrice.HasValue)
        {
            filtered = filtered.Where(p => p.Price.Amount >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            filtered = filtered.Where(p => p.Price.Amount <= maxPrice.Value);
        }

        if (status.HasValue)
        {
            filtered = filtered.Where(p => p.Status == status.Value);
        }

        // ソート
        filtered = orderBy switch
        {
            "Price" => isDescending
                ? filtered.OrderByDescending(p => p.Price.Amount)
                : filtered.OrderBy(p => p.Price.Amount),
            "Stock" => isDescending
                ? filtered.OrderByDescending(p => p.Stock)
                : filtered.OrderBy(p => p.Stock),
            "Status" => isDescending
                ? filtered.OrderByDescending(p => p.Status)
                : filtered.OrderBy(p => p.Status),
            _ => isDescending
                ? filtered.OrderByDescending(p => p.Name)
                : filtered.OrderBy(p => p.Name)
        };

        var totalCount = filtered.Count();
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto(
                p.Id.Value,
                p.Name,
                p.Description,
                p.Price.Amount,
                p.Price.Currency,
                p.Stock,
                // DisplayPriceを計算
                p.Price.Currency == "JPY" ? $"¥{p.Price.Amount:N0}" :
                p.Price.Currency == "USD" ? $"${p.Price.Amount:N2}" :
                $"{p.Price.Amount:N2} {p.Price.Currency}",
                p.Status.ToString(),
                (int)p.Version
            ))
            .ToList();

        return PagedResult<ProductDto>.Create(items, totalCount, page, pageSize);
    }
}

/// <summary>
/// テスト用の認証ハンドラー（ASP.NET Core ミドルウェア用）
/// すべてのリクエストを Admin ユーザーとして認証済みとして扱う
/// </summary>
internal class TestAuthenticationHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-user-id"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "TestUser"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, Roles.Admin),
        };

        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "Test");

        return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// テスト用の認証状態プロバイダー（Blazor Components用）
/// </summary>
internal class TestAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-user-id"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "TestUser"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, Roles.Admin),
        }, "Test");

        var user = new System.Security.Claims.ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }
}

/// <summary>
/// テスト環境では全てのAuthorize要求を許可するAuthorizationHandler
/// </summary>
internal class AllowAnonymousAuthorizationHandler : Microsoft.AspNetCore.Authorization.IAuthorizationHandler
{
    public Task HandleAsync(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context)
    {
        foreach (var requirement in context.PendingRequirements.ToList())
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// Playwright E2Eテストの基底クラス
///
/// 【パターン: E2Eテスト基盤】
///
/// 使用シナリオ:
/// - ブラウザの自動操作
/// - エンドツーエンドのユーザーシナリオテスト
/// - クロスブラウザテスト
///
/// 実装ガイド:
/// - IAsyncLifetimeでテスト前後のセットアップ/クリーンアップ
/// - WebApplicationで直接テストサーバー起動
/// - Playwrightでブラウザ制御
///
/// AI実装時の注意:
/// - Headlessモードで実行（CI環境）
/// - テスト完了後は必ずリソース解放
/// - ページ遷移は WaitForURLAsync で確実に待機
/// - 要素の表示待ちは WaitForSelectorAsync を使用
/// </summary>
public abstract class PlaywrightTestBase : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private WebApplication? _app;
    protected string BaseUrl { get; private set; } = string.Empty;
    protected IPage? Page { get; private set; }

    public async Task InitializeAsync()
    {
        // Playwrightセットアップ
        _playwright = await Playwright.CreateAsync();

        // ブラウザ起動（ヘッドレスモード）
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true, // CI環境用。ローカルデバッグ時はfalseに変更可能
            SlowMo = 0 // ミリ秒単位で操作を遅くする（デバッグ用）
        });

        // テストサーバー起動（Kestrelで実際のポートをリッスン）
        // wwwrootパスを設定（Host プロジェクトの wwwroot を参照）
        var webProjectPath = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "..", "src", "ProductCatalog.Host"));

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Test",
            ContentRootPath = webProjectPath,
            WebRootPath = Path.Combine(webProjectPath, "wwwroot")
        });

        builder.WebHost.UseUrls("http://127.0.0.1:0");

        // === Program.csと同じサービス設定 ===

        // Razor Components
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // SignalR
        builder.Services.AddSignalR();

        // HttpContextAccessor
        builder.Services.AddHttpContextAccessor();

        // MediatR
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(GetProducts.Application.GetProductsHandler).Assembly);
        });

        // FluentValidation
        builder.Services.AddValidatorsFromAssembly(typeof(GetProducts.Application.GetProductsHandler).Assembly);

        // Pipeline Behaviors
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        // Authentication（テスト用の認証スキーム）
        builder.Services.AddAuthentication("Test")
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthenticationHandler>(
                "Test", options => { });

        // Authentication State Provider（Blazor Components用）
        builder.Services.AddScoped<AuthenticationStateProvider, TestAuthenticationStateProvider>();
        builder.Services.AddCascadingAuthenticationState();

        // Authorization（テスト環境 - すべての認証要求を許可）
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, AllowAnonymousAuthorizationHandler>();

        // Memory Cache
        builder.Services.AddMemoryCache();

        // Current User Service
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Product Notification Service (SignalR)
        builder.Services.AddScoped<IProductNotificationService, ProductCatalog.Host.Services.ProductNotificationService>();

        // Correlation ID Accessor
        builder.Services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();

        // Idempotency Store
        builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();

        // DbContext (InMemory for Test)
        // ProductCatalog BC DbContext (Product entities)
        builder.Services.AddDbContext<ProductCatalogDbContext>(options =>
            options.UseInMemoryDatabase("TestDatabase_ProductCatalog"));

        // Platform DbContext (Identity, RefreshToken, Outbox, AuditLog)
        builder.Services.AddDbContext<PlatformDbContext>(options =>
            options.UseInMemoryDatabase("TestDatabase_Platform"));

        // Legacy AppDbContext for compatibility
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("TestDatabase"));

        // Note: ASP.NET Core Identity is NOT configured for tests
        // We use TestAuthenticationHandler instead for simplified authentication

        // Repositories（テスト環境ではEF Coreリポジトリを使用）
        // Note: DapperProductReadRepository は InMemory database では動作しない（Relational database が必要）
        builder.Services.AddScoped<IProductRepository, EfProductRepository>();
        // Remove all existing IProductReadRepository registrations and add TestProductReadRepository
        builder.Services.RemoveAll<IProductReadRepository>();
        builder.Services.AddScoped<IProductReadRepository, TestProductReadRepository>();
        // Add stub for IAuditLogRepository if needed
        builder.Services.RemoveAll<IAuditLogRepository>();
        builder.Services.AddScoped<IAuditLogRepository, TestAuditLogRepository>();

        // Stores (Scoped for Blazor Server circuits)
        builder.Services.AddScoped<ProductsStore>();
        builder.Services.AddScoped<ProductDetailStore>();
        builder.Services.AddScoped<ProductEditStore>();
        builder.Services.AddScoped<ProductSearchStore>();

        // Actions (Scoped for Blazor Server circuits)
        builder.Services.AddScoped<ProductListActions>();
        builder.Services.AddScoped<ProductDetailActions>();
        builder.Services.AddScoped<ProductEditActions>();
        builder.Services.AddScoped<ProductSearchActions>();

        // === アプリケーションのビルドと設定 ===

        _app = builder.Build();

        // ミドルウェア設定（テスト環境用に最小限）
        _app.UseStaticFiles();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseAntiforgery();

        // Razor Components
        _app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies(Program.FeatureUIAssemblies);

        // SignalR Hub
        _app.MapHub<ProductCatalog.Host.Hubs.ProductHub>("/hubs/products");

        // サーバーを起動
        await _app.StartAsync();

        // サーバーアドレスを取得
        var addresses = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>();
        BaseUrl = addresses!.Addresses.First();

        // 新しいページを作成
        Page = await _browser.NewPageAsync();

        // デフォルトタイムアウト設定（30秒）
        Page.SetDefaultTimeout(30000);
    }

    public async Task DisposeAsync()
    {
        if (Page != null)
        {
            await Page.CloseAsync();
        }

        if (_browser != null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();

        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    /// <summary>
    /// 指定したURLに移動
    /// </summary>
    protected async Task GotoAsync(string path)
    {
        if (Page == null) throw new InvalidOperationException("Page is not initialized");
        await Page.GotoAsync($"{BaseUrl}{path}");
    }

    /// <summary>
    /// スクリーンショットを保存（デバッグ用）
    /// </summary>
    protected async Task<byte[]> TakeScreenshotAsync()
    {
        if (Page == null) throw new InvalidOperationException("Page is not initialized");
        return await Page.ScreenshotAsync();
    }

    /// <summary>
    /// テスト用商品を作成してDBに保存
    /// </summary>
    protected async Task<Guid> CreateTestProductAsync(
        string name,
        string description,
        decimal price,
        int stock,
        bool publish = false)
    {
        // AppDbContextを取得
        using var scope = _app!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Productエンティティを作成
        var product = Product.Create(
            name,
            description,
            new Money(price, "JPY"),
            stock
        );

        // 公開指定の場合は公開
        if (publish && stock > 0)
        {
            product.Publish();
        }

        // DBに保存
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        return product.Id.Value;
    }

    /// <summary>
    /// テスト用商品を複数作成
    /// </summary>
    protected async Task<List<Guid>> CreateTestProductsAsync(int count)
    {
        var productIds = new List<Guid>();
        for (int i = 0; i < count; i++)
        {
            var productId = await CreateTestProductAsync(
                $"テスト商品{i + 1}",
                $"これはテスト商品{i + 1}の説明です",
                1000 * (i + 1),
                10 * (i + 1),
                publish: i % 2 == 0 // 偶数番目のみ公開
            );
            productIds.Add(productId);
        }
        return productIds;
    }
}
