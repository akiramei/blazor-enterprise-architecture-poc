using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Common.Interfaces;
using ProductCatalog.Domain.AuditLogs;

namespace ProductCatalog.Infrastructure.Behaviors;

/// <summary>
/// 監査ログ記録Behavior
///
/// 【パターン: Pipeline Behavior - Audit Log】
///
/// 使用シナリオ:
/// - IAuditableCommandを実装したCommandの実行を自動記録
/// - ユーザーアクション、データ変更履歴の追跡
/// - コンプライアンス・セキュリティ監査
///
/// 実装方針:
/// - Command実行成功後にのみ記録（失敗時は記録しない）
/// - IAppContextから自動的にユーザー・リクエスト情報を取得
/// - 変更データはJSON形式で保存
/// - TransactionBehaviorより前に実行され、同一トランザクション内でコミット
///
/// 設計ガイド:
/// - 監査が必要なCommandにIAuditableCommandを実装
/// - GetAuditInfo()で監査情報を提供
/// - OldValues/NewValuesは必要に応じてCommandで設定
///
/// Pipeline順序:
/// - LoggingBehavior → ValidationBehavior → AuthorizationBehavior →
///   IdempotencyBehavior → CachingBehavior → **AuditLogBehavior** → TransactionBehavior
/// </summary>
/// <typeparam name="TRequest">リクエスト型</typeparam>
/// <typeparam name="TResponse">レスポンス型</typeparam>
public sealed class AuditLogBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<AuditLogBehavior<TRequest, TResponse>> _logger;
    private readonly IAppContext _appContext;
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogBehavior(
        ILogger<AuditLogBehavior<TRequest, TResponse>> logger,
        IAppContext appContext,
        IAuditLogRepository auditLogRepository)
    {
        _logger = logger;
        _appContext = appContext;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // IAuditableCommandを実装していない場合はスキップ
        if (request is not IAuditableCommand auditableCommand)
        {
            return await next();
        }

        // Command実行
        var response = await next();

        // 実行成功後に監査ログを記録
        // Note: エラー時は例外がスローされるため、ここには到達しない
        try
        {
            await RecordAuditLogAsync(auditableCommand, cancellationToken);
        }
        catch (Exception ex)
        {
            // 監査ログ記録エラーは警告ログを出力するが、Command実行結果には影響しない
            // （監査ログ記録失敗によってビジネス処理を失敗させない）
            _logger.LogWarning(ex,
                "監査ログの記録に失敗しました: {Action} [CorrelationId: {CorrelationId}]",
                auditableCommand.GetAuditInfo().Action,
                _appContext.CorrelationId);
        }

        return response;
    }

    private async Task RecordAuditLogAsync(
        IAuditableCommand auditableCommand,
        CancellationToken cancellationToken)
    {
        var auditInfo = auditableCommand.GetAuditInfo();

        // AdditionalDataをJSON形式に変換
        string? additionalDataJson = null;
        if (auditInfo.AdditionalData != null)
        {
            try
            {
                additionalDataJson = JsonSerializer.Serialize(auditInfo.AdditionalData);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AdditionalDataのJSON変換に失敗しました");
            }
        }

        // AuditLogエンティティを作成
        var auditLog = AuditLog.Create(
            userId: _appContext.UserId,
            userName: _appContext.UserName,
            tenantId: _appContext.TenantId,
            action: auditInfo.Action,
            entityType: auditInfo.EntityType,
            entityId: auditInfo.EntityId,
            oldValues: null, // TODO: 必要に応じてCommandから提供
            newValues: additionalDataJson,
            correlationId: _appContext.CorrelationId,
            requestId: _appContext.RequestId,
            requestPath: _appContext.RequestPath,
            httpMethod: _appContext.HttpMethod,
            clientIpAddress: _appContext.ClientIpAddress,
            userAgent: _appContext.UserAgent);

        // リポジトリに追加（SaveChangesはTransactionBehaviorで実行される）
        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        _logger.LogInformation(
            "監査ログを記録: {Action} on {EntityType}:{EntityId} by {UserName} [CorrelationId: {CorrelationId}]",
            auditInfo.Action,
            auditInfo.EntityType,
            auditInfo.EntityId,
            _appContext.UserName,
            _appContext.CorrelationId);
    }
}
