using FluentAssertions;
using SearchProducts.Application;
using Domain.ProductCatalog.Products;

namespace ProductCatalog.Application.UnitTests;

public class SearchProductsValidatorTests
{
    private readonly SearchProductsValidator _validator;

    public SearchProductsValidatorTests()
    {
        _validator = new SearchProductsValidator();
    }

    [Fact]
    public async Task Validate_ShouldSucceed_WhenAllParametersAreValid()
    {
        // Arrange
        var query = new SearchProductsQuery(
            NameFilter: "商品",
            MinPrice: 100,
            MaxPrice: 1000,
            Status: ProductStatus.Published,
            Page: 1,
            PageSize: 20,
            OrderBy: "Name",
            IsDescending: false
        );

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenPageIsZero()
    {
        // Arrange
        var query = new SearchProductsQuery(Page: 0);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page" && e.ErrorMessage == "ページ番号は1以上である必要があります");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenPageSizeIsTooLarge()
    {
        // Arrange
        var query = new SearchProductsQuery(PageSize: 101);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize" && e.ErrorMessage == "ページサイズは1〜100の範囲で指定してください");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenPageSizeIsZero()
    {
        // Arrange
        var query = new SearchProductsQuery(PageSize: 0);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenMinPriceIsNegative()
    {
        // Arrange
        var query = new SearchProductsQuery(MinPrice: -100);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MinPrice" && e.ErrorMessage == "最低価格は0以上である必要があります");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenMaxPriceIsNegative()
    {
        // Arrange
        var query = new SearchProductsQuery(MaxPrice: -100);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxPrice" && e.ErrorMessage == "最高価格は0以上である必要があります");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenMinPriceIsGreaterThanMaxPrice()
    {
        // Arrange
        var query = new SearchProductsQuery(MinPrice: 1000, MaxPrice: 100);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MinPrice" && e.ErrorMessage == "最低価格は最高価格以下である必要があります");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenOrderByIsEmpty()
    {
        // Arrange
        var query = new SearchProductsQuery(OrderBy: "");

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderBy" && e.ErrorMessage == "ソート項目は必須です");
    }

    [Fact]
    public async Task Validate_ShouldSucceed_WhenOnlyRequiredParametersProvided()
    {
        // Arrange
        var query = new SearchProductsQuery();

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
