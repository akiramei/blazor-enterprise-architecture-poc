using Application.Infrastructure.LibraryManagement.Persistence;
using ApprovalWorkflow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
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
using Shared.Domain.Identity;
using Shared.Infrastructure.Platform;
using Shared.Infrastructure.Platform.Persistence;

namespace ProductCatalog.Web.IntegrationTests;

/// <summary>
/// カスタムWebApplicationFactory（ProductCatalog統合テスト用）
///
/// SQLite In-Memoryを使用し、テスト用のDbContextとIdentityデータをセットアップする
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // SQLite接続を維持（Disposeされるとデータベースが消えるため）
    private readonly SqliteConnection _productCatalogConnection;
    private readonly SqliteConnection _platformConnection;
    private readonly SqliteConnection _purchaseManagementConnection;
    private readonly SqliteConnection _libraryManagementConnection;
    private readonly SqliteConnection _approvalWorkflowConnection;

    public CustomWebApplicationFactory()
    {
        // SQLite In-Memory 接続を作成して開く（テストクラス全体で共有）
        _productCatalogConnection = new SqliteConnection("DataSource=:memory:");
        _productCatalogConnection.Open();

        _platformConnection = new SqliteConnection("DataSource=:memory:");
        _platformConnection.Open();

        _purchaseManagementConnection = new SqliteConnection("DataSource=:memory:");
        _purchaseManagementConnection.Open();

        _libraryManagementConnection = new SqliteConnection("DataSource=:memory:");
        _libraryManagementConnection.Open();

        _approvalWorkflowConnection = new SqliteConnection("DataSource=:memory:");
        _approvalWorkflowConnection.Open();

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

        // PurchaseManagementDbContext のスキーマ作成
        var purchaseManagementOptions = new DbContextOptionsBuilder<PurchaseManagementDbContext>()
            .UseSqlite(_purchaseManagementConnection)
            .Options;
        // ダミーのIAppContextとIWebHostEnvironmentを使用
        var dummyAppContext = new DummyAppContext();
        var dummyEnvironment = new DummyWebHostEnvironment();
        using (var context = new PurchaseManagementDbContext(
            purchaseManagementOptions,
            dummyAppContext,
            dummyEnvironment))
        {
            context.Database.EnsureCreated();
        }

        // LibraryManagementDbContext のスキーマ作成
        var libraryManagementOptions = new DbContextOptionsBuilder<LibraryManagementDbContext>()
            .UseSqlite(_libraryManagementConnection)
            .Options;
        var libraryLogger = NullLogger<LibraryManagementDbContext>.Instance;
        using (var context = new LibraryManagementDbContext(libraryManagementOptions, libraryLogger))
        {
            context.Database.EnsureCreated();
        }

        // ApprovalWorkflowDbContext のスキーマ作成
        var approvalWorkflowOptions = new DbContextOptionsBuilder<ApprovalWorkflowDbContext>()
            .UseSqlite(_approvalWorkflowConnection)
            .Options;
        using (var context = new ApprovalWorkflowDbContext(
            approvalWorkflowOptions,
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
            services.RemoveAll(typeof(DbContextOptions<LibraryManagementDbContext>));
            services.RemoveAll(typeof(DbContextOptions<ApprovalWorkflowDbContext>));
            services.RemoveAll(typeof(ProductCatalogDbContext));
            services.RemoveAll(typeof(PlatformDbContext));
            services.RemoveAll(typeof(PurchaseManagementDbContext));
            services.RemoveAll(typeof(LibraryManagementDbContext));
            services.RemoveAll(typeof(ApprovalWorkflowDbContext));

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

            services.AddDbContext<LibraryManagementDbContext>(options =>
            {
                options.UseSqlite(_libraryManagementConnection)
                       .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.AmbientTransactionWarning));
            });

            services.AddDbContext<ApprovalWorkflowDbContext>(options =>
            {
                options.UseSqlite(_approvalWorkflowConnection)
                       .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.AmbientTransactionWarning));
            });

            // Dapper 誤使用防止：ThrowingConnectionFactory を登録
            services.RemoveAll<IDbConnectionFactory>();
            services.AddSingleton<IDbConnectionFactory, ThrowingConnectionFactory>();

            // テスト用のEF Core実装のリポジトリを登録
            services.RemoveAll<ProductCatalog.Shared.Application.IProductReadRepository>();
            services.AddScoped<ProductCatalog.Shared.Application.IProductReadRepository, ProductCatalog.Web.IntegrationTests.TestDoubles.EfProductReadRepository>();

	            // Rate Limitingは Program.cs で Test環境では無効化されるため、テスト側での追加登録は不要
	        });
	    }

    /// <summary>
    /// Identityデータをシード（スキーマは既にコンストラクタで作成済み）
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        // .NET 10+: WebApplicationFactory.Services を直接使用（Server.Host は非推奨）
        using var scope = Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Identityデータをシード
        var seeder = serviceProvider.GetRequiredService<IdentityDataSeeder>();
        await seeder.SeedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _productCatalogConnection?.Dispose();
            _platformConnection?.Dispose();
            _purchaseManagementConnection?.Dispose();
            _libraryManagementConnection?.Dispose();
            _approvalWorkflowConnection?.Dispose();
        }
        base.Dispose(disposing);
    }
}
