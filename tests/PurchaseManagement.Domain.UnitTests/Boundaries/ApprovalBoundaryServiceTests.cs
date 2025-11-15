using FluentAssertions;
using Domain.PurchaseManagement.PurchaseRequests;
using Domain.PurchaseManagement.PurchaseRequests.Boundaries;
using Shared.Kernel;

namespace PurchaseManagement.Domain.UnitTests.Boundaries;

/// <summary>
/// 承認バウンダリーサービスのユニットテスト
/// バウンダリーパターン: ドメインロジックとUI間の契約をテスト
/// </summary>
public class ApprovalBoundaryServiceTests
{
    private readonly ApprovalBoundaryService _sut;

    public ApprovalBoundaryServiceTests()
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

    #region CheckEligibility Tests

    [Fact]
    public void CheckEligibility_承認者が正しい場合_承認可能を返す()
    {
        // Arrange
        var approverId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = CreatePurchaseRequestWithPendingApproval(approverId, tenantId);

        // Act
        var eligibility = _sut.CheckEligibility(request, approverId);

        // Assert
        eligibility.CanApprove.Should().BeTrue();
        eligibility.CanReject.Should().BeTrue();
        eligibility.CurrentStepApproverId.Should().Be(approverId);
        eligibility.BlockingReasons.Should().BeEmpty();
    }

    [Fact]
    public void CheckEligibility_承認者が異なる場合_承認不可を返す()
    {
        // Arrange
        var approverId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = CreatePurchaseRequestWithPendingApproval(approverId, tenantId);

        // Act
        var eligibility = _sut.CheckEligibility(request, otherUserId);

        // Assert
        eligibility.CanApprove.Should().BeFalse();
        eligibility.CanReject.Should().BeFalse();
        eligibility.BlockingReasons.Should().Contain(r => r.Code == "NOT_ASSIGNED_APPROVER");
    }

    [Fact]
    public void CheckEligibility_申請が承認済みの場合_承認不可を返す()
    {
        // Arrange
        var approverId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = CreatePurchaseRequestWithPendingApproval(approverId, tenantId);

        // 承認処理を実行
        request.Approve(approverId, "Approved");

        // Act
        var eligibility = _sut.CheckEligibility(request, approverId);

        // Assert
        eligibility.CanApprove.Should().BeFalse();
        eligibility.CanReject.Should().BeFalse();
        eligibility.BlockingReasons.Should().Contain(r => r.Code == "TERMINAL_STATE");
    }

    [Fact]
    public void CheckEligibility_申請が却下済みの場合_承認不可を返す()
    {
        // Arrange
        var approverId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = CreatePurchaseRequestWithPendingApproval(approverId, tenantId);

        // 却下処理を実行
        request.Reject(approverId, "Rejected");

        // Act
        var eligibility = _sut.CheckEligibility(request, approverId);

        // Assert
        eligibility.CanApprove.Should().BeFalse();
        eligibility.CanReject.Should().BeFalse();
        eligibility.BlockingReasons.Should().Contain(r => r.Code == "TERMINAL_STATE");
    }

    [Fact]
    public void CheckEligibility_ユーザーが未認証の場合_承認不可を返す()
    {
        // Arrange
        var approverId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = CreatePurchaseRequestWithPendingApproval(approverId, tenantId);

        // Act
        var eligibility = _sut.CheckEligibility(request, Guid.Empty);

        // Assert
        eligibility.CanApprove.Should().BeFalse();
        eligibility.CanReject.Should().BeFalse();
        eligibility.BlockingReasons.Should().Contain(r => r.Code == "USER_NOT_AUTHENTICATED");
    }

    [Fact]
    public void CheckEligibility_申請がnullの場合_承認不可を返す()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var eligibility = _sut.CheckEligibility(null!, userId);

