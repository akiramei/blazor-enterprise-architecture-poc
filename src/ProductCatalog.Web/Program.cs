using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProductCatalog.Application.Common.Behaviors;
using ProductCatalog.Application.Common.Interfaces;
using ProductCatalog.Domain.Identity;
using ProductCatalog.Domain.Products;
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

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ProductCatalog.Application.Features.Products.GetProducts.GetProductsHandler).Assembly);
});

// FluentValidation（自動検出）
builder.Services.AddValidatorsFromAssembly(typeof(ProductCatalog.Application.Features.Products.GetProducts.GetProductsHandler).Assembly);

// Pipeline Behaviors（登録順序が重要！）
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Application.Common.Behaviors.LoggingBehavior<,>));        // 1. Logging
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Application.Common.Behaviors.ValidationBehavior<,>));     // 2. Validation
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Infrastructure.Behaviors.AuthorizationBehavior<,>));  // 3. Authorization
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Infrastructure.Behaviors.IdempotencyBehavior<,>));    // 4. Idempotency (Command)
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Infrastructure.Behaviors.CachingBehavior<,>));        // 5. Caching (Query)
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ProductCatalog.Infrastructure.Behaviors.TransactionBehavior<,>));    // 6. Transaction (Command)

// Authorization
builder.Services.AddAuthorization();

// Memory Cache
builder.Services.AddMemoryCache();

// Current User Service
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Product Notification Service (SignalR)
builder.Services.AddScoped<IProductNotificationService, ProductCatalog.Web.Services.ProductNotificationService>();

// Correlation ID Accessor (Distributed Tracing)
builder.Services.AddScoped<ICorrelationIdAccessor, ProductCatalog.Infrastructure.Services.CorrelationIdAccessor>();

// Idempotency Store
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();

// DbContext (PostgreSQL for production)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Repositories
builder.Services.AddScoped<IProductRepository, EfProductRepository>();
// Dapper を使用した高速読み取りリポジトリ（本番環境で効果を発揮）
builder.Services.AddScoped<IProductReadRepository, ProductCatalog.Infrastructure.Persistence.Repositories.DapperProductReadRepository>();

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

var app = builder.Build();

// Database migration and seed
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

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

app.Run();
