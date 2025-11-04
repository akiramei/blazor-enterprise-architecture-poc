using ProductCatalog.Shared.Application.DTOs;

namespace ProductCatalog.Shared.UI.Store;

/// <summary>
/// 商品詳細画面の状態（不変）
/// </summary>
public sealed record ProductDetailState
{
    public static readonly ProductDetailState Empty = new();

    public ProductDetailDto? Product { get; init; }
    public bool IsLoading { get; init; }
    public string? ErrorMessage { get; init; }
}
