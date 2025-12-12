using FluentAssertions;
using Domain.PurchaseManagement.PurchaseRequests;
using Domain.PurchaseManagement.Boundaries;

namespace PurchaseManagement.Domain.UnitTests.Boundaries;

/// <summary>
/// フィルタリングバウンダリーサービスのユニットテスト
/// バウンダリーパターン: ドメインロジックとUI間の契約をテスト
/// </summary>
public class FilteringBoundaryServiceTests
{
    private readonly FilteringBoundaryService _sut;

    public FilteringBoundaryServiceTests()
    {
        _sut = new FilteringBoundaryService();
    }

    #region GetFilterOptions Tests

    [Fact]
    public void GetFilterOptions_すべてのステータスオプションを返す()
    {
        // Act
        var options = _sut.GetFilterOptions();

        // Assert
        options.Should().NotBeNull();
        options.StatusOptions.Should().NotBeEmpty();

        // 「すべて」オプション + 8つのステータス = 9個
        options.StatusOptions.Should().HaveCount(9);

        // 最初のオプションは「すべて」
        options.StatusOptions[0].Value.Should().BeNull();
        options.StatusOptions[0].Label.Should().Be("すべてのステータス");
    }

    [Fact]
    public void GetFilterOptions_全てのステータスが含まれる()
    {
        // Act
        var options = _sut.GetFilterOptions();

        // Assert
        var statuses = Enum.GetValues<PurchaseRequestStatus>();
        foreach (var status in statuses)
        {
            options.StatusOptions.Should().Contain(o => o.Value == status);
        }
    }

    [Fact]
    public void GetFilterOptions_ソート可能フィールドを返す()
    {
        // Act
        var options = _sut.GetFilterOptions();

        // Assert
        options.AllowedSortFields.Should().NotBeEmpty();
        options.AllowedSortFields.Should().Contain("RequestNumber");
        options.AllowedSortFields.Should().Contain("Title");
        options.AllowedSortFields.Should().Contain("Status");
        options.AllowedSortFields.Should().Contain("TotalAmount");
        options.AllowedSortFields.Should().Contain("CreatedAt");
    }

    [Fact]
    public void GetFilterOptions_ステータスオプションが表示順にソートされている()
    {
        // Act
        var options = _sut.GetFilterOptions();

        // Assert
        var displayOrders = options.StatusOptions.Select(o => o.DisplayOrder).ToArray();
        displayOrders.Should().BeInAscendingOrder();
    }

    #endregion

    #region GetStatusDisplay Tests

    [Fact]
    public void GetStatusDisplay_下書きの場合_正しい表示情報を返す()
    {
        // Act
        var display = _sut.GetStatusDisplay(PurchaseRequestStatus.Draft);

        // Assert
        display.Label.Should().Be("下書き");
        display.BadgeColorClass.Should().Be("bg-secondary");
        display.Severity.Should().Be("Info");
    }

    [Fact]
    public void GetStatusDisplay_提出済みの場合_正しい表示情報を返す()
    {
        // Act
        var display = _sut.GetStatusDisplay(PurchaseRequestStatus.Submitted);

        // Assert
        display.Label.Should().Be("提出済み");
        display.BadgeColorClass.Should().Be("bg-info");
    }

    [Fact]
    public void GetStatusDisplay_承認待ちの場合_警告色を返す()
    {
        // Arrange & Act
        var display1 = _sut.GetStatusDisplay(PurchaseRequestStatus.PendingFirstApproval);
        var display2 = _sut.GetStatusDisplay(PurchaseRequestStatus.PendingSecondApproval);
        var display3 = _sut.GetStatusDisplay(PurchaseRequestStatus.PendingFinalApproval);

        // Assert
        display1.BadgeColorClass.Should().Be("bg-warning");
        display2.BadgeColorClass.Should().Be("bg-warning");
        display3.BadgeColorClass.Should().Be("bg-warning");

        display1.Label.Should().Be("1次承認待ち");
        display2.Label.Should().Be("2次承認待ち");
        display3.Label.Should().Be("3次承認待ち");
    }

    [Fact]
    public void GetStatusDisplay_承認済みの場合_成功色を返す()
    {
        // Act
        var display = _sut.GetStatusDisplay(PurchaseRequestStatus.Approved);

        // Assert
        display.Label.Should().Be("承認済み");
        display.BadgeColorClass.Should().Be("bg-success");
        display.Severity.Should().Be("Success");
    }

