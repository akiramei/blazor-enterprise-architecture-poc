using System.Collections.Immutable;
using ProductCatalog.Shared.Application.DTOs;

namespace ProductCatalog.Shared.UI.Store;

/// <summary>
/// 商品一覧画面の状態（不変）
/// </summary>
public sealed record ProductsState
{
    public static readonly ProductsState Empty = new();

    public ImmutableList<ProductDto> Products { get; init; } = ImmutableList<ProductDto>.Empty;
    public bool IsLoading { get; init; }
    public string? ErrorMessage { get; init; }
}
