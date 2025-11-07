using System.Text;
using AspNetCoreRateLimit;
using Asp.Versioning;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.Infrastructure.Behaviors;
using Shared.Application.Interfaces;
using Shared.Domain.Identity;
using ProductCatalog.Shared.Domain.Products;
using Shared.Infrastructure.Authentication;
using Shared.Infrastructure.Platform.Stores;
using ProductCatalog.Shared.Infrastructure.Persistence;
using ProductCatalog.Shared.Infrastructure.Persistence.Repositories;
using ProductCatalog.Host.Services;
using ProductCatalog.Host.Components;
using ProductCatalog.Shared.UI.Actions;
using ProductCatalog.Shared.UI.Store;
using Serilog;
using GetProducts.Application;
using Shared.Infrastructure.Platform.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Serilog 初期化（appsettings.jsonから設定を読み込む）
builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SignalR（リアルタイム更新通知）
builder.Services.AddSignalR();

// HttpContextAccessor（現在のユーザー情報取得に必要）
builder.Services.AddHttpContextAccessor();

// OpenTelemetry Metrics（メトリクス収集）
builder.Services.AddSingleton<Shared.Infrastructure.Metrics.ApplicationMetrics>();
// TODO: Add OpenTelemetry package to enable this
// builder.Services.AddOpenTelemetry()
//     .WithMetrics(metrics =>
//     {
//         // ProductCatalogアプリケーションのメトリクスを収集
//         metrics.AddMeter("ProductCatalog");
//
//         // エクスポーター設定（開発環境）
//         // 本番環境では Prometheus や Azure Monitor等を使用
//         // .AddPrometheusExporter();
//         // .AddAzureMonitorMetricExporter();
//     });

// MediatR - すべてのFeature Applicationアセンブリを登録
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GetProductsHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(GetProductById.Application.GetProductByIdHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(CreateProduct.Application.CreateProductHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(UpdateProduct.Application.UpdateProductHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(DeleteProduct.Application.DeleteProductHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(SearchProducts.Application.SearchProductsHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(BulkDeleteProducts.Application.BulkDeleteProductsHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(BulkUpdateProductPrices.Application.BulkUpdateProductPricesHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(ExportProductsToCsv.Application.ExportProductsToCsvHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(ImportProductsFromCsv.Application.ImportProductsFromCsvHandler).Assembly);
});

// FluentValidation - すべてのFeature Applicationアセンブリを登録
builder.Services.AddValidatorsFromAssemblies(new[]
{
    typeof(GetProductsHandler).Assembly,
    typeof(GetProductById.Application.GetProductByIdHandler).Assembly,
    typeof(CreateProduct.Application.CreateProductHandler).Assembly,
    typeof(UpdateProduct.Application.UpdateProductHandler).Assembly,
    typeof(DeleteProduct.Application.DeleteProductHandler).Assembly,
    typeof(SearchProducts.Application.SearchProductsHandler).Assembly,
    typeof(BulkDeleteProducts.Application.BulkDeleteProductsHandler).Assembly,
    typeof(BulkUpdateProductPrices.Application.BulkUpdateProductPricesHandler).Assembly,
    typeof(ExportProductsToCsv.Application.ExportProductsToCsvHandler).Assembly,
    typeof(ImportProductsFromCsv.Application.ImportProductsFromCsvHandler).Assembly
});

// Pipeline Behaviors（登録順序が重要！）
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Shared.Infrastructure.Behaviors.MetricsBehavior<,>));        // 0. Metrics - 全体の実行時間を計測
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Shared.Infrastructure.Behaviors.LoggingBehavior<,>));        // 1. Logging
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Shared.Infrastructure.Behaviors.ValidationBehavior<,>));     // 2. Validation
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Shared.Infrastructure.Behaviors.AuthorizationBehavior<,>));  // 3. Authorization
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Shared.Infrastructure.Behaviors.IdempotencyBehavior<,>));    // 4. Idempotency (Command)
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Shared.Infrastructure.Behaviors.CachingBehavior<,>));        // 5. Caching (Query)
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Shared.Infrastructure.Behaviors.AuditLogBehavior<,>));       // 6. AuditLog (Command) - 監査ログ記録
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Shared.Infrastructure.Persistence.Behaviors.TransactionBehavior<,>));    // 7. Transaction (Command) - BC固有

// Authorization
builder.Services.AddAuthorization();

// Memory Cache
builder.Services.AddMemoryCache();

// Current User Service
builder.Services.AddScoped<ICurrentUserService, Shared.Infrastructure.Services.CurrentUserService>();

// App Context (統合コンテキスト - ユーザー情報、リクエスト情報、環境情報を一元管理)
builder.Services.AddScoped<IAppContext, Shared.Infrastructure.Services.AppContext>();

// Product Notification Service (SignalR)
builder.Services.AddScoped<IProductNotificationService, ProductCatalog.Host.Services.ProductNotificationService>();

