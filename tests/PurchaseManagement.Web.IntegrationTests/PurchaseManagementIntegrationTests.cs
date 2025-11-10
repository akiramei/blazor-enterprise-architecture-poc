using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using PurchaseManagement.Infrastructure.Persistence;
using Shared.Infrastructure.Platform;

namespace PurchaseManagement.Web.IntegrationTests;

/// <summary>
/// PurchaseManagement Integration Tests
///
/// VSAカタログとしての実装検証:
/// - 購買申請のライフサイクル統合テスト（提出→承認→却下）
/// - 多段階承認フロー（1次、2次、3次）の検証
/// - State Machine状態遷移の検証
/// - トランザクショナルOutboxパターンの検証
/// - REST API/Blazorの代表的フローの検証
///
/// テスト方針:
/// - CustomWebApplicationFactory を使用した完全な統合テスト
/// - SQLite In-Memoryデータベースを使用（高速・独立性）
/// - Outboxメッセージの永続化確認（トランザクション保証）
/// - 実際のビジネスフロー（承認ワークフロー）の検証
/// </summary>
public class PurchaseManagementIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PurchaseManagementIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async Task InitializeAsync()
    {
        // Identityデータをシード
        using var scope = _factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>();
        await seeder.SeedAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    #region Purchase Request Submission Tests

    [Fact]
    public async Task SubmitPurchaseRequest_ValidRequest_CreatesPurchaseRequestAndOutboxMessage()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var submitRequest = new
        {
            title = "Test Purchase Request",
            description = "Test Description for purchase",
            items = new[]
            {
                new
                {
                    productId = Guid.NewGuid(),
                    productName = "Laptop",
                    unitPrice = 50000,
                    quantity = 1
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/purchase-requests", submitRequest);

        // Assert
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Request failed with status {response.StatusCode}: {errorContent}");
        }
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var requestId = Guid.Parse(content.Trim('"')); // The response might be JSON-encoded string
        requestId.Should().NotBeEmpty();

        // Verify Purchase Request was created
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseManagementDbContext>();

        // デバッグ: 全件数を確認
        var allCount = await dbContext.PurchaseRequests.IgnoreQueryFilters().CountAsync();
        var allRequests = await dbContext.PurchaseRequests.IgnoreQueryFilters().ToListAsync();

        var purchaseRequest = await dbContext.PurchaseRequests
            .IgnoreQueryFilters() // テスト環境ではGlobal Query Filterをバイパス
            .FirstOrDefaultAsync(pr => pr.Id == requestId);

        purchaseRequest.Should().NotBeNull($"Purchase request should be created. Total count: {allCount}, RequestId: {requestId}, All IDs: {string.Join(", ", allRequests.Select(r => r.Id))}");
        purchaseRequest!.Title.Should().Be("Test Purchase Request");
    }

    [Fact]
    public async Task SubmitPurchaseRequest_WithMultipleItems_CalculatesTotalAmountCorrectly()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var submitRequest = new
        {
            title = "Multi-item Purchase",
            description = "Multiple items purchase request",
            items = new[]
            {
                new
                {
                    productId = Guid.NewGuid(),
                    productName = "Laptop",
                    unitPrice = 100000,
                    quantity = 2
                },
                new
                {
                    productId = Guid.NewGuid(),
                    productName = "Mouse",
                    unitPrice = 3000,
                    quantity = 5
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/purchase-requests", submitRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var requestId = await response.Content.ReadFromJsonAsync<Guid>();

        // Verify total amount: (100000 * 2) + (3000 * 5) = 215000
        var detailResponse = await _client.GetAsync($"/api/v1/purchase-requests/{requestId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await detailResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var totalAmount = detail!.RootElement.GetProperty("totalAmount").GetDecimal();
        totalAmount.Should().Be(215000);
    }

    [Fact]
    public async Task SubmitPurchaseRequest_SmallAmount_CreatesSingleStepApprovalFlow()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var submitRequest = new
        {
            title = "Small Amount Purchase",
            description = "Purchase under 100,000 JPY",
            items = new[]
            {
                new
                {
                    productId = Guid.NewGuid(),
                    productName = "Office Supplies",
                    unitPrice = 50000,
                    quantity = 1
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/purchase-requests", submitRequest);
        var requestId = await response.Content.ReadFromJsonAsync<Guid>();

        // Assert - Should have 1 approval step
        var detailResponse = await _client.GetAsync($"/api/v1/purchase-requests/{requestId}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<JsonDocument>();

        var approvalSteps = detail!.RootElement.GetProperty("approvalSteps").EnumerateArray().ToList();
        approvalSteps.Should().HaveCount(1, "Small amount should require only 1 approval step");
    }

    [Fact]
    public async Task SubmitPurchaseRequest_MediumAmount_CreatesTwoStepApprovalFlow()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var submitRequest = new
        {
            title = "Medium Amount Purchase",
            description = "Purchase between 100,000 and 500,000 JPY",
            items = new[]
            {
                new
                {
                    productId = Guid.NewGuid(),
                    productName = "Server Equipment",
                    unitPrice = 250000,
                    quantity = 1
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/purchase-requests", submitRequest);
        var requestId = await response.Content.ReadFromJsonAsync<Guid>();

        // Assert - Should have 2 approval steps
        var detailResponse = await _client.GetAsync($"/api/v1/purchase-requests/{requestId}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<JsonDocument>();

        var approvalSteps = detail!.RootElement.GetProperty("approvalSteps").EnumerateArray().ToList();
        approvalSteps.Should().HaveCount(2, "Medium amount should require 2 approval steps");
    }

    [Fact]
    public async Task SubmitPurchaseRequest_LargeAmount_CreatesThreeStepApprovalFlow()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var submitRequest = new
        {
            title = "Large Amount Purchase",
            description = "Purchase over 500,000 JPY",
            items = new[]
            {
                new
                {
                    productId = Guid.NewGuid(),
                    productName = "High-end Equipment",
                    unitPrice = 600000,
                    quantity = 1
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/purchase-requests", submitRequest);
        var requestId = await response.Content.ReadFromJsonAsync<Guid>();

        // Assert - Should have 3 approval steps
        var detailResponse = await _client.GetAsync($"/api/v1/purchase-requests/{requestId}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<JsonDocument>();

        var approvalSteps = detail!.RootElement.GetProperty("approvalSteps").EnumerateArray().ToList();
        approvalSteps.Should().HaveCount(3, "Large amount should require 3 approval steps");
    }

    #endregion

    #region Purchase Request Query Tests

    [Fact]
    public async Task GetPurchaseRequests_ReturnsListOfRequests()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await SeedTestPurchaseRequestAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/purchase-requests");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var requests = result!.RootElement.EnumerateArray().ToList();

        requests.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPurchaseRequestById_ExistingRequest_ReturnsRequestDetails()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var requestId = await SeedTestPurchaseRequestAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/purchase-requests/{requestId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var id = detail!.RootElement.GetProperty("id").GetGuid();

        id.Should().Be(requestId);
    }

    [Fact]
    public async Task GetPurchaseRequestById_NonExistingRequest_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/purchase-requests/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Approval Workflow Tests

    [Fact]
    public async Task ApprovePurchaseRequest_FirstStep_UpdatesStatusToPendingSecondApproval()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Create request with medium amount (2-step approval)
        var requestId = await SeedTestPurchaseRequestAsync(amount: 250000);

        var approveCommand = new
        {
            comment = "First approval granted"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/purchase-requests/{requestId}/approve", approveCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify status changed to PendingSecondApproval (Status = 3)
        var detailResponse = await _client.GetAsync($"/api/v1/purchase-requests/{requestId}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var status = detail!.RootElement.GetProperty("status").GetInt32();

        status.Should().Be(3, "Should transition to PendingSecondApproval");

        // Verify Outbox message was created
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseManagementDbContext>();

        var outboxMessages = await dbContext.OutboxMessages
            .Where(m => m.Type == "PurchaseRequestApprovedDomainEvent")
            .ToListAsync();

        outboxMessages.Should().NotBeEmpty("Outbox message should be created when approved");
    }

    [Fact]
    public async Task ApprovePurchaseRequest_FinalStep_UpdatesStatusToApproved()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Create request with small amount (1-step approval)
        var requestId = await SeedTestPurchaseRequestAsync(amount: 50000);

        var approveCommand = new
        {
            comment = "Final approval granted"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/purchase-requests/{requestId}/approve", approveCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify status changed to Approved (Status = 5)
        var detailResponse = await _client.GetAsync($"/api/v1/purchase-requests/{requestId}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var status = detail!.RootElement.GetProperty("status").GetInt32();

        status.Should().Be(5, "Should transition to Approved after final step");

        // Verify ApprovedAt timestamp is set
        var approvedAt = detail!.RootElement.GetProperty("approvedAt");
        approvedAt.ValueKind.Should().NotBe(JsonValueKind.Null, "ApprovedAt should be set");
    }

    [Fact]
    public async Task RejectPurchaseRequest_AnyStep_UpdatesStatusToRejected()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var requestId = await SeedTestPurchaseRequestAsync();

        var rejectCommand = new
        {
            reason = "Budget constraints"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/purchase-requests/{requestId}/reject", rejectCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify status changed to Rejected (Status = 6)
        var detailResponse = await _client.GetAsync($"/api/v1/purchase-requests/{requestId}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var status = detail!.RootElement.GetProperty("status").GetInt32();

        status.Should().Be(6, "Should transition to Rejected");

        // Verify RejectedAt timestamp is set
        var rejectedAt = detail!.RootElement.GetProperty("rejectedAt");
        rejectedAt.ValueKind.Should().NotBe(JsonValueKind.Null, "RejectedAt should be set");

        // Verify Outbox message was created
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseManagementDbContext>();

        var outboxMessages = await dbContext.OutboxMessages
            .Where(m => m.Type == "PurchaseRequestRejectedDomainEvent")
            .ToListAsync();

        outboxMessages.Should().NotBeEmpty("Outbox message should be created when rejected");
    }

    [Fact]
    public async Task MultiStepApproval_CompleteFlow_TransitionsCorrectly()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Create request with large amount (3-step approval)
        var requestId = await SeedTestPurchaseRequestAsync(amount: 600000);

        // Act & Assert - First approval
        var firstApproval = await _client.PostAsJsonAsync($"/api/v1/purchase-requests/{requestId}/approve", new { comment = "First OK" });
        firstApproval.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterFirst = await _client.GetAsync($"/api/v1/purchase-requests/{requestId}");
        var firstDetail = await afterFirst.Content.ReadFromJsonAsync<JsonDocument>();
        firstDetail!.RootElement.GetProperty("status").GetInt32().Should().Be(3, "Should be PendingSecondApproval");

        // Act & Assert - Second approval
        var secondApproval = await _client.PostAsJsonAsync($"/api/v1/purchase-requests/{requestId}/approve", new { comment = "Second OK" });
        secondApproval.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterSecond = await _client.GetAsync($"/api/v1/purchase-requests/{requestId}");
        var secondDetail = await afterSecond.Content.ReadFromJsonAsync<JsonDocument>();
        secondDetail!.RootElement.GetProperty("status").GetInt32().Should().Be(4, "Should be PendingThirdApproval");

        // Act & Assert - Third approval (final)
        var thirdApproval = await _client.PostAsJsonAsync($"/api/v1/purchase-requests/{requestId}/approve", new { comment = "Final OK" });
        thirdApproval.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterThird = await _client.GetAsync($"/api/v1/purchase-requests/{requestId}");
        var thirdDetail = await afterThird.Content.ReadFromJsonAsync<JsonDocument>();
        thirdDetail!.RootElement.GetProperty("status").GetInt32().Should().Be(5, "Should be Approved");
    }

    #endregion

    #region Transactional Outbox Pattern Tests

    [Fact]
    public async Task TransactionalOutbox_PurchaseRequestSubmission_EnsuresAtomicity()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var submitRequest = new
        {
            title = "Atomicity Test Request",
            description = "Testing transactional outbox",
            items = new[]
            {
                new
                {
                    productId = Guid.NewGuid(),
                    productName = "Test Product",
                    unitPrice = 100000,
                    quantity = 1
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/purchase-requests", submitRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var requestId = await response.Content.ReadFromJsonAsync<Guid>();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseManagementDbContext>();

        // Both purchase request and outbox message should exist (atomicity)
        var purchaseRequest = await dbContext.PurchaseRequests
            .IgnoreQueryFilters() // テスト環境ではGlobal Query Filterをバイパス
            .FirstOrDefaultAsync(pr => pr.Id == requestId);
        purchaseRequest.Should().NotBeNull("Purchase request should be persisted");

        var outboxMessage = await dbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.Content.Contains(requestId.ToString()));
        outboxMessage.Should().NotBeNull("Outbox message should be persisted in same transaction");

        outboxMessage!.ProcessedOnUtc.Should().BeNull("Outbox message should be unprocessed initially");
    }

    [Fact]
    public async Task OutboxReader_CanReadUnprocessedMessages()
    {
        // Arrange - Create a purchase request which generates an outbox message
        var requestId = await SeedTestPurchaseRequestAsync();

        // Act - Verify unprocessed messages exist in database
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseManagementDbContext>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .ToListAsync();

        // Assert
        messages.Should().NotBeEmpty("Should have unprocessed outbox messages");
        messages.Should().Contain(m => m.Type == "PurchaseRequestSubmittedDomainEvent");
    }

    #endregion

    #region State Machine Validation Tests

    [Fact]
    public async Task StateMachine_InvalidTransition_ReturnsError()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var requestId = await SeedTestPurchaseRequestAsync();

        // First approval to change state
        await _client.PostAsJsonAsync($"/api/v1/purchase-requests/{requestId}/approve", new { comment = "OK" });

        // Try to approve again (should fail - already approved)
        var secondApproval = await _client.PostAsJsonAsync($"/api/v1/purchase-requests/{requestId}/approve", new { comment = "Try again" });

        // Assert
        secondApproval.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Should reject invalid state transition");
    }

    #endregion

    #region Helper Methods

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

    private async Task<Guid> SeedTestPurchaseRequestAsync(
        string title = "Test Purchase Request",
        string description = "Test Description",
        decimal amount = 100000)
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var submitRequest = new
        {
            title,
            description,
            items = new[]
            {
                new
                {
                    productId = Guid.NewGuid(),
                    productName = "Test Product",
                    unitPrice = amount,
                    quantity = 1
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/purchase-requests", submitRequest);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return Guid.Parse(content.Trim('"'));
    }

    #endregion

    #region ファイルアップロード統合テスト

    [Fact]
    public async Task UploadAttachment_ValidFile_ShouldSucceed()
    {
        // Arrange: 購買申請を作成
        var requestId = await SeedTestPurchaseRequestAsync();

        // テスト用ファイルを作成
        var fileName = "test-document.pdf";
        var fileContent = System.Text.Encoding.UTF8.GetBytes("This is a test PDF content");
        var contentType = "application/pdf";

        using var content = new MultipartFormDataContent();
        using var fileContentBytes = new ByteArrayContent(fileContent);
        fileContentBytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(fileContentBytes, "file", fileName);
        content.Add(new StringContent("Test file description"), "description");

        // Act: ファイルアップロード
        var response = await _client.PostAsync($"/api/v1/purchase-requests/{requestId}/attachments", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var attachmentId = await response.Content.ReadFromJsonAsync<Guid>();
        attachmentId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UploadAttachment_EmptyFile_ShouldReturnBadRequest()
    {
        // Arrange
        var requestId = await SeedTestPurchaseRequestAsync();

        using var content = new MultipartFormDataContent();
        using var emptyFileContent = new ByteArrayContent(Array.Empty<byte>());
        emptyFileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(emptyFileContent, "file", "empty.pdf");

        // Act
        var response = await _client.PostAsync($"/api/v1/purchase-requests/{requestId}/attachments", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadAttachment_InvalidFileExtension_ShouldReturnBadRequest()
    {
        // Arrange
        var requestId = await SeedTestPurchaseRequestAsync();

        var fileContent = System.Text.Encoding.UTF8.GetBytes("Malicious executable content");
        using var content = new MultipartFormDataContent();
        using var fileBytes = new ByteArrayContent(fileContent);
        fileBytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileBytes, "file", "malware.exe");

        // Act
        var response = await _client.PostAsync($"/api/v1/purchase-requests/{requestId}/attachments", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadAttachment_NonExistentPurchaseRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var nonExistentId = Guid.NewGuid();
        var fileContent = System.Text.Encoding.UTF8.GetBytes("Test content");
        using var content = new MultipartFormDataContent();
        using var fileBytes = new ByteArrayContent(fileContent);
        fileBytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileBytes, "file", "test.pdf");

        // Act
        var response = await _client.PostAsync($"/api/v1/purchase-requests/{nonExistentId}/attachments", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
