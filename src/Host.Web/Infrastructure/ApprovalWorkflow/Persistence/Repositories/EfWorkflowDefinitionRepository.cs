using Microsoft.EntityFrameworkCore;
using Domain.ApprovalWorkflow.Applications;
using Domain.ApprovalWorkflow.WorkflowDefinitions;

namespace ApprovalWorkflow.Infrastructure.Persistence.Repositories;

/// <summary>
/// WorkflowDefinition リポジトリのEF Core実装
///
/// 【パターン: EF Core Repository】
///
/// 責務:
/// - WorkflowDefinitionエンティティの永続化（追加・更新）
/// - 集約ルートとして子エンティティ（WorkflowStep）も含めて取得
/// </summary>
public sealed class EfWorkflowDefinitionRepository : IWorkflowDefinitionRepository
{
    private readonly ApprovalWorkflowDbContext _context;

    public EfWorkflowDefinitionRepository(ApprovalWorkflowDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// IDでワークフロー定義を取得
    /// </summary>
    public async Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.WorkflowDefinitions
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    /// <summary>
    /// IDでワークフロー定義を取得（ステップを含む）
    /// </summary>
    /// <remarks>
    /// OwnsMany設定によりステップは自動的に読み込まれるため、
    /// GetByIdAsyncと同じ実装になる
    /// </remarks>
    public async Task<WorkflowDefinition?> GetByIdWithStepsAsync(Guid id, CancellationToken ct = default)
    {
        return await GetByIdAsync(id, ct);
    }

    /// <summary>
    /// 申請タイプでアクティブなワークフロー定義を取得
    /// </summary>
    public async Task<WorkflowDefinition?> GetByApplicationTypeAsync(
        ApplicationType applicationType,
        CancellationToken ct = default)
    {
        return await _context.WorkflowDefinitions
            .Where(w => w.ApplicationType == applicationType && w.IsActive)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// 全ワークフロー定義を取得
    /// </summary>
    public async Task<IReadOnlyList<WorkflowDefinition>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.WorkflowDefinitions
            .OrderBy(w => w.ApplicationType)
            .ThenByDescending(w => w.IsActive)
            .ThenByDescending(w => w.CreatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// ワークフロー定義を追加
    /// </summary>
    public async Task AddAsync(WorkflowDefinition definition, CancellationToken ct = default)
    {
        await _context.WorkflowDefinitions.AddAsync(definition, ct);
        // SaveChanges is handled by TransactionBehavior
    }

    /// <summary>
    /// ワークフロー定義を更新
    /// </summary>
    public Task UpdateAsync(WorkflowDefinition definition, CancellationToken ct = default)
    {
        _context.WorkflowDefinitions.Update(definition);
        // SaveChanges is handled by TransactionBehavior
        return Task.CompletedTask;
    }
}
