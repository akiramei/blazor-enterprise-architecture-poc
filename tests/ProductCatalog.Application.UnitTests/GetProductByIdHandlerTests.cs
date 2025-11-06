using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using GetProductById.Application;
using ProductCatalog.Shared.Application;
using ProductCatalog.Shared.Application.DTOs;

namespace ProductCatalog.Application.UnitTests;

public class GetProductByIdHandlerTests
{
    private readonly Mock<IProductReadRepository> _readRepositoryMock;
    private readonly Mock<ILogger<GetProductByIdHandler>> _loggerMock;
    private readonly GetProductByIdHandler _handler;

    public GetProductByIdHandlerTests()
    {
        _readRepositoryMock = new Mock<IProductReadRepository>();
        _loggerMock = new Mock<ILogger<GetProductByIdHandler>>();
        _handler = new GetProductByIdHandler(
            _readRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnProductDetails_WhenProductExists()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productDto = new ProductDetailDto
        {
            Id = productId,
            Name = "商品名",
            Description = "商品説明",
            Price = 1000,
            Stock = 10,
            Status = "Draft",
            Version = 1,
            Images = Array.Empty<ProductImageDto>()
        };

        var query = new GetProductByIdQuery(productId);

        _readRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(productDto);

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
        result.Value.Version.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnProductWithImages_WhenProductHasImages()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productDto = new ProductDetailDto
        {
            Id = productId,
            Name = "商品名",
            Description = "商品説明",
            Price = 1000,
            Stock = 10,
            Status = "Draft",
            Version = 1,
            Images = new List<ProductImageDto>
            {
                new() { Id = Guid.NewGuid(), Url = "https://example.com/image1.jpg", DisplayOrder = 0 },
                new() { Id = Guid.NewGuid(), Url = "https://example.com/image2.jpg", DisplayOrder = 1 }
            }
        };

        var query = new GetProductByIdQuery(productId);

        _readRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(productDto);

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

        _readRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDetailDto?)null);

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
        var productDto = new ProductDetailDto
        {
            Id = productId,
            Name = "商品名",
            Description = "商品説明",
            Price = 1000,
            Stock = 10,
            Status = "Published",
            Version = 2,
            Images = Array.Empty<ProductImageDto>()
        };

        var query = new GetProductByIdQuery(productId);

        _readRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(productDto);

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
        var productDto = new ProductDetailDto
        {
            Id = productId,
            Name = "商品名",
            Description = "商品説明",
            Price = 1000,
            Stock = 10,
            Status = "Archived",
            Version = 3,
            Images = Array.Empty<ProductImageDto>()
        };

        var query = new GetProductByIdQuery(productId);

        _readRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(productDto);

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
        var productDto = new ProductDetailDto
        {
            Id = productId,
            Name = "商品名",
            Description = "商品説明",
            Price = 1000,
            Stock = 10,
            Status = "Draft",
            Version = 1,
            Images = Array.Empty<ProductImageDto>()
        };
        var query = new GetProductByIdQuery(productId);

        _readRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(productDto);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _readRepositoryMock.Verify(
            r => r.GetByIdAsync(
                productId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
