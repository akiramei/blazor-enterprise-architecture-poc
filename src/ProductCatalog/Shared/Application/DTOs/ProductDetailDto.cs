using Domain.ProductCatalog.Products;

namespace ProductCatalog.Shared.Application.DTOs;

/// <summary>
/// 商品詳細DTO
///
/// 【パターン: 詳細情報用DTO】
///
/// 使用シナリオ:
/// - 詳細画面で表示する場合
/// - 編集画面で初期値として使用する場合
/// - 関連データ（画像など）も含めて返す必要がある場合
///
/// 実装ガイド:
/// - 一覧用DTO（ProductDto）より多くの情報を含む
/// - 子エンティティの情報も含める（ProductImages）
/// - 楽観的排他制御用のVersionを含める
/// - FromDomain()メソッドでDomainエンティティから変換
///
/// AI実装時の注意:
/// - 画面表示に必要な情報のみを含める（不要な内部情報は含めない）
/// - DTOはイミュータブル（record）
/// - ネストしたDTOは適切に分離（ProductImageDto）
/// </summary>
public sealed record ProductDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsDeleted { get; init; }
    public long Version { get; init; }  // 楽観的排他制御用
    public IReadOnlyList<ProductImageDto> Images { get; init; } = Array.Empty<ProductImageDto>();

    /// <summary>
    /// Domainエンティティから変換
    /// </summary>
    public static ProductDetailDto FromDomain(Product product)
    {
        return new ProductDetailDto
        {
            Id = product.Id.Value,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price.Amount,
            Stock = product.Stock,
            Status = product.Status.ToString(),
            IsDeleted = product.IsDeleted,
            Version = product.Version,
            Images = product.Images
                .Select(ProductImageDto.FromDomain)
                .ToList()
        };
    }
}

/// <summary>
/// 商品画像DTO
/// </summary>
public sealed record ProductImageDto
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }

    /// <summary>
    /// Domainエンティティから変換
    /// </summary>
    public static ProductImageDto FromDomain(ProductImage image)
    {
        return new ProductImageDto
        {
            Id = image.Id.Value,
            Url = image.Url,
            DisplayOrder = image.DisplayOrder
        };
    }
}
