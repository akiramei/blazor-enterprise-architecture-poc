using Application.Core.Commands;
using Domain.ApprovalWorkflow.Applications;
using Domain.ApprovalWorkflow.Applications.Boundaries;
using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.ResubmitApplication;

/// <summary>
/// 申請再提出コマンドハンドラー
///
/// 【パターン: CommandPipeline継承 + Boundary】
/// </summary>
public sealed class ResubmitApplicationCommandHandler
    : CommandPipeline<ResubmitApplicationCommand, Unit>
{
    private readonly IApplicationRepository _repository;
    private readonly IApplicationBoundary _boundary;
    private readonly IAppContext _appContext;

    public ResubmitApplicationCommandHandler(
        IApplicationRepository repository,
        IApplicationBoundary boundary,
        IAppContext appContext)
    {
        _repository = repository;
        _boundary = boundary;
        _appContext = appContext;
    }

    protected override async Task<Result<Unit>> ExecuteAsync(
        ResubmitApplicationCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Boundary経由で再提出資格チェック
        var canResubmit = await _boundary.ValidateResubmitAsync(
            command.ApplicationId,
            _appContext.UserId,
            cancellationToken);

        if (!canResubmit.IsAllowed)
            return Result.Fail<Unit>(canResubmit.Reason!);

        // 2. エンティティ取得
        var application = await _repository.GetByIdAsync(command.ApplicationId, cancellationToken);
        if (application == null)
            return Result.Fail<Unit>("申請が見つかりません");

        // 3. ドメインオペレーション（再提出）
        application.Resubmit(_appContext.UserId);

        // 4. 永続化
        await _repository.UpdateAsync(application, cancellationToken);

        return Result.Success(Unit.Value);
    }
}
