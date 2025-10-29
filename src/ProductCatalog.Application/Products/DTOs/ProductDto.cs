namespace ProductCatalog.Application.Products.DTOs;

/// <summary>
/// 商品DTO
/// </summary>
public sealed record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int Stock,
    string DisplayPrice,
    string Status,
    int Version);
