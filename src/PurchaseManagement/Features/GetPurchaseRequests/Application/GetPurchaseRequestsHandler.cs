using Dapper;
using MediatR;
using Npgsql;
using PurchaseManagement.Shared.Application;
using Shared.Application;
using Shared.Application.Interfaces;

namespace GetPurchaseRequests.Application;

/// <summary>
/// 購買申請一覧取得ハンドラ
///
/// 【パターン: CQRS Query Handler】
///
/// 責務:
/// - Dapperを使用してPostgreSQLから購買申請一覧を効率的に取得
/// - フィルタリング・ソート・ページング処理
/// - DTOへの変換
///
/// AI実装時の注意:
/// - Dapperの動的SQLビルダーを使用してSQLインジェクション対策
/// - JOINを使用して子テーブルの集計値を取得
/// - ステータス名の変換はアプリケーション層で実施
/// </summary>
public sealed class GetPurchaseRequestsHandler : IRequestHandler<GetPurchaseRequestsQuery, Result<List<PurchaseRequestListItemDto>>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetPurchaseRequestsHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<List<PurchaseRequestListItemDto>>> Handle(
        GetPurchaseRequestsQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = BuildQuery(query);
            var parameters = BuildParameters(query);

            var items = await connection.QueryAsync<PurchaseRequestListItemDto>(
                sql,
                parameters);

            var result = items
                .Select(item => item with
                {
                    StatusName = GetStatusName(item.Status)
                })
                .ToList();

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            return Result.Fail<List<PurchaseRequestListItemDto>>(
                $"購買申請一覧の取得中にエラーが発生しました: {ex.Message}");
        }
    }

    private static string BuildQuery(GetPurchaseRequestsQuery query)
    {
        var orderByClause = query.SortBy switch
        {
            "RequestNumber" => $"pr.\"RequestNumber\" {(query.Ascending ? "ASC" : "DESC")}",
            "Title" => $"pr.\"Title\" {(query.Ascending ? "ASC" : "DESC")}",
            "Status" => $"pr.\"Status\" {(query.Ascending ? "ASC" : "DESC")}",
            "TotalAmount" => $"TotalAmount {(query.Ascending ? "ASC" : "DESC")}", // Fixed: Match SELECT alias
            "CreatedAt" => $"pr.\"CreatedAt\" {(query.Ascending ? "ASC" : "DESC")}",
            "SubmittedAt" => $"pr.\"SubmittedAt\" {(query.Ascending ? "ASC" : "DESC")}",
            _ => $"pr.\"CreatedAt\" {(query.Ascending ? "ASC" : "DESC")}"
        };

        return $@"
            SELECT
                pr.""Id"",
                pr.""RequestNumber"",
                pr.""RequesterId"",
                pr.""RequesterName"",
                pr.""Title"",
                pr.""Status"",
                pr.""CreatedAt"",
                pr.""SubmittedAt"",
                pr.""ApprovedAt"",
                pr.""RejectedAt"",
                COALESCE(SUM(pri.""Amount""), 0) as TotalAmount,
                COALESCE(MAX(pri.""Currency""), 'JPY') as Currency,
                COUNT(DISTINCT aps.""Id"") as ApprovalStepCount,
                COUNT(DISTINCT CASE WHEN aps.""Status"" = 1 THEN aps.""Id"" END) as ApprovedStepCount
            FROM ""PurchaseRequests"" pr
            LEFT JOIN ""PurchaseRequestItems"" pri ON pr.""Id"" = pri.""PurchaseRequestId""
            LEFT JOIN ""ApprovalSteps"" aps ON pr.""Id"" = aps.""PurchaseRequestId""
            WHERE 1=1
                {(query.Status.HasValue ? "AND pr.\"Status\" = @Status" : "")}
                {(query.RequesterId.HasValue ? "AND pr.\"RequesterId\" = @RequesterId" : "")}
            GROUP BY
                pr.""Id"",
                pr.""RequestNumber"",
                pr.""RequesterId"",
                pr.""RequesterName"",
                pr.""Title"",
                pr.""Status"",
                pr.""CreatedAt"",
                pr.""SubmittedAt"",
                pr.""ApprovedAt"",
                pr.""RejectedAt""
            ORDER BY {orderByClause}
            LIMIT @PageSize OFFSET @Offset";
    }

    private static object BuildParameters(GetPurchaseRequestsQuery query)
    {
        return new
        {
            query.Status,
            query.RequesterId,
            query.PageSize,
            Offset = (query.PageNumber - 1) * query.PageSize
        };
    }

    private static string GetStatusName(int status)
    {
        return status switch
        {
            0 => "下書き",
            1 => "提出済み",
            2 => "1次承認待ち",
            3 => "2次承認待ち",
            4 => "3次承認待ち",
            5 => "承認済み",
            6 => "却下",
            7 => "キャンセル",
            _ => "不明"
        };
    }
}
