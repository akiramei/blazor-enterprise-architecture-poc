using FluentAssertions;
using Shared.Kernel;
using ProductCatalog.Shared.Domain.Products;

namespace ProductCatalog.Domain.UnitTests;

public class MoneyTests
{
    [Fact]
    public void Constructor_ShouldCreateMoneyWithValidParameters()
    {
        // Arrange & Act
        var money = new Money(1000, "JPY");

        // Assert
        money.Amount.Should().Be(1000);
        money.Currency.Should().Be("JPY");
    }

    [Fact]
    public void Add_ShouldAddTwoMoneyObjects_WhenCurrenciesAreSame()
    {
        // Arrange
        var money1 = new Money(1000, "JPY");
        var money2 = new Money(500, "JPY");

        // Act
        var result = money1 + money2;

        // Assert
        result.Amount.Should().Be(1500);
        result.Currency.Should().Be("JPY");
    }

    [Fact]
    public void Add_ShouldThrowDomainException_WhenCurrenciesDiffer()
    {
        // Arrange
        var money1 = new Money(1000, "JPY");
        var money2 = new Money(10, "USD");

        // Act
        var act = () => money1 + money2;

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("異なる通貨を加算できません");
    }

    [Fact]
    public void ToDisplayString_ShouldFormatJPYCorrectly()
    {
        // Arrange
        var money = new Money(150000, "JPY");

        // Act
        var result = money.ToDisplayString();

        // Assert
        result.Should().Be("¥150,000");
    }

    [Fact]
    public void ToDisplayString_ShouldFormatUSDCorrectly()
    {
        // Arrange
        var money = new Money(99.99m, "USD");

        // Act
        var result = money.ToDisplayString();

        // Assert
        result.Should().Be("$99.99");
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenMoneyObjectsAreEqual()
    {
        // Arrange
        var money1 = new Money(1000, "JPY");
        var money2 = new Money(1000, "JPY");

        // Act & Assert
        money1.Should().Be(money2);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenAmountsAreDifferent()
    {
        // Arrange
        var money1 = new Money(1000, "JPY");
        var money2 = new Money(500, "JPY");

        // Act & Assert
        money1.Should().NotBe(money2);
    }
}
