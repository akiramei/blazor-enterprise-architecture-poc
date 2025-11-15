using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.PurchaseManagement.PurchaseRequests;

namespace Boundaries.Persistence.PurchaseManagement.Configurations;

/// <summary>
/// PurchaseRequest エンティティのEF Core設定
///
/// 【パターン: EF Core Configuration】
///
/// 責務:
/// - Domainエンティティのプライベートフィールドをマッピング
/// - AggregateRootの子エンティティ（ApprovalStep、PurchaseRequestItem）をマッピング
/// - ValueObjectの埋め込み設定（PurchaseRequestNumber、Money）
/// - 楽観的同時実行制御（Version）の設定
///
/// 実装ガイド:
/// - プライベートフィールドは文字列で指定（例: "_approvalSteps", "_items"）
/// - 子エンティティはOwnsMany()で設定（カスケード削除）
/// - Versionフィールドは自動インクリメントされる（ConcurrencyCheck）
/// - PurchaseRequestStatusはEnumとしてintでDBに保存
///
/// AI実装時の注意:
/// - プライベートフィールドへのアクセスは文字列で指定
/// - 子エンティティの主キーは自動生成（Id）
/// - 楽観的同時実行制御にはIsConcurrencyToken()を使用
/// </summary>
public sealed class PurchaseRequestConfiguration : IEntityTypeConfiguration<PurchaseRequest>
{
    public void Configure(EntityTypeBuilder<PurchaseRequest> builder)
    {
        builder.ToTable("PurchaseRequests");

        // 公開プロパティを無視（privateフィールドを直接マッピングするため）
        builder.Ignore(pr => pr.ApprovalSteps);
        builder.Ignore(pr => pr.Items);
        builder.Ignore(pr => pr.TotalAmount);
        builder.Ignore(pr => pr.CurrentApprovalStep);
        builder.Ignore(pr => pr.DomainEvents); // Entity base class property

        // 主キー
        builder.HasKey(pr => pr.Id);

        // 購買申請番号（ValueObject）
        builder.OwnsOne(pr => pr.RequestNumber, rnBuilder =>
        {
            rnBuilder.Property(rn => rn.Value)
                .HasColumnName("RequestNumber")
                .HasMaxLength(50)
                .IsRequired();
        });

        // マルチテナント対応
        builder.Property(pr => pr.TenantId)
            .HasColumnName("TenantId")
            .IsRequired();

        // 申請者
        builder.Property(pr => pr.RequesterId)
            .HasColumnName("RequesterId")
            .IsRequired();

        builder.Property(pr => pr.RequesterName)
            .HasColumnName("RequesterName")
            .HasMaxLength(200)
            .IsRequired();

        // 基本情報
        builder.Property(pr => pr.Title)
            .HasColumnName("Title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(pr => pr.Description)
            .HasColumnName("Description")
            .HasMaxLength(2000)
            .IsRequired();

        // ステータス（Enum → int）
        builder.Property(pr => pr.Status)
            .HasColumnName("Status")
            .HasConversion<int>()
            .IsRequired();

        // 日時
        builder.Property(pr => pr.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(pr => pr.SubmittedAt)
            .HasColumnName("SubmittedAt");

        builder.Property(pr => pr.ApprovedAt)
            .HasColumnName("ApprovedAt");

        builder.Property(pr => pr.RejectedAt)
            .HasColumnName("RejectedAt");

        builder.Property(pr => pr.CancelledAt)
            .HasColumnName("CancelledAt");

        // 楽観的同時実行制御用バージョン
        builder.Property<long>("Version")
            .HasColumnName("Version")
            .IsConcurrencyToken()
            .IsRequired();

        // 子エンティティ: ApprovalStep（親子関係）
        builder.OwnsMany(pr => pr.ApprovalSteps, stepBuilder =>
        {
            stepBuilder.ToTable("ApprovalSteps");

            stepBuilder.WithOwner().HasForeignKey("PurchaseRequestId");

            stepBuilder.HasKey(s => s.Id);

            // Ignore Entity base class properties
            stepBuilder.Ignore(s => s.DomainEvents);

            stepBuilder.Property(s => s.StepNumber)
                .HasColumnName("StepNumber")
                .IsRequired();

            stepBuilder.Property(s => s.ApproverId)
                .HasColumnName("ApproverId")
                .IsRequired();

            stepBuilder.Property(s => s.ApproverName)
                .HasColumnName("ApproverName")
                .HasMaxLength(200)
                .IsRequired();

            stepBuilder.Property(s => s.ApproverRole)
                .HasColumnName("ApproverRole")
                .HasMaxLength(100)
                .IsRequired();

            stepBuilder.Property(s => s.Status)
                .HasColumnName("Status")
                .HasConversion<int>()
                .IsRequired();

            stepBuilder.Property(s => s.ApprovedAt)
                .HasColumnName("ApprovedAt");

            stepBuilder.Property(s => s.RejectedAt)
                .HasColumnName("RejectedAt");

            stepBuilder.Property(s => s.Comment)
                .HasColumnName("Comment")
                .HasMaxLength(2000);
        });

        // 子エンティティ: PurchaseRequestItem（親子関係）
        builder.OwnsMany(pr => pr.Items, itemBuilder =>
        {
            itemBuilder.ToTable("PurchaseRequestItems");

            itemBuilder.WithOwner().HasForeignKey("PurchaseRequestId");

            itemBuilder.HasKey(i => i.Id);

            // Ignore Entity base class properties
            itemBuilder.Ignore(i => i.DomainEvents);

            itemBuilder.Property(i => i.ProductId)
                .HasColumnName("ProductId")
                .IsRequired();

            itemBuilder.Property(i => i.ProductName)
                .HasColumnName("ProductName")
                .HasMaxLength(200)
                .IsRequired();

            itemBuilder.Property(i => i.Quantity)
                .HasColumnName("Quantity")
                .IsRequired();

            // UnitPrice Money ValueObject
            itemBuilder.OwnsOne(i => i.UnitPrice, priceBuilder =>
            {
                priceBuilder.Property(m => m.Amount)
                    .HasColumnName("UnitPrice")
                    .HasPrecision(18, 2)
                    .IsRequired();

                priceBuilder.Property(m => m.Currency)
                    .HasColumnName("Currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            // Amount Money ValueObject
            itemBuilder.OwnsOne(i => i.Amount, amountBuilder =>
            {
                amountBuilder.Property(m => m.Amount)
                    .HasColumnName("Amount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                amountBuilder.Property(m => m.Currency)
                    .HasColumnName("Currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });
        });

        // インデックス
        builder.HasIndex(pr => pr.TenantId)
            .HasDatabaseName("IX_PurchaseRequests_TenantId");

        builder.HasIndex(pr => pr.Status)
            .HasDatabaseName("IX_PurchaseRequests_Status");

        builder.HasIndex(pr => pr.RequesterId)
            .HasDatabaseName("IX_PurchaseRequests_RequesterId");

        builder.HasIndex(pr => pr.CreatedAt)
            .HasDatabaseName("IX_PurchaseRequests_CreatedAt");

        // Global Query Filter: マルチテナント分離
        // NOTE: IAppContextはDbContext内で解決され、現在のユーザーのTenantIdでフィルタリング
        // 管理者が全テナントのデータを見る必要がある場合は IgnoreQueryFilters() を使用
        // 例: context.PurchaseRequests.IgnoreQueryFilters().Where(...)
    }
}
