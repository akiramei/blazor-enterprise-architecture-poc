using Microsoft.EntityFrameworkCore;
using ProductCatalog.Application.Common.Interfaces;
using ProductCatalog.Application.Products.DTOs;

namespace ProductCatalog.Infrastructure.Persistence.Repositories;

/// <summary>
/// Product 読み取り専用リポジトリのEF Core実装
/// </summary>
public sealed class EfProductReadRepository : IProductReadRepository
{
    private readonly AppDbContext _context;

    public EfProductReadRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _context.Products
            .Select(p => new ProductDto(
                p.Id.Value,
                p.Name,
                p.Description,
                p.Price.Amount,
                p.Price.Currency,
                p.Stock,
                p.Price.ToDisplayString()
            ))
            .ToListAsync(cancellationToken);

        return products;
    }
}
