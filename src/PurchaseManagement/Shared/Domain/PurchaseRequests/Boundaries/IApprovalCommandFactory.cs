namespace PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;

/// <summary>
/// 承認コマンドファクトリー：Intent→Command変換の責務
///
/// 【パターン: Abstract Factory + Intent-Command分離】
///
/// リフレクションを使わずに、ドメイン層がコマンド生成を抽象化する。
/// 各Feature層が具体的なコマンド生成ロジックを実装する。
///
/// 設計上の利点:
/// - リフレクション不要（型安全、AOT対応）
/// - ドメイン層がApplication層のコマンドに依存しない
/// - Feature層の独立性を保ちながら、ドメインロジックを一元化
/// - UI層がIdempotencyKeyを制御できる
/// </summary>
public interface IApprovalCommandFactory
{
    /// <summary>
    /// 承認コマンドを生成
    /// </summary>
    /// <param name="requestId">申請ID</param>
    /// <param name="comment">承認コメント</param>
    /// <param name="idempotencyKey">冪等性キー（重複送信防止）</param>
    /// <returns>MediatRコマンド（IRequest）</returns>
    object CreateApproveCommand(Guid requestId, string comment, string idempotencyKey);

    /// <summary>
    /// 却下コマンドを生成
    /// </summary>
    /// <param name="requestId">申請ID</param>
    /// <param name="reason">却下理由</param>
    /// <param name="idempotencyKey">冪等性キー（重複送信防止）</param>
    /// <returns>MediatRコマンド（IRequest）</returns>
    object CreateRejectCommand(Guid requestId, string reason, string idempotencyKey);
}
