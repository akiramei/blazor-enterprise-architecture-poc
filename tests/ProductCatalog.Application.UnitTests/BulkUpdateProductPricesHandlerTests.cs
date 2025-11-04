using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Application.Interfaces;
using BulkUpdateProductPrices.Application;
using ProductCatalog.Shared.Domain.Products;

namespace ProductCatalog.Application.UnitTests;

public class BulkUpdateProductPricesHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<IProductNotificationService> _notificationMock;
    private readonly Mock<ILogger<BulkUpdateProductPricesHandler>> _loggerMock;
    private readonly BulkUpdateProductPricesHandler _handler;

    public BulkUpdateProductPricesHandlerTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _notificationMock = new Mock<IProductNotificationService>();
        _loggerMock = new Mock<ILogger<BulkUpdateProductPricesHandler>>();
        _handler = new BulkUpdateProductPricesHandler(
            _repositoryMock.Object,
            _notificationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldUpdateAllPrices_WhenAllProductsAreValid()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        var product1 = Product.Create("商品1", "説明1", new Money(1000, "JPY"), 10);
        var product2 = Product.Create("商品2", "説明2", new Money(2000, "JPY"), 20);

        var updates = new List<ProductPriceUpdate>
        {
            new(productId1, 1500, (int)product1.Version),
            new(productId2, 2500, (int)product2.Version)
        };

        var command = new BulkUpdateProductPricesCommand(updates);

        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product1);
        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product2);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SucceededCount.Should().Be(2);
        result.Value.FailedCount.Should().Be(0);
        result.Value.IsAllSucceeded.Should().BeTrue();

        product1.Price.Amount.Should().Be(1500);
        product2.Price.Amount.Should().Be(2500);

        _repositoryMock.Verify(r => r.SaveAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _notificationMock.Verify(n => n.NotifyProductChangedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUpdatesIsEmpty()
    {
        // Arrange
        var command = new BulkUpdateProductPricesCommand(new List<ProductPriceUpdate>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("更新対象の商品が指定されていません");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenExceedsMaxCount()
    {
        // Arrange
        var updates = Enumerable.Range(0, 101)
            .Select(i => new ProductPriceUpdate(Guid.NewGuid(), 1000, 1))
            .ToList();

        var command = new BulkUpdateProductPricesCommand(updates);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("一括更新は100件までです");
    }

    [Fact]
    public async Task Handle_ShouldSkipNotFoundProducts()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        var product1 = Product.Create("商品1", "説明1", new Money(1000, "JPY"), 10);

        var updates = new List<ProductPriceUpdate>
        {
            new(productId1, 1500, (int)product1.Version),
            new(productId2, 2500, 1) // 存在しない商品
        };

        var command = new BulkUpdateProductPricesCommand(updates);

        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product1);
        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId2), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.SucceededCount.Should().Be(1);
        result.Value.FailedCount.Should().Be(1);
        result.Value.Errors.Should().Contain(e => e.Contains(productId2.ToString()) && e.Contains("見つかりません"));
    }

    [Fact]
    public async Task Handle_ShouldDetectVersionMismatch()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("商品", "説明", new Money(1000, "JPY"), 10);

        var updates = new List<ProductPriceUpdate>
        {
            new(productId, 1500, (int)product.Version + 1) // Version不一致
        };

        var command = new BulkUpdateProductPricesCommand(updates);

        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.SucceededCount.Should().Be(0);
        result.Value.FailedCount.Should().Be(1);
        result.Value.Errors.Should().Contain(e => e.Contains("Version不一致"));
    }

    [Fact]
    public async Task Handle_ShouldEnforcePriceReductionLimit()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("商品", "説明", new Money(1000, "JPY"), 10);
        product.Publish(); // 公開状態にする

        var updates = new List<ProductPriceUpdate>
        {
            new(productId, 400, (int)product.Version) // 60%値下げ（制限超過）
        };

        var command = new BulkUpdateProductPricesCommand(updates);

        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.SucceededCount.Should().Be(0);
        result.Value.FailedCount.Should().Be(1);
        result.Value.Errors.Should().Contain(e => e.Contains("50%以上の値下げ"));
    }

    [Fact]
    public async Task Handle_ShouldAllowPriceReductionWithinLimit()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("商品", "説明", new Money(1000, "JPY"), 10);
        product.Publish(); // 公開状態にする

        var updates = new List<ProductPriceUpdate>
        {
            new(productId, 600, (int)product.Version) // 40%値下げ（制限内）
        };

        var command = new BulkUpdateProductPricesCommand(updates);

        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.SucceededCount.Should().Be(1);
        result.Value.FailedCount.Should().Be(0);
        product.Price.Amount.Should().Be(600);
    }

    [Fact]
    public async Task Handle_ShouldContinueOnException()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        var product1 = Product.Create("商品1", "説明1", new Money(1000, "JPY"), 10);
        var product2 = Product.Create("商品2", "説明2", new Money(2000, "JPY"), 20);

        var updates = new List<ProductPriceUpdate>
        {
            new(productId1, 1500, (int)product1.Version),
            new(productId2, 2500, (int)product2.Version)
        };

        var command = new BulkUpdateProductPricesCommand(updates);

        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId1), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));
        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product2);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.SucceededCount.Should().Be(1); // product2は成功
        result.Value.FailedCount.Should().Be(1); // product1は失敗
        result.Value.Errors.Should().Contain(e => e.Contains(productId1.ToString()) && e.Contains("Database error"));
    }

    [Fact]
    public async Task Handle_ShouldNotNotify_WhenAllFailed()
    {
        // Arrange
        var productId = Guid.NewGuid();

        var updates = new List<ProductPriceUpdate>
        {
            new(productId, 1500, 1)
        };

        var command = new BulkUpdateProductPricesCommand(updates);

        _repositoryMock
            .Setup(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _notificationMock.Verify(n => n.NotifyProductChangedAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldTrackMixedResults()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var productId3 = Guid.NewGuid();

        var product1 = Product.Create("商品1", "説明1", new Money(1000, "JPY"), 10);
        var product3 = Product.Create("商品3", "説明3", new Money(3000, "JPY"), 30);

        var updates = new List<ProductPriceUpdate>
        {
            new(productId1, 1500, (int)product1.Version),
            new(productId2, 2500, 1), // 存在しない
            new(productId3, 3500, (int)product3.Version)
        };

        var command = new BulkUpdateProductPricesCommand(updates);

        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product1);
        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId2), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId3), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product3);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(3);
        result.Value.SucceededCount.Should().Be(2);
        result.Value.FailedCount.Should().Be(1);
        result.Value.HasAnySucceeded.Should().BeTrue();
        result.Value.Errors.Should().HaveCount(1);
    }
}
