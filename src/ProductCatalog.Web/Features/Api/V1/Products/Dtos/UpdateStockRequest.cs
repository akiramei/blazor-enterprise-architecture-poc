using System.ComponentModel.DataAnnotations;

namespace ProductCatalog.Web.Features.Api.V1.Products.Dtos;

/// <summary>
/// 在庫更新リクエスト
/// </summary>
public sealed record UpdateStockRequest
{
    /// <summary>
    /// 新しい在庫数
    /// </summary>
    [Required(ErrorMessage = "Stock quantity is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be 0 or greater")]
    public int StockQuantity { get; init; }
}
