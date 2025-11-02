using System.ComponentModel.DataAnnotations;

namespace ProductCatalog.Web.Features.Api.V1.Products.Dtos;

/// <summary>
/// 商品作成リクエスト
/// </summary>
public sealed record CreateProductRequest
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
    [Required(ErrorMessage = "Price amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal PriceAmount { get; init; }

    /// <summary>
    /// 通貨コード（例: JPY, USD）
    /// </summary>
    [Required(ErrorMessage = "Price currency is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be 3 characters")]
    public string PriceCurrency { get; init; } = default!;

    /// <summary>
    /// 在庫数
    /// </summary>
    [Required(ErrorMessage = "Stock quantity is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be 0 or greater")]
    public int StockQuantity { get; init; }
}
