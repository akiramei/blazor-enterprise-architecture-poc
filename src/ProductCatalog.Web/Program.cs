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
using ProductCatalog.Application.Common.Behaviors;
using ProductCatalog.Application.Common.Interfaces;
using ProductCatalog.Domain.Identity;
using ProductCatalog.Domain.Products;
using ProductCatalog.Infrastructure.Authentication;
using ProductCatalog.Infrastructure.Behaviors;
using ProductCatalog.Infrastructure.Idempotency;
using ProductCatalog.Infrastructure.Persistence;
using ProductCatalog.Infrastructure.Persistence.Repositories;
using ProductCatalog.Infrastructure.Services;
using ProductCatalog.Web.Components;
using ProductCatalog.Web.Features.Products.Actions;
using ProductCatalog.Web.Features.Products.Store;
using Serilog;

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
builder.Services.AddSingleton<ProductCatalog.Infrastructure.Metrics.ApplicationMetrics>();
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        // ProductCatalogアプリケーションのメトリクスを収集
        metrics.AddMeter("ProductCatalog");

        // エクスポーター設定（開発環境）
        // 本番環境では Prometheus や Azure Monitor等を使用
        // .AddPrometheusExporter();
        // .AddAzureMonitorMetricExporter();
    });

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ProductCatalog.Application.Features.Products.GetProducts.GetProductsHandler).Assembly);
});

// FluentValidation（自動検出）
builder.Services.AddValidatorsFromAssembly(typeof(ProductCatalog.Application.Features.Products.GetProducts.GetProductsHandler).Assembly);

// Pipeline Behaviors（登録順序が重要！）
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Infrastructure.Behaviors.MetricsBehavior<,>));        // 0. Metrics - 全体の実行時間を計測
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Application.Common.Behaviors.LoggingBehavior<,>));        // 1. Logging
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Application.Common.Behaviors.ValidationBehavior<,>));     // 2. Validation
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Infrastructure.Behaviors.AuthorizationBehavior<,>));  // 3. Authorization
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Infrastructure.Behaviors.IdempotencyBehavior<,>));    // 4. Idempotency (Command)
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Infrastructure.Behaviors.CachingBehavior<,>));        // 5. Caching (Query)
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Infrastructure.Behaviors.AuditLogBehavior<,>));       // 6. AuditLog (Command) - 監査ログ記録
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Infrastructure.Behaviors.TransactionBehavior<,>));    // 7. Transaction (Command)

// Authorization
builder.Services.AddAuthorization();

// Memory Cache
builder.Services.AddMemoryCache();

// Current User Service
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// App Context (統合コンテキスト - ユーザー情報、リクエスト情報、環境情報を一元管理)
builder.Services.AddScoped<IAppContext, ProductCatalog.Infrastructure.Services.AppContext>();

// Product Notification Service (SignalR)
builder.Services.AddScoped<IProductNotificationService, ProductCatalog.Web.Services.ProductNotificationService>();

// Correlation ID Accessor (Distributed Tracing)
builder.Services.AddScoped<ICorrelationIdAccessor, ProductCatalog.Infrastructure.Services.CorrelationIdAccessor>();

// Idempotency Store
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();

// DbContext (PostgreSQL for production, InMemory for Test)
if (builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("TestDatabase"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// ASP.NET Core Identity（本番用認証・認可）
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
builder.Services.AddScoped<IProductReadRepository, ProductCatalog.Infrastructure.Persistence.Repositories.DapperProductReadRepository>();
// Audit Log Repository（監査ログ）
builder.Services.AddScoped<IAuditLogRepository, ProductCatalog.Infrastructure.Persistence.Repositories.AuditLogRepository>();

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

// Outbox Background Service (Outbox Patternによる統合イベント配信)
builder.Services.AddHostedService<ProductCatalog.Infrastructure.Outbox.OutboxBackgroundService>();

// Identity Data Seeder
builder.Services.AddScoped<ProductCatalog.Infrastructure.Identity.IdentityDataSeeder>();

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
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        // Apply pending migrations
        await context.Database.MigrateAsync();

    if (!context.Products.Any())
    {
        var product1 = ProductCatalog.Domain.Products.Product.Create(
            "ノートパソコン",
            "高性能なビジネス用ノートパソコン",
            new ProductCatalog.Domain.Products.Money(150000, "JPY"),
            10);

        var product2 = ProductCatalog.Domain.Products.Product.Create(
            "ワイヤレスマウス",
            "Bluetoothワイヤレスマウス",
            new ProductCatalog.Domain.Products.Money(3500, "JPY"),
            50);

        var product3 = ProductCatalog.Domain.Products.Product.Create(
            "USBキーボード",
            "メカニカルキーボード",
            new ProductCatalog.Domain.Products.Money(12000, "JPY"),
            20);

        await repository.SaveAsync(product1);
        await repository.SaveAsync(product2);
        await repository.SaveAsync(product3);
    }

        // Seed Identity data (Roles and Users)
        var identitySeeder = scope.ServiceProvider.GetRequiredService<ProductCatalog.Infrastructure.Identity.IdentityDataSeeder>();
        await identitySeeder.SeedAsync();
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
app.UseMiddleware<ProductCatalog.Web.Middleware.CorrelationIdMiddleware>();

// Global Exception Handler (グローバルエラーハンドリング)
app.UseMiddleware<ProductCatalog.Web.Middleware.GlobalExceptionHandlerMiddleware>();

// Serilog Request Logging (HTTPリクエスト/レスポンスのログ記録)
app.UseSerilogRequestLogging();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// SignalR Hub エンドポイント
app.MapHub<ProductCatalog.Web.Hubs.ProductHub>("/hubs/products");

// REST API Controllers エンドポイント
app.MapControllers();

app.Run();

// E2Eテストからアクセスできるようにするため、Programクラスを公開
public partial class Program
{
    // E2Eテスト用: WebApplicationBuilderを作成するヘルパーメソッド
    public static WebApplicationBuilder CreateTestBuilder(string[] args)
    {
        return WebApplication.CreateBuilder(args);
    }
}
