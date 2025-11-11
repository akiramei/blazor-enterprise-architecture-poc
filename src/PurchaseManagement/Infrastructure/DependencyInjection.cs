using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PurchaseManagement.Infrastructure.Persistence;
using PurchaseManagement.Infrastructure.Persistence.Repositories;
using PurchaseManagement.Infrastructure.Services;
using PurchaseManagement.Shared.Application;
using PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;

namespace PurchaseManagement.Infrastructure;

/// <summary>
/// PurchaseManagement BCのDI設定
///
/// 【パターン: DI Configuration】
///
/// 責務:
/// - PurchaseManagement BCの全サービスを登録
/// - DbContext設定
/// - Repository登録
/// - Domain Service登録
///
/// VSA構造:
/// - Infrastructure層が全てのDI設定を管理
/// - Host層はAddPurchaseManagementServices()を呼び出すだけ
///
/// AI実装時の注意:
/// - 各インターフェースと実装のペアを正しく登録
/// - DbContextの接続文字列は設定ファイルから取得
/// - Scoped/Singleton/Transientを適切に選択
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// PurchaseManagement BCの全サービスを登録
    /// </summary>
    public static IServiceCollection AddPurchaseManagementServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // NOTE: DbContextはProgram.csで登録されるため、ここでは登録しない
        // （ProductCatalogと同じパターンに従う）

        // Repositories
        services.AddScoped<IPurchaseRequestRepository, EfPurchaseRequestRepository>();

        // Read Repositories
        services.AddScoped<IDbConnectionFactory, Persistence.DbConnectionFactory>();

        // Domain Services
        services.AddScoped<IApprovalFlowService, ApprovalFlowService>();

        // Boundary Services（バウンダリーパターン: ドメインロジックとUI間の契約）
        services.AddScoped<IApprovalBoundary, ApprovalBoundaryService>();
        services.AddScoped<ISubmissionBoundary, SubmissionBoundaryService>();
        services.AddScoped<IFilteringBoundary, FilteringBoundaryService>();

        // Current User Service
        // NOTE: 実際にはShared.Infrastructure.Services.CurrentUserServiceが使用されるが、
        //       ICurrentUserServiceはPurchaseManagement.Shared.Applicationで定義されているため、
        //       アダプターが必要。一時的にここで仮実装を登録。
        // TODO: Shared.Infrastructure.Services.CurrentUserServiceを使用するよう変更
        services.AddScoped<PurchaseManagement.Shared.Application.ICurrentUserService, CurrentUserServiceAdapter>();

        // NOTE: OutboxReaderはProgram.csで登録される（ProductCatalogと同じパターンに従う）

        return services;
    }
}

/// <summary>
/// 現在のユーザー情報を取得するサービスアダプター
///
/// Shared.Infrastructure.Services.CurrentUserServiceをPurchaseManagement.Shared.Application.ICurrentUserServiceに適応
/// </summary>
/// <summary>
/// Shared.Application.Interfaces.ICurrentUserService から
/// PurchaseManagement.Shared.Application.ICurrentUserService へのアダプター
///
/// 【アダプターパターンの理由】
/// - PurchaseManagement BC が独自のインターフェースを持つ場合に使用
/// - Shared層のインターフェースをラップして、BC固有の要件に適合
/// </summary>
internal sealed class CurrentUserServiceAdapter : PurchaseManagement.Shared.Application.ICurrentUserService
{
    private readonly global::Shared.Application.Interfaces.ICurrentUserService _sharedCurrentUserService;

    public CurrentUserServiceAdapter(global::Shared.Application.Interfaces.ICurrentUserService sharedCurrentUserService)
    {
        _sharedCurrentUserService = sharedCurrentUserService;
    }

    public Guid UserId => _sharedCurrentUserService.UserId;
    public Guid? TenantId => _sharedCurrentUserService.TenantId;
    public string? UserName => _sharedCurrentUserService.UserName;
}
