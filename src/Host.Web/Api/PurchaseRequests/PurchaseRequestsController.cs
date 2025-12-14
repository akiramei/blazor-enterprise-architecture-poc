using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Application.Features.ApprovePurchaseRequest;
using Application.Features.GetPurchaseRequestById;
using Application.Features.GetPurchaseRequests;
using Application.Features.RejectPurchaseRequest;
using Application.Features.SubmitPurchaseRequest;
using Application.Features.UploadAttachment;
using Domain.PurchaseManagement.PurchaseRequests;

namespace PurchaseManagement.Api.V1.PurchaseRequests;

/// <summary>
/// 購買申請API
///
/// 【パターン: MediatR統合REST APIController】
///
/// このControllerは、既存のMediatR Command/Queryパターンを活用し、
/// REST APIエンドポイントとして公開します。
///
/// ## エンドポイント一覧
///
/// | HTTPメソッド | パス | 説明 |
/// |-------------|------|------|
/// | POST | /api/v1/purchase-requests | 購買申請の提出 |
/// | GET | /api/v1/purchase-requests | 購買申請一覧取得 |
/// | GET | /api/v1/purchase-requests/{id} | 購買申請詳細取得 |
/// | POST | /api/v1/purchase-requests/{id}/approve | 承認 |
/// | POST | /api/v1/purchase-requests/{id}/reject | 却下 |
/// | POST | /api/v1/purchase-requests/{id}/attachments | 添付ファイルアップロード |
///
/// ## 多段階承認フロー
///
/// 金額に応じて承認ステップ数が決まります:
/// - 10万円未満: 1段階承認
/// - 10万円以上〜50万円未満: 2段階承認
/// - 50万円以上: 3段階承認
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/purchase-requests")]
[Authorize]
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
    /// 購買申請を提出
    /// </summary>
    /// <param name="request">提出リクエスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>作成された購買申請ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> SubmitPurchaseRequest(
        [FromBody] SubmitPurchaseRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new SubmitPurchaseRequestCommand
        {
            Title = request.Title,
            Description = request.Description,
            Items = request.Items.Select(i => new Application.Features.SubmitPurchaseRequest.PurchaseRequestItemDto(
                i.ProductId,
                i.ProductName,
                i.UnitPrice,
                i.Quantity
            )).ToList()
        };

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
        return CreatedAtAction(nameof(GetPurchaseRequestById), new { id = requestId }, requestId);
    }

    /// <summary>
    /// 購買申請一覧取得
    /// </summary>
    /// <param name="status">ステータスフィルター（任意）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>購買申請一覧</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PurchaseRequestListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PurchaseRequestListItemDto>>> GetPurchaseRequests(
        [FromQuery] int? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPurchaseRequestsQuery { Status = status };
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
    [ProducesResponseType(typeof(PurchaseRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseRequestDetailDto>> GetPurchaseRequestById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPurchaseRequestByIdQuery(id);
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

        // Entity から DTO へ変換
        var entity = result.Value;
        var dto = MapToDetailDto(entity);

        return Ok(dto);
    }

    /// <summary>
    /// 購買申請を承認
    /// </summary>
    /// <param name="id">購買申請ID</param>
    /// <param name="request">承認リクエスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>承認結果</returns>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApprovePurchaseRequest(
        Guid id,
        [FromBody] ApproveRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new ApprovePurchaseRequestCommand
        {
            RequestId = id,
            Comment = request.Comment
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Approval failed",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return NoContent();
    }

    /// <summary>
    /// 購買申請を却下
    /// </summary>
    /// <param name="id">購買申請ID</param>
    /// <param name="request">却下リクエスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>却下結果</returns>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectPurchaseRequest(
        Guid id,
        [FromBody] RejectRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new RejectPurchaseRequestCommand
        {
            RequestId = id,
            Reason = request.Reason
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Rejection failed",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return NoContent();
    }

    /// <summary>
    /// 添付ファイルをアップロード
    /// </summary>
    /// <param name="id">購買申請ID</param>
    /// <param name="file">アップロードするファイル</param>
    /// <param name="description">説明（任意）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>作成された添付ファイルID</returns>
    [HttpPost("{id:guid}/attachments")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> UploadAttachment(
        Guid id,
        IFormFile file,
        [FromForm] string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid file",
                Detail = "File is required and cannot be empty",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // 許可されたファイル拡張子
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid file type",
                Detail = $"File extension '{extension}' is not allowed. Allowed extensions: {string.Join(", ", allowedExtensions)}",
                Status = StatusCodes.Status400BadRequest
            });
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);

        var command = new UploadAttachmentCommand
        {
            PurchaseRequestId = id,
            FileName = file.FileName,
            FileContent = memoryStream.ToArray(),
            ContentType = file.ContentType,
            Description = description
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Upload failed",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    #region Private Methods

    private static PurchaseRequestDetailDto MapToDetailDto(PurchaseRequest entity)
    {
        return new PurchaseRequestDetailDto
        {
            Id = entity.Id,
            RequestNumber = entity.RequestNumber.Value,
            Title = entity.Title,
            Description = entity.Description,
            RequesterId = entity.RequesterId,
            RequesterName = entity.RequesterName,
            Status = (int)entity.Status,
            StatusName = entity.Status.ToString(),
            TotalAmount = entity.TotalAmount.Amount,
            Currency = "JPY",
            CreatedAt = entity.CreatedAt,
            SubmittedAt = entity.SubmittedAt,
            ApprovedAt = entity.ApprovedAt,
            RejectedAt = entity.RejectedAt,
            Items = entity.Items.Select(i => new Application.Features.GetPurchaseRequestById.PurchaseRequestItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice.Amount,
                Quantity = i.Quantity,
                Amount = i.Amount.Amount,
                Currency = "JPY"
            }).ToList(),
            ApprovalSteps = entity.ApprovalSteps.Select(s => new ApprovalStepDto
            {
                StepNumber = s.StepNumber,
                ApproverId = s.ApproverId,
                ApproverName = s.ApproverName,
                ApproverRole = s.ApproverRole,
                Status = (int)s.Status,
                StatusName = s.Status.ToString(),
                Comment = s.Comment,
                ApprovedAt = s.ApprovedAt,
                RejectedAt = s.RejectedAt
            }).ToList()
        };
    }

    #endregion
}

#region Request DTOs

/// <summary>
/// 購買申請提出リクエスト
/// </summary>
public sealed record SubmitPurchaseRequestRequest
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<SubmitPurchaseRequestItemRequest> Items { get; init; } = new();
}

/// <summary>
/// 購買申請明細リクエスト
/// </summary>
public sealed record SubmitPurchaseRequestItemRequest
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
}

/// <summary>
/// 承認リクエスト
/// </summary>
public sealed record ApproveRequest
{
    public string Comment { get; init; } = string.Empty;
}

/// <summary>
/// 却下リクエスト
/// </summary>
public sealed record RejectRequest
{
    public string Reason { get; init; } = string.Empty;
}

#endregion
