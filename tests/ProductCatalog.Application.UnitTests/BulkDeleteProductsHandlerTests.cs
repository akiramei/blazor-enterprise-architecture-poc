using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProductCatalog.Application.Common.Interfaces;
using ProductCatalog.Application.Features.Products.BulkDeleteProducts;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.UnitTests;

public class BulkDeleteProductsHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<IProductNotificationService> _notificationMock;
    private readonly Mock<ILogger<BulkDeleteProductsHandler>> _loggerMock;
    private readonly BulkDeleteProductsHandler _handler;

    public BulkDeleteProductsHandlerTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _notificationMock = new Mock<IProductNotificationService>();
        _loggerMock = new Mock<ILogger<BulkDeleteProductsHandler>>();
        _handler = new BulkDeleteProductsHandler(
            _repositoryMock.Object,
            _notificationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldDeleteAllProducts_WhenAllProductsHaveZeroStock()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var productId3 = Guid.NewGuid();

        var product1 = Product.Create("商品1", "説明1", new Money(1000, "JPY"), 0);
        var product2 = Product.Create("商品2", "説明2", new Money(2000, "JPY"), 0);
        var product3 = Product.Create("商品3", "説明3", new Money(3000, "JPY"), 0);

        var command = new BulkDeleteProductsCommand(new[] { productId1, productId2, productId3 });

        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product1);
        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product2);
        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId3), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product3);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SucceededCount.Should().Be(3);
        result.Value.FailedCount.Should().Be(0);
        result.Value.IsAllSucceeded.Should().BeTrue();
        result.Value.Errors.Should().BeEmpty();

        product1.IsDeleted.Should().BeTrue();
        product2.IsDeleted.Should().BeTrue();
        product3.IsDeleted.Should().BeTrue();

        _repositoryMock.Verify(r => r.SaveAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _notificationMock.Verify(n => n.NotifyProductChangedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllFailed_WhenNoProductsFound()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        var command = new BulkDeleteProductsCommand(new[] { productId1, productId2 });

        _repositoryMock
            .Setup(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SucceededCount.Should().Be(0);
        result.Value.FailedCount.Should().Be(2);
        result.Value.IsAllSucceeded.Should().BeFalse();
        result.Value.Errors.Should().HaveCount(2);
        result.Value.Errors.Should().Contain(e => e.Contains("見つかりません"));

        _repositoryMock.Verify(r => r.SaveAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        _notificationMock.Verify(n => n.NotifyProductChangedAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnPartialSuccess_WhenSomeProductsHaveStock()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var productId3 = Guid.NewGuid();

        var product1 = Product.Create("商品1", "説明1", new Money(1000, "JPY"), 0);  // 削除可能
        var product2 = Product.Create("商品2", "説明2", new Money(2000, "JPY"), 10); // 在庫あり（削除不可）
        var product3 = Product.Create("商品3", "説明3", new Money(3000, "JPY"), 0);  // 削除可能

        var command = new BulkDeleteProductsCommand(new[] { productId1, productId2, productId3 });

        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product1);
        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product2);
        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId3), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product3);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SucceededCount.Should().Be(2);
        result.Value.FailedCount.Should().Be(1);
        result.Value.IsAllSucceeded.Should().BeFalse();
        result.Value.HasAnySucceeded.Should().BeTrue();
        result.Value.Errors.Should().HaveCount(1);
        result.Value.Errors.Should().Contain(e => e.Contains(productId2.ToString()) && e.Contains("在庫"));

        product1.IsDeleted.Should().BeTrue();
        product2.IsDeleted.Should().BeFalse();
        product3.IsDeleted.Should().BeTrue();

        _repositoryMock.Verify(r => r.SaveAsync(product1, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveAsync(product2, It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveAsync(product3, It.IsAny<CancellationToken>()), Times.Once);
        _notificationMock.Verify(n => n.NotifyProductChangedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductIdsIsEmpty()
    {
        // Arrange
        var command = new BulkDeleteProductsCommand(Array.Empty<Guid>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("削除対象の商品が指定されていません");

        _repositoryMock.Verify(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductCountExceeds100()
    {
        // Arrange
        var productIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToArray();
        var command = new BulkDeleteProductsCommand(productIds);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("一括削除は100件までです");

        _repositoryMock.Verify(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldContinueProcessing_WhenOneProductThrowsException()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var productId3 = Guid.NewGuid();

        var product1 = Product.Create("商品1", "説明1", new Money(1000, "JPY"), 0);
        var product3 = Product.Create("商品3", "説明3", new Money(3000, "JPY"), 0);

        var command = new BulkDeleteProductsCommand(new[] { productId1, productId2, productId3 });

        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product1);
        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId2), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));
        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId3), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product3);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SucceededCount.Should().Be(2);
        result.Value.FailedCount.Should().Be(1);
        result.Value.Errors.Should().HaveCount(1);
        result.Value.Errors.Should().Contain(e => e.Contains(productId2.ToString()) && e.Contains("システムエラー"));

        product1.IsDeleted.Should().BeTrue();
        product3.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldTrackCorrectCounts_WhenMixedResults()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var productId3 = Guid.NewGuid();
        var productId4 = Guid.NewGuid();

        var product1 = Product.Create("商品1", "説明1", new Money(1000, "JPY"), 0);  // 成功
        var product3 = Product.Create("商品3", "説明3", new Money(3000, "JPY"), 5);  // 失敗（在庫あり）
        var product4 = Product.Create("商品4", "説明4", new Money(4000, "JPY"), 0);  // 成功

        var command = new BulkDeleteProductsCommand(new[] { productId1, productId2, productId3, productId4 });

        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product1);
        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId2), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null); // 失敗（見つからない）
        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId3), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product3);
        _repositoryMock
            .Setup(r => r.GetAsync(It.Is<ProductId>(id => id.Value == productId4), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product4);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(4);
        result.Value.SucceededCount.Should().Be(2);
        result.Value.FailedCount.Should().Be(2);
        result.Value.Errors.Should().HaveCount(2);
    }
}
