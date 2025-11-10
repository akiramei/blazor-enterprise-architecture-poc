using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductCatalog.Shared.Domain.Products;

namespace ProductCatalog.Shared.Infrastructure.Persistence.Configurations;

/// <summary>
/// Product エンティティのEF Core設定
///
/// 【パターン: EF Core Configuration】
///
/// 責務:
/// - Domainエンティティのプライベートフィールドをマッピング
/// - AggregateRootの子エンティティ（ProductImage）をマッピング
/// - ValueObjectの埋め込み設定（Money）
/// - 楽観的同時実行制御（Version）の設定
///
/// 実装ガイド:
/// - プライベートフィールドは文字列で指定（例: "_id", "_name"）
/// - 子エンティティはOwnsMany()で設定（カスケード削除）
/// - Versionフィールドは自動インクリメントされる（ConcurrencyCheck）
/// - ProductStatusはEnumとしてintでDBに保存
///
/// AI実装時の注意:
/// - プライベートフィールドへのアクセスは文字列で指定
/// - 子エンティティの主キーはComposite Key（ProductId + ProductImageId）
/// - 楽観的同時実行制御にはIsConcurrencyToken()を使用
/// - グローバルクエリフィルターで論理削除をフィルタ
/// </summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        // 公開プロパティを無視（privateフィールドを直接マッピングするため）
        builder.Ignore(p => p.Id);
        builder.Ignore(p => p.Images);
        builder.Ignore(p => p.IsDeleted);
        builder.Ignore(p => p.Name);
        builder.Ignore(p => p.Description);
        builder.Ignore(p => p.Price);
        builder.Ignore(p => p.Stock);
        builder.Ignore(p => p.Status);

        // 主キー
        builder.HasKey("_id");

        // ProductId（Typed ID）
        builder.Property<ProductId>("_id")
            .HasColumnName("Id")
            .HasConversion(
                id => id.Value,
                value => new ProductId(value))
            .IsRequired();

        // 名前
        builder.Property("_name")
            .HasColumnName("Name")
            .HasMaxLength(200)
            .IsRequired();

        // 説明
        builder.Property("_description")
            .HasColumnName("Description")
            .HasMaxLength(2000)
            .IsRequired();

        // Moneyの埋め込み（ValueObject）
        builder.OwnsOne<Money>("_price", priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("Price")
                .HasPrecision(18, 2)
                .IsRequired();

            priceBuilder.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // 在庫
        builder.Property("_stock")
            .HasColumnName("Stock")
            .IsRequired();

        // ステータス（Enum → int）
        builder.Property<ProductStatus>("_status")
            .HasColumnName("Status")
            .HasConversion<int>()
            .IsRequired();

        // 論理削除フラグ
        builder.Property("_isDeleted")
            .HasColumnName("IsDeleted")
            .IsRequired();

        // 楽観的同時実行制御用バージョン
        // ※ EF CoreがUpdateのたびに自動的にインクリメント
        // SQLiteでは手動でインクリメント（ProductCatalogDbContext.SaveChangesAsyncで実装）
        builder.Property<long>("Version")
            .HasColumnName("Version")
            .IsConcurrencyToken()
            .IsRequired();

        // 子エンティティ: ProductImage（親子関係）
        builder.OwnsMany<ProductImage>("_images", imageBuilder =>
        {
            imageBuilder.ToTable("ProductImages");

            // Entityクラスの共通プロパティを無視
            imageBuilder.Ignore(i => i.DomainEvents);

            // ProductImageId
            imageBuilder.Property<ProductImageId>("_id")
                .HasColumnName("Id")
                .HasConversion(
                    id => id.Value,
                    value => ProductImageId.From(value))
                .IsRequired();

            // ProductId（外部キー）
            imageBuilder.Property<ProductId>("_productId")
                .HasColumnName("ProductId")
                .HasConversion(
                    id => id.Value,
                    value => new ProductId(value))
                .IsRequired();

            // Composite Key: ProductId + ProductImageId (プロパティ定義の後に設定)
            imageBuilder.HasKey("_productId", "_id");

            // URL
            imageBuilder.Property<string>("_url")
                .HasColumnName("Url")
                .HasMaxLength(1000)
                .IsRequired();

            // 表示順
            imageBuilder.Property<int>("_displayOrder")
                .HasColumnName("DisplayOrder")
                .IsRequired();

            // インデックス（検索最適化）
            imageBuilder.HasIndex("_productId", "_displayOrder");
        });

        // DomainEventsは無視（永続化しない）
        builder.Ignore(p => p.DomainEvents);

        // 削除済み商品をフィルター（グローバルクエリフィルター）
        builder.HasQueryFilter(p => !EF.Property<bool>(p, "_isDeleted"));
    }
}
