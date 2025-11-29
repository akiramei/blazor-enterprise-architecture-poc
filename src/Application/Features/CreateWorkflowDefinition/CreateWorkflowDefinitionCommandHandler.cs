using Application.Core.Commands;
using Domain.ApprovalWorkflow.WorkflowDefinitions;
using Shared.Application;

namespace Application.Features.CreateWorkflowDefinition;

/// <summary>
/// ワークフロー定義作成コマンドハンドラー
///
/// 【パターン: CommandPipeline継承】
/// </summary>
public sealed class CreateWorkflowDefinitionCommandHandler
    : CommandPipeline<CreateWorkflowDefinitionCommand, Guid>
{
    private readonly IWorkflowDefinitionRepository _repository;

    public CreateWorkflowDefinitionCommandHandler(IWorkflowDefinitionRepository repository)
    {
        _repository = repository;
    }

    protected override async Task<Result<Guid>> ExecuteAsync(
        CreateWorkflowDefinitionCommand command,
        CancellationToken cancellationToken)
    {
        // 1. 同じ申請タイプに対して既存のアクティブな定義があるかチェック
        var existingDefinition = await _repository.GetByApplicationTypeAsync(
            command.ApplicationType, cancellationToken);

        if (existingDefinition != null)
        {
            // 既存のアクティブな定義を非アクティブ化
            existingDefinition.Deactivate();
            await _repository.UpdateAsync(existingDefinition, cancellationToken);
        }

        // 2. ドメインオペレーション（ワークフロー定義作成）
        var steps = command.Steps.Select(s => (s.Role, s.Name)).ToList();
        var definition = WorkflowDefinition.Create(
            command.ApplicationType,
            command.Name,
            command.Description,
            steps);

        // 3. 永続化
        await _repository.AddAsync(definition, cancellationToken);

        return Result.Success(definition.Id);
    }
}
