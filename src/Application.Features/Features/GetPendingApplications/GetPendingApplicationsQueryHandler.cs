using Application.Core.Queries;
using Domain.ApprovalWorkflow.Applications;
using Domain.ApprovalWorkflow.WorkflowDefinitions;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.GetPendingApplications;

/// <summary>
/// 承認待ち申請一覧取得クエリハンドラー
///
/// 【パターン: QueryPipeline継承】
///
/// 責務:
/// - 指定されたロールが承認可能な申請一覧を取得
/// - ワークフロー定義と照合して、現在のステップのロールに一致する申請のみ返す
/// </summary>
public sealed class GetPendingApplicationsQueryHandler
    : QueryPipeline<GetPendingApplicationsQuery, List<PendingApplicationDto>>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IWorkflowDefinitionRepository _workflowDefinitionRepository;

    public GetPendingApplicationsQueryHandler(
        IApplicationRepository applicationRepository,
        IWorkflowDefinitionRepository workflowDefinitionRepository)
    {
        _applicationRepository = applicationRepository;
        _workflowDefinitionRepository = workflowDefinitionRepository;
    }

    protected override async Task<Result<List<PendingApplicationDto>>> ExecuteAsync(
        GetPendingApplicationsQuery query,
        CancellationToken cancellationToken)
    {
        // 全ワークフロー定義を取得
        var allWorkflowDefinitions = await _workflowDefinitionRepository.GetAllAsync(cancellationToken);
        var activeDefinitions = allWorkflowDefinitions.Where(w => w.IsActive).ToList();

        // 指定されたロールが担当するステップを持つワークフロー定義とステップ番号を特定
        var roleSteps = new List<(Guid DefinitionId, int StepNumber)>();
        foreach (var definition in activeDefinitions)
        {
            foreach (var step in definition.Steps)
            {
                if (step.Role == query.ApproverRole)
                {
                    roleSteps.Add((definition.Id, step.StepNumber));
                }
            }
        }

        if (roleSteps.Count == 0)
        {
            // このロールが担当するステップがない場合は空リストを返す
            return Result.Success(new List<PendingApplicationDto>());
        }

        // 承認待ち状態の全申請を取得（各ステップごとに取得して結合）
        var pendingApplications = new List<ApprovalApplication>();
        foreach (var (definitionId, stepNumber) in roleSteps)
        {
            var applications = await _applicationRepository.GetPendingForRoleAsync(
                query.ApproverRole, stepNumber, cancellationToken);

            // ワークフロー定義IDが一致するものだけを追加
            pendingApplications.AddRange(applications.Where(a =>
                a.WorkflowDefinitionId == definitionId &&
                a.CurrentStepNumber == stepNumber));
        }

        // 重複を除去して日付順にソート
        var distinctApplications = pendingApplications
            .GroupBy(a => a.Id)
            .Select(g => g.First())
            .OrderBy(a => a.SubmittedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize);

        // DTOに変換
        var result = distinctApplications.Select(a =>
        {
            // ワークフロー定義から現在のステップのロールを取得
            var definition = activeDefinitions.FirstOrDefault(d => d.Id == a.WorkflowDefinitionId);
            var currentStep = definition?.GetStep(a.CurrentStepNumber);

            return new PendingApplicationDto(
                a.Id,
                a.ApplicantId,
                a.Type,
                GetTypeName(a.Type),
                a.Content.Length > 100 ? a.Content.Substring(0, 100) + "..." : a.Content,
                a.Status,
                GetStatusName(a.Status),
                a.CurrentStepNumber,
                currentStep?.Role ?? "不明",
                a.CreatedAt,
                a.SubmittedAt
            );
        }).ToList();

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
