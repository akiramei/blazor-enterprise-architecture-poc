using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
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
    private readonly TestConfiguration.TestCredentials _testCredentials;

    public ErrorLoggingTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // テスト認証情報を取得（環境変数または設定ファイルから）
        _testCredentials = TestConfiguration.GetTestCredentials();
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

        // Log response details
        _output.WriteLine($"Status Code: {response.StatusCode}");
        _output.WriteLine($"Headers: {response.Headers}");

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Body: {content}");
    }

    /// <summary>
    /// 認証トークンを取得（設定ファイルまたは環境変数から認証情報を読み込む）
    /// </summary>
    private async Task<string> GetAuthTokenAsync()
    {
        var loginRequest = new
        {
            email = _testCredentials.AdminEmail,
            password = _testCredentials.AdminPassword
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Login failed for email: {_testCredentials.AdminEmail}");
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Error: {errorContent}");
            response.EnsureSuccessStatusCode();
        }

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
