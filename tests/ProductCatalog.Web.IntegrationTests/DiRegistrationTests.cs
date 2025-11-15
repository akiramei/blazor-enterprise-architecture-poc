using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;

namespace ProductCatalog.Web.IntegrationTests;

/// <summary>
/// DI登録を検証するテスト
/// </summary>
public class DiRegistrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DiRegistrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task IProductReadRepository_ShouldBeRegistered()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        // Act
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetService<ProductCatalog.Shared.Application.IProductReadRepository>();

        // Assert
        repository.Should().NotBeNull("IProductReadRepository should be registered in DI container");
        repository.Should().BeOfType<TestDoubles.EfProductReadRepository>("Should use test double implementation");
    }
}
