using FluentAssertions;
using CreateProduct.Application;

namespace ProductCatalog.Application.UnitTests;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductValidator _validator;

    public CreateProductCommandValidatorTests()
    {
        _validator = new CreateProductValidator();
    }

    [Fact]
    public async Task Validate_ShouldSucceed_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateProductCommand("商品名", "商品説明", 1000, "JPY", 10);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenNameIsEmpty()
    {
        // Arrange
        var command = new CreateProductCommand("", "商品説明", 1000, "JPY", 10);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "商品名は必須です");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenNameIsTooLong()
    {
        // Arrange
        var longName = new string('あ', 201);
        var command = new CreateProductCommand(longName, "商品説明", 1000, "JPY", 10);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "商品名は200文字以内で入力してください");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenPriceIsZero()
    {
        // Arrange
        var command = new CreateProductCommand("商品名", "商品説明", 0, "JPY", 10);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price" && e.ErrorMessage == "価格は0より大きい値を設定してください");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenPriceIsNegative()
    {
        // Arrange
        var command = new CreateProductCommand("商品名", "商品説明", -100, "JPY", 10);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenStockIsNegative()
    {
        // Arrange
        var command = new CreateProductCommand("商品名", "商品説明", 1000, "JPY", -1);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "InitialStock" && e.ErrorMessage == "在庫数は0以上である必要があります");
    }
}
