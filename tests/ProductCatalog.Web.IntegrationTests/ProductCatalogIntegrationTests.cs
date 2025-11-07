using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using ProductCatalog.Shared.Infrastructure.Persistence;
using Shared.Infrastructure.Platform;

namespace ProductCatalog.Web.IntegrationTests;

/// <summary>
/// ProductCatalog Integration Tests
///
/// VSAカタログとしての実装検証:
/// - Product CRUD操作の統合テスト
/// - トランザクショナルOutboxパターンの検証
/// - REST API/Blazorの代表的フローの検証
///
/// テスト方針:
/// - WebApplicationFactory を使用した完全な統合テスト
/// - InMemoryデータベースを使用（高速・独立性）
/// - Outboxメッセージの永続化確認（トランザクション保証）
/// </summary>
public class ProductCatalogIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProductCatalogIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");

            builder.ConfigureServices(services =>
            {
                // テスト用にInMemoryデータベースを使用（Program.csで既に設定済み）
                // ここでは追加設定は不要
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false // Disable auto redirect to test HTTP status codes properly
        });
    }

    #region Authentication Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwtToken()
    {
        // Arrange
        var loginRequest = new
        {
            email = "admin@example.com",
            password = "Admin@123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        result.Should().NotBeNull();

        var token = result!.RootElement.GetProperty("data").GetProperty("token").GetString();
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            email = "invalid@example.com",
            password = "wrong"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Product CRUD Tests

    [Fact]
    public async Task CreateProduct_ValidRequest_CreatesProductAndOutboxMessage()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createRequest = new
        {
            name = "Test Product",
            description = "Test Description",
            price = 1000,
            currency = "JPY",
            stockQuantity = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/products", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var productId = result!.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        productId.Should().NotBeEmpty();

        // Verify Outbox message was created (Transactional Outbox Pattern)
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProductCatalogDbContext>();

        var outboxMessages = await dbContext.OutboxMessages
            .Where(m => m.Type == "ProductCreatedDomainEvent")
            .ToListAsync();

        outboxMessages.Should().NotBeEmpty("Outbox message should be created in same transaction");
        outboxMessages.Should().Contain(m => m.Content.Contains(productId.ToString()));
    }

    [Fact]
    public async Task GetProducts_ReturnsProductList()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await SeedTestProductAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var products = result!.RootElement.GetProperty("data").EnumerateArray().ToList();

        products.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetProductById_ExistingProduct_ReturnsProduct()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await SeedTestProductAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var id = result!.RootElement.GetProperty("data").GetProperty("id").GetGuid();

        id.Should().Be(productId);
    }

    [Fact]
    public async Task GetProductById_NonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/products/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProduct_ValidRequest_UpdatesProductAndCreatesOutboxMessage()
    {
        // Arrange
        var productId = await SeedTestProductAsync();
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new
        {
            name = "Updated Product",
            description = "Updated Description",
            price = 2000,
            currency = "JPY",
            stockQuantity = 20
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/products/{productId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify product was updated
        var getResponse = await _client.GetAsync($"/api/v1/products/{productId}");
        var result = await getResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var name = result!.RootElement.GetProperty("data").GetProperty("name").GetString();

        name.Should().Be("Updated Product");

        // Verify Outbox message was created
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProductCatalogDbContext>();

        var outboxMessages = await dbContext.OutboxMessages
            .Where(m => m.Type == "ProductUpdatedDomainEvent")
            .ToListAsync();

        outboxMessages.Should().NotBeEmpty("Outbox message should be created when product is updated");
    }

    [Fact]
    public async Task DeleteProduct_ExistingProduct_DeletesProductAndCreatesOutboxMessage()
    {
        // Arrange
        var productId = await SeedTestProductAsync();
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/v1/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify product is soft-deleted
        var getResponse = await _client.GetAsync($"/api/v1/products/{productId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify Outbox message was created
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProductCatalogDbContext>();

        var outboxMessages = await dbContext.OutboxMessages
            .Where(m => m.Type == "ProductDeletedDomainEvent")
            .ToListAsync();

        outboxMessages.Should().NotBeEmpty("Outbox message should be created when product is deleted");
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchProducts_WithKeyword_ReturnsMatchingProducts()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await SeedTestProductAsync("Laptop Pro", "High-end laptop");
        await SeedTestProductAsync("Desktop PC", "Gaming desktop");

        // Act
        var response = await _client.GetAsync("/api/v1/products/search?keyword=laptop");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var products = result!.RootElement.GetProperty("data").EnumerateArray().ToList();

        products.Should().Contain(p =>
            p.GetProperty("name").GetString()!.Contains("Laptop", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Transactional Outbox Pattern Tests

    [Fact]
    public async Task TransactionalOutbox_ProductCreation_EnsuresAtomicity()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createRequest = new
        {
            name = "Atomicity Test Product",
            description = "Testing transactional outbox",
            price = 5000,
            currency = "JPY",
            stockQuantity = 5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/products", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var scope = _factory.Services.CreateAsyncScope();
        var productDbContext = scope.ServiceProvider.GetRequiredService<ProductCatalogDbContext>();

        // Both product and outbox message should exist (atomicity)
        var product = await productDbContext.Products
            .FirstOrDefaultAsync(p => p.Name == "Atomicity Test Product");
        product.Should().NotBeNull("Product should be persisted");

        var outboxMessage = await productDbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.Content.Contains(product!.Id.ToString()));
        outboxMessage.Should().NotBeNull("Outbox message should be persisted in same transaction");

        outboxMessage!.ProcessedOnUtc.Should().BeNull("Outbox message should be unprocessed initially");
    }

    [Fact]
    public async Task OutboxReader_CanReadUnprocessedMessages()
    {
        // Arrange - Create a product which generates an outbox message
        var productId = await SeedTestProductAsync("Outbox Test Product", "Testing outbox reader");

        // Act - Verify unprocessed messages exist in database
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProductCatalogDbContext>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .ToListAsync();

        // Assert
        messages.Should().NotBeEmpty("Should have unprocessed outbox messages");
        messages.Should().Contain(m => m.Type == "ProductCreatedDomainEvent");
    }

    #endregion

    #region Helper Methods

    private async Task<string> GetAuthTokenAsync()
    {
        // First, ensure admin user exists
        await using var scope = _factory.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>();
        await seeder.SeedAsync();

        var loginRequest = new
        {
            email = "admin@example.com",
            password = "Admin@123"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return result!.RootElement.GetProperty("data").GetProperty("token").GetString()!;
    }

    private async Task<Guid> SeedTestProductAsync(string name = "Test Product", string description = "Test Description")
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createRequest = new
        {
            name,
            description,
            price = 1000,
            currency = "JPY",
            stockQuantity = 10
        };

        var response = await _client.PostAsJsonAsync("/api/v1/products", createRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return result!.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    #endregion
}
