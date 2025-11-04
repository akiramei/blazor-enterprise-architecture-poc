using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Application.Interfaces;
using UpdateProduct.Application;
using ProductCatalog.Shared.Domain.Products;

namespace ProductCatalog.Application.UnitTests;

public class UpdateProductHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<IProductNotificationService> _notificationMock;
    private readonly Mock<ILogger<UpdateProductHandler>> _loggerMock;
    private readonly UpdateProductHandler _handler;

    public UpdateProductHandlerTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _notificationMock = new Mock<IProductNotificationService>();
        _loggerMock = new Mock<ILogger<UpdateProductHandler>>();
        _handler = new UpdateProductHandler(
            _repositoryMock.Object,
            _notificationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldUpdateProduct_WhenDataIsValid()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("元の商品名", "元の説明", new Money(1000, "JPY"), 10);
        var command = new UpdateProductCommand(
            productId,
            "新しい商品名",
            "新しい説明",
            2000,
            20,
            product.Version);

        _repositoryMock
            .Setup(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.Name.Should().Be("新しい商品名");
        product.Description.Should().Be("新しい説明");
        product.Price.Amount.Should().Be(2000);
        product.Stock.Should().Be(20);
        _repositoryMock.Verify(r => r.SaveAsync(product, It.IsAny<CancellationToken>()), Times.Once);
        _notificationMock.Verify(n => n.NotifyProductChangedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new UpdateProductCommand(
            productId,
            "商品名",
            "説明",
            1000,
            10,
            1);

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
    public async Task Handle_ShouldReturnFailure_WhenVersionMismatch()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("商品", "説明", new Money(1000, "JPY"), 10);
        var command = new UpdateProductCommand(
            productId,
            "新しい商品名",
            "新しい説明",
            2000,
            20,
            product.Version + 1); // Version不一致

        _repositoryMock
            .Setup(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("他のユーザーによって更新されています");
        _repositoryMock.Verify(r => r.SaveAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPriceReductionExceeds50Percent()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("商品", "説明", new Money(1000, "JPY"), 10);

        // 商品を公開状態にする
        product.Publish();

        var command = new UpdateProductCommand(
            productId,
            "商品",
            "説明",
            400, // 60%値下げ（1000 -> 400）
            10,
            product.Version);

        _repositoryMock
            .Setup(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("50%以上の値下げはできません");
        _repositoryMock.Verify(r => r.SaveAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUpdatePrice_WhenReductionIsWithin50Percent()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("商品", "説明", new Money(1000, "JPY"), 10);

        // 商品を公開状態にする
        product.Publish();

        var command = new UpdateProductCommand(
            productId,
            "商品",
            "説明",
            600, // 40%値下げ（1000 -> 600）OK
            10,
            product.Version);

        _repositoryMock
            .Setup(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.Price.Amount.Should().Be(600);
        _repositoryMock.Verify(r => r.SaveAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenNameIsEmpty()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("商品", "説明", new Money(1000, "JPY"), 10);
        var command = new UpdateProductCommand(
            productId,
            "", // 空の名前
            "説明",
            1000,
            10,
            product.Version);

        _repositoryMock
            .Setup(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("商品名");
        _repositoryMock.Verify(r => r.SaveAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenStockIsNegative()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("商品", "説明", new Money(1000, "JPY"), 10);
        var command = new UpdateProductCommand(
            productId,
            "商品",
            "説明",
            1000,
            -5, // 負の在庫
            product.Version);

        _repositoryMock
            .Setup(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("在庫");
        _repositoryMock.Verify(r => r.SaveAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
