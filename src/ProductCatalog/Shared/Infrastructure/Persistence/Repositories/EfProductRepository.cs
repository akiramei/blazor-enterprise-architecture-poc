using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<EfProductRepository> _logger;

    public EfProductRepository(
        ProductCatalogDbContext context,
        ILogger<EfProductRepository> logger)
    {
        _context = context;
        _logger = logger;
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
        _logger.LogDebug("SaveAsync called for Product: Id={ProductId}, Name={ProductName}",
            product.Id, product.Name);

        // EF CoreのChangeTrackerで既に追跡されているかチェック
        // 公開プロパティのIdを使用（EF.Propertyは LINQ query内でのみ使用可能）
        var trackedEntry = _context.ChangeTracker.Entries<Product>()
            .FirstOrDefault(e => e.Entity.Id == product.Id);

        if (trackedEntry is null)
        {
            _logger.LogDebug("Product not tracked, checking if exists in DB");

            // 追跡されていない → DBに存在するかチェック
            var existsInDb = await _context.Products
                .AsNoTracking()
                .AnyAsync(p => EF.Property<ProductId>(p, "_id") == product.Id, cancellationToken);

            if (existsInDb)
            {
                // 更新
                _logger.LogDebug("Product exists in DB, calling Update()");
                // ※ EF CoreのChangeTrackerが自動的に変更を検出
                // ※ 子エンティティの追加・削除・更新も自動検出
                // ※ Versionフィールドは自動的にインクリメント（楽観的同時実行制御）
                _context.Products.Update(product);
            }
            else
            {
                // 新規追加
                _logger.LogDebug("Product does not exist in DB, adding new");
                // ※ 子エンティティ（ProductImage）も自動的に追加される
                await _context.Products.AddAsync(product, cancellationToken);
            }
        }
        else
        {
            // 既に追跡されている場合: Detachしてから再度Update()を呼ぶ
            // これによりEF Coreが全てのプロパティを更新対象としてマークする
            _logger.LogDebug("Product already tracked (State={State}), detaching and updating",
                trackedEntry.State);

            _context.Entry(product).State = EntityState.Detached;
            _context.Products.Update(product);

            _logger.LogDebug("After detach and update, Entry.State={State}", _context.Entry(product).State);
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
