using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using PurchaseManagement.Shared.Application;
using Shared.Application;

namespace GetPendingApprovals.Application;

/// <summary>
/// 承認待ち申請一覧取得ハンドラー
///
/// 【パターン: CQRS Query Handler (Dapper)】
///
/// 責務:
/// - 現在のユーザーIDを取得
/// - pm_PurchaseRequests と pm_ApprovalSteps を JOIN
/// - 承認者 = 現在のユーザー かつ IsPending = true でフィルタ
/// - ページング・ソート適用
/// - PendingApprovalDto にマッピング
///
/// パフォーマンス:
/// - EF Coreではなく Dapper を使用（読み取り専用クエリ最適化）
/// - 必要なカラムのみSELECT
/// - インデックス活用（ApproverId, IsPending）
///
/// トランザクション:
/// - 不要（読み取り専用）
/// </summary>
public sealed class GetPendingApprovalsHandler : IRequestHandler<GetPendingApprovalsQuery, Result<List<PendingApprovalDto>>>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetPendingApprovalsHandler> _logger;

    public GetPendingApprovalsHandler(
        IDbConnectionFactory connectionFactory,
        ICurrentUserService currentUserService,
        ILogger<GetPendingApprovalsHandler> logger)
    {
        _connectionFactory = connectionFactory;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<List<PendingApprovalDto>>> Handle(
        GetPendingApprovalsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 現在のユーザーIDを取得
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                return Result.Fail<List<PendingApprovalDto>>("ユーザー情報が取得できません");
            }

            using var connection = _connectionFactory.CreateConnection();

            // ソート順を決定
            var orderByClause = request.SortBy switch
            {
                "TotalAmount" => request.Ascending ? "TotalAmount ASC" : "TotalAmount DESC",
                "SubmittedAt" => request.Ascending ? "pr.\"SubmittedAt\" ASC" : "pr.\"SubmittedAt\" DESC",
                "RequestNumber" => request.Ascending ? "pr.\"RequestNumber\" ASC" : "pr.\"RequestNumber\" DESC",
                _ => "TotalAmount DESC" // デフォルト: 金額降順
            };

            // SQL: 承認待ち申請を取得
            // NOTE: TotalAmountは計算プロパティ（PurchaseRequestItemsから集計）
            // NOTE: IsPendingは計算プロパティ（Status = 0でPending）
            var sql = $@"
                SELECT
                    pr.""Id"" AS PurchaseRequestId,
                    pr.""RequestNumber"",
                    pr.""Title"",
                    COALESCE((
                        SELECT SUM(pri.""Amount"")
                        FROM ""PurchaseRequestItems"" pri
                        WHERE pri.""PurchaseRequestId"" = pr.""Id""
                    ), 0) AS TotalAmount,
                    pr.""RequesterId"",
                    pr.""RequesterName"",
                    pr.""SubmittedAt"",
                    ast.""StepNumber"" AS CurrentStepNumber,
                    (SELECT COUNT(*) FROM ""ApprovalSteps"" WHERE ""PurchaseRequestId"" = pr.""Id"") AS TotalSteps
                FROM ""PurchaseRequests"" pr
                INNER JOIN ""ApprovalSteps"" ast
                    ON pr.""Id"" = ast.""PurchaseRequestId""
                WHERE ast.""ApproverId"" = @UserId
                  AND ast.""Status"" = 0  -- ApprovalStepStatus.Pending = 0
                  AND pr.""Status"" IN (1, 2)  -- Submitted = 1, InApproval = 2
                ORDER BY {orderByClause}
                LIMIT @PageSize OFFSET @Offset
            ";

            var offset = (request.PageNumber - 1) * request.PageSize;

            var results = await connection.QueryAsync<PendingApprovalDto>(
                sql,
                new
                {
                    UserId = userId.Value,
                    PageSize = request.PageSize,
                    Offset = offset
                });

            var pendingApprovals = results.ToList();

            _logger.LogInformation(
                "承認待ち申請を取得しました。[UserId: {UserId}] [Count: {Count}]",
                userId.Value,
                pendingApprovals.Count);

            return Result.Success(pendingApprovals);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "承認待ち申請の取得中にエラーが発生しました");

            return Result.Fail<List<PendingApprovalDto>>(
                $"承認待ち申請の取得中にエラーが発生しました: {ex.Message}");
        }
    }
}
