using Application.Core.Commands;
using Domain.ApprovalWorkflow.Applications;
using Domain.ApprovalWorkflow.Boundaries;
using Domain.ApprovalWorkflow.WorkflowDefinitions;
using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.ApproveApplication;

/// <summary>
/// 申請承認コマンドハンドラー
///
/// 【パターン: CommandPipeline継承 + Boundary】
///
/// 責務:
/// 1. Boundary経由で承認資格チェック
/// 2. ワークフロー定義から総ステップ数を取得
/// 3. ドメインオペレーション実行（承認）
/// 4. Repository経由で永続化
/// </summary>
public sealed class ApproveApplicationCommandHandler
    : CommandPipeline<ApproveApplicationCommand, Unit>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IWorkflowDefinitionRepository _workflowDefinitionRepository;
    private readonly IApplicationBoundary _boundary;
    private readonly IAppContext _appContext;

    public ApproveApplicationCommandHandler(
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

    protected override async Task<Result<Unit>> ExecuteAsync(
        ApproveApplicationCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Boundary経由で承認資格チェック
        var canApprove = await _boundary.ValidateApproveAsync(
            command.ApplicationId,
            _appContext.UserId,
            command.ApproverRole,
            cancellationToken);

        if (!canApprove.IsAllowed)
            return Result.Fail<Unit>(canApprove.Reason!);

        // 2. エンティティ取得
        var application = await _applicationRepository.GetByIdAsync(command.ApplicationId, cancellationToken);
        if (application == null)
            return Result.Fail<Unit>("申請が見つかりません");

        // 3. ワークフロー定義から総ステップ数を取得
        if (application.WorkflowDefinitionId == null)
            return Result.Fail<Unit>("ワークフロー定義が設定されていません");

        var workflowDefinition = await _workflowDefinitionRepository.GetByIdWithStepsAsync(
            application.WorkflowDefinitionId.Value, cancellationToken);
        if (workflowDefinition == null)
            return Result.Fail<Unit>("ワークフロー定義が見つかりません");

        // 4. ドメインオペレーション（承認）
        application.Approve(
            _appContext.UserId,
            command.ApproverRole,
            workflowDefinition.TotalSteps,
            command.Comment);

        // 5. 永続化
        await _applicationRepository.UpdateAsync(application, cancellationToken);

        return Result.Success(Unit.Value);
    }
}
