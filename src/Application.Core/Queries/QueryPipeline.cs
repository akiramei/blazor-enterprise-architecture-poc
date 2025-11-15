using MediatR;
using Shared.Application;

namespace Application.Core.Queries;

/// <summary>
/// 汎用クエリ実行パイプライン基底クラス
///
/// 【目的】
/// - 個別QueryHandlerでのボイラープレート排除
/// - キャッシング・ログ・エラーハンドリングをBehaviorに委譲
/// - Handlerはデータ取得ロジックのみに集中
///
/// 【使用方法】
/// <code>
/// public class GetPurchaseRequestsHandler
///     : QueryPipeline&lt;GetPurchaseRequestsQuery, List&lt;PurchaseRequestDto&gt;&gt;
/// {
///     protected override async Task&lt;Result&lt;List&lt;PurchaseRequestDto&gt;&gt;&gt; ExecuteAsync(
///         GetPurchaseRequestsQuery query, CancellationToken ct)
///     {
///         // データ取得ロジックのみ (3-5行)
///         var items = await _connection.QueryAsync&lt;PurchaseRequestDto&gt;(...);
///         return Result.Success(items.ToList());
///     }
/// }
/// </code>
///
/// 【Commandとの違い】
/// - Command: 書き込み操作 (トランザクション必要)
/// - Query: 読み取り専用 (トランザクション不要、キャッシング可能)
///
/// 【Behaviorとの役割分担】
/// - このクラス: 例外のResult変換のみ
/// - CachingBehavior: クエリ結果のキャッシング
/// - LoggingBehavior: 実行ログ・パフォーマンス計測
/// - ValidationBehavior: 入力検証
///
/// 【工業製品化への貢献】
/// - QueryHandlerの行数を30-50行 → 3-5行に削減
/// - すべてのQueryで一貫したエラーハンドリング
/// - キャッシング戦略をBehaviorに集約
/// </summary>
/// <typeparam name="TQuery">クエリ型</typeparam>
/// <typeparam name="TResult">戻り値の型 (Result内の値)</typeparam>
public abstract class QueryPipeline<TQuery, TResult>
    : IRequestHandler<TQuery, Result<TResult>>
    where TQuery : IRequest<Result<TResult>>
{
    /// <summary>
    /// データ取得ロジックを実装するメソッド
    /// 派生クラスはこのメソッドのみをオーバーライド
    ///
    /// 【実装ガイドライン】
    /// 1. Dapper/EF Core Queryでデータ取得
    /// 2. DTOにマッピング
    /// 3. Result返却
    ///
    /// キャッシング・ログ・例外処理は不要 (Behaviorが処理)
    /// </summary>
    /// <param name="query">クエリ</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>実行結果</returns>
    protected abstract Task<Result<TResult>> ExecuteAsync(
        TQuery query,
        CancellationToken cancellationToken);

    /// <summary>
    /// MediatRから呼ばれるHandleメソッド
    /// 例外をResultに変換
    /// その他の横断的関心事はすべてBehaviorで処理
    /// </summary>
    public async Task<Result<TResult>> Handle(
        TQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            // データ取得ロジック実行
            // (CachingBehavior, LoggingBehavior等のPipelineを経由)
            return await ExecuteAsync(query, cancellationToken);
        }
        catch (Exception ex)
        {
            // すべての例外をResult.Failに変換
            // LoggingBehaviorが詳細を記録
            return Result.Fail<TResult>($"クエリ実行エラー: {ex.Message}");
        }
    }
}
