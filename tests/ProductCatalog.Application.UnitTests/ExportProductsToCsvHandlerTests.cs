using FluentAssertions;
using Moq;
using Shared.Application.Common;
using Shared.Application.Interfaces;
using Application.Features.ExportProductsToCsv;
using ProductCatalog.Shared.Application;
using ProductCatalog.Shared.Application.DTOs;
using Domain.ProductCatalog.Products;

namespace ProductCatalog.Application.UnitTests;

public class ExportProductsToCsvHandlerTests
{
    private readonly Mock<IProductReadRepository> _readRepositoryMock;
    private readonly ExportProductsToCsvQueryHandler _handler;

    public ExportProductsToCsvHandlerTests()
    {
        _readRepositoryMock = new Mock<IProductReadRepository>();
        _handler = new ExportProductsToCsvQueryHandler(
            _readRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnCsvBytes_WhenProductsExist()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new(Guid.NewGuid(), "商品A", "説明A", 1000, "JPY", 10, "¥1,000", "Draft", 1),
            new(Guid.NewGuid(), "商品B", "説明B", 2000, "JPY", 20, "¥2,000", "Published", 1)
        };

        var pagedResult = new PagedResult<ProductDto>
        {
            Items = products,
            TotalCount = 2,
            CurrentPage = 1,
            PageSize = 10000
        };

        var query = new ExportProductsToCsvQuery
        {
            NameFilter = null,
            MinPrice = null,
            MaxPrice = null,
            Status = null
        };

        _readRepositoryMock
            .Setup(r => r.SearchAsync(
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<ProductStatus?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Length.Should().BeGreaterThan(0);

        // CSVの内容を確認（簡易チェック）
        var csvContent = System.Text.Encoding.UTF8.GetString(result.Value);
        csvContent.Should().Contain("商品ID");
        csvContent.Should().Contain("商品名");
        csvContent.Should().Contain("商品A");
        csvContent.Should().Contain("商品B");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenExceedsMaxCount()
    {
        // Arrange
        var pagedResult = new PagedResult<ProductDto>
        {
            Items = new List<ProductDto>(),
            TotalCount = 10001, // 上限超過
            CurrentPage = 1,
            PageSize = 10001
        };

        var query = new ExportProductsToCsvQuery
        {
            NameFilter = null,
            MinPrice = null,
            MaxPrice = null,
            Status = null
        };

        _readRepositoryMock
            .Setup(r => r.SearchAsync(
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<ProductStatus?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("上限");
    }

    [Fact]
    public async Task Handle_ShouldFilterByStatus_WhenStatusProvided()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new(Guid.NewGuid(), "公開商品", "説明", 1000, "JPY", 10, "¥1,000", "Published", 1)
        };

        var pagedResult = new PagedResult<ProductDto>
        {
            Items = products,
            TotalCount = 1,
            CurrentPage = 1,
            PageSize = 10000
        };

        var query = new ExportProductsToCsvQuery
        {
            NameFilter = null,
            MinPrice = null,
            MaxPrice = null,
            Status = ProductStatus.Published
        };

        _readRepositoryMock
            .Setup(r => r.SearchAsync(
                null,
                null,
                null,
                ProductStatus.Published,
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var csvContent = System.Text.Encoding.UTF8.GetString(result.Value!);
        csvContent.Should().Contain("公開商品");
    }

    [Fact]
    public async Task Handle_ShouldConvertStatusToJapanese()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new(Guid.NewGuid(), "商品A", "説明", 1000, "JPY", 10, "¥1,000", "Draft", 1),
            new(Guid.NewGuid(), "商品B", "説明", 2000, "JPY", 20, "¥2,000", "Published", 1),
            new(Guid.NewGuid(), "商品C", "説明", 3000, "JPY", 30, "¥3,000", "Archived", 1)
        };

        var pagedResult = new PagedResult<ProductDto>
        {
            Items = products,
            TotalCount = 3,
            CurrentPage = 1,
            PageSize = 10000
        };

        var query = new ExportProductsToCsvQuery
        {
            NameFilter = null,
            MinPrice = null,
            MaxPrice = null,
            Status = null
        };

        _readRepositoryMock
            .Setup(r => r.SearchAsync(
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<ProductStatus?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var csvContent = System.Text.Encoding.UTF8.GetString(result.Value!);
        csvContent.Should().Contain("下書き");
        csvContent.Should().Contain("公開中");
        csvContent.Should().Contain("アーカイブ済み");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenRepositoryThrowsException()
    {
        // Arrange
        var query = new ExportProductsToCsvQuery
        {
            NameFilter = null,
            MinPrice = null,
            MaxPrice = null,
            Status = null
        };

        _readRepositoryMock
            .Setup(r => r.SearchAsync(
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<ProductStatus?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("エラー");
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectParameters()
    {
        // Arrange
        var pagedResult = new PagedResult<ProductDto>
        {
            Items = new List<ProductDto>(),
            TotalCount = 0,
            CurrentPage = 1,
            PageSize = 10000
        };

        var query = new ExportProductsToCsvQuery
        {
            NameFilter = "テスト",
            MinPrice = 1000,
            MaxPrice = 5000,
            Status = ProductStatus.Published
        };

        _readRepositoryMock
            .Setup(r => r.SearchAsync(
                "テスト",
                1000m,
                5000m,
                ProductStatus.Published,
                1,
                10001,
                "Name",
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _readRepositoryMock.Verify(
            r => r.SearchAsync(
                "テスト",
                1000m,
                5000m,
                ProductStatus.Published,
                1,
                10001,
                "Name",
                false,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
