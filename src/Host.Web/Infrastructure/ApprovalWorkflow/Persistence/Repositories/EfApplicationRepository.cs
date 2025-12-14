using Microsoft.EntityFrameworkCore;
using Domain.ApprovalWorkflow.Applications;

namespace ApprovalWorkflow.Infrastructure.Persistence.Repositories;

/// <summary>
/// Application リポジトリのEF Core実装
///
/// 【パターン: EF Core Repository】
///
/// 責務:
/// - Applicationエンティティの永続化（追加・更新）
/// - 集約ルートとして子エンティティ（ApprovalHistoryEntry）も含めて取得
///
/// 実装ガイド:
/// - GetByIdAsync()では子エンティティも含めて取得
/// - SaveChangesはTransactionBehaviorが実行（ここでは実行しない）
/// </summary>
public sealed class EfApplicationRepository : IApplicationRepository
{
    private readonly ApprovalWorkflowDbContext _context;

    public EfApplicationRepository(ApprovalWorkflowDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// IDで申請を取得
    /// </summary>
    public async Task<ApprovalApplication?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    /// <summary>
    /// IDで申請を取得（承認履歴を含む）
    /// </summary>
    /// <remarks>
    /// OwnsMany設定により承認履歴は自動的に読み込まれるため、
    /// GetByIdAsyncと同じ実装になる
    /// </remarks>
    public async Task<ApprovalApplication?> GetByIdWithHistoryAsync(Guid id, CancellationToken ct = default)
    {
        return await GetByIdAsync(id, ct);
    }

    /// <summary>
    /// 申請者IDで申請一覧を取得
    /// </summary>
    public async Task<IReadOnlyList<ApprovalApplication>> GetByApplicantIdAsync(Guid applicantId, CancellationToken ct = default)
    {
        return await _context.Applications
            .Where(a => a.ApplicantId == applicantId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// 承認待ちの申請一覧を取得（指定ロール、指定ステップ番号）
    /// </summary>
    public async Task<IReadOnlyList<ApprovalApplication>> GetPendingForRoleAsync(
        string role,
        int stepNumber,
        CancellationToken ct = default)
    {
        // InReview状態で、指定されたステップ番号に該当する申請を取得
        // 実際のロール判定はワークフロー定義との照合が必要なため、
        // ここでは基本的なフィルタリングのみ行う
        return await _context.Applications
            .Where(a => a.Status == ApplicationStatus.InReview
                     && a.CurrentStepNumber == stepNumber)
            .OrderBy(a => a.SubmittedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// 全申請一覧を取得（Admin用）
    /// </summary>
    public async Task<IReadOnlyList<ApprovalApplication>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Applications
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// 申請を追加
    /// </summary>
    public async Task AddAsync(ApprovalApplication application, CancellationToken ct = default)
    {
        await _context.Applications.AddAsync(application, ct);
        // SaveChanges is handled by TransactionBehavior
    }

    /// <summary>
    /// 申請を更新
    /// </summary>
    public Task UpdateAsync(ApprovalApplication application, CancellationToken ct = default)
    {
        _context.Applications.Update(application);
        // SaveChanges is handled by TransactionBehavior
        return Task.CompletedTask;
    }
}
