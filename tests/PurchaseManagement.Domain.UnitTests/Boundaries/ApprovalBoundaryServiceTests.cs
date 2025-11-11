using FluentAssertions;
using PurchaseManagement.Shared.Domain.PurchaseRequests;
using PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;
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
        _sut = new ApprovalBoundaryService();
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