// Correlation ID Accessor (Distributed Tracing)
builder.Services.AddScoped<ICorrelationIdAccessor, Shared.Infrastructure.Services.CorrelationIdAccessor>();

// Infrastructure.Platform Stores (Port/Adapter Pattern)
builder.Services.AddScoped<Shared.Abstractions.Platform.IOutboxStore, Shared.Infrastructure.Platform.Stores.OutboxStore>();
builder.Services.AddScoped<Shared.Abstractions.Platform.IAuditLogStore, Shared.Infrastructure.Platform.Stores.AuditLogStore>();
builder.Services.AddSingleton<Shared.Abstractions.Platform.IIdempotencyStore, Shared.Infrastructure.Platform.Stores.InMemoryIdempotencyStore>();

// Outbox Readers (トランザクショナルOutboxパターン - 読み取り実装)
builder.Services.AddScoped<Shared.Abstractions.Platform.IOutboxReader, ProductCatalog.Shared.Infrastructure.Persistence.ProductCatalogOutboxReader>();

// Legacy Idempotency Store (Old Interface - for compatibility) - removed as it's deprecated

// DbContext (Infrastructure.Platform Pattern)
if (builder.Environment.IsEnvironment("Test"))
{
    // Test environment: InMemory databases
    builder.Services.AddDbContext<ProductCatalogDbContext>(options =>
        options.UseInMemoryDatabase("TestDatabase_ProductCatalog"));

    builder.Services.AddDbContext<Shared.Infrastructure.Platform.Persistence.PlatformDbContext>(options =>
        options.UseInMemoryDatabase("TestDatabase_Platform"));
}
else
{
    // Production environment: PostgreSQL
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    builder.Services.AddDbContext<ProductCatalogDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddDbContext<Shared.Infrastructure.Platform.Persistence.PlatformDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// ASP.NET Core Identity（本番用認証・認可）
// Identity は技術的関心事なので PlatformDbContext を使用
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    // パスワード要件（本番環境では厳格に設定）
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // ユーザー設定
    options.User.RequireUniqueEmail = true;

    // ロックアウト設定
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<Shared.Infrastructure.Platform.Persistence.PlatformDbContext>()
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

// JWT設定をバインド
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

// JWT Token Generator
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// JWT Bearer認証設定（REST API用）
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings are not configured.");

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Repositories
builder.Services.AddScoped<IProductRepository, EfProductRepository>();
// Dapper を使用した高速読み取りリポジトリ（本番環境で効果を発揮）
// テスト環境ではDapperは使えないため、EfProductRepositoryをベースにしたラッパーを使用
if (builder.Environment.IsEnvironment("Test"))
{
    // テスト環境ではInMemoryデータベースを使うため、DapperProductReadRepositoryは使えない
    // 代わりに、登録しない（E2Eテストで独自に登録される）
}
else
{
    builder.Services.AddScoped<ProductCatalog.Shared.Application.IProductReadRepository, DapperProductReadRepository>();
}
// Audit Log Repository（監査ログ）
builder.Services.AddScoped<IAuditLogRepository, Shared.Infrastructure.Platform.Repositories.AuditLogRepository>();

// Infrastructure Services (Scoped for Blazor Server circuits)
builder.Services.AddScoped<ProductCatalog.Host.Infrastructure.Services.LocalStorageService>();

// Infrastructure Stores (Scoped for Blazor Server circuits - システムレベル状態管理)
builder.Services.AddScoped<ProductCatalog.Host.Infrastructure.Stores.PreferencesStore>();
builder.Services.AddScoped<ProductCatalog.Host.Infrastructure.Stores.LayoutStore>();
builder.Services.AddScoped<ProductCatalog.Host.Infrastructure.Stores.NotificationStore>();

// Feature Stores (Scoped for Blazor Server circuits - ドメイン固有状態管理)
builder.Services.AddScoped<ProductsStore>();
builder.Services.AddScoped<ProductDetailStore>();
builder.Services.AddScoped<ProductEditStore>();
builder.Services.AddScoped<ProductSearchStore>();

// Actions (Scoped for Blazor Server circuits)
builder.Services.AddScoped<ProductListActions>();
builder.Services.AddScoped<ProductDetailActions>();
builder.Services.AddScoped<ProductEditActions>();
builder.Services.AddScoped<ProductSearchActions>();

// Outbox Background Service (Outbox Patternによる統合イベント配信)
// TODO: Restore when OutboxBackgroundService is moved to correct location
// builder.Services.AddHostedService<Shared.Infrastructure.Platform.Outbox.OutboxBackgroundService>();

// Identity Data Seeder
// TODO: Restore when IdentityDataSeeder is moved to correct location
// builder.Services.AddScoped<Shared.Infrastructure.Platform.Identity.IdentityDataSeeder>();

// Controllers（REST API用）
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddMvc();

// CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiCorsPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Rate Limiting（メモリベース）
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ProductCatalog API",
        Version = "v1",
        Description = "Product Catalog REST API with JWT Bearer authentication"
    });

    // JWT Bearer認証をSwaggerに追加
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Database migration and seed (Skip in Test environment)
if (!app.Environment.IsEnvironment("Test"))
{
    using (var scope = app.Services.CreateScope())
    {
        // Platform migrations (Identity, RefreshToken, Outbox, AuditLog)
        var platformContext = scope.ServiceProvider.GetRequiredService<Shared.Infrastructure.Platform.Persistence.PlatformDbContext>();
        await platformContext.Database.MigrateAsync();

        // ProductCatalog migrations (Product entities)
        var productCatalogContext = scope.ServiceProvider.GetRequiredService<ProductCatalogDbContext>();
        await productCatalogContext.Database.MigrateAsync();

        var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        // Seed sample data if empty
        if (!productCatalogContext.Products.Any())
    {
        var product1 = Product.Create(
            "ノートパソコン",
            "高性能なビジネス用ノートパソコン",
            new Money(150000, "JPY"),
            10);

        var product2 = Product.Create(
            "ワイヤレスマウス",
            "Bluetoothワイヤレスマウス",
            new Money(3500, "JPY"),
            50);

        var product3 = Product.Create(
            "USBキーボード",
            "メカニカルキーボード",
            new Money(12000, "JPY"),
            20);

        await repository.SaveAsync(product1);
        await repository.SaveAsync(product2);
        await repository.SaveAsync(product3);
    }

        // Seed Identity data (Roles and Users)
        // TODO: Restore when IdentityDataSeeder is moved to correct location
        // var identitySeeder = scope.ServiceProvider.GetRequiredService<Shared.Infrastructure.Platform.Identity.IdentityDataSeeder>();
        // await identitySeeder.SeedAsync();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Swagger UI（開発環境のみ）
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductCatalog API v1");
        options.RoutePrefix = "swagger"; // https://localhost:5001/swagger
    });
}

