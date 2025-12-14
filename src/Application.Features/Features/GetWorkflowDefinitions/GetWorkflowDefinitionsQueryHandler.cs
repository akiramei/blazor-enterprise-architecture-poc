using Application.Core.Queries;
using Domain.ApprovalWorkflow.Applications;
using Domain.ApprovalWorkflow.WorkflowDefinitions;
using Shared.Application;

namespace Application.Features.GetWorkflowDefinitions;

/// <summary>
/// ワークフロー定義一覧取得クエリハンドラー
///
/// 【パターン: QueryPipeline継承】
/// </summary>
public sealed class GetWorkflowDefinitionsQueryHandler
    : QueryPipeline<GetWorkflowDefinitionsQuery, List<WorkflowDefinitionDto>>
{
    private readonly IWorkflowDefinitionRepository _repository;

    public GetWorkflowDefinitionsQueryHandler(IWorkflowDefinitionRepository repository)
    {
        _repository = repository;
    }

    protected override async Task<Result<List<WorkflowDefinitionDto>>> ExecuteAsync(
        GetWorkflowDefinitionsQuery query,
        CancellationToken cancellationToken)
    {
        var definitions = await _repository.GetAllAsync(cancellationToken);

        // アクティブフィルタ
        var filtered = query.ActiveOnly.HasValue && query.ActiveOnly.Value
            ? definitions.Where(d => d.IsActive)
            : definitions;

        // DTOに変換
        var result = filtered.Select(d => new WorkflowDefinitionDto(
            d.Id,
            d.ApplicationType,
            GetTypeName(d.ApplicationType),
            d.Name,
            d.Description,
            d.IsActive,
            d.TotalSteps,
            d.Steps.Select(s => new WorkflowStepDto(
                s.Id,
                s.StepNumber,
                s.Role,
                s.Name
            )).ToList(),
            d.CreatedAt,
            d.UpdatedAt
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
}
