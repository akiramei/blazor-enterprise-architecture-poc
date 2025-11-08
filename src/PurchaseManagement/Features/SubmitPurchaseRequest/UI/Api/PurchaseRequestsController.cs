using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Application;
using Shared.Domain.Identity;
using SubmitPurchaseRequest.Application;
using ApprovePurchaseRequest.Application;
using RejectPurchaseRequest.Application;
using CancelPurchaseRequest.Application;
using GetPurchaseRequests.Application;
using GetPurchaseRequestById.Application;
using GetPendingApprovals.Application;
using GetDashboardStatistics.Application;

namespace PurchaseManagement.Features.Api.V1.PurchaseRequests;

/// <summary>
/// 購買申請API
///
/// 【パターン: MediatR統合REST APIController】
///
/// 購買申請の提出・承認・却下・照会機能を提供するREST APIエンドポイント。
/// すべてのビジネスロジックはMediatR Handlerに集約されており、
/// Controllerはシン（Thin）なレイヤーとして機能します。
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
/// ## レスポンスパターン
///
/// | HTTPメソッド | エンドポイント | 成功 | エラー例 |
/// |-------------|-------------|------|---------|
/// | GET | /api/v1/purchase-requests | 200 OK + データ | 400 Bad Request |
/// | GET | /api/v1/purchase-requests/{id} | 200 OK + データ | 404 Not Found |
/// | POST | /api/v1/purchase-requests | 201 Created + ID | 400 Bad Request |
/// | POST | /api/v1/purchase-requests/{id}/approve | 204 No Content | 400 Bad Request |
/// | POST | /api/v1/purchase-requests/{id}/reject | 204 No Content | 400 Bad Request |
///
/// ## AI実装時の注意
///
/// - **Controllerはシンに保つ**: ビジネスロジックをControllerに書かない
/// - **MediatR経由で呼び出す**: Handlerを直接インスタンス化しない
/// - **Result<T>を適切に処理**: IsSuccessをチェックしてレスポンスを返す
/// - **DTOを使用**: Application層のDTOをそのまま返す
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/purchase-requests")]
public sealed class PurchaseRequestsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PurchaseRequestsController> _logger;

    public PurchaseRequestsController(
        IMediator mediator,
        ILogger<PurchaseRequestsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// 購買申請一覧取得
    /// </summary>
    /// <param name="status">ステータスフィルタ（オプション）</param>
    /// <param name="requesterId">申請者IDフィルタ（オプション）</param>
    /// <param name="pageNumber">ページ番号（デフォルト: 1）</param>
    /// <param name="pageSize">ページサイズ（デフォルト: 20）</param>
    /// <param name="sortBy">ソートフィールド（デフォルト: CreatedAt）</param>
    /// <param name="ascending">昇順ソート（デフォルト: false）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>購買申請一覧</returns>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Requester},{Roles.Approver}")]
    [ProducesResponseType(typeof(List<PurchaseRequestListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<PurchaseRequestListItemDto>>> GetPurchaseRequests(
        [FromQuery] int? status,
        [FromQuery] Guid? requesterId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] bool ascending = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPurchaseRequestsQuery
        {
            Status = status,
            RequesterId = requesterId,
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = sortBy,
            Ascending = ascending
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to retrieve purchase requests",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// 購買申請詳細取得
    /// </summary>
    /// <param name="id">購買申請ID</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>購買申請詳細</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Requester},{Roles.Approver}")]
    [ProducesResponseType(typeof(PurchaseRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseRequestDetailDto>> GetPurchaseRequestById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPurchaseRequestByIdQuery { Id = id };
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Purchase request not found",
                Detail = $"Purchase request with ID {id} was not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// 購買申請提出
    /// </summary>
    /// <param name="command">購買申請提出コマンド</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>作成された購買申請ID</returns>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Requester}")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> SubmitPurchaseRequest(
        [FromBody] SubmitPurchaseRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Purchase request submission failed",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var requestId = result.Value;
        return CreatedAtAction(
            nameof(GetPurchaseRequestById),
            new { id = requestId },
            requestId);
    }

    /// <summary>
    /// 購買申請承認
    /// </summary>
    /// <param name="id">購買申請ID</param>
    /// <param name="command">承認コマンド</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>承認結果</returns>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Approver}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApprovePurchaseRequest(
        Guid id,
        [FromBody] ApprovePurchaseRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        // Ensure the ID from route matches the command
        var commandWithId = new ApprovePurchaseRequestCommand
        {
            RequestId = id,
            Comment = command.Comment,
            IdempotencyKey = command.IdempotencyKey
        };

        var result = await _mediator.Send(commandWithId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Purchase request approval failed",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return NoContent();
    }

    /// <summary>
    /// 購買申請却下
    /// </summary>
    /// <param name="id">購買申請ID</param>
    /// <param name="command">却下コマンド</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>却下結果</returns>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Approver}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectPurchaseRequest(
        Guid id,
        [FromBody] RejectPurchaseRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        // Ensure the ID from route matches the command
        var commandWithId = new RejectPurchaseRequestCommand
        {
            RequestId = id,
            Reason = command.Reason,
            IdempotencyKey = command.IdempotencyKey
        };

        var result = await _mediator.Send(commandWithId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Purchase request rejection failed",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return NoContent();
    }

    /// <summary>
    /// 購買申請キャンセル
    /// </summary>
    /// <param name="id">購買申請ID</param>
    /// <param name="command">キャンセルコマンド</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>キャンセル結果</returns>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Requester}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelPurchaseRequest(
        Guid id,
        [FromBody] CancelPurchaseRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        // Ensure the ID from route matches the command
        var commandWithId = new CancelPurchaseRequestCommand
        {
            PurchaseRequestId = id,
            Reason = command.Reason
        };

        var result = await _mediator.Send(commandWithId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Purchase request cancellation failed",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return NoContent();
    }

    /// <summary>
    /// 承認待ち申請一覧取得
    /// </summary>
    /// <param name="pageNumber">ページ番号（デフォルト: 1）</param>
    /// <param name="pageSize">ページサイズ（デフォルト: 20）</param>
    /// <param name="sortBy">ソートフィールド（デフォルト: TotalAmount）</param>
    /// <param name="ascending">昇順ソート（デフォルト: false）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>承認待ち申請一覧</returns>
    [HttpGet("pending-approvals")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Approver}")]
    [ProducesResponseType(typeof(List<PendingApprovalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<PendingApprovalDto>>> GetPendingApprovals(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "TotalAmount",
        [FromQuery] bool ascending = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPendingApprovalsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = sortBy,
            Ascending = ascending
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to retrieve pending approvals",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// ダッシュボード統計情報取得
    /// </summary>
    /// <param name="monthsToInclude">月次統計の取得月数（デフォルト: 12）</param>
    /// <param name="topRequestsCount">トップ申請の取得件数（デフォルト: 10）</param>
    /// <param name="topDepartmentsCount">部門統計の取得件数（デフォルト: 5）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>ダッシュボード統計情報</returns>
    [HttpGet("dashboard/statistics")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Approver}")]
    [ProducesResponseType(typeof(DashboardStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DashboardStatisticsDto>> GetDashboardStatistics(
        [FromQuery] int monthsToInclude = 12,
        [FromQuery] int topRequestsCount = 10,
        [FromQuery] int topDepartmentsCount = 5,
        CancellationToken cancellationToken = default)
    {
        var query = new GetDashboardStatisticsQuery
        {
            MonthsToInclude = monthsToInclude,
            TopRequestsCount = topRequestsCount,
            TopDepartmentsCount = topDepartmentsCount
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to retrieve dashboard statistics",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(result.Value);
    }
}
