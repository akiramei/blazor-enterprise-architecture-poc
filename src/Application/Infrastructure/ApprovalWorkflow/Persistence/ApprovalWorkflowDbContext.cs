using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Domain.ApprovalWorkflow.Applications;
using Domain.ApprovalWorkflow.WorkflowDefinitions;
using Shared.Application.Interfaces;
using Shared.Domain.Outbox;
using Shared.Kernel;

namespace ApprovalWorkflow.Infrastructure.Persistence;

/// <summary>
/// ApprovalWorkflow Bounded ContextのDbContext
///
/// 【VSA + Infrastructure.Platform パターン】
///
/// 責務:
/// - ApprovalWorkflow BCのビジネスエンティティ管理（Application、WorkflowDefinition）
/// - トランザクショナルOutbox（物理同居、論理所有=Platform）
///
/// 設計原則:
/// - Bounded Context単位でのトランザクション境界
/// - ビジネスドメインロジックに集中
/// - Outbox物理同居により単一トランザクションでの原子性確保
/// </summary>
public sealed class ApprovalWorkflowDbContext : DbContext
{
    private readonly IAppContext _appContext;
    private readonly IWebHostEnvironment _environment;

    public ApprovalWorkflowDbContext(
        DbContextOptions<ApprovalWorkflowDbContext> options,
        IAppContext appContext,
        IWebHostEnvironment environment) : base(options)
    {
        _appContext = appContext;
        _environment = environment;
    }

    // ビジネスエンティティ
    public DbSet<ApprovalApplication> Applications => Set<ApprovalApplication>();
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();

    // Outbox（物理同居、論理所有=Platform）
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // DomainEventは別テーブルで管理されるため無視
        modelBuilder.Ignore<DomainEvent>();

        // ApprovalWorkflow BC のConfiguration適用（名前空間フィルタリング）
        // 同一アセンブリ内の他BC Configurationを除外するため、名前空間でフィルタリング
        var approvalWorkflowNamespace = typeof(ApprovalWorkflowDbContext).Namespace!;
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApprovalWorkflowDbContext).Assembly,
            type => type.Namespace?.StartsWith(approvalWorkflowNamespace.Replace(".Persistence", "")) == true);

        // Global Query Filter: マルチテナント分離（Test環境では無効化）
        if (!_environment.IsEnvironment("Test"))
        {
            ApplyGlobalFilters(modelBuilder);
        }
    }

    /// <summary>
    /// Global Query Filterの適用
    /// </summary>
    private void ApplyGlobalFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IMultiTenant).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ApprovalWorkflowDbContext)
                    .GetMethod(nameof(SetMultiTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .MakeGenericMethod(entityType.ClrType);

                method?.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    /// <summary>
    /// マルチテナントフィルタを設定
    /// </summary>
    private void SetMultiTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IMultiTenant
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(e => _appContext.TenantId.HasValue && e.TenantId == _appContext.TenantId);
    }
}
