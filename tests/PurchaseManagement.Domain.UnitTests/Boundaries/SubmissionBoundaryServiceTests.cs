using FluentAssertions;
using Domain.PurchaseManagement.Boundaries;

namespace PurchaseManagement.Domain.UnitTests.Boundaries;

/// <summary>
/// 提出バウンダリーサービスのユニットテスト
/// バウンダリーパターン: ドメインロジックとUI間の契約をテスト
/// </summary>
public class SubmissionBoundaryServiceTests
{
    private readonly SubmissionBoundaryService _sut;

    public SubmissionBoundaryServiceTests()
    {
        _sut = new SubmissionBoundaryService();
    }

    #region CheckEligibility Tests

    [Fact]
    public void CheckEligibility_すべての条件を満たす場合_提出可能を返す()
    {
        // Arrange
        var title = "Test Purchase Request";
        var description = "Test Description";
        var items = new[]
        {
            new PurchaseRequestItemInput(Guid.NewGuid(), "Product 1", 10000m, 1),
            new PurchaseRequestItemInput(Guid.NewGuid(), "Product 2", 20000m, 2)
        };

        // Act
        var eligibility = _sut.CheckEligibility(title, description, items);

        // Assert
        eligibility.CanSubmit.Should().BeTrue();
        eligibility.HasItems.Should().BeTrue();
        eligibility.IsWithinAmountLimit.Should().BeTrue();
        eligibility.CurrentTotal.Should().Be(50000m);
        eligibility.MaxAllowed.Should().Be(1_000_000m);
        eligibility.BlockingReasons.Should().BeEmpty();
    }

    [Fact]
    public void CheckEligibility_タイトルが空の場合_提出不可を返す()
    {
        // Arrange
        var title = "";
        var description = "Test Description";
        var items = new[]
        {
            new PurchaseRequestItemInput(Guid.NewGuid(), "Product 1", 10000m, 1)
        };

        // Act
        var eligibility = _sut.CheckEligibility(title, description, items);

        // Assert
        eligibility.CanSubmit.Should().BeFalse();
        eligibility.BlockingReasons.Should().Contain(r => r.Code == "TITLE_REQUIRED");
    }

    [Fact]
    public void CheckEligibility_明細が0件の場合_提出不可を返す()
    {
        // Arrange
        var title = "Test Purchase Request";
        var description = "Test Description";
        var items = Array.Empty<PurchaseRequestItemInput>();

        // Act
        var eligibility = _sut.CheckEligibility(title, description, items);

        // Assert
        eligibility.CanSubmit.Should().BeFalse();
        eligibility.HasItems.Should().BeFalse();
        eligibility.BlockingReasons.Should().Contain(r => r.Code == "NO_ITEMS");
    }

    [Fact]
    public void CheckEligibility_商品IDが無効な場合_提出不可を返す()
    {
        // Arrange
        var title = "Test Purchase Request";
        var description = "Test Description";
        var items = new[]
        {
            new PurchaseRequestItemInput(Guid.Empty, "Product 1", 10000m, 1)
        };

        // Act
        var eligibility = _sut.CheckEligibility(title, description, items);

        // Assert
        eligibility.CanSubmit.Should().BeFalse();
        eligibility.BlockingReasons.Should().Contain(r => r.Code == "INVALID_PRODUCT_ID");
    }

    [Fact]
    public void CheckEligibility_商品名が空の場合_提出不可を返す()
    {
        // Arrange
        var title = "Test Purchase Request";
        var description = "Test Description";
        var items = new[]
        {
            new PurchaseRequestItemInput(Guid.NewGuid(), "", 10000m, 1)
        };

        // Act
        var eligibility = _sut.CheckEligibility(title, description, items);

        // Assert
        eligibility.CanSubmit.Should().BeFalse();
        eligibility.BlockingReasons.Should().Contain(r => r.Code == "PRODUCT_NAME_REQUIRED");
    }

    [Fact]
    public void CheckEligibility_単価が0以下の場合_提出不可を返す()
    {
        // Arrange
        var title = "Test Purchase Request";
        var description = "Test Description";
        var items = new[]
        {
            new PurchaseRequestItemInput(Guid.NewGuid(), "Product 1", 0m, 1)
        };

        // Act
        var eligibility = _sut.CheckEligibility(title, description, items);

        // Assert
        eligibility.CanSubmit.Should().BeFalse();
        eligibility.BlockingReasons.Should().Contain(r => r.Code == "INVALID_UNIT_PRICE");
    }

    [Fact]
    public void CheckEligibility_数量が0以下の場合_提出不可を返す()
    {
        // Arrange
        var title = "Test Purchase Request";
        var description = "Test Description";
        var items = new[]
        {
            new PurchaseRequestItemInput(Guid.NewGuid(), "Product 1", 10000m, 0)
        };

        // Act
        var eligibility = _sut.CheckEligibility(title, description, items);

        // Assert
        eligibility.CanSubmit.Should().BeFalse();
        eligibility.BlockingReasons.Should().Contain(r => r.Code == "INVALID_QUANTITY");
    }

