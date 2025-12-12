namespace Shared.Application.Interfaces;

/// <summary>
/// 監査ログ対象のCommandマーカーインターフェース
///
/// 【パターン: マーカーインターフェース】
///
/// 使用シナリオ:
/// - Commandが監査ログの記録対象であることを示す
/// - AuditLogBehaviorがこのインターフェースを実装したCommandのみ記録
///
/// 実装ガイド:
/// - 監査が必要なCommand（Create, Update, Delete等）に実装
/// - GetAuditInfo()で監査に必要な情報を提供
///
/// 使用例:
/// <code>
/// public record DeleteProductCommand(Guid ProductId)
///     : ICommand<Result>, IAuditableCommand
/// {
///     public AuditInfo GetAuditInfo()
///     {
///         return new AuditInfo(
///             Action: "DeleteProduct",
///             EntityType: "Product",
///             EntityId: ProductId.ToString());
///     }
/// }
/// </code>
/// </summary>
public interface IAuditableCommand
{
    /// <summary>
    /// 監査情報を取得
    /// </summary>
    AuditInfo GetAuditInfo();
}

/// <summary>
/// 監査情報
/// </summary>
/// <param name="Action">アクション名（例: CreateProduct, UpdateProduct, DeleteProduct）</param>
/// <param name="EntityType">エンティティ型（例: Product, Order）</param>
/// <param name="EntityId">エンティティID</param>
/// <param name="AdditionalData">追加データ（任意、JSON形式で保存される）</param>
public sealed record AuditInfo(
    string Action,
    string EntityType,
    string EntityId,
    object? AdditionalData = null);
