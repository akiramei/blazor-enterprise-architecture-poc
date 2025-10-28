using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Infrastructure.Persistence.Configurations;

/// <summary>
/// Product エンティティのEF Core設定
/// </summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey("_id");

        builder.Property<ProductId>("_id")
            .HasColumnName("Id")
            .HasConversion(
                id => id.Value,
                value => new ProductId(value))
            .IsRequired();

        builder.Property("_name")
            .HasColumnName("Name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property("_description")
            .HasColumnName("Description")
            .HasMaxLength(2000)
            .IsRequired();

        // Moneyの埋め込み
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

        builder.Property("_stock")
            .HasColumnName("Stock")
            .IsRequired();

        builder.Property("_isDeleted")
            .HasColumnName("IsDeleted")
            .IsRequired();

        // DomainEventsは無視（永続化しない）
        builder.Ignore(p => p.DomainEvents);

        // 削除済み商品をフィルター（グローバルクエリフィルター）
        builder.HasQueryFilter(p => !EF.Property<bool>(p, "_isDeleted"));
    }
}
