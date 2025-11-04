using MediatR;
using ProductCatalog.Shared.Application.DTOs;
using Microsoft.Extensions.Logging;
using Shared.Application;
using ProductCatalog.Shared.Application;

namespace GetProductById.Application;

/// <summary>
/// 商品単一取得Handler
///
/// 【パターン: 単一取得Handler】
///
/// 処理フロー:
/// 1. Read Repositoryから商品詳細DTOを取得
/// 2. 存在チェック
/// 3. 結果を返す
///
/// 実装ガイド:
/// - 参照系なのでRead Repository（Dapper）を使用
/// - 存在しない場合はFailureを返す（nullではなく）
/// - DTOで直接返す（Domain層を経由しない）
/// - キャッシュは自動的に効く（CachingBehavior）
///
/// AI実装時の注意:
/// - 参照系なので、IProductReadRepositoryを使用
/// - 更新系の操作が必要な場合はIProductRepositoryを検討
/// - ログは適切なレベルで出力
/// - 例外は適切にハンドリング
/// </summary>
public sealed class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDetailDto>>
{
    private readonly IProductReadRepository _readRepository;
    private readonly ILogger<GetProductByIdHandler> _logger;

    public GetProductByIdHandler(
        IProductReadRepository readRepository,
        ILogger<GetProductByIdHandler> logger)
    {
        _readRepository = readRepository;
        _logger = logger;
    }

    public async Task<Result<ProductDetailDto>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Handler] GetProductByIdHandler started for ProductId: {ProductId}", query.ProductId);

        // Read Repository経由でDTOを直接取得
        _logger.LogInformation("[Handler] Calling ReadRepository.GetByIdAsync");
        var productDto = await _readRepository.GetByIdAsync(query.ProductId, cancellationToken);

        if (productDto is null)
        {
            _logger.LogWarning("[Handler] 商品が見つかりません: {ProductId}", query.ProductId);
            return Result.Fail<ProductDetailDto>("商品が見つかりません");
        }

        _logger.LogInformation("[Handler] 商品を取得しました: {ProductId}, Name: {Name}", query.ProductId, productDto.Name);

        return Result.Success(productDto);
    }
}
