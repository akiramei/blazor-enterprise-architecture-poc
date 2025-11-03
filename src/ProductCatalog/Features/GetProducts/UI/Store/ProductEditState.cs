using ProductCatalog.Application.Features.Products.GetProductById;

namespace ProductCatalog.Web.Features.Products.Store;

/// <summary>
/// 商品編集画面の状態（不変）
/// </summary>
public sealed record ProductEditState
{
    public static readonly ProductEditState Empty = new();

    public ProductDetailDto? Product { get; init; }
    public bool IsLoading { get; init; }
    public bool IsSaving { get; init; }
    public string? ErrorMessage { get; init; }
    public string? SuccessMessage { get; init; }
}
