using FluentAssertions;
using Application.Features.GetProductById;

namespace ProductCatalog.Application.UnitTests;

public class GetProductByIdValidatorTests
{
    private readonly GetProductByIdValidator _validator;

    public GetProductByIdValidatorTests()
    {
        _validator = new GetProductByIdValidator();
    }

    [Fact]
    public async Task Validate_ShouldSucceed_WhenProductIdIsValid()
    {
        // Arrange
        var query = new GetProductByIdQuery(Guid.NewGuid());

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenProductIdIsEmpty()
    {
        // Arrange
        var query = new GetProductByIdQuery(Guid.Empty);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId" && e.ErrorMessage == "商品IDは必須です");
    }
}
