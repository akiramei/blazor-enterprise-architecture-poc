using Microsoft.EntityFrameworkCore;
using PurchaseManagement.Shared.Application;
using PurchaseManagement.Shared.Domain.PurchaseRequests;

namespace PurchaseManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// PurchaseRequest リポジトリのEF Core実装
///
/// 【パターン: EF Core Repository + Infrastructure.Platform】
///
/// 責務:
/// - PurchaseRequestエンティティの永続化（追加・更新・削除）
/// - 集約ルートとして子エンティティ（ApprovalStep、PurchaseRequestItem）も含めて取得
/// - AggregateRootの整合性を保つ
///
/// VSA構造:
/// - PurchaseManagementDbContextを使用（ビジネスエンティティ専用）
/// - 技術的関心事（Outbox、AuditLog）はPlatformDbContextで管理
///
/// 実装ガイド:
/// - GetByIdAsync()では子エンティティも含めて取得（Include）
/// - SaveAsync()は新規追加/更新を判定
/// - 楽観的同時実行制御はEF Coreが自動的に処理（Versionフィールド）
/// - SaveChangesはTransactionBehaviorが実行（ここでは実行しない）
///
/// AI実装時の注意:
/// - 集約ルートを取得する際は、必ず子エンティティもIncludeする
/// - AsNoTracking()は使用しない（更新が必要なため）
/// - EF CoreのChangeTrackerが自動的に子エンティティの変更を検出
/// - Versionフィールドによる同時実行制御はEF Coreが自動処理
/// </summary>
public sealed class EfPurchaseRequestRepository : IPurchaseRequestRepository
{
    private readonly PurchaseManagementDbContext _context;

    public EfPurchaseRequestRepository(PurchaseManagementDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 購買申請を取得（子エンティティ含む）
    /// </summary>
    public async Task<PurchaseRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        // 集約ルートとして、子エンティティ（ApprovalStep、PurchaseRequestItem）も含めて取得
        // ※ OwnsMany()で設定されている場合は自動的にIncludeされる
        return await _context.PurchaseRequests
            .FirstOrDefaultAsync(pr => pr.Id == id, cancellationToken);
    }

    /// <summary>
    /// 購買申請を保存（追加または更新）
    /// </summary>
    public async Task SaveAsync(PurchaseRequest purchaseRequest, CancellationToken cancellationToken)
    {
        var existing = await GetByIdAsync(purchaseRequest.Id, cancellationToken);

        if (existing is null)
        {
            // 新規追加
            // ※ 子エンティティ（ApprovalStep、PurchaseRequestItem）も自動的に追加される
            await _context.PurchaseRequests.AddAsync(purchaseRequest, cancellationToken);
        }
        else
        {
            // 更新
            // ※ EF CoreのChangeTrackerが自動的に変更を検出
            // ※ 子エンティティの追加・削除・更新も自動検出
            // ※ Versionフィールドは自動的にインクリメント（楽観的同時実行制御）
            _context.PurchaseRequests.Update(purchaseRequest);
        }

        // SaveChanges is handled by TransactionBehavior
    }
}
