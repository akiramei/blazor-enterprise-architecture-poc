using GetPurchaseRequestById.Application;
using GetPurchaseRequests.Application;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProductCatalog.Shared.Infrastructure.Persistence;
using PurchaseManagement.Infrastructure.Persistence;
using PurchaseManagement.Shared.Application;
using PurchaseManagement.Web.IntegrationTests.TestDoubles;
using Shared.Application;
using Shared.Infrastructure.Platform.Persistence;

namespace PurchaseManagement.Web.IntegrationTests;

/// <summary>
/// カスタムWebApplicationFactory（Fast テスト用）
///
/// SQLite In-Memoryを使用し、Dapperハンドラを EF Core ハンドラに置き換える
///
/// 特徴:
/// - SQLite In-Memory で Relational 挙動を保証（InMemory より厳密）
/// - Dapper 誤使用を防ぐため ThrowingConnectionFactory を登録
/// - EF Core ハンドラで高速テストを実現
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // SQLite接続を維持（Disposeされるとデータベースが消えるため）
    private readonly SqliteConnection _productCatalogConnection;
    private readonly SqliteConnection _platformConnection;
    private readonly SqliteConnection _purchaseManagementConnection;

    public CustomWebApplicationFactory()
    {
        // SQLite In-Memory 接続を作成して開く（テストクラス全体で共有）
        _productCatalogConnection = new SqliteConnection("DataSource=:memory:");
        _productCatalogConnection.Open();

        _platformConnection = new SqliteConnection("DataSource=:memory:");
        _platformConnection.Open();

        _purchaseManagementConnection = new SqliteConnection("DataSource=:memory:");
        _purchaseManagementConnection.Open();

        // データベーススキーマを初期化
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        // ProductCatalogDbContext のスキーマ作成
        var productCatalogOptions = new DbContextOptionsBuilder<ProductCatalogDbContext>()
            .UseSqlite(_productCatalogConnection)
            .Options;
        var logger = NullLogger<ProductCatalogDbContext>.Instance;
        using (var context = new ProductCatalogDbContext(productCatalogOptions, logger))
        {
            context.Database.EnsureCreated();
        }

        // PlatformDbContext のスキーマ作成
        var platformOptions = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlite(_platformConnection)
            .Options;
        using (var context = new PlatformDbContext(platformOptions))
        {
            context.Database.EnsureCreated();
        }

        // PurchaseManagementDbContext のスキーマ作成（IWebHostEnvironment が必要）
        // NOTE: Test環境ではGlobal Query Filterが無効化されているため、
        //       ダミーのIWebHostEnvironmentを使用してもGlobal Query Filterは適用されない
        var purchaseManagementOptions = new DbContextOptionsBuilder<PurchaseManagementDbContext>()
            .UseSqlite(_purchaseManagementConnection)
            .Options;

        // IAppContextとIWebHostEnvironmentのダミー実装を使用
        var dummyAppContext = new DummyAppContext();
        var dummyEnvironment = new DummyWebHostEnvironment();

        using (var context = new PurchaseManagementDbContext(
            purchaseManagementOptions,
            dummyAppContext,
            dummyEnvironment))
        {
            context.Database.EnsureCreated();
        }
    }

    // ダミー実装
    private class DummyAppContext : global::Shared.Application.Interfaces.IAppContext
    {
        public Guid UserId => Guid.Empty;
        public string UserName => "Test";
        public Guid? TenantId => null;
        public bool IsAuthenticated => false;
        public System.Security.Claims.ClaimsPrincipal User => new();
        public string CorrelationId => "test";
        public Guid RequestId => Guid.NewGuid();
        public DateTime RequestStartTimeUtc => DateTime.UtcNow;
        public string RequestPath => "/test";
        public string HttpMethod => "GET";
        public string? ClientIpAddress => null;
        public string? UserAgent => null;
        public string EnvironmentName => "Test";
        public string HostName => "test";
        public string ApplicationName => "test";
        public bool IsInRole(string role) => false;
        public bool IsInAnyRole(params string[] roles) => false;
    }

    private class DummyWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "Test";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Test";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // 既存のDbContext登録を完全に削除
            services.RemoveAll(typeof(DbContextOptions<ProductCatalogDbContext>));
            services.RemoveAll(typeof(DbContextOptions<PlatformDbContext>));
            services.RemoveAll(typeof(DbContextOptions<PurchaseManagementDbContext>));

            services.RemoveAll(typeof(ProductCatalogDbContext));
            services.RemoveAll(typeof(PlatformDbContext));
            services.RemoveAll(typeof(PurchaseManagementDbContext));

            // SQLite In-Memory でDbContextを再登録
            services.AddDbContext<ProductCatalogDbContext>(options =>
            {
                options.UseSqlite(_productCatalogConnection)
                       .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.AmbientTransactionWarning));
            });

            services.AddDbContext<PlatformDbContext>(options =>
            {
                options.UseSqlite(_platformConnection)
                       .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.AmbientTransactionWarning));
            });

            services.AddDbContext<PurchaseManagementDbContext>(options =>
            {
                options.UseSqlite(_purchaseManagementConnection)
                       .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.AmbientTransactionWarning));
            });

            // Dapper 誤使用防止：ThrowingConnectionFactory を登録
            services.RemoveAll<IDbConnectionFactory>();
            services.AddSingleton<IDbConnectionFactory, ThrowingConnectionFactory>();

            // Dapper ハンドラを EF Core ハンドラに置き換え
            services.RemoveAll<IRequestHandler<GetPurchaseRequestByIdQuery, Result<PurchaseRequestDetailDto?>>>();
            services.AddScoped<IRequestHandler<GetPurchaseRequestByIdQuery, Result<PurchaseRequestDetailDto?>>, EfGetPurchaseRequestByIdHandler>();

            services.RemoveAll<IRequestHandler<GetPurchaseRequestsQuery, Result<List<PurchaseRequestListItemDto>>>>();
            services.AddScoped<IRequestHandler<GetPurchaseRequestsQuery, Result<List<PurchaseRequestListItemDto>>>, EfGetPurchaseRequestsHandler>();

            // ApprovalFlowService をテスト用に置き換え（固定のApproverIdを使用）
            services.RemoveAll<PurchaseManagement.Shared.Application.IApprovalFlowService>();
            services.AddScoped<PurchaseManagement.Shared.Application.IApprovalFlowService, TestApprovalFlowService>();
        });
    }

    /// <summary>
    /// TenantId を null に設定したファクトリーを返す（セキュリティテスト用）
    /// </summary>
    public CustomWebApplicationFactory WithNullTenant()
    {
        return (CustomWebApplicationFactory)WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // IAppContext を null TenantId に置き換え
                services.RemoveAll<global::Shared.Application.Interfaces.IAppContext>();
                services.AddScoped<global::Shared.Application.Interfaces.IAppContext>(_ => new TestAppContext(null));
            });
        });
    }

    /// <summary>
    /// 指定された TenantId を設定したファクトリーを返す
    /// </summary>
    public CustomWebApplicationFactory WithTenant(Guid tenantId)
    {
        return (CustomWebApplicationFactory)WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // IAppContext を指定 TenantId に置き換え
                services.RemoveAll<global::Shared.Application.Interfaces.IAppContext>();
                services.AddScoped<global::Shared.Application.Interfaces.IAppContext>(_ => new TestAppContext(tenantId));
            });
        });
    }

    /// <summary>
    /// テスト用 IAppContext 実装
    /// </summary>
    private class TestAppContext : global::Shared.Application.Interfaces.IAppContext
    {
        public TestAppContext(Guid? tenantId)
        {
            TenantId = tenantId;
        }

        public Guid UserId => Guid.Empty;
        public string UserName => "Test";
        public Guid? TenantId { get; }
        public bool IsAuthenticated => TenantId.HasValue;
        public System.Security.Claims.ClaimsPrincipal User => new();
        public string CorrelationId => "test";
        public Guid RequestId => Guid.NewGuid();
        public DateTime RequestStartTimeUtc => DateTime.UtcNow;
        public string RequestPath => "/test";
        public string HttpMethod => "GET";
        public string? ClientIpAddress => null;
        public string? UserAgent => null;
        public string EnvironmentName => "Test";
        public string HostName => "test";
        public string ApplicationName => "test";
        public bool IsInRole(string role) => false;
        public bool IsInAnyRole(params string[] roles) => false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _productCatalogConnection?.Dispose();
            _platformConnection?.Dispose();
            _purchaseManagementConnection?.Dispose();
        }
        base.Dispose(disposing);
    }
}
