using Application.Core.Commands;
using Domain.ApprovalWorkflow.Applications;
using Domain.ApprovalWorkflow.Boundaries;
using Domain.ApprovalWorkflow.WorkflowDefinitions;
using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.SubmitApplication;

/// <summary>
/// 申請提出コマンドハンドラー
///
/// 【パターン: CommandPipeline継承 + Boundary】
///
/// 責務:
/// 1. Boundary経由で提出資格チェック
/// 2. ワークフロー定義を取得
/// 3. ドメインオペレーション実行（状態遷移）
/// 4. Repository経由で永続化
/// </summary>
public sealed class SubmitApplicationCommandHandler
    : CommandPipeline<SubmitApplicationCommand, Unit>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IWorkflowDefinitionRepository _workflowDefinitionRepository;
    private readonly IApplicationBoundary _boundary;

    public SubmitApplicationCommandHandler(
        IApplicationRepository applicationRepository,
        IWorkflowDefinitionRepository workflowDefinitionRepository,
        IApplicationBoundary boundary)
    {
        _applicationRepository = applicationRepository;
        _workflowDefinitionRepository = workflowDefinitionRepository;
        _boundary = boundary;
    }

    protected override async Task<Result<Unit>> ExecuteAsync(
        SubmitApplicationCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Boundary経由で提出資格チェック
        var canSubmit = await _boundary.ValidateSubmitAsync(
            command.ApplicationId,
            cancellationToken);

        if (!canSubmit.IsAllowed)
            return Result.Fail<Unit>(canSubmit.Reason!);

        // 2. エンティティ取得
        var application = await _applicationRepository.GetByIdAsync(command.ApplicationId, cancellationToken);
        if (application == null)
            return Result.Fail<Unit>("申請が見つかりません");

        // 3. ワークフロー定義取得
        var workflowDefinition = await _workflowDefinitionRepository.GetByApplicationTypeAsync(
            application.Type, cancellationToken);
        if (workflowDefinition == null)
            return Result.Fail<Unit>("この申請タイプに対応するワークフロー定義がありません");

        // 4. ドメインオペレーション（提出）
        application.Submit(workflowDefinition);

        // 5. 永続化
        await _applicationRepository.UpdateAsync(application, cancellationToken);

        return Result.Success(Unit.Value);
    }
}
