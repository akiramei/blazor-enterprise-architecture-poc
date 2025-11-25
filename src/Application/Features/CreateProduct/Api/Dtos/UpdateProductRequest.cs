using System.ComponentModel.DataAnnotations;

namespace ProductCatalog.Features.Api.V1.Products.Dtos;

/// <summary>
/// 商品更新リクエスト
/// </summary>
public sealed record UpdateProductRequest
{
    /// <summary>
    /// 商品名
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
    public string Name { get; init; } = default!;

    /// <summary>
    /// 商品説明
    /// </summary>
    [Required(ErrorMessage = "Description is required")]
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string Description { get; init; } = default!;

    /// <summary>
    /// 価格
    /// </summary>
    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; init; }

    /// <summary>
    /// 在庫数
    /// </summary>
    [Required(ErrorMessage = "Stock quantity is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be 0 or greater")]
    public int Stock { get; init; }

    /// <summary>
    /// バージョン（楽観的排他制御用）
    /// </summary>
    [Required(ErrorMessage = "Version is required")]
    public long Version { get; init; }
}
