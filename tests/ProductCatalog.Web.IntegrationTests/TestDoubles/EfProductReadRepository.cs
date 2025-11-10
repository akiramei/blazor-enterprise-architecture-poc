using Microsoft.EntityFrameworkCore;
using ProductCatalog.Shared.Application;
using ProductCatalog.Shared.Application.DTOs;
using ProductCatalog.Shared.Domain.Products;
using ProductCatalog.Shared.Infrastructure.Persistence;
using Shared.Application.Common;

namespace ProductCatalog.Web.IntegrationTests.TestDoubles;

/// <summary>
/// テスト用のEF Core実装のProductReadRepository
/// </summary>
public class EfProductReadRepository : IProductReadRepository
{
    private readonly ProductCatalogDbContext _context;

    public EfProductReadRepository(ProductCatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var baseQuery = _context.Products
            .IgnoreQueryFilters()
            .Where(p => !EF.Property<bool>(p, "_isDeleted"));

        var products = await baseQuery
            .OrderBy(p => EF.Property<string>(p, "_name"))
            .ToListAsync(cancellationToken);

        return products.Select(p => new ProductDto(
            p.Id.Value,
            p.Name,
            p.Description,
            p.Price.Amount,
            p.Price.Currency,
            p.Stock,
            // DisplayPrice計算
            p.Price.Currency == "JPY" ? $"¥{p.Price.Amount:N0}" :
            p.Price.Currency == "USD" ? $"${p.Price.Amount:N2}" :
            $"{p.Price.Amount:N2} {p.Price.Currency}",
            p.Status.ToString(),
            (int)p.Version
        ));
    }

    public async Task<ProductDetailDto?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // ProductIdはHasConversionでGuidに変換されているため、ProductIdとして比較
        var targetId = new ProductId(productId);

        // Versionはshadow propertyのため、EF.Propertyで明示的に取得
        var result = await _context.Products
            .IgnoreQueryFilters()
            .Where(p => EF.Property<ProductId>(p, "_id") == targetId)
            .Where(p => !EF.Property<bool>(p, "_isDeleted"))
            .Select(p => new
            {
                Product = p,
                Version = EF.Property<long>(p, "Version")
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            return null;
        }

        var product = result.Product;
        return new ProductDetailDto
        {
            Id = product.Id.Value,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price.Amount,
            Stock = product.Stock,
            Status = product.Status.ToString(),
            IsDeleted = product.IsDeleted,
            Version = result.Version,  // shadow propertyから取得した値を使用
            Images = product.Images
                .Select(img => new ProductImageDto
                {
                    Id = img.Id.Value,
                    Url = img.Url,
                    DisplayOrder = img.DisplayOrder
                })
                .ToList()
        };
    }

    public async Task<PagedResult<ProductDto>> SearchAsync(
        string? nameFilter,
        decimal? minPrice,
        decimal? maxPrice,
        ProductStatus? status,
        int page,
        int pageSize,
        string orderBy,
        bool isDescending,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .IgnoreQueryFilters()
            .Where(p => !EF.Property<bool>(p, "_isDeleted"));

        // フィルター適用
        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            query = query.Where(p =>
                EF.Property<string>(p, "_name").Contains(nameFilter) ||
                EF.Property<string>(p, "_description").Contains(nameFilter));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price.Amount >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price.Amount <= maxPrice.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        // ソート
        query = orderBy.ToLower() switch
        {
            "price" => isDescending ? query.OrderByDescending(p => p.Price.Amount) : query.OrderBy(p => p.Price.Amount),
            "stock" => isDescending ? query.OrderByDescending(p => EF.Property<int>(p, "_stock")) : query.OrderBy(p => EF.Property<int>(p, "_stock")),
            _ => isDescending ? query.OrderByDescending(p => EF.Property<string>(p, "_name")) : query.OrderBy(p => EF.Property<string>(p, "_name"))
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = products.Select(p => new ProductDto(
            p.Id.Value,
            p.Name,
            p.Description,
            p.Price.Amount,
            p.Price.Currency,
            p.Stock,
            // DisplayPrice計算
            p.Price.Currency == "JPY" ? $"¥{p.Price.Amount:N0}" :
            p.Price.Currency == "USD" ? $"${p.Price.Amount:N2}" :
            $"{p.Price.Amount:N2} {p.Price.Currency}",
            p.Status.ToString(),
            (int)p.Version
        )).ToList();

        return new PagedResult<ProductDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }
}
