using ProductCatalog.Application.Products.DTOs;

namespace ProductCatalog.Application.Common.Interfaces;

/// <summary>
/// 商品読み取り専用リポジトリインターフェース
/// </summary>
public interface IProductReadRepository
{
    Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
