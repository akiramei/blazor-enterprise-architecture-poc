using Application.Core.Queries;
using Domain.ApprovalWorkflow.Applications;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.GetApplicationList;

/// <summary>
/// 申請一覧取得クエリハンドラー
///
/// 【パターン: QueryPipeline継承】
///
/// 責務:
/// - 現在のユーザーの申請一覧を取得
/// - ステータスでフィルタリング
/// </summary>
public sealed class GetApplicationListQueryHandler
    : QueryPipeline<GetApplicationListQuery, List<ApplicationListItemDto>>
{
    private readonly IApplicationRepository _repository;
    private readonly IAppContext _appContext;

    public GetApplicationListQueryHandler(
        IApplicationRepository repository,
        IAppContext appContext)
    {
        _repository = repository;
        _appContext = appContext;
    }

    protected override async Task<Result<List<ApplicationListItemDto>>> ExecuteAsync(
        GetApplicationListQuery query,
        CancellationToken cancellationToken)
    {
        // 現在のユーザーの申請一覧を取得
        var applications = await _repository.GetByApplicantIdAsync(_appContext.UserId, cancellationToken);

        // ステータスでフィルタリング
        var filtered = query.Status.HasValue
            ? applications.Where(a => a.Status == query.Status.Value)
            : applications;

        // ページング
        var paged = filtered
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize);

        // DTOに変換
        var result = paged.Select(a => new ApplicationListItemDto(
            a.Id,
            a.Type,
            GetTypeName(a.Type),
            a.Status,
            GetStatusName(a.Status),
            a.CurrentStepNumber,
            a.CreatedAt,
            a.SubmittedAt
        )).ToList();

        return Result.Success(result);
    }

    private static string GetTypeName(ApplicationType type)
    {
        return type switch
        {
            ApplicationType.LeaveRequest => "休暇申請",
            ApplicationType.ExpenseRequest => "経費申請",
            ApplicationType.PurchaseRequest => "購買依頼",
            _ => "不明"
        };
    }

    private static string GetStatusName(ApplicationStatus status)
    {
        return status switch
        {
            ApplicationStatus.Draft => "下書き",
            ApplicationStatus.Submitted => "提出済み",
            ApplicationStatus.InReview => "承認待ち",
            ApplicationStatus.Approved => "承認済み",
            ApplicationStatus.Rejected => "却下",
            ApplicationStatus.Returned => "差し戻し",
            _ => "不明"
        };
    }
}
