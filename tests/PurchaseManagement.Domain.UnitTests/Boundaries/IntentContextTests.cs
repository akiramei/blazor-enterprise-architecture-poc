using FluentAssertions;
using Domain.PurchaseManagement.PurchaseRequests;
using Domain.PurchaseManagement.PurchaseRequests.Boundaries;

namespace PurchaseManagement.Domain.UnitTests.Boundaries;

/// <summary>
/// IntentContext（Intent-Command分離パターン）のユニットテスト
/// </summary>
public class IntentContextTests
{
    private readonly ApprovalBoundaryService _sut;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _requesterId = Guid.NewGuid();
    private readonly Guid _approverId = Guid.NewGuid();

    public IntentContextTests()
    {
        // Mock IApprovalCommandFactory（テスト用）
        var mockFactory = new MockApprovalCommandFactory();
        var mockLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ApprovalBoundaryService>.Instance;
        _sut = new ApprovalBoundaryService(mockFactory, mockLogger);
    }

    /// <summary>
    /// テスト用のモックファクトリー
    /// </summary>
    private class MockApprovalCommandFactory : IApprovalCommandFactory
    {
        public object CreateApproveCommand(Guid requestId, string comment, string idempotencyKey)
        {
            return new { RequestId = requestId, Comment = comment, IdempotencyKey = idempotencyKey, Type = "Approve" };
        }

        public object CreateRejectCommand(Guid requestId, string reason, string idempotencyKey)
        {
            return new { RequestId = requestId, Reason = reason, IdempotencyKey = idempotencyKey, Type = "Reject" };
        }
    }

    #region GetIntentContext Tests

    [Fact]
    public void GetIntentContext_1次承認待ちの場合_PerformFirstApprovalを返す()
    {
        // Arrange
        var request = CreatePurchaseRequest();
        SetStatus(request, PurchaseRequestStatus.PendingFirstApproval);
        AddApprovalStep(request, _approverId, "承認者1", 1);

        // Act
        var context = _sut.GetIntentContext(request, _approverId);

        // Assert
        context.HasAvailableIntents.Should().BeTrue();
        context.AvailableIntents.Should().HaveCount(3); // 1次承認 + 差し戻し + 却下

        var approvalIntent = context.AvailableIntents.FirstOrDefault(i => i.Intent == ApprovalIntent.PerformFirstApproval);
        approvalIntent.Should().NotBeNull();
        approvalIntent!.IsEnabled.Should().BeTrue();
        approvalIntent.Metadata.Label.Should().Be("1次承認");
    }

    [Fact]
    public void GetIntentContext_2次承認待ちの場合_PerformSecondApprovalを返す()
    {
        // Arrange
        var request = CreatePurchaseRequest();
        SetStatus(request, PurchaseRequestStatus.PendingSecondApproval);
        AddApprovalStep(request, _approverId, "承認者2", 2);

        // Act
        var context = _sut.GetIntentContext(request, _approverId);

        // Assert
        var approvalIntent = context.AvailableIntents.FirstOrDefault(i => i.Intent == ApprovalIntent.PerformSecondApproval);
        approvalIntent.Should().NotBeNull();
        approvalIntent!.Metadata.Label.Should().Be("2次承認");
    }

    [Fact]
    public void GetIntentContext_最終承認待ちの場合_PerformFinalApprovalを返す()
    {
        // Arrange
        var request = CreatePurchaseRequest();
        SetStatus(request, PurchaseRequestStatus.PendingFinalApproval);
        AddApprovalStep(request, _approverId, "最終承認者", 3);

        // Act
        var context = _sut.GetIntentContext(request, _approverId);

        // Assert
        var approvalIntent = context.AvailableIntents.FirstOrDefault(i => i.Intent == ApprovalIntent.PerformFinalApproval);
        approvalIntent.Should().NotBeNull();
        approvalIntent!.Metadata.Label.Should().Be("最終承認");
        approvalIntent.Metadata.ButtonClass.Should().Be("btn-primary");
    }

    [Fact]
    public void GetIntentContext_承認権限がある場合_差し戻しと却下の両方を含む()
    {
        // Arrange
        var request = CreatePurchaseRequest();
        SetStatus(request, PurchaseRequestStatus.PendingFirstApproval);
        AddApprovalStep(request, _approverId, "承認者1", 1);

        // Act
        var context = _sut.GetIntentContext(request, _approverId);

        // Assert
        var sendBackIntent = context.AvailableIntents.FirstOrDefault(i => i.Intent == ApprovalIntent.SendBackForRevision);
        var rejectIntent = context.AvailableIntents.FirstOrDefault(i => i.Intent == ApprovalIntent.RejectPermanently);

        sendBackIntent.Should().NotBeNull();
        sendBackIntent!.Metadata.Label.Should().Be("差し戻し");
        sendBackIntent.Metadata.ButtonClass.Should().Be("btn-warning");

        rejectIntent.Should().NotBeNull();
        rejectIntent!.Metadata.Label.Should().Be("却下");
        rejectIntent!.Metadata.ButtonClass.Should().Be("btn-danger");
    }

