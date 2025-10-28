using Microsoft.EntityFrameworkCore;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Infrastructure.Persistence.Repositories;

/// <summary>
/// Product リポジトリのEF Core実装
/// </summary>
public sealed class EfProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public EfProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetAsync(ProductId id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => EF.Property<ProductId>(p, "_id") == id, cancellationToken);
    }

    public async Task SaveAsync(Product product, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync(product.Id, cancellationToken);

        if (existing is null)
        {
            await _context.Products.AddAsync(product, cancellationToken);
        }
        else
        {
            _context.Products.Update(product);
        }

        // SaveChanges is handled by TransactionBehavior
    }

    public Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Remove(product);
        // SaveChanges is handled by TransactionBehavior
        return Task.CompletedTask;
    }
}