    [Fact]
    public void CheckEligibility_合計金額が上限を超える場合_提出不可を返す()
    {
        // Arrange
        var title = "Test Purchase Request";
        var description = "Test Description";
        var items = new[]
        {
            new PurchaseRequestItemInput(Guid.NewGuid(), "Product 1", 1_000_001m, 1)
        };

        // Act
        var eligibility = _sut.CheckEligibility(title, description, items);

        // Assert
        eligibility.CanSubmit.Should().BeFalse();
        eligibility.IsWithinAmountLimit.Should().BeFalse();
        eligibility.CurrentTotal.Should().Be(1_000_001m);
        eligibility.BlockingReasons.Should().Contain(r => r.Code == "AMOUNT_LIMIT_EXCEEDED");
    }

    [Fact]
    public void CheckEligibility_合計金額が上限ちょうどの場合_提出可能を返す()
    {
        // Arrange
        var title = "Test Purchase Request";
        var description = "Test Description";
        var items = new[]
        {
            new PurchaseRequestItemInput(Guid.NewGuid(), "Product 1", 1_000_000m, 1)
        };

        // Act
        var eligibility = _sut.CheckEligibility(title, description, items);

        // Assert
        eligibility.CanSubmit.Should().BeTrue();
        eligibility.IsWithinAmountLimit.Should().BeTrue();
        eligibility.CurrentTotal.Should().Be(1_000_000m);
    }

    #endregion

    #region GetContext Tests

    [Fact]
    public void GetContext_正常な入力の場合_完全なコンテキストを返す()
    {
        // Arrange
        var title = "Test Purchase Request";
        var description = "Test Description";
        var items = new[]
        {
            new PurchaseRequestItemInput(Guid.NewGuid(), "Product 1", 10000m, 1),
            new PurchaseRequestItemInput(Guid.NewGuid(), "Product 2", 20000m, 2)
        };

        // Act
        var context = _sut.GetContext(title, description, items);

        // Assert
        context.IsTitleValid.Should().BeTrue();
        context.ItemCount.Should().Be(2);
        context.TotalAmount.Should().Be(50000m);
        context.Currency.Should().Be("JPY");
        context.MaxAllowedAmount.Should().Be(1_000_000m);
        context.RemainingAmount.Should().Be(950000m);
        context.IsNearLimit.Should().BeFalse();
        context.IsOverLimit.Should().BeFalse();
        context.CanSubmit.Should().BeTrue();
    }

    [Fact]
    public void GetContext_上限近接の場合_警告フラグを返す()
    {
        // Arrange
        var title = "Test Purchase Request";
        var description = "Test Description";
        var items = new[]
        {
            // 80%以上 = 800,000円以上
            new PurchaseRequestItemInput(Guid.NewGuid(), "Product 1", 850_000m, 1)
        };

        // Act
        var context = _sut.GetContext(title, description, items);

        // Assert
        context.IsNearLimit.Should().BeTrue();
        context.IsOverLimit.Should().BeFalse();
        context.RemainingAmount.Should().Be(150_000m);
    }

    [Fact]
    public void GetContext_上限超過の場合_超過フラグを返す()
    {
        // Arrange
        var title = "Test Purchase Request";
        var description = "Test Description";
        var items = new[]
        {
            new PurchaseRequestItemInput(Guid.NewGuid(), "Product 1", 1_100_000m, 1)
        };

        // Act
        var context = _sut.GetContext(title, description, items);

        // Assert
        context.IsOverLimit.Should().BeTrue();
        context.RemainingAmount.Should().Be(-100_000m);
        context.CanSubmit.Should().BeFalse();
    }

    [Fact]
    public void GetContext_検証エラーがある場合_エラーメッセージを返す()
    {
        // Arrange
        var title = ""; // タイトルなし
        var description = "Test Description";
        var items = Array.Empty<PurchaseRequestItemInput>(); // 明細なし

        // Act
        var context = _sut.GetContext(title, description, items);

        // Assert
        context.IsTitleValid.Should().BeFalse();
        context.ValidationErrors.Should().HaveCount(2);
        context.ValidationErrors.Should().Contain("タイトルは必須です");
        context.ValidationErrors.Should().Contain("明細が1件もありません");
        context.CanSubmit.Should().BeFalse();
    }

    #endregion

    #region CalculateTotalAmount Tests

    [Fact]
    public void CalculateTotalAmount_正常な明細の場合_合計を返す()
    {
        // Arrange
        var items = new[]
        {
            new PurchaseRequestItemInput(Guid.NewGuid(), "Product 1", 10000m, 2),  // 20,000
            new PurchaseRequestItemInput(Guid.NewGuid(), "Product 2", 5000m, 3),   // 15,000
            new PurchaseRequestItemInput(Guid.NewGuid(), "Product 3", 3000m, 5)    // 15,000
        };

        // Act
        var total = _sut.CalculateTotalAmount(items);

        // Assert
        total.Should().Be(50000m);
    }

    [Fact]
    public void CalculateTotalAmount_明細がnullの場合_0を返す()
    {
        // Act
        var total = _sut.CalculateTotalAmount(null!);

        // Assert
        total.Should().Be(0m);
    }

    [Fact]
    public void CalculateTotalAmount_明細が空の場合_0を返す()
    {
        // Act
        var total = _sut.CalculateTotalAmount(Array.Empty<PurchaseRequestItemInput>());

        // Assert
        total.Should().Be(0m);
    }

    #endregion
}
