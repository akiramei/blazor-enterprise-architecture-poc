using FluentAssertions;
using PurchaseManagement.Shared.Domain.PurchaseRequests;
using PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;

namespace PurchaseManagement.Domain.UnitTests.Boundaries;

/// <summary>
/// UIメタデータのユニットテスト（UIポリシープッシュパターン）
/// </summary>
public class UIMetadataTests
{
    #region ForApprovalStep Tests

    [Fact]
    public void ForApprovalStep_Pending_正しいメタデータを返す()
    {
        // Act
        var metadata = UIMetadata.ForApprovalStep(ApprovalStepStatus.Pending);

        // Assert
        metadata.Rendering.BadgeColorClass.Should().Be("bg-warning text-dark");
        metadata.Rendering.BorderColorClass.Should().Be("border-warning");
        metadata.Rendering.IconClass.Should().Be("bi-hourglass-split");
        metadata.Rendering.EmphasisLevel.Should().Be(EmphasisLevel.Medium);

        metadata.Accessibility.AriaLabel.Should().Be("承認待ち");
        metadata.Accessibility.Role.Should().Be("status");
        metadata.Accessibility.LiveRegion.Should().Be("polite");
    }

    [Fact]
    public void ForApprovalStep_Approved_正しいメタデータを返す()
    {
        // Act
        var metadata = UIMetadata.ForApprovalStep(ApprovalStepStatus.Approved);

        // Assert
        metadata.Rendering.BadgeColorClass.Should().Be("bg-success");
        metadata.Rendering.BorderColorClass.Should().Be("border-success");
        metadata.Rendering.IconClass.Should().Be("bi-check-circle-fill");
        metadata.Rendering.EmphasisLevel.Should().Be(EmphasisLevel.Low);

        metadata.Accessibility.AriaLabel.Should().Be("承認済み");
        metadata.Accessibility.Role.Should().Be("status");
        metadata.Accessibility.LiveRegion.Should().Be("off");
    }

    [Fact]
    public void ForApprovalStep_Rejected_正しいメタデータを返す()
    {
        // Act
        var metadata = UIMetadata.ForApprovalStep(ApprovalStepStatus.Rejected);

        // Assert
        metadata.Rendering.BadgeColorClass.Should().Be("bg-danger");
        metadata.Rendering.BorderColorClass.Should().Be("border-danger");
        metadata.Rendering.IconClass.Should().Be("bi-x-circle-fill");
        metadata.Rendering.EmphasisLevel.Should().Be(EmphasisLevel.High);

        metadata.Accessibility.AriaLabel.Should().Be("却下");
        metadata.Accessibility.Role.Should().Be("alert");
        metadata.Accessibility.LiveRegion.Should().Be("assertive");
    }

    #endregion

    #region ForRequestStatus Tests

    [Fact]
    public void ForRequestStatus_Draft_正しいメタデータを返す()
    {
        // Act
        var metadata = UIMetadata.ForRequestStatus(PurchaseRequestStatus.Draft);

        // Assert
        metadata.Rendering.BadgeColorClass.Should().Be("bg-secondary");
        metadata.Rendering.IconClass.Should().Be("bi-file-earmark");
        metadata.Rendering.EmphasisLevel.Should().Be(EmphasisLevel.Low);

        metadata.Accessibility.AriaLabel.Should().Be("下書き");
        metadata.Accessibility.Role.Should().Be("status");
    }

    [Fact]
    public void ForRequestStatus_Submitted_正しいメタデータを返す()
    {
        // Act
        var metadata = UIMetadata.ForRequestStatus(PurchaseRequestStatus.Submitted);

        // Assert
        metadata.Rendering.BadgeColorClass.Should().Be("bg-info");
        metadata.Rendering.IconClass.Should().Be("bi-send");
        metadata.Accessibility.AriaLabel.Should().Be("提出済み");
    }

    [Fact]
    public void ForRequestStatus_PendingFirstApproval_正しいメタデータを返す()
    {
        // Act
        var metadata = UIMetadata.ForRequestStatus(PurchaseRequestStatus.PendingFirstApproval);

        // Assert
        metadata.Rendering.BadgeColorClass.Should().Be("bg-warning");
        metadata.Rendering.IconClass.Should().Be("bi-hourglass-split");
        metadata.Rendering.EmphasisLevel.Should().Be(EmphasisLevel.Medium);

        metadata.Accessibility.AriaLabel.Should().Be("1次承認待ち");
        metadata.Accessibility.Role.Should().Be("status");
        metadata.Accessibility.LiveRegion.Should().Be("polite");
    }

