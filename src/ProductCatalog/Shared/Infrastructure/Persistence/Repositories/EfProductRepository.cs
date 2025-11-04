using Microsoft.EntityFrameworkCore;
using ProductCatalog.Shared.Domain.Products;

namespace ProductCatalog.Shared.Infrastructure.Persistence.Repositories;

/// <summary>
/// Product リポジトリのEF Core実装
///
/// 【パターン: EF Core Repository + Infrastructure.Platform】
///
/// 責務:
/// - Productエンティティの永続化（追加・更新・削除）
/// - 集約ルートとして子エンティティ（ProductImage）も含めて取得
/// - AggregateRootの整合性を保つ
///
/// VSA構造:
/// - ProductCatalogDbContextを使用（ビジネスエンティティ専用）
/// - 技術的関心事（Outbox、AuditLog）はPlatformDbContextで管理
///
/// 実装ガイド:
/// - GetAsync()では子エンティティも含めて取得（Include）
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
public sealed class EfProductRepository : IProductRepository
{
    private readonly ProductCatalogDbContext _context;

    public EfProductRepository(ProductCatalogDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 商品を取得（子エンティティ含む）
    /// </summary>
    public async Task<Product?> GetAsync(ProductId id, CancellationToken cancellationToken = default)
    {
        // 集約ルートとして、子エンティティ（ProductImage）も含めて取得
        // ※ OwnsMany()で設定されているため、自動的にIncludeされる
        return await _context.Products
            .FirstOrDefaultAsync(p => EF.Property<ProductId>(p, "_id") == id, cancellationToken);
    }

    /// <summary>
    /// 商品を保存（追加または更新）
    /// </summary>
    public async Task SaveAsync(Product product, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync(product.Id, cancellationToken);

        if (existing is null)
        {
            // 新規追加
            // ※ 子エンティティ（ProductImage）も自動的に追加される
            await _context.Products.AddAsync(product, cancellationToken);
        }
        else
        {
            // 更新
            // ※ EF CoreのChangeTrackerが自動的に変更を検出
            // ※ 子エンティティの追加・削除・更新も自動検出
            // ※ Versionフィールドは自動的にインクリメント（楽観的同時実行制御）
            _context.Products.Update(product);
        }

        // SaveChanges is handled by TransactionBehavior
    }

    /// <summary>
    /// 商品を削除（物理削除）
    /// </summary>
    public Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
    {
        // 物理削除
        // ※ 子エンティティ（ProductImage）もカスケード削除される
        _context.Products.Remove(product);

        // SaveChanges is handled by TransactionBehavior
        return Task.CompletedTask;
    }
}
