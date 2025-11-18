using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductCatalog.Shared.Application.DTOs;
using ProductCatalog.Shared.Infrastructure.Persistence;
using Shared.Application.Common;
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
/// - CustomWebApplicationFactory を使用した完全な統合テスト
/// - SQLite In-Memoryデータベースを使用（高速・独立性）
/// - Outboxメッセージの永続化確認（トランザクション保証）
/// </summary>
public class ProductCatalogIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly TestConfiguration.TestCredentials _testCredentials;

    public ProductCatalogIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false // Disable auto redirect to test HTTP status codes properly
        });

        // テスト認証情報を取得（環境変数または設定ファイルから）
        _testCredentials = TestConfiguration.GetTestCredentials();
    }

    public async Task InitializeAsync()
    {
        // データベーススキーマとシードデータを初期化
        await _factory.InitializeDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    #region Authentication Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwtToken()
    {
        // Arrange
        var loginRequest = new
        {
            email = _testCredentials.AdminEmail,
            password = _testCredentials.AdminPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        result.Should().NotBeNull();

        // 新しいLoginResponse形式: accessTokenが直接ルートにある
        var token = result!.RootElement.GetProperty("accessToken").GetString();
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            email = "invalid@example.com",
            password = "WrongPass123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert - レスポンスボディを出力して詳細確認
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected 401 Unauthorized but got {response.StatusCode}. Response: {errorContent}");
        }
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
            initialStock = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/products", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // APIは直接GUIDを返す（Envelopeなし）
        var productId = await response.Content.ReadFromJsonAsync<Guid>();
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

        // APIは直接ProductDto[]を返す（Envelopeなし）
        var products = await response.Content.ReadFromJsonAsync<List<ProductCatalog.Shared.Application.DTOs.ProductDto>>();

        products.Should().NotBeNull().And.NotBeEmpty();
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

        // Assert - まずレスポンス内容を確認
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected 200 OK but got {response.StatusCode}. Response: {errorContent}");
        }
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // APIは直接ProductDetailDtoを返す（Envelopeなし）
        var product = await response.Content.ReadFromJsonAsync<ProductCatalog.Shared.Application.DTOs.ProductDetailDto>();

        product.Should().NotBeNull();
        product!.Id.Should().Be(productId);
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

        var existingResponse = await _client.GetAsync($"/api/v1/products/{productId}");
        existingResponse.EnsureSuccessStatusCode();
        var existingProduct = await existingResponse.Content.ReadFromJsonAsync<ProductCatalog.Shared.Application.DTOs.ProductDetailDto>();
        existingProduct.Should().NotBeNull();

        var updateRequest = new
        {
            name = "Updated Product",
            description = "Updated Description",
            price = 2000,
            currency = "JPY",
            stock = 20,
            version = existingProduct!.Version
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/products/{productId}", updateRequest);

        // Assert - レスポンスボディを出力して詳細確認
        if (response.StatusCode != HttpStatusCode.NoContent)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected 204 NoContent but got {response.StatusCode}. Response: {errorContent}");
        }
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify product was updated
        var getResponse = await _client.GetAsync($"/api/v1/products/{productId}");
        var product = await getResponse.Content.ReadFromJsonAsync<ProductCatalog.Shared.Application.DTOs.ProductDetailDto>();

        product.Should().NotBeNull();
        product!.Name.Should().Be("Updated Product");

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
        var productId = await SeedTestProductAsync(initialStock: 0);
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

        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();

        pagedResult.Should().NotBeNull();
        pagedResult!.Items.Should().NotBeNull().And.Contain(p =>
            p.Name.Contains("Laptop", StringComparison.OrdinalIgnoreCase));
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
            initialStock = 5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/products", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var scope = _factory.Services.CreateAsyncScope();
        var productDbContext = scope.ServiceProvider.GetRequiredService<ProductCatalogDbContext>();

        // Both product and outbox message should exist (atomicity)
        var products = await productDbContext.Products.ToListAsync();
        var product = products.FirstOrDefault(p => p.Name == "Atomicity Test Product");
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
        var loginRequest = new
        {
            email = _testCredentials.AdminEmail,
            password = _testCredentials.AdminPassword
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        // 新しいLoginResponse形式: accessTokenが直接ルートにある
        return result!.RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task<Guid> SeedTestProductAsync(
        string name = "Test Product",
        string description = "Test Description",
        int initialStock = 10)
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
            initialStock
        };

        var response = await _client.PostAsJsonAsync("/api/v1/products", createRequest);
        response.EnsureSuccessStatusCode();

        // APIは直接GUIDを返す（Envelopeなし）
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    #endregion
}
