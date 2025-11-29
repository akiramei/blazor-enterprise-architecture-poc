using Application.Core.Commands;
using Domain.ApprovalWorkflow.Applications;
using Domain.ApprovalWorkflow.Applications.Boundaries;
using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.EditApplication;

/// <summary>
/// 申請編集コマンドハンドラー
///
/// 【パターン: CommandPipeline継承 + Boundary】
///
/// 責務:
/// 1. Boundary経由で編集資格チェック
/// 2. ドメインオペレーション実行
/// 3. Repository経由で永続化
/// </summary>
public sealed class EditApplicationCommandHandler
    : CommandPipeline<EditApplicationCommand, Unit>
{
    private readonly IApplicationRepository _repository;
    private readonly IApplicationBoundary _boundary;
    private readonly IAppContext _appContext;

    public EditApplicationCommandHandler(
        IApplicationRepository repository,
        IApplicationBoundary boundary,
        IAppContext appContext)
    {
        _repository = repository;
        _boundary = boundary;
        _appContext = appContext;
    }

    protected override async Task<Result<Unit>> ExecuteAsync(
        EditApplicationCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Boundary経由で編集資格チェック
        var canEdit = await _boundary.ValidateEditAsync(
            command.ApplicationId,
            _appContext.UserId,
            cancellationToken);

        if (!canEdit.IsAllowed)
            return Result.Fail<Unit>(canEdit.Reason!);

        // 2. エンティティ取得
        var application = await _repository.GetByIdAsync(command.ApplicationId, cancellationToken);
        if (application == null)
            return Result.Fail<Unit>("申請が見つかりません");

        // 3. ドメインオペレーション
        application.Edit(command.Content, _appContext.UserId);

        // 4. 永続化
        await _repository.UpdateAsync(application, cancellationToken);

        return Result.Success(Unit.Value);
    }
}
