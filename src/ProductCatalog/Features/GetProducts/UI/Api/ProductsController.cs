using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Application;
using Shared.Application.Common;
using Application.Features.ProductCatalog.CreateProduct;
using Application.Features.ProductCatalog.DeleteProduct;
using Application.Features.ProductCatalog.GetProductById;
using Application.Features.ProductCatalog.GetProducts;
using Application.Features.ProductCatalog.SearchProducts;
using Application.Features.ProductCatalog.UpdateProduct;
using ProductCatalog.Shared.Application.DTOs;

namespace ProductCatalog.Features.Api.V1.Products;

/// <summary>
/// 商品API
///
/// 【パターン: MediatR統合REST APIController】
///
/// このControllerは、既存のMediatR Command/Queryパターンを活用し、
/// REST APIエンドポイントとして公開します。
///
/// ## 設計判断
///
/// ### なぜController-based？Minimal APIではない？
/// - **エンドポイント数が多い**: 10個以上のエンドポイント
/// - **Swagger生成が容易**: 属性ベースのドキュメント生成
/// - **バージョニングが統一的**: `[ApiVersion]`属性で一元管理
/// - **既存パターンとの整合性**: Blazor Serverとのコード共有
///
/// ### MediatRとの統合メリット
/// - **Controllerはシン（Thin）**: ビジネスロジックはHandlerに集約
/// - **Pipeline Behaviorsが自動適用**: Validation, Authorization, Logging, Caching等
/// - **既存実装の再利用**: Blazor ServerとAPIで同じHandlerを使用
/// - **テスト容易性**: Handlerを直接テスト可能
///
/// ## Pipeline Behaviors自動適用
///
/// すべてのAPI呼び出しは以下のBehaviorsを自動的に通過します:
/// 1. **MetricsBehavior**: 実行時間・成功率を計測
/// 2. **LoggingBehavior**: リクエスト/レスポンスをログ記録
/// 3. **ValidationBehavior**: FluentValidationで入力検証
/// 4. **AuthorizationBehavior**: ロールベース認可チェック
/// 5. **IdempotencyBehavior**: 冪等性キーで重複実行を防止
/// 6. **CachingBehavior**: Queryの結果をキャッシュ
/// 7. **AuditLogBehavior**: Commandの実行を監査ログに記録
/// 8. **TransactionBehavior**: Commandをトランザクション内で実行
///
/// Controllerで実装する必要はありません！
///
/// ## APIクライアントへの契約
///
/// 1. **認証**: すべてのエンドポイントはJWT Bearer認証が必要
///    - `Authorization: Bearer {token}` ヘッダーを含める
///
/// 2. **楽観的排他制御**: 更新系エンドポイント（PUT）
///    - リクエストに `version` フィールドを含める
///    - 競合時は 409 Conflict が返される
///
/// 3. **冪等性**: 作成系エンドポイント（POST）
///    - リクエストに `idempotencyKey` を含める（推奨）
///    - 同じキーでのリトライは重複作成を防止
///
/// 4. **エラーハンドリング**: RFC 7807 Problem Details
///    - すべてのエラーは統一フォーマット
///    - `status`, `title`, `detail` フィールドを参照
///
/// ## レスポンスパターン
///
/// | HTTPメソッド | 成功 | エラー例 |
/// |-------------|------|---------|
/// | GET | 200 OK + データ | 404 Not Found |
/// | POST | 201 Created + ID | 400 Bad Request (Validation) |
/// | PUT | 204 No Content | 409 Conflict (Version) |
/// | DELETE | 204 No Content | 404 Not Found |
///
/// ## AI実装時の注意
///
/// - **Controllerはシンに保つ**: ビジネスロジックをControllerに書かない
/// - **MediatR経由で呼び出す**: Handlerを直接インスタンス化しない
/// - **Result<T>を適切に処理**: IsSuccessをチェックしてレスポンスを返す
/// - **DTOを使用**: Application層のDTOをそのまま返す（マッピング不要）
/// - **エラーはProblem Details**: 統一フォーマットで返す
///
/// ## 関連ドキュメント
/// - docs/patterns/REST-API-DESIGN-GUIDE.md - API設計ガイド
/// - docs/patterns/API-CLIENT-CONTRACT.md - クライアント契約
/// - docs/patterns/CQRS-PATTERN-GUIDE.md - Command/Query実装
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/products")]
[Authorize]  // ❗ JWT認証が必要（認証済みユーザーのみアクセス可）
public sealed class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IMediator mediator,
        ILogger<ProductsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// 商品一覧取得
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>商品一覧</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductsQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to retrieve products",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// 商品詳細取得
    /// </summary>
    /// <param name="id">商品ID</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>商品詳細</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailDto>> GetProductById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Product not found",
                Detail = $"Product with ID {id} was not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// 商品検索
    /// </summary>
    /// <param name="keyword">検索キーワード</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>検索結果</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> SearchProducts(
        [FromQuery] string keyword,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchProductsQuery(keyword);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Search failed",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// 商品作成
    /// </summary>
    /// <param name="request">商品作成リクエスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>作成された商品ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateProduct(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product creation failed",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var productId = result.Value;
        return CreatedAtAction(nameof(GetProductById), new { id = productId }, productId);
    }

    /// <summary>
    /// 商品更新
    /// </summary>
    /// <param name="id">商品ID</param>
    /// <param name="request">商品更新リクエスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>更新結果</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var commandWithId = new UpdateProductCommand(
            id,
            command.Name,
            command.Description,
            command.Price,
            command.Stock,
            command.Version);

        var result = await _mediator.Send(commandWithId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product update failed",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return NoContent();
    }

    /// <summary>
    /// 商品削除
    /// </summary>
    /// <param name="id">商品ID</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>削除結果</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteProductCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product deletion failed",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return NoContent();
    }
}
