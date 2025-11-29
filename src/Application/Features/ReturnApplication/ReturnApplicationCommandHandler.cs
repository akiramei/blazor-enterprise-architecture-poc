using Application.Core.Commands;
using Domain.ApprovalWorkflow.Applications;
using Domain.ApprovalWorkflow.Applications.Boundaries;
using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.ReturnApplication;

/// <summary>
/// 申請差し戻しコマンドハンドラー
///
/// 【パターン: CommandPipeline継承 + Boundary】
/// </summary>
public sealed class ReturnApplicationCommandHandler
    : CommandPipeline<ReturnApplicationCommand, Unit>
{
    private readonly IApplicationRepository _repository;
    private readonly IApplicationBoundary _boundary;
    private readonly IAppContext _appContext;

    public ReturnApplicationCommandHandler(
        IApplicationRepository repository,
        IApplicationBoundary boundary,
        IAppContext appContext)
    {
        _repository = repository;
        _boundary = boundary;
        _appContext = appContext;
    }

    protected override async Task<Result<Unit>> ExecuteAsync(
        ReturnApplicationCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Boundary経由で差し戻し資格チェック
        var canReturn = await _boundary.ValidateReturnAsync(
            command.ApplicationId,
            _appContext.UserId,
            command.ApproverRole,
            cancellationToken);

        if (!canReturn.IsAllowed)
            return Result.Fail<Unit>(canReturn.Reason!);

        // 2. エンティティ取得
        var application = await _repository.GetByIdAsync(command.ApplicationId, cancellationToken);
        if (application == null)
            return Result.Fail<Unit>("申請が見つかりません");

        // 3. ドメインオペレーション（差し戻し）
        application.Return(_appContext.UserId, command.ApproverRole, command.Reason);

        // 4. 永続化
        await _repository.UpdateAsync(application, cancellationToken);

        return Result.Success(Unit.Value);
    }
}
