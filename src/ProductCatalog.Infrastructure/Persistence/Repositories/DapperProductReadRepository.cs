using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Common.Interfaces;
using ProductCatalog.Application.Products.DTOs;

namespace ProductCatalog.Infrastructure.Persistence.Repositories;

/// <summary>
/// Dapper を使用した高速読み取り専用リポジトリ（PostgreSQL対応）
/// </summary>
public sealed class DapperProductReadRepository : IProductReadRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<DapperProductReadRepository> _logger;

    public DapperProductReadRepository(
        AppDbContext context,
        ILogger<DapperProductReadRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("商品一覧を取得（Dapper使用）");

        using var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT
                ""Id"",
                ""Name"",
                ""Description"",
                ""Price"",
                ""Currency"",
                ""Stock""
            FROM ""Products""
            WHERE ""IsDeleted"" = false
            ORDER BY ""Name""";

        // CommandDefinition を使用してCancellationTokenを伝播
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<ProductDapperDto>(command);

        var products = results.Select(r => new ProductDto(
            r.Id,
            r.Name,
            r.Description,
            r.Price ?? 0,
            r.Currency ?? "JPY",
            r.Stock,
            // DisplayPriceはDTOで計算
            (r.Currency ?? "JPY") == "JPY" ? $"¥{r.Price:N0}" :
            (r.Currency ?? "JPY") == "USD" ? $"${r.Price:N2}" :
            $"{r.Price:N2} {r.Currency}"
        )).ToList();

        _logger.LogInformation("商品一覧を取得しました: {Count}件", products.Count);

        return products;
    }

    // Dapper用のDTO（DBカラムと1:1マッピング）
    private sealed record ProductDapperDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal? Price { get; init; }
        public string? Currency { get; init; }
        public int Stock { get; init; }
    }
}
