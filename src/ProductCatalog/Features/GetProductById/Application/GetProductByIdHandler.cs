using MediatR;
using ProductCatalog.Shared.Application.DTOs;
using Microsoft.Extensions.Logging;
using Shared.Application;
using ProductCatalog.Shared.Domain.Products;

namespace GetProductById.Application;

/// <summary>
/// 商品単一取得Handler
///
/// 【パターン: 単一取得Handler】
///
/// 処理フロー:
/// 1. Repositoryから集約を取得（子エンティティも含む）
/// 2. 存在チェック
/// 3. DTOに変換
/// 4. 結果を返す
///
/// 実装ガイド:
/// - Repository経由で集約全体を取得（Include処理）
/// - 存在しない場合はFailureを返す（nullではなく）
/// - DTOに変換して返す（Domainエンティティをそのまま返さない）
/// - キャッシュは自動的に効く（CachingBehavior）
///
/// AI実装時の注意:
/// - 参照系なので、Repositoryの読み取り専用メソッドを使用
/// - 複雑なクエリが必要な場合はRead Model（Dapper）を検討
/// - ログは適切なレベルで出力
/// - 例外は適切にハンドリング
/// </summary>
public sealed class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDetailDto>>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<GetProductByIdHandler> _logger;

    public GetProductByIdHandler(
        IProductRepository repository,
        ILogger<GetProductByIdHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<ProductDetailDto>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken)
    {
        // Repository経由で集約を取得（子エンティティ（Images）も含む）
        var product = await _repository.GetAsync(new ProductId(query.ProductId), cancellationToken);

        if (product is null)
        {
            _logger.LogWarning("商品が見つかりません: {ProductId}", query.ProductId);
            return Result.Fail<ProductDetailDto>("商品が見つかりません");
        }

        // DTOに変換
        var dto = ProductDetailDto.FromDomain(product);

        _logger.LogInformation("商品を取得しました: {ProductId}, Name: {Name}", query.ProductId, product.Name);

        return Result.Success(dto);
    }
}
