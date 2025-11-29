using Application.Core.Commands;
using Domain.ApprovalWorkflow.Applications;
using Domain.ApprovalWorkflow.Applications.Boundaries;
using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.RejectApplication;

/// <summary>
/// 申請却下コマンドハンドラー
///
/// 【パターン: CommandPipeline継承 + Boundary】
/// </summary>
public sealed class RejectApplicationCommandHandler
    : CommandPipeline<RejectApplicationCommand, Unit>
{
    private readonly IApplicationRepository _repository;
    private readonly IApplicationBoundary _boundary;
    private readonly IAppContext _appContext;

    public RejectApplicationCommandHandler(
        IApplicationRepository repository,
        IApplicationBoundary boundary,
        IAppContext appContext)
    {
        _repository = repository;
        _boundary = boundary;
        _appContext = appContext;
    }

    protected override async Task<Result<Unit>> ExecuteAsync(
        RejectApplicationCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Boundary経由で却下資格チェック
        var canReject = await _boundary.ValidateRejectAsync(
            command.ApplicationId,
            _appContext.UserId,
            command.ApproverRole,
            cancellationToken);

        if (!canReject.IsAllowed)
            return Result.Fail<Unit>(canReject.Reason!);

        // 2. エンティティ取得
        var application = await _repository.GetByIdAsync(command.ApplicationId, cancellationToken);
        if (application == null)
            return Result.Fail<Unit>("申請が見つかりません");

        // 3. ドメインオペレーション（却下）
        application.Reject(_appContext.UserId, command.ApproverRole, command.Reason);

        // 4. 永続化
        await _repository.UpdateAsync(application, cancellationToken);

        return Result.Success(Unit.Value);
    }
}
