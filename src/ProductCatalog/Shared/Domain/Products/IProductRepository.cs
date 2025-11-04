namespace ProductCatalog.Shared.Domain.Products;

/// <summary>
/// 商品リポジトリインターフェース
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// IDで商品を取得
    /// </summary>
    Task<Product?> GetAsync(ProductId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 商品を保存（追加または更新）
    /// </summary>
    Task SaveAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// 商品を削除
    /// </summary>
    Task DeleteAsync(Product product, CancellationToken cancellationToken = default);
}
