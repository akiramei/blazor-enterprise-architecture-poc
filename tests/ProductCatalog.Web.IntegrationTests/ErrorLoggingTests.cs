using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ProductCatalog.Shared.Application.DTOs;
using Xunit;
using Xunit.Abstractions;

namespace ProductCatalog.Web.IntegrationTests;

/// <summary>
/// エラーログを確認するためのテスト
/// </summary>
public class ErrorLoggingTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public ErrorLoggingTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetProducts_LogErrorDetails()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // 先にテストデータを作成
        await SeedTestProductAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/products");

        // Log response details
        _output.WriteLine($"Status Code: {response.StatusCode}");
        _output.WriteLine($"Headers: {response.Headers}");

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Body: {content}");

        // Assert - とりあえずステータスコードを確認
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "500エラーではないはず");
    }

    [Fact]
    public async Task GetProductById_LogErrorDetails()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productId = await SeedTestProductAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/products/{productId}");

        // Log response details for debugging
        _output.WriteLine($"Status Code: {response.StatusCode}");
        _output.WriteLine($"Headers: {response.Headers}");

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Body: {content}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "the product should be retrieved successfully");

        content.Should().NotBeNullOrEmpty(
            "the response body should contain product data");

        var product = JsonSerializer.Deserialize<ProductDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        product.Should().NotBeNull(
            "the response should deserialize to a ProductDto");
        product!.Id.Should().Be(productId,
            "the returned product ID should match the seeded product ID");
        product.Name.Should().Be("Test Product",
            "the product name should match the seeded value");
        product.Description.Should().Be("Test Description",
            "the product description should match the seeded value");
        product.Price.Should().Be(1000,
            "the product price should match the seeded value");
        product.Currency.Should().Be("JPY",
            "the product currency should match the seeded value");
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var loginRequest = new
        {
            email = "admin@example.com",
            password = "Admin@123"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return result!.RootElement.GetProperty("accessToken").GetString()!;
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
            initialStock = 10
        };

        var response = await _client.PostAsJsonAsync("/api/v1/products", createRequest);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"CreateProduct failed: {response.StatusCode}");
            _output.WriteLine($"Error: {errorContent}");
            response.EnsureSuccessStatusCode();
        }

        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