        // Assert
        eligibility.CanApprove.Should().BeFalse();
        eligibility.CanReject.Should().BeFalse();
        eligibility.BlockingReasons.Should().Contain(r => r.Code == "REQUEST_NOT_FOUND");
    }

    [Fact]
    public void CheckEligibility_承認待ちステップがない場合_承認不可を返す()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // 下書き状態の申請（承認ステップなし）
        var request = PurchaseRequest.Create(
            userId,
            "Test User",
            "Test Request",
            "Description",
            tenantId
        );

        request.AddItem(Guid.NewGuid(), "Test Product", 1000m, 1);

        // Act
        var eligibility = _sut.CheckEligibility(request, userId);

        // Assert
        eligibility.CanApprove.Should().BeFalse();
        eligibility.CanReject.Should().BeFalse();
        eligibility.BlockingReasons.Should().Contain(r => r.Code == "TERMINAL_STATE" || r.Code == "NO_PENDING_STEP");
    }

    #endregion

    #region GetContext Tests

    [Fact]
    public void GetContext_正常なリクエストの場合_完全なコンテキストを返す()
    {
        // Arrange
        var approverId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = CreatePurchaseRequestWithPendingApproval(approverId, tenantId);

        // Act
        var context = _sut.GetContext(request, approverId);

        // Assert
        context.Request.Should().Be(request);
        context.CurrentStep.Should().NotBeNull();
        context.CurrentStep!.ApproverId.Should().Be(approverId);
        context.IsTerminalState.Should().BeFalse();
        context.AllowedActions.Should().Contain("Approve");
        context.AllowedActions.Should().Contain("Reject");
        context.StatusDisplay.Should().NotBeNull();
    }

    [Fact]
    public void GetContext_承認済みの場合_終端状態を返す()
    {
        // Arrange
        var approverId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = CreatePurchaseRequestWithPendingApproval(approverId, tenantId);

        // 承認処理
        request.Approve(approverId, "Approved");

        // Act
        var context = _sut.GetContext(request, approverId);

        // Assert
        context.IsTerminalState.Should().BeTrue();
        context.CurrentStep.Should().BeNull();
        context.CompletedSteps.Should().HaveCount(1);
        context.AllowedActions.Should().BeEmpty();
    }

    #endregion

    #region GetContext - Cancel Action Tests

    [Fact]
    public void GetContext_申請者がCurrentUser_Cancelアクションを含む()
    {
        // Arrange
        var requesterId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var request = PurchaseRequest.Create(
            requesterId,
            "Requester",
            "Test Request",
            "Description",
            tenantId
        );
        request.AddItem(Guid.NewGuid(), "Product", 5000m, 1);

        var approvalFlow = new ApprovalFlow(
            new[] { new ApprovalFlowStep(1, approverId, "Approver", "Manager") }
        );
        request.Submit(approvalFlow);

        // Submitted に設定
        var statusProperty = typeof(PurchaseRequest).GetProperty("Status");
        statusProperty?.SetValue(request, PurchaseRequestStatus.Submitted);

        // Act: 申請者自身がコンテキストを取得
        var context = _sut.GetContext(request, requesterId);

        // Assert: SECURITY FIX - Cancel は申請者のみに許可
        context.AllowedActions.Should().Contain("Cancel",
            "申請者は自分の申請をキャンセルできる");
    }

    [Fact]
    public void GetContext_申請者以外のUser_Cancelアクションを含まない()
    {
        // Arrange
        var requesterId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid(); // 申請者でも承認者でもない第三者
        var tenantId = Guid.NewGuid();

        var request = PurchaseRequest.Create(
            requesterId,
            "Requester",
            "Test Request",
            "Description",
            tenantId
        );
        request.AddItem(Guid.NewGuid(), "Product", 5000m, 1);

        var approvalFlow = new ApprovalFlow(
            new[] { new ApprovalFlowStep(1, approverId, "Approver", "Manager") }
        );
        request.Submit(approvalFlow);

        // Submitted に設定
        var statusProperty = typeof(PurchaseRequest).GetProperty("Status");
        statusProperty?.SetValue(request, PurchaseRequestStatus.Submitted);

        // Act: 第三者がコンテキストを取得
        var context = _sut.GetContext(request, otherUserId);

        // Assert: SECURITY FIX - Cancel は申請者以外には表示しない
        context.AllowedActions.Should().NotContain("Cancel",
            "申請者以外のユーザーはキャンセルできない（セキュリティ修正）");
    }

    [Fact]
    public void GetContext_承認者がCurrentUser_Cancelアクションを含まない()
    {
        // Arrange
        var requesterId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var request = PurchaseRequest.Create(
            requesterId,
            "Requester",
            "Test Request",
            "Description",
            tenantId
        );
        request.AddItem(Guid.NewGuid(), "Product", 5000m, 1);

        var approvalFlow = new ApprovalFlow(
            new[] { new ApprovalFlowStep(1, approverId, "Approver", "Manager") }
        );
        request.Submit(approvalFlow);

        // PendingFirstApproval に設定
        var statusProperty = typeof(PurchaseRequest).GetProperty("Status");
        statusProperty?.SetValue(request, PurchaseRequestStatus.PendingFirstApproval);

        // Act: 承認者がコンテキストを取得
        var context = _sut.GetContext(request, approverId);

        // Assert: SECURITY FIX - 承認者は Cancel できない
        context.AllowedActions.Should().NotContain("Cancel",
            "承認者は申請をキャンセルできない");

        // 承認者には Approve と Reject のみ
        context.AllowedActions.Should().Contain("Approve");
        context.AllowedActions.Should().Contain("Reject");
    }

    [Fact]
    public void GetContext_終端状態_Cancelアクションを含まない()
    {
        // Arrange
        var requesterId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var request = CreatePurchaseRequestWithPendingApproval(approverId, tenantId);

        // 承認処理（終端状態）
        request.Approve(approverId, "Approved");

        // Act: 申請者でもコンテキストを取得
        var context = _sut.GetContext(request, requesterId);

        // Assert: 終端状態では Cancel できない
        context.AllowedActions.Should().BeEmpty("終端状態ではどのアクションも許可されない");
        context.IsTerminalState.Should().BeTrue();
    }

    [Fact]
    public void GetContext_Draft状態_Cancelアクションを含まない()
    {
        // Arrange
        var requesterId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var request = PurchaseRequest.Create(
            requesterId,
            "Requester",
            "Test Request",
            "Description",
            tenantId
        );
        request.AddItem(Guid.NewGuid(), "Product", 5000m, 1);

        // Draft状態（未提出）

        // Act
        var context = _sut.GetContext(request, requesterId);

        // Assert: Draft状態では Cancel できない
        context.AllowedActions.Should().BeEmpty("Draft状態ではアクションがない");
    }

    #endregion

    #region UIポリシープッシュ Tests

    [Fact]
    public void GetContext_正常なリクエストの場合_UIメタデータを含む()
    {
        // Arrange
        var approverId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = CreatePurchaseRequestWithPendingApproval(approverId, tenantId);

        // Act
        var context = _sut.GetContext(request, approverId);

        // Assert
        context.UIMetadata.Should().NotBeNull("UIポリシープッシュでメタデータが設定される");
        context.UIMetadata!.Rendering.Should().NotBeNull();
        context.UIMetadata.Rendering.BadgeColorClass.Should().NotBeNullOrEmpty();
        context.UIMetadata.Rendering.IconClass.Should().NotBeNullOrEmpty();
        context.UIMetadata.Accessibility.Should().NotBeNull();
        context.UIMetadata.Accessibility.AriaLabel.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetContext_承認ステップあり_ステップUIメタデータを含む()
    {
        // Arrange
        var approverId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = CreatePurchaseRequestWithPendingApproval(approverId, tenantId);

        // Act
        var context = _sut.GetContext(request, approverId);

        // Assert
        context.StepUIMetadata.Should().NotBeNull();
        context.StepUIMetadata.Should().ContainKey(1, "ステップ1のメタデータが存在する");

        var stepMeta = context.StepUIMetadata![1];
        stepMeta.Rendering.BadgeColorClass.Should().NotBeNullOrEmpty();
        stepMeta.Rendering.IconClass.Should().NotBeNullOrEmpty();
        stepMeta.Accessibility.AriaLabel.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetContext_PendingFirstApproval_警告色のメタデータを返す()
    {
        // Arrange
        var approverId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = CreatePurchaseRequestWithPendingApproval(approverId, tenantId);

        // Act
        var context = _sut.GetContext(request, approverId);

        // Assert
        context.UIMetadata.Should().NotBeNull();
        context.UIMetadata!.Rendering.BadgeColorClass.Should().Contain("warning");
        context.UIMetadata.Rendering.EmphasisLevel.Should().Be(EmphasisLevel.Medium);
    }

    [Fact]
    public void GetContext_Approved状態_成功色のメタデータを返す()
    {
        // Arrange
        var approverId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = CreatePurchaseRequestWithPendingApproval(approverId, tenantId);

        // 承認処理
        request.Approve(approverId, "Approved");

        // Act
        var context = _sut.GetContext(request, approverId);

        // Assert
        context.UIMetadata.Should().NotBeNull();
        context.UIMetadata!.Rendering.BadgeColorClass.Should().Contain("success");
        context.UIMetadata.Rendering.EmphasisLevel.Should().Be(EmphasisLevel.Low);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 承認待ちステップを持つ購買申請を作成
    /// 状態: Submitted → PendingFirstApproval（承認可能状態）
    /// </summary>
    private PurchaseRequest CreatePurchaseRequestWithPendingApproval(Guid approverId, Guid tenantId)
    {
        var requesterId = Guid.NewGuid();
        var request = PurchaseRequest.Create(
            requesterId,
            "Test Requester",
            "Test Purchase Request",
            "Test Description",
            tenantId
        );

        // 明細を追加
        request.AddItem(Guid.NewGuid(), "Test Product", 10000m, 1);

        // 承認フローを設定して提出
        var approvalFlow = new ApprovalFlow(
            new[]
            {
                new ApprovalFlowStep(1, approverId, "Test Approver", "Manager")
            }
        );

        request.Submit(approvalFlow);

        // Submitted → PendingFirstApproval に状態遷移（テスト用にReflectionで直接設定）
        // 実際のシステムではアプリケーションサービスが状態遷移を処理
        var statusProperty = typeof(PurchaseRequest).GetProperty("Status");
        statusProperty?.SetValue(request, PurchaseRequestStatus.PendingFirstApproval);

        return request;
    }

    #endregion
}
