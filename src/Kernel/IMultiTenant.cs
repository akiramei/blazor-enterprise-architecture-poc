namespace Shared.Kernel;

/// <summary>
/// マルチテナント対応エンティティのマーカーインターフェース
///
/// 【パターン: Multi-Tenant Entity Pattern】
///
/// 責務:
/// - マルチテナント対応が必要なエンティティを識別
/// - TenantIdプロパティの標準化
/// - Global Query Filterの自動適用対象として使用
///
/// 使用例:
/// ```csharp
/// public sealed class PurchaseRequest : Entity, IMultiTenant
/// {
///     public Guid TenantId { get; private set; }
///
///     public static PurchaseRequest Create(..., Guid tenantId)
///     {
///         return new PurchaseRequest { TenantId = tenantId, ... };
///     }
/// }
/// ```
///
/// Global Query Filter適用例:
/// ```csharp
/// modelBuilder.Entity<PurchaseRequest>()
///     .HasQueryFilter(e => e.TenantId == appContext.TenantId);
/// ```
///
/// AI実装時の注意:
/// - マルチテナント対応が必要なエンティティにこのインターフェースを実装
/// - TenantIdはCreate時に設定し、以降変更不可（private set）
/// - DbContextでGlobal Query Filterを適用してテナント分離を実現
/// - 管理者が全テナントのデータを見る場合は IgnoreQueryFilters() を使用
/// </summary>
public interface IMultiTenant
{
    /// <summary>
    /// テナントID
    /// </summary>
    Guid TenantId { get; }
}
