using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ImportProductsFromCsv.Application;
using Domain.ProductCatalog.Products;

namespace ProductCatalog.Application.UnitTests;

public class ImportProductsFromCsvHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<ILogger<ImportProductsFromCsvHandler>> _loggerMock;
    private readonly ImportProductsFromCsvHandler _handler;

    public ImportProductsFromCsvHandlerTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _loggerMock = new Mock<ILogger<ImportProductsFromCsvHandler>>();
        _handler = new ImportProductsFromCsvHandler(
            _repositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldImportAllProducts_WhenCsvIsValid()
    {
        // Arrange
        var csvContent = @"Name,Description,Price,Currency,Stock
商品A,説明A,1000,JPY,10
商品B,説明B,2000,JPY,20";

        var stream = CreateStreamFromString(csvContent);
        var command = new ImportProductsFromCsvCommand(stream);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SucceededCount.Should().Be(2);
        result.Value.FailedCount.Should().Be(0);
        result.Value.IsAllSucceeded.Should().BeTrue();

        _repositoryMock.Verify(r => r.SaveAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCsvIsEmpty()
    {
        // Arrange
        var csvContent = @"Name,Description,Price,Currency,Stock";

        var stream = CreateStreamFromString(csvContent);
        var command = new ImportProductsFromCsvCommand(stream);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("データがありません");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenExceedsMaxCount()
    {
        // Arrange
        var csvBuilder = new StringBuilder("Name,Description,Price,Currency,Stock\n");
        for (int i = 0; i < 1001; i++)
        {
            csvBuilder.AppendLine($"商品{i},説明{i},1000,JPY,10");
        }

        var stream = CreateStreamFromString(csvBuilder.ToString());
        var command = new ImportProductsFromCsvCommand(stream);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("上限");
    }

    [Fact]
    public async Task Handle_ShouldRejectInvalidRows_AndContinueProcessing()
    {
        // Arrange
        var csvContent = @"Name,Description,Price,Currency,Stock
商品A,説明A,1000,JPY,10
,説明B,2000,JPY,20
商品C,説明C,0,JPY,30
商品D,説明D,3000,JPY,-5
商品E,説明E,4000,JPY,40";

        var stream = CreateStreamFromString(csvContent);
        var command = new ImportProductsFromCsvCommand(stream);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SucceededCount.Should().Be(2); // 商品A, 商品E
        result.Value.FailedCount.Should().Be(3);
        result.Value.Errors.Should().HaveCount(3);
        result.Value.Errors.Should().Contain(e => e.Contains("行3")); // 空の商品名
        result.Value.Errors.Should().Contain(e => e.Contains("行4")); // 価格0
        result.Value.Errors.Should().Contain(e => e.Contains("行5")); // 負の在庫
    }

    [Fact]
    public async Task Handle_ShouldValidateRequiredFields()
    {
        // Arrange
        var csvContent = @"Name,Description,Price,Currency,Stock
,説明,1000,JPY,10";

        var stream = CreateStreamFromString(csvContent);
        var command = new ImportProductsFromCsvCommand(stream);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.SucceededCount.Should().Be(0);
        result.Value.FailedCount.Should().Be(1);
        result.Value.Errors.Should().Contain(e => e.Contains("商品名は必須"));
    }

    [Fact]
    public async Task Handle_ShouldValidatePriceIsPositive()
    {
        // Arrange
        var csvContent = @"Name,Description,Price,Currency,Stock
商品A,説明,0,JPY,10
商品B,説明,-100,JPY,20";

        var stream = CreateStreamFromString(csvContent);
        var command = new ImportProductsFromCsvCommand(stream);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.SucceededCount.Should().Be(0);
        result.Value.FailedCount.Should().Be(2);
        result.Value.Errors.Should().Contain(e => e.Contains("価格は0より大きい値"));
    }

    [Fact]
    public async Task Handle_ShouldValidateStockIsNonNegative()
    {
        // Arrange
        var csvContent = @"Name,Description,Price,Currency,Stock
商品A,説明,1000,JPY,-10";

        var stream = CreateStreamFromString(csvContent);
        var command = new ImportProductsFromCsvCommand(stream);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.SucceededCount.Should().Be(0);
        result.Value.FailedCount.Should().Be(1);
        result.Value.Errors.Should().Contain(e => e.Contains("在庫数は0以上"));
    }

    [Fact]
    public async Task Handle_ShouldDefaultToJPY_WhenCurrencyIsEmpty()
    {
        // Arrange
        var csvContent = @"Name,Description,Price,Currency,Stock
商品A,説明,1000,,10";

        var stream = CreateStreamFromString(csvContent);
        var command = new ImportProductsFromCsvCommand(stream);

        var savedProduct = (Product?)null;
        _repositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, ct) => savedProduct = p);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.SucceededCount.Should().Be(1);
        savedProduct.Should().NotBeNull();
        savedProduct!.Price.Currency.Should().Be("JPY");
    }

    [Fact]
    public async Task Handle_ShouldTrackErrorsWithLineNumbers()
    {
        // Arrange
        var csvContent = @"Name,Description,Price,Currency,Stock
商品A,説明,1000,JPY,10
,説明,2000,JPY,20
商品C,説明,0,JPY,30";

        var stream = CreateStreamFromString(csvContent);
        var command = new ImportProductsFromCsvCommand(stream);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Errors.Should().Contain(e => e.Contains("行3")); // 空の商品名
        result.Value.Errors.Should().Contain(e => e.Contains("行4")); // 価格0
    }

    [Fact]
    public async Task Handle_ShouldContinueOnDomainException()
    {
        // Arrange
        var csvContent = @"Name,Description,Price,Currency,Stock
商品A,説明,1000,JPY,10
商品B,説明,2000,JPY,20";

        var stream = CreateStreamFromString(csvContent);
        var command = new ImportProductsFromCsvCommand(stream);

        var saveCount = 0;
        _repositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                saveCount++;
                if (saveCount == 1)
                {
                    throw new Exception("Database error");
                }
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.SucceededCount.Should().Be(1); // 2件目は成功
        result.Value.FailedCount.Should().Be(1); // 1件目は失敗
    }

    private static Stream CreateStreamFromString(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new MemoryStream(bytes);
    }
}
