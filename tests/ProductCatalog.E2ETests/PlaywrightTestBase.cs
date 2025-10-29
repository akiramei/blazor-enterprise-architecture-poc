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
using Microsoft.Playwright;
using ProductCatalog.Application.Common.Interfaces;
using ProductCatalog.Domain.Identity;
using ProductCatalog.Domain.Products;
using ProductCatalog.Infrastructure.Idempotency;
using ProductCatalog.Infrastructure.Persistence;
using ProductCatalog.Infrastructure.Persistence.Repositories;
using ProductCatalog.Infrastructure.Services;
using ProductCatalog.Web.Components;
using ProductCatalog.Web.Features.Products.Actions;
using ProductCatalog.Web.Features.Products.Store;

namespace ProductCatalog.E2ETests;

/// <summary>
/// テスト用の認証状態プロバイダー（常に認証済みとして扱う）
/// </summary>
internal class TestAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "TestUser"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, Roles.Admin),
        }, "Test");

        var user = new System.Security.Claims.ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
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
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Test";
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        // wwwrootパスを設定（Web プロジェクトの wwwroot を参照）
        var webProjectPath = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "..", "src", "ProductCatalog.Web"));
        builder.Environment.WebRootPath = Path.Combine(webProjectPath, "wwwroot");
        builder.Environment.ContentRootPath = webProjectPath;

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
            cfg.RegisterServicesFromAssembly(typeof(ProductCatalog.Application.Features.Products.GetProducts.GetProductsHandler).Assembly);
        });

        // FluentValidation
        builder.Services.AddValidatorsFromAssembly(typeof(ProductCatalog.Application.Features.Products.GetProducts.GetProductsHandler).Assembly);

        // Pipeline Behaviors
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Application.Common.Behaviors.LoggingBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Application.Common.Behaviors.ValidationBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Infrastructure.Behaviors.AuthorizationBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Infrastructure.Behaviors.IdempotencyBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Infrastructure.Behaviors.CachingBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Infrastructure.Behaviors.TransactionBehavior<,>));

        // Authentication State Provider（テスト用）
        builder.Services.AddScoped<AuthenticationStateProvider, TestAuthenticationStateProvider>();
        builder.Services.AddCascadingAuthenticationState();

        // Authorization（テスト環境では全て許可）
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAssertion(_ => true) // テスト環境では全てのリクエストを許可
                .Build();
        });

        // Memory Cache
        builder.Services.AddMemoryCache();

        // Current User Service
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Product Notification Service (SignalR)
        builder.Services.AddScoped<IProductNotificationService, ProductCatalog.Web.Services.ProductNotificationService>();

        // Correlation ID Accessor
        builder.Services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();

        // Idempotency Store
        builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();

        // DbContext (InMemory for Test)
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("TestDatabase"));

        // ASP.NET Core Identity
        builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // Cookie認証設定
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;
        });

        // Repositories
        builder.Services.AddScoped<IProductRepository, EfProductRepository>();
        builder.Services.AddScoped<IProductReadRepository, DapperProductReadRepository>();

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
            .AddInteractiveServerRenderMode();

        // SignalR Hub
        _app.MapHub<ProductCatalog.Web.Hubs.ProductHub>("/hubs/products");

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
}
