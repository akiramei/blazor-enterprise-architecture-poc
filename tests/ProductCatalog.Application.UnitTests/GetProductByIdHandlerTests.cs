using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using GetProductById.Application;
using ProductCatalog.Shared.Domain.Products;

namespace ProductCatalog.Application.UnitTests;

public class GetProductByIdHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<ILogger<GetProductByIdHandler>> _loggerMock;
    private readonly GetProductByIdHandler _handler;

    public GetProductByIdHandlerTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _loggerMock = new Mock<ILogger<GetProductByIdHandler>>();
        _handler = new GetProductByIdHandler(
            _repositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnProductDetails_WhenProductExists()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("商品名", "商品説明", new Money(1000, "JPY"), 10);

        var query = new GetProductByIdQuery(productId);

        _repositoryMock
            .Setup(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("商品名");
        result.Value.Description.Should().Be("商品説明");
        result.Value.Price.Should().Be(1000);
        result.Value.Stock.Should().Be(10);
        result.Value.Status.Should().Be("Draft");
        result.Value.Version.Should().Be(product.Version);
    }

    [Fact]
    public async Task Handle_ShouldReturnProductWithImages_WhenProductHasImages()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("商品名", "商品説明", new Money(1000, "JPY"), 10);

        // 画像を追加
        product.AddImage("https://example.com/image1.jpg");
        product.AddImage("https://example.com/image2.jpg");

        var query = new GetProductByIdQuery(productId);

        _repositoryMock
            .Setup(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Images.Should().HaveCount(2);
        result.Value.Images.Should().Contain(img => img.Url == "https://example.com/image1.jpg" && img.DisplayOrder == 0);
        result.Value.Images.Should().Contain(img => img.Url == "https://example.com/image2.jpg" && img.DisplayOrder == 1);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var query = new GetProductByIdQuery(productId);

        _repositoryMock
            .Setup(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("商品が見つかりません");
    }

    [Fact]
    public async Task Handle_ShouldReturnPublishedStatus_WhenProductIsPublished()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("商品名", "商品説明", new Money(1000, "JPY"), 10);

        // 商品を公開
        product.Publish();

        var query = new GetProductByIdQuery(productId);

        _repositoryMock
            .Setup(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("Published");
    }

    [Fact]
    public async Task Handle_ShouldReturnArchivedStatus_WhenProductIsArchived()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("商品名", "商品説明", new Money(1000, "JPY"), 10);

        // 商品を公開してからアーカイブ
        product.Publish();
        product.Archive();

        var query = new GetProductByIdQuery(productId);

        _repositoryMock
            .Setup(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("Archived");
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryOnce_WhenCalled()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = Product.Create("商品名", "商品説明", new Money(1000, "JPY"), 10);
        var query = new GetProductByIdQuery(productId);

        _repositoryMock
            .Setup(r => r.GetAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(
            r => r.GetAsync(
                It.Is<ProductId>(id => id.Value == productId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
