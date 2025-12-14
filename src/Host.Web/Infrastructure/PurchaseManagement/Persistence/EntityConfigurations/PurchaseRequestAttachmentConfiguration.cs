using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.PurchaseManagement;

namespace PurchaseManagement.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 購買申請添付ファイル エンティティ設定
/// </summary>
public sealed class PurchaseRequestAttachmentConfiguration : IEntityTypeConfiguration<PurchaseRequestAttachment>
{
    public void Configure(EntityTypeBuilder<PurchaseRequestAttachment> builder)
    {
        builder.ToTable("pm_PurchaseRequestAttachments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.PurchaseRequestId)
            .IsRequired();

        builder.Property(x => x.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.StorageFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.StoragePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.FileSizeBytes)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.FileExtension)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.UploadedAt)
            .IsRequired();

        builder.Property(x => x.UploadedBy)
            .IsRequired();

        builder.Property(x => x.UploadedByName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.IsScanned)
            .IsRequired();

        builder.Property(x => x.IsMalwareDetected)
            .IsRequired();

        builder.Property(x => x.IsDeleted)
            .IsRequired();

        builder.Property(x => x.DeletedAt);

        // Indexes
        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("IX_PurchaseRequestAttachments_TenantId");

        builder.HasIndex(x => x.PurchaseRequestId)
            .HasDatabaseName("IX_PurchaseRequestAttachments_PurchaseRequestId");

        builder.HasIndex(x => x.IsDeleted)
            .HasDatabaseName("IX_PurchaseRequestAttachments_IsDeleted");

        builder.HasIndex(x => x.UploadedAt)
            .HasDatabaseName("IX_PurchaseRequestAttachments_UploadedAt");

        // Global Query Filter: 論理削除されたファイルを除外
        // Note: IMultiTenantによるテナント分離フィルタは DbContext で自動適用されます
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
