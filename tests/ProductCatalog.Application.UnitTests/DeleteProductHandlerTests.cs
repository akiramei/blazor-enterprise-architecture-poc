using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProductCatalog.Application.Common.Interfaces;
using ProductCatalog.Application.Products.Commands;
using ProductCatalog.Application.Products.Handlers;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.UnitTests;

public class DeleteProductHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<IProductNotificationService> _notificationMock;
    private readonly Mock<ILogger<DeleteProductHandler>> _loggerMock;
    private readonly DeleteProductHandler _handler;

    public DeleteProductHandlerTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _notificationMock = new Mock<IProductNotificationService>();
        _loggerMock = new Mock<ILogger<DeleteProductHandler>>();
        _handler = new DeleteProductHandler(
            _repositoryMock.Object,
            _notificationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldDeleteProduct_WhenProductExistsAndStockIsZero()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("商品", "説明", new Money(1000, "JPY"), 0);
        var command = new DeleteProductCommand(productId);

        _repositoryMock
            .Setup(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.IsDeleted.Should().BeTrue();
        _repositoryMock.Verify(r => r.SaveAsync(product, It.IsAny<CancellationToken>()), Times.Once);
        _notificationMock.Verify(n => n.NotifyProductChangedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new DeleteProductCommand(productId);

        _repositoryMock
            .Setup(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("商品が見つかりません");
        _repositoryMock.Verify(r => r.SaveAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenStockIsNotZero()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("商品", "説明", new Money(1000, "JPY"), 10);
        var command = new DeleteProductCommand(productId);

        _repositoryMock
            .Setup(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("在庫がある商品は削除できません");
        product.IsDeleted.Should().BeFalse();
        _repositoryMock.Verify(r => r.SaveAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
