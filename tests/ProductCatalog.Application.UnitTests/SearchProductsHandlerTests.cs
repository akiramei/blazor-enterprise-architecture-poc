using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Interfaces;
using ProductCatalog.Application.Features.Products.SearchProducts;
using ProductCatalog.Application.Products.DTOs;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.UnitTests;

public class SearchProductsHandlerTests
{
    private readonly Mock<IProductReadRepository> _readRepositoryMock;
    private readonly Mock<ILogger<SearchProductsHandler>> _loggerMock;
    private readonly SearchProductsHandler _handler;

    public SearchProductsHandlerTests()
    {
        _readRepositoryMock = new Mock<IProductReadRepository>();
        _loggerMock = new Mock<ILogger<SearchProductsHandler>>();
        _handler = new SearchProductsHandler(
            _readRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnPagedResult_WhenSearchIsSuccessful()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new(Guid.NewGuid(), "商品A", "説明A", 1000, "JPY", 10, "¥1,000"),
            new(Guid.NewGuid(), "商品B", "説明B", 2000, "JPY", 20, "¥2,000")
        };

        var pagedResult = new PagedResult<ProductDto>
        {
            Items = products,
            TotalCount = 10,
            CurrentPage = 1,
            PageSize = 2
        };

        var query = new SearchProductsQuery(
            NameFilter: null,
            MinPrice: null,
            MaxPrice: null,
            Status: null,
            Page: 1,
            PageSize: 2,
            OrderBy: "Name",
            IsDescending: false);

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
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(10);
        result.Value.CurrentPage.Should().Be(1);
        result.Value.PageSize.Should().Be(2);
        result.Value.TotalPages.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ShouldFilterByName_WhenNameFilterIsProvided()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new(Guid.NewGuid(), "ノートパソコン", "説明", 100000, "JPY", 5, "¥100,000")
        };

        var pagedResult = new PagedResult<ProductDto>
        {
            Items = products,
            TotalCount = 1,
            CurrentPage = 1,
            PageSize = 10
        };

        var query = new SearchProductsQuery(
            NameFilter: "ノート",
            MinPrice: null,
            MaxPrice: null,
            Status: null,
            Page: 1,
            PageSize: 10,
            OrderBy: "Name",
            IsDescending: false);

        _readRepositoryMock
            .Setup(r => r.SearchAsync(
                "ノート",
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
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.First().Name.Should().Contain("ノート");
    }

    [Fact]
    public async Task Handle_ShouldFilterByPriceRange_WhenPriceRangeIsProvided()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new(Guid.NewGuid(), "商品A", "説明", 1500, "JPY", 10, "¥1,500"),
            new(Guid.NewGuid(), "商品B", "説明", 2500, "JPY", 20, "¥2,500")
        };

        var pagedResult = new PagedResult<ProductDto>
        {
            Items = products,
            TotalCount = 2,
            CurrentPage = 1,
            PageSize = 10
        };

        var query = new SearchProductsQuery(
            NameFilter: null,
            MinPrice: 1000,
            MaxPrice: 3000,
            Status: null,
            Page: 1,
            PageSize: 10,
            OrderBy: "Price",
            IsDescending: false);

        _readRepositoryMock
            .Setup(r => r.SearchAsync(
                It.IsAny<string?>(),
                1000,
                3000,
                It.IsAny<ProductStatus?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                "Price",
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items.Should().AllSatisfy(p =>
        {
            p.Price.Should().BeInRange(1000, 3000);
        });
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPageIsZeroOrNegative()
    {
        // Arrange
        var query = new SearchProductsQuery(
            NameFilter: null,
            MinPrice: null,
            MaxPrice: null,
            Status: null,
            Page: 0,
            PageSize: 10,
            OrderBy: "Name",
            IsDescending: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("ページ番号は1以上である必要があります");

        _readRepositoryMock.Verify(
            r => r.SearchAsync(
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<ProductStatus?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPageSizeIsInvalid()
    {
        // Arrange
        var query = new SearchProductsQuery(
            NameFilter: null,
            MinPrice: null,
            MaxPrice: null,
            Status: null,
            Page: 1,
            PageSize: 101, // 100より大きい
            OrderBy: "Name",
            IsDescending: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("ページサイズは1以上100以下である必要があります");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenOrderByIsInvalid()
    {
        // Arrange
        var query = new SearchProductsQuery(
            NameFilter: null,
            MinPrice: null,
            MaxPrice: null,
            Status: null,
            Page: 1,
            PageSize: 10,
            OrderBy: "DROP TABLE", // SQLインジェクション試行
            IsDescending: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("無効なソート項目です");
    }

    [Fact]
    public async Task Handle_ShouldSortDescending_WhenIsDescendingIsTrue()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new(Guid.NewGuid(), "商品C", "説明", 3000, "JPY", 30, "¥3,000"),
            new(Guid.NewGuid(), "商品B", "説明", 2000, "JPY", 20, "¥2,000"),
            new(Guid.NewGuid(), "商品A", "説明", 1000, "JPY", 10, "¥1,000")
        };

        var pagedResult = new PagedResult<ProductDto>
        {
            Items = products,
            TotalCount = 3,
            CurrentPage = 1,
            PageSize = 10
        };

        var query = new SearchProductsQuery(
            NameFilter: null,
            MinPrice: null,
            MaxPrice: null,
            Status: null,
            Page: 1,
            PageSize: 10,
            OrderBy: "Price",
            IsDescending: true);

        _readRepositoryMock
            .Setup(r => r.SearchAsync(
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<ProductStatus?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                "Price",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.Items.First().Price.Should().Be(3000);
        result.Value.Items.Last().Price.Should().Be(1000);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyResult_WhenNoProductsMatch()
    {
        // Arrange
        var pagedResult = new PagedResult<ProductDto>
        {
            Items = new List<ProductDto>(),
            TotalCount = 0,
            CurrentPage = 1,
            PageSize = 10
        };

        var query = new SearchProductsQuery(
            NameFilter: "存在しない商品",
            MinPrice: null,
            MaxPrice: null,
            Status: null,
            Page: 1,
            PageSize: 10,
            OrderBy: "Name",
            IsDescending: false);

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
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldHandlePagination_WhenMultiplePagesExist()
    {
        // Arrange - Page 2 of 5
        var products = new List<ProductDto>
        {
            new(Guid.NewGuid(), "商品11", "説明", 1000, "JPY", 10, "¥1,000"),
            new(Guid.NewGuid(), "商品12", "説明", 2000, "JPY", 20, "¥2,000")
        };

        var pagedResult = new PagedResult<ProductDto>
        {
            Items = products,
            TotalCount = 50,
            CurrentPage = 2,
            PageSize = 10
        };

        var query = new SearchProductsQuery(
            NameFilter: null,
            MinPrice: null,
            MaxPrice: null,
            Status: null,
            Page: 2,
            PageSize: 10,
            OrderBy: "Name",
            IsDescending: false);

        _readRepositoryMock
            .Setup(r => r.SearchAsync(
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<ProductStatus?>(),
                2,
                10,
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.CurrentPage.Should().Be(2);
        result.Value.TotalPages.Should().Be(5);
        result.Value.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenRepositoryThrowsException()
    {
        // Arrange
        var query = new SearchProductsQuery(
            NameFilter: null,
            MinPrice: null,
            MaxPrice: null,
            Status: null,
            Page: 1,
            PageSize: 10,
            OrderBy: "Name",
            IsDescending: false);

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
        result.Error.Should().Be("商品検索中にエラーが発生しました");
    }
}
