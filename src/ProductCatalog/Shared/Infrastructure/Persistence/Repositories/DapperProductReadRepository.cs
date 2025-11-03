using System.Text;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Common.Interfaces;
using ProductCatalog.Application.Features.Products.GetProductById;
using ProductCatalog.Application.Products.DTOs;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Infrastructure.Persistence.Repositories;

/// <summary>
/// Dapper を使用した高速読み取り専用リポジトリ（PostgreSQL対応）
///
/// 【パターン: Dapper Read Repository】
///
/// 責務:
/// - 参照系クエリを高速に実行
/// - 複雑なJOINやフィルタリングに対応
/// - DTOへの直接マッピング
///
/// 実装ガイド:
/// - 生SQLを使用して最適化
/// - パラメータ化クエリでSQLインジェクション対策
/// - 複雑なクエリは動的SQLビルダーを使用
/// - マルチマッピングで親子関係をマッピング
///
/// AI実装時の注意:
/// - 必ずパラメータ化クエリを使用（文字列連結は禁止）
/// - OrderByのカラム名はホワイトリスト検証
/// - ページングはOFFSET/FETCHで実装（PostgreSQL/SQL Server）
/// - 総件数取得は別クエリで効率化
/// </summary>
public sealed class DapperProductReadRepository : IProductReadRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<DapperProductReadRepository> _logger;

    public DapperProductReadRepository(
        AppDbContext context,
        ILogger<DapperProductReadRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("商品一覧を取得（Dapper使用）");

        using var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT
                ""Id"",
                ""Name"",
                ""Description"",
                ""Price"",
                ""Currency"",
                ""Stock"",
                ""Status"",
                ""Version""
            FROM ""Products""
            WHERE ""IsDeleted"" = false
            ORDER BY ""Name""";

        // CommandDefinition を使用してCancellationTokenを伝播
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<ProductDapperDto>(command);

        var products = results.Select(r => new ProductDto(
            r.Id,
            r.Name,
            r.Description,
            r.Price ?? 0,
            r.Currency ?? "JPY",
            r.Stock,
            // DisplayPriceはDTOで計算
            (r.Currency ?? "JPY") == "JPY" ? $"¥{r.Price:N0}" :
            (r.Currency ?? "JPY") == "USD" ? $"${r.Price:N2}" :
            $"{r.Price:N2} {r.Currency}",
            r.Status?.ToString() ?? "Draft",
            r.Version
        )).ToList();

        _logger.LogInformation("商品一覧を取得しました: {Count}件", products.Count);

        return products;
    }

    public async Task<ProductDetailDto?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("商品詳細を取得（ID: {ProductId}）", productId);

        using var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        // 商品情報とProductImagesを取得（LEFT JOIN）
        const string sql = @"
            SELECT
                p.""Id"",
                p.""Name"",
                p.""Description"",
                p.""Price"",
                p.""Currency"",
                p.""Stock"",
                p.""Status"",
                p.""IsDeleted"",
                p.""Version"",
                pi.""Id"" AS ImageId,
                pi.""Url"",
                pi.""DisplayOrder""
            FROM ""Products"" p
            LEFT JOIN ""ProductImages"" pi ON p.""Id"" = pi.""ProductId""
            WHERE p.""Id"" = @ProductId
            ORDER BY pi.""DisplayOrder""";

        var command = new CommandDefinition(sql, new { ProductId = productId }, cancellationToken: cancellationToken);

        // Multi-mapping: 1つのProductに複数のProductImageをマッピング
        var productDictionary = new Dictionary<Guid, ProductDetailDto>();

        await connection.QueryAsync<ProductDetailDapperDto, ProductImageDapperDto?, ProductDetailDto>(
            command,
            (product, image) =>
            {
                // 初回のレコードでProductDetailDtoを作成
                if (!productDictionary.TryGetValue(product.Id, out var productDto))
                {
                    productDto = new ProductDetailDto
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Description = product.Description,
                        Price = product.Price ?? 0,
                        Stock = product.Stock,
                        Status = product.Status.ToString(),
                        IsDeleted = product.IsDeleted,
                        Version = product.Version,
                        Images = new List<ProductImageDto>()
                    };
                    productDictionary.Add(product.Id, productDto);
                }

                // 画像が存在する場合は追加
                if (image?.ImageId != null)
                {
                    var images = (List<ProductImageDto>)productDto.Images;
                    images.Add(new ProductImageDto
                    {
                        Id = image.ImageId.Value,
                        Url = image.Url ?? string.Empty,
                        DisplayOrder = image.DisplayOrder
                    });
                }

                return productDto;
            },
            splitOn: "ImageId");

        var result = productDictionary.Values.FirstOrDefault();

        if (result == null)
        {
            _logger.LogWarning("商品が見つかりません（ID: {ProductId}）", productId);
        }
        else
        {
            _logger.LogInformation("商品詳細を取得しました（ID: {ProductId}, 画像数: {ImageCount}）",
                productId, result.Images.Count);
        }

        return result;
    }

    public async Task<PagedResult<ProductDto>> SearchAsync(
        string? nameFilter,
        decimal? minPrice,
        decimal? maxPrice,
        ProductStatus? status,
        int page,
        int pageSize,
        string orderBy,
        bool isDescending,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("商品検索（NameFilter: {NameFilter}, Page: {Page}, PageSize: {PageSize}）",
            nameFilter, page, pageSize);

        using var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        // 動的WHERE句の構築
        var whereBuilder = new StringBuilder("WHERE \"IsDeleted\" = false");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            whereBuilder.Append(" AND \"Name\" ILIKE @NameFilter");
            parameters.Add("NameFilter", $"%{nameFilter}%");
        }

        if (minPrice.HasValue)
        {
            whereBuilder.Append(" AND \"Price\" >= @MinPrice");
            parameters.Add("MinPrice", minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            whereBuilder.Append(" AND \"Price\" <= @MaxPrice");
            parameters.Add("MaxPrice", maxPrice.Value);
        }

        if (status.HasValue)
        {
            whereBuilder.Append(" AND \"Status\" = @Status");
            parameters.Add("Status", (int)status.Value);
        }

        var whereClause = whereBuilder.ToString();

        // ソート項目の検証（SQLインジェクション対策）
        var validOrderByColumns = new[] { "Name", "Price", "Stock", "Status" };
        if (!validOrderByColumns.Contains(orderBy))
        {
            orderBy = "Name";  // デフォルト
            _logger.LogWarning("無効なOrderBy指定: {OrderBy} -> デフォルト（Name）に変更", orderBy);
        }

        var orderDirection = isDescending ? "DESC" : "ASC";

        // 総件数を取得
        var countSql = $@"
            SELECT COUNT(*)
            FROM ""Products""
            {whereClause}";

        var countCommand = new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken);
        var totalCount = await connection.ExecuteScalarAsync<int>(countCommand);

        // データを取得（ページング）
        var offset = (page - 1) * pageSize;
        var dataSql = $@"
            SELECT
                ""Id"",
                ""Name"",
                ""Description"",
                ""Price"",
                ""Currency"",
                ""Stock"",
                ""Status"",
                ""Version""
            FROM ""Products""
            {whereClause}
            ORDER BY ""{orderBy}"" {orderDirection}
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY";

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var dataCommand = new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<ProductDapperDto>(dataCommand);

        var products = results.Select(r => new ProductDto(
            r.Id,
            r.Name,
            r.Description,
            r.Price ?? 0,
            r.Currency ?? "JPY",
            r.Stock,
            // DisplayPriceはDTOで計算
            (r.Currency ?? "JPY") == "JPY" ? $"¥{r.Price:N0}" :
            (r.Currency ?? "JPY") == "USD" ? $"${r.Price:N2}" :
            $"{r.Price:N2} {r.Currency}",
            r.Status?.ToString() ?? "Draft",
            r.Version
        )).ToList();

        _logger.LogInformation("商品検索完了（Total: {TotalCount}, Page: {Page}/{TotalPages}）",
            totalCount, page, (int)Math.Ceiling((double)totalCount / pageSize));

        return new PagedResult<ProductDto>
        {
            Items = products,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    // Dapper用のDTO（DBカラムと1:1マッピング）
    private sealed record ProductDapperDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal? Price { get; init; }
        public string? Currency { get; init; }
        public int Stock { get; init; }
        public ProductStatus? Status { get; init; }
        public int Version { get; init; }
    }

    private sealed record ProductDetailDapperDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal? Price { get; init; }
        public string? Currency { get; init; }
        public int Stock { get; init; }
        public ProductStatus Status { get; init; }
        public bool IsDeleted { get; init; }
        public long Version { get; init; }
    }

    private sealed record ProductImageDapperDto
    {
        public Guid? ImageId { get; init; }
        public string? Url { get; init; }
        public int DisplayOrder { get; init; }
    }
}