    [Fact]
    public void GetIntentContext_承認権限がない場合_空のIntent一覧を返す()
    {
        // Arrange
        var request = CreatePurchaseRequest();
        SetStatus(request, PurchaseRequestStatus.PendingFirstApproval);
        AddApprovalStep(request, _approverId, "承認者1", 1);

        var otherUserId = Guid.NewGuid();

        // Act
        var context = _sut.GetIntentContext(request, otherUserId);

        // Assert
        context.HasAvailableIntents.Should().BeFalse();
        context.AvailableIntents.Should().BeEmpty();
    }

    [Fact]
    public void GetIntentContext_終端状態の場合_空のIntent一覧を返す()
    {
        // Arrange
        var request = CreatePurchaseRequest();
        SetStatus(request, PurchaseRequestStatus.Approved);

        // Act
        var context = _sut.GetIntentContext(request, _approverId);

        // Assert
        context.HasAvailableIntents.Should().BeFalse();
        context.AvailableIntents.Should().BeEmpty();
    }

    #endregion

    #region CanExecuteIntent Tests

    [Fact]
    public void CanExecuteIntent_利用可能なIntentの場合_trueを返す()
    {
        // Arrange
        var request = CreatePurchaseRequest();
        SetStatus(request, PurchaseRequestStatus.PendingFirstApproval);
        AddApprovalStep(request, _approverId, "承認者1", 1);

        // Act
        var canExecute = _sut.CanExecuteIntent(request, ApprovalIntent.PerformFirstApproval, _approverId);

        // Assert
        canExecute.Should().BeTrue();
    }

    [Fact]
    public void CanExecuteIntent_利用不可能なIntentの場合_falseを返す()
    {
        // Arrange
        var request = CreatePurchaseRequest();
        SetStatus(request, PurchaseRequestStatus.PendingFirstApproval);
        AddApprovalStep(request, _approverId, "承認者1", 1);

        // Act - 2次承認は実行できない
        var canExecute = _sut.CanExecuteIntent(request, ApprovalIntent.PerformSecondApproval, _approverId);

        // Assert
        canExecute.Should().BeFalse();
    }

    #endregion

    #region CreateCommandFromIntent Tests
    // NOTE: CreateCommandFromIntentはリフレクションを使ってApplication層のコマンドを生成するため、
    // ドメイン層のユニットテストではApplication層への参照がないためテストをスキップ
    // 統合テストで検証する

    #endregion

    #region IntentMetadata Tests

    [Fact]
    public void IntentMetadata_FromIntent_全てのIntentで正しいメタデータを返す()
    {
        // Arrange & Act & Assert
        var firstApproval = IntentMetadata.FromIntent(ApprovalIntent.PerformFirstApproval);
        firstApproval.Label.Should().Be("1次承認");
        firstApproval.ButtonClass.Should().Be("btn-success");
        firstApproval.Icon.Should().Be("bi-check-circle");
        firstApproval.RequiresConfirmation.Should().BeTrue();

        var secondApproval = IntentMetadata.FromIntent(ApprovalIntent.PerformSecondApproval);
        secondApproval.Label.Should().Be("2次承認");

        var finalApproval = IntentMetadata.FromIntent(ApprovalIntent.PerformFinalApproval);
        finalApproval.Label.Should().Be("最終承認");
        finalApproval.ButtonClass.Should().Be("btn-primary");

        var sendBack = IntentMetadata.FromIntent(ApprovalIntent.SendBackForRevision);
        sendBack.Label.Should().Be("差し戻し");
        sendBack.ButtonClass.Should().Be("btn-warning");

        var reject = IntentMetadata.FromIntent(ApprovalIntent.RejectPermanently);
        reject.Label.Should().Be("却下");
        reject.ButtonClass.Should().Be("btn-danger");
    }

    #endregion

    #region Helper Methods

    private PurchaseRequest CreatePurchaseRequest()
    {
        var request = PurchaseRequest.Create(
            _requesterId,
            "テスト申請者",
            "テスト申請",
            "テスト説明",
            _tenantId
        );
        return request;
    }

    private void SetStatus(PurchaseRequest request, PurchaseRequestStatus status)
    {
        var statusProperty = typeof(PurchaseRequest).GetProperty("Status");
        statusProperty?.SetValue(request, status);
    }

    private void AddApprovalStep(PurchaseRequest request, Guid approverId, string approverName, int stepNumber)
    {
        var step = new ApprovalStep(stepNumber, approverId, approverName, "承認者");
        var stepsField = typeof(PurchaseRequest).GetField("_approvalSteps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var steps = (List<ApprovalStep>?)stepsField?.GetValue(request);
        steps?.Add(step);
    }

    #endregion
}