    [Fact]
    public void GetStatusDisplay_却下の場合_危険色を返す()
    {
        // Act
        var display = _sut.GetStatusDisplay(PurchaseRequestStatus.Rejected);

        // Assert
        display.Label.Should().Be("却下");
        display.BadgeColorClass.Should().Be("bg-danger");
        display.Severity.Should().Be("Danger");
    }

    [Fact]
    public void GetStatusDisplay_キャンセルの場合_ダーク色を返す()
    {
        // Act
        var display = _sut.GetStatusDisplay(PurchaseRequestStatus.Cancelled);

        // Assert
        display.Label.Should().Be("キャンセル");
        display.BadgeColorClass.Should().Be("bg-dark");
    }

    #endregion

    #region GetSortOptions Tests

    [Fact]
    public void GetSortOptions_すべてのソートオプションを返す()
    {
        // Act
        var options = _sut.GetSortOptions();

        // Assert
        options.Should().NotBeNull();
        options.AvailableFields.Should().HaveCount(5);
    }

    [Fact]
    public void GetSortOptions_デフォルトソートフィールドを含む()
    {
        // Act
        var options = _sut.GetSortOptions();

        // Assert
        var defaultField = options.AvailableFields.FirstOrDefault(f => f.IsDefault);
        defaultField.Should().NotBeNull();
        defaultField!.FieldName.Should().Be("CreatedAt");
        defaultField.DisplayName.Should().Be("作成日時");
    }

    [Fact]
    public void GetSortOptions_全てのフィールドが昇順降順ソート可能()
    {
        // Act
        var options = _sut.GetSortOptions();

        // Assert
        foreach (var field in options.AvailableFields)
        {
            field.AllowAscending.Should().BeTrue();
            field.AllowDescending.Should().BeTrue();
        }
    }

    #endregion

    #region IsFilterableStatus Tests

    [Fact]
    public void IsFilterableStatus_有効なステータスの場合_trueを返す()
    {
        // Arrange
        var statuses = Enum.GetValues<PurchaseRequestStatus>();

        // Act & Assert
        foreach (var status in statuses)
        {
            _sut.IsFilterableStatus(status).Should().BeTrue($"{status} はフィルター可能であるべき");
        }
    }

    [Fact]
    public void IsFilterableStatus_下書きの場合_trueを返す()
    {
        // Act
        var result = _sut.IsFilterableStatus(PurchaseRequestStatus.Draft);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsFilterableStatus_承認済みの場合_trueを返す()
    {
        // Act
        var result = _sut.IsFilterableStatus(PurchaseRequestStatus.Approved);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IsSortableField Tests

    [Fact]
    public void IsSortableField_許可されたフィールドの場合_trueを返す()
    {
        // Act & Assert
        _sut.IsSortableField("RequestNumber").Should().BeTrue();
        _sut.IsSortableField("Title").Should().BeTrue();
        _sut.IsSortableField("Status").Should().BeTrue();
        _sut.IsSortableField("TotalAmount").Should().BeTrue();
        _sut.IsSortableField("CreatedAt").Should().BeTrue();
    }

    [Fact]
    public void IsSortableField_大文字小文字を区別しない()
    {
        // Act & Assert
        _sut.IsSortableField("requestnumber").Should().BeTrue();
        _sut.IsSortableField("TITLE").Should().BeTrue();
        _sut.IsSortableField("CreatedAt").Should().BeTrue();
    }

    [Fact]
    public void IsSortableField_許可されていないフィールドの場合_falseを返す()
    {
        // Act & Assert
        _sut.IsSortableField("InvalidField").Should().BeFalse();
        _sut.IsSortableField("Description").Should().BeFalse();
        _sut.IsSortableField("RequesterId").Should().BeFalse();
    }

    [Fact]
    public void IsSortableField_nullまたは空文字の場合_falseを返す()
    {
        // Act & Assert
        _sut.IsSortableField(null!).Should().BeFalse();
        _sut.IsSortableField("").Should().BeFalse();
        _sut.IsSortableField("   ").Should().BeFalse();
    }

    #endregion
}
