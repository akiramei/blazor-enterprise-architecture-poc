using Application.Core.Commands;
using Domain.ApprovalWorkflow.Applications;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.CreateApplication;

/// <summary>
/// 申請作成コマンドハンドラー
///
/// 【パターン: CommandPipeline継承】
///
/// 責務:
/// - 申請を下書き状態で作成
/// - Repository経由で永続化
///
/// 横断的関心事はBehaviorが処理:
/// - トランザクション管理: TransactionBehavior
/// - ログ出力: LoggingBehavior
/// - 監査ログ: AuditLogBehavior
/// </summary>
public sealed class CreateApplicationCommandHandler
    : CommandPipeline<CreateApplicationCommand, Guid>
{
    private readonly IApplicationRepository _repository;
    private readonly IAppContext _appContext;

    public CreateApplicationCommandHandler(
        IApplicationRepository repository,
        IAppContext appContext)
    {
        _repository = repository;
        _appContext = appContext;
    }

    protected override async Task<Result<Guid>> ExecuteAsync(
        CreateApplicationCommand command,
        CancellationToken cancellationToken)
    {
        // 1. ドメインオペレーション（申請作成）
        var application = Domain.ApprovalWorkflow.Applications.ApprovalApplication.Create(
            _appContext.UserId,
            command.Type,
            command.Content);

        // 2. 永続化
        await _repository.AddAsync(application, cancellationToken);

        return Result.Success(application.Id);
    }
}
