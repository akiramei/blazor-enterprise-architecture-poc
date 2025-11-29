using Application.Core.Queries;
using Domain.ApprovalWorkflow.Applications;
using Domain.ApprovalWorkflow.Applications.Boundaries;
using Domain.ApprovalWorkflow.WorkflowDefinitions;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.GetApplicationDetail;

/// <summary>
/// 申請詳細取得クエリハンドラー
///
/// 【パターン: QueryPipeline継承 + Boundary情報付加】
///
/// 責務:
/// - 申請の詳細情報を取得
/// - 承認履歴を含む
/// - Boundary情報をDTOに含める（UI制御用）
/// </summary>
public sealed class GetApplicationDetailQueryHandler
    : QueryPipeline<GetApplicationDetailQuery, ApplicationDetailDto>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IWorkflowDefinitionRepository _workflowDefinitionRepository;
    private readonly IApplicationBoundary _boundary;
    private readonly IAppContext _appContext;

    public GetApplicationDetailQueryHandler(
        IApplicationRepository applicationRepository,
        IWorkflowDefinitionRepository workflowDefinitionRepository,
        IApplicationBoundary boundary,
        IAppContext appContext)
    {
        _applicationRepository = applicationRepository;
        _workflowDefinitionRepository = workflowDefinitionRepository;
        _boundary = boundary;
        _appContext = appContext;
    }

    protected override async Task<Result<ApplicationDetailDto>> ExecuteAsync(
        GetApplicationDetailQuery query,
        CancellationToken cancellationToken)
    {
        // 申請を取得（承認履歴を含む）
        var application = await _applicationRepository.GetByIdWithHistoryAsync(
            query.ApplicationId, cancellationToken);

        if (application == null)
            return Result.Fail<ApplicationDetailDto>("申請が見つかりません");

        // 閲覧権限チェック
        var canView = await _boundary.ValidateViewAsync(
            query.ApplicationId,
            _appContext.UserId,
            "User", // TODO: 実際のユーザーロールを取得
            cancellationToken);

        if (!canView.IsAllowed)
            return Result.Fail<ApplicationDetailDto>(canView.Reason!);

        // ワークフロー定義から総ステップ数を取得
        int totalSteps = 0;
        if (application.WorkflowDefinitionId.HasValue)
        {
            var workflowDefinition = await _workflowDefinitionRepository.GetByIdWithStepsAsync(
                application.WorkflowDefinitionId.Value, cancellationToken);
            totalSteps = workflowDefinition?.TotalSteps ?? 0;
        }

        // Boundary判定（UI制御用）
        var canEdit = application.CanEdit(_appContext.UserId);
        var canSubmit = application.CanSubmit();
        var canResubmit = application.CanResubmit(_appContext.UserId);

        // DTOに変換
        var dto = new ApplicationDetailDto(
            application.Id,
            application.ApplicantId,
            application.Type,
            GetTypeName(application.Type),
            application.Content,
            application.Status,
            GetStatusName(application.Status),
            application.CurrentStepNumber,
            totalSteps,
            application.WorkflowDefinitionId,
            application.CreatedAt,
            application.UpdatedAt,
            application.SubmittedAt,
            application.ApprovedAt,
            application.RejectedAt,
            application.ReturnedAt,
            application.ApprovalHistory.Select(h => new ApprovalHistoryItemDto(
                h.Id,
                h.StepNumber,
                h.ApproverId,
                h.Action,
                GetActionName(h.Action),
                h.Comment,
                h.Timestamp
            )).ToList(),
            canEdit.IsAllowed,
            canSubmit.IsAllowed,
            canResubmit.IsAllowed
        );

        return Result.Success(dto);
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

    private static string GetActionName(ApprovalAction action)
    {
        return action switch
        {
            ApprovalAction.Approved => "承認",
            ApprovalAction.Rejected => "却下",
            ApprovalAction.Returned => "差し戻し",
            _ => "不明"
        };
    }
}