app.UseHttpsRedirection();

// Rate Limiting（レート制限を最初に適用）
app.UseIpRateLimiting();

// CORS（クロスオリジンリクエスト許可）
app.UseCors("ApiCorsPolicy");

// Correlation ID Middleware (最初に実行)
app.UseMiddleware<ProductCatalog.Host.Middleware.CorrelationIdMiddleware>();

// Global Exception Handler (グローバルエラーハンドリング)
app.UseMiddleware<ProductCatalog.Host.Middleware.GlobalExceptionHandlerMiddleware>();

// Serilog Request Logging (HTTPリクエスト/レスポンスのログ記録)
app.UseSerilogRequestLogging();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseStaticFiles();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(Program.FeatureUIAssemblies);

// SignalR Hub エンドポイント
app.MapHub<ProductCatalog.Host.Hubs.ProductHub>("/hubs/products");

// REST API Controllers エンドポイント
app.MapControllers();

// === E2E Debug: Endpoint Discovery (AFTER mapping) ===
foreach (var ds in app.Services.GetServices<Microsoft.AspNetCore.Routing.EndpointDataSource>())
{
    foreach (var e in ds.Endpoints.OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>())
    {
        app.Logger.LogInformation("[ENDPOINT] Pattern: {Pattern} => DisplayName: {DisplayName}",
            e.RoutePattern.RawText ?? "(null)", e.DisplayName);
    }
}
// === End E2E Debug ===

app.Run();

// E2Eテストからアクセスできるようにするため、Programクラスを公開
public partial class Program
{
    // E2Eテスト用: WebApplicationBuilderを作成するヘルパーメソッド
    public static WebApplicationBuilder CreateTestBuilder(string[] args)
    {
        return WebApplication.CreateBuilder(args);
    }

    /// <summary>
    /// Feature UI assemblies for Blazor route discovery.
    ///
    /// CRITICAL: Both MapRazorComponents (server-side) and Router component (client-side)
    /// need this same assembly list to discover @page routes from Feature UI projects.
    ///
    /// Without this:
    /// - MapRazorComponents: Returns 404 for direct URL access (e.g., browser refresh)
    /// - Router: Can't resolve routes during in-app navigation
    ///
    /// VSA Pattern: Each Feature has its own UI assembly with Razor components.
    /// This array explicitly loads all Feature UI assemblies for route discovery.
    /// </summary>
    public static readonly System.Reflection.Assembly[] FeatureUIAssemblies = new[]
    {
        "GetProducts.UI",
        "GetProductById.UI",
        "UpdateProduct.UI",
        "CreateProduct.UI",
        "DeleteProduct.UI",
        "SearchProducts.UI",
        "BulkDeleteProducts.UI",
        "BulkUpdateProductPrices.UI",
        "ExportProductsToCsv.UI",
        "ImportProductsFromCsv.UI"
    }
    .Select(name =>
    {
        try { return System.Reflection.Assembly.Load(name); }
        catch { return null; }
    })
    .Where(a => a != null)
    .ToArray()!;
}
