using MediatR;
using Shared.Application;
using Shared.Kernel;

namespace Application.Core.Commands;

/// <summary>
/// 汎用コマンド実行パイプライン基底クラス
///
/// 【目的】
/// - 個別Handlerでのボイラープレート排除
/// - トランザクション・ログ・エラーハンドリングをBehaviorに委譲
/// - Handlerは純粋なドメインロジックのみに集中
///
/// 【使用方法】
/// <code>
/// public class SubmitPurchaseRequestHandler
///     : CommandPipeline&lt;SubmitPurchaseRequestCommand, Guid&gt;
/// {
///     protected override async Task&lt;Result&lt;Guid&gt;&gt; ExecuteAsync(
///         SubmitPurchaseRequestCommand cmd, CancellationToken ct)
///     {
///         // ドメインロジックのみ (3-5行)
///         var request = PurchaseRequest.Create(...);
///         await _repository.SaveAsync(request, ct);
///         return Result.Success(request.Id);
///     }
/// }
/// </code>
///
/// 【Behaviorとの役割分担】
/// - このクラス: ドメイン例外のResult変換のみ
/// - TransactionBehavior: トランザクション管理・Commit/Rollback
/// - LoggingBehavior: 実行ログ・パフォーマンス計測
/// - ValidationBehavior: 入力検証
/// - AuditLogBehavior: 監査ログ
///
/// 【工業製品化への貢献】
/// - Handlerの行数を50-100行 → 5-10行に削減
/// - すべてのHandlerで一貫したエラーハンドリング
/// - 横断的関心事をBehaviorに集約 → 個別実装不要
/// </summary>
/// <typeparam name="TCommand">コマンド型</typeparam>
/// <typeparam name="TResult">戻り値の型 (Result内の値)</typeparam>
public abstract class CommandPipeline<TCommand, TResult>
    : IRequestHandler<TCommand, Result<TResult>>
    where TCommand : IRequest<Result<TResult>>
{
    /// <summary>
    /// ドメインロジックを実装するメソッド
    /// 派生クラスはこのメソッドのみをオーバーライド
    ///
    /// 【実装ガイドライン】
    /// 1. Boundary経由で資格チェック
    /// 2. Domainオペレーション実行
    /// 3. Repository経由で永続化
    /// 4. Result返却
    ///
    /// トランザクション・ログ・例外処理は不要 (Behaviorが処理)
    /// </summary>
    /// <param name="command">コマンド</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>実行結果</returns>
    protected abstract Task<Result<TResult>> ExecuteAsync(
        TCommand command,
        CancellationToken cancellationToken);

    /// <summary>
    /// MediatRから呼ばれるHandleメソッド
    /// ドメイン例外をResultに変換
    /// その他の横断的関心事はすべてBehaviorで処理
    /// </summary>
    public async Task<Result<TResult>> Handle(
        TCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            // ドメインロジック実行
            // (TransactionBehavior, LoggingBehavior等のPipelineを経由)
            return await ExecuteAsync(command, cancellationToken);
        }
        catch (DomainException ex)
        {
            // ドメインルール違反 → Result.Failに変換
            // TransactionBehaviorがロールバック
            return Result.Fail<TResult>(ex.Message);
        }
        // インフラ例外は再スロー → LoggingBehaviorが記録
    }
}