    [Fact]
    public void ForRequestStatus_Approved_正しいメタデータを返す()
    {
        // Act
        var metadata = UIMetadata.ForRequestStatus(PurchaseRequestStatus.Approved);

        // Assert
        metadata.Rendering.BadgeColorClass.Should().Be("bg-success");
        metadata.Rendering.IconClass.Should().Be("bi-check-circle-fill");
        metadata.Rendering.EmphasisLevel.Should().Be(EmphasisLevel.Low);

        metadata.Accessibility.AriaLabel.Should().Be("承認済み");
        metadata.Accessibility.Role.Should().Be("status");
    }

    [Fact]
    public void ForRequestStatus_Rejected_正しいメタデータを返す()
    {
        // Act
        var metadata = UIMetadata.ForRequestStatus(PurchaseRequestStatus.Rejected);

        // Assert
        metadata.Rendering.BadgeColorClass.Should().Be("bg-danger");
        metadata.Rendering.IconClass.Should().Be("bi-x-circle-fill");
        metadata.Rendering.EmphasisLevel.Should().Be(EmphasisLevel.High); // Danger -> High

        metadata.Accessibility.AriaLabel.Should().Be("却下");
        metadata.Accessibility.Role.Should().Be("alert");
    }

    [Fact]
    public void ForRequestStatus_Cancelled_正しいメタデータを返す()
    {
        // Act
        var metadata = UIMetadata.ForRequestStatus(PurchaseRequestStatus.Cancelled);

        // Assert
        metadata.Rendering.BadgeColorClass.Should().Be("bg-dark");
        metadata.Rendering.IconClass.Should().Be("bi-slash-circle");
        metadata.Accessibility.AriaLabel.Should().Be("キャンセル");
    }

    #endregion

    #region アクセシビリティ Tests

    [Fact]
    public void ForApprovalStep_全ステータス_アクセシビリティ情報を含む()
    {
        // Arrange
        var allStatuses = new[]
        {
            ApprovalStepStatus.Pending,
            ApprovalStepStatus.Approved,
            ApprovalStepStatus.Rejected
        };

        // Act & Assert
        foreach (var status in allStatuses)
        {
            var metadata = UIMetadata.ForApprovalStep(status);

            metadata.Accessibility.Should().NotBeNull();
            metadata.Accessibility.AriaLabel.Should().NotBeNullOrEmpty();
            metadata.Accessibility.Role.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void ForRequestStatus_全ステータス_アクセシビリティ情報を含む()
    {
        // Arrange
        var allStatuses = new[]
        {
            PurchaseRequestStatus.Draft,
            PurchaseRequestStatus.Submitted,
            PurchaseRequestStatus.PendingFirstApproval,
            PurchaseRequestStatus.PendingSecondApproval,
            PurchaseRequestStatus.PendingFinalApproval,
            PurchaseRequestStatus.Approved,
            PurchaseRequestStatus.Rejected,
            PurchaseRequestStatus.Cancelled
        };

        // Act & Assert
        foreach (var status in allStatuses)
        {
            var metadata = UIMetadata.ForRequestStatus(status);

            metadata.Accessibility.Should().NotBeNull();
            metadata.Accessibility.AriaLabel.Should().NotBeNullOrEmpty();
            metadata.Accessibility.Role.Should().NotBeNullOrEmpty();
        }
    }

    #endregion

    #region 強調レベル Tests

    [Theory]
    [InlineData(ApprovalStepStatus.Pending, EmphasisLevel.Medium)]
    [InlineData(ApprovalStepStatus.Approved, EmphasisLevel.Low)]
    [InlineData(ApprovalStepStatus.Rejected, EmphasisLevel.High)]
    public void ForApprovalStep_各ステータス_正しい強調レベルを返す(
        ApprovalStepStatus status,
        EmphasisLevel expectedLevel)
    {
        // Act
        var metadata = UIMetadata.ForApprovalStep(status);

        // Assert
        metadata.Rendering.EmphasisLevel.Should().Be(expectedLevel);
    }

    [Fact]
    public void ForRequestStatus_警告状態_Medium強調レベルを返す()
    {
        // Arrange
        var warningStatuses = new[]
        {
            PurchaseRequestStatus.PendingFirstApproval,
            PurchaseRequestStatus.PendingSecondApproval,
            PurchaseRequestStatus.PendingFinalApproval
        };

        // Act & Assert
        foreach (var status in warningStatuses)
        {
            var metadata = UIMetadata.ForRequestStatus(status);
            metadata.Rendering.EmphasisLevel.Should().Be(EmphasisLevel.Medium);
        }
    }

    #endregion
}
