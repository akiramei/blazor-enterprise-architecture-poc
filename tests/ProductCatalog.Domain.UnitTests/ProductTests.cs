using FluentAssertions;
using ProductCatalog.Domain.Common;
using ProductCatalog.Domain.Products;
using ProductCatalog.Domain.Products.Events;

namespace ProductCatalog.Domain.UnitTests;

public class ProductTests
{
    [Fact]
    public void Create_ShouldCreateProductWithValidParameters()
    {
        // Arrange
        var name = "テスト商品";
        var description = "テスト説明";
        var price = new Money(1000, "JPY");
        var stock = 10;

        // Act
        var product = Product.Create(name, description, price, stock);

        // Assert
        product.Should().NotBeNull();
        product.Name.Should().Be(name);
        product.Description.Should().Be(description);
        product.Price.Should().Be(price);
        product.Stock.Should().Be(stock);
        product.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Delete_ShouldMarkProductAsDeleted_WhenStockIsZero()
    {
        // Arrange
        var product = Product.Create("商品", "説明", new Money(1000, "JPY"), 0);

        // Act
        product.Delete();

        // Assert
        product.IsDeleted.Should().BeTrue();
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductDeletedDomainEvent>();
    }

    [Fact]
    public void Delete_ShouldThrowDomainException_WhenStockIsNotZero()
    {
        // Arrange
        var product = Product.Create("商品", "説明", new Money(1000, "JPY"), 10);

        // Act
        var act = () => product.Delete();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("在庫がある商品は削除できません。現在在庫: 10");
        product.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Delete_ShouldThrowDomainException_WhenAlreadyDeleted()
    {
        // Arrange
        var product = Product.Create("商品", "説明", new Money(1000, "JPY"), 0);
        product.Delete();

        // Act
        var act = () => product.Delete();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("既に削除されています");
    }
}
