using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ApprovalWorkflow.Infrastructure.Persistence.Repositories;
using ApprovalWorkflow.Infrastructure.Services;
using Domain.ApprovalWorkflow.Applications;
using Domain.ApprovalWorkflow.Boundaries;
using Domain.ApprovalWorkflow.WorkflowDefinitions;

namespace ApprovalWorkflow.Infrastructure;

/// <summary>
/// ApprovalWorkflow BCのDI設定
///
/// 【パターン: DI Configuration】
///
/// 責務:
/// - ApprovalWorkflow BCの全サービスを登録
/// - Repository登録
/// - Boundary Service登録
///
/// VSA構造:
/// - Infrastructure層が全てのDI設定を管理
/// - Host層はAddApprovalWorkflowServices()を呼び出すだけ
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// ApprovalWorkflow BCの全サービスを登録
    /// </summary>
    public static IServiceCollection AddApprovalWorkflowServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // NOTE: DbContextはProgram.csで登録されるため、ここでは登録しない

        // Repositories
        services.AddScoped<IApplicationRepository, EfApplicationRepository>();
        services.AddScoped<IWorkflowDefinitionRepository, EfWorkflowDefinitionRepository>();

        // Boundary Services
        services.AddScoped<IApplicationBoundary, ApplicationBoundaryService>();

        return services;
    }
}
