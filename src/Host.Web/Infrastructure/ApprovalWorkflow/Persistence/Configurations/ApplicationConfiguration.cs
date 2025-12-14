using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.ApprovalWorkflow.Applications;

namespace ApprovalWorkflow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Application エンティティのEF Core設定
///
/// 【パターン: EF Core Configuration】
///
/// 責務:
/// - Domainエンティティのプライベートフィールドをマッピング
/// - AggregateRootの子エンティティ（ApprovalHistoryEntry）をマッピング
/// - 楽観的同時実行制御（Version）の設定
/// </summary>
public sealed class ApplicationConfiguration : IEntityTypeConfiguration<ApprovalApplication>
{
    public void Configure(EntityTypeBuilder<ApprovalApplication> builder)
    {
        builder.ToTable("aw_Applications");

        // 公開プロパティを無視（privateフィールドを直接マッピングするため）
        builder.Ignore(a => a.ApprovalHistory);
        builder.Ignore(a => a.DomainEvents);

        // 主キー
        builder.HasKey(a => a.Id);

        // 申請者ID
        builder.Property(a => a.ApplicantId)
            .HasColumnName("ApplicantId")
            .IsRequired();

        // 申請タイプ（Enum → int）
        builder.Property(a => a.Type)
            .HasColumnName("Type")
            .HasConversion<int>()
            .IsRequired();

        // 申請内容
        builder.Property(a => a.Content)
            .HasColumnName("Content")
            .HasMaxLength(4000)
            .IsRequired();

        // ステータス（Enum → int）
        builder.Property(a => a.Status)
            .HasColumnName("Status")
            .HasConversion<int>()
            .IsRequired();

        // 現在の承認ステップ番号
        builder.Property(a => a.CurrentStepNumber)
            .HasColumnName("CurrentStepNumber")
            .IsRequired();

        // ワークフロー定義ID
        builder.Property(a => a.WorkflowDefinitionId)
            .HasColumnName("WorkflowDefinitionId");

        // 日時フィールド
        builder.Property(a => a.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .IsRequired();

        builder.Property(a => a.SubmittedAt)
            .HasColumnName("SubmittedAt");

        builder.Property(a => a.ApprovedAt)
            .HasColumnName("ApprovedAt");

        builder.Property(a => a.RejectedAt)
            .HasColumnName("RejectedAt");

        builder.Property(a => a.ReturnedAt)
            .HasColumnName("ReturnedAt");

        // 楽観的同時実行制御用バージョン
        builder.Property<long>("Version")
            .HasColumnName("Version")
            .IsConcurrencyToken()
            .IsRequired();

        // 子エンティティ: ApprovalHistoryEntry（親子関係）
        builder.OwnsMany(a => a.ApprovalHistory, historyBuilder =>
        {
            historyBuilder.ToTable("aw_ApprovalHistory");

            historyBuilder.WithOwner().HasForeignKey("ApplicationId");

            historyBuilder.HasKey(h => h.Id);

            // Ignore Entity base class properties
            historyBuilder.Ignore(h => h.DomainEvents);

            historyBuilder.Property(h => h.StepNumber)
                .HasColumnName("StepNumber")
                .IsRequired();

            historyBuilder.Property(h => h.ApproverId)
                .HasColumnName("ApproverId")
                .IsRequired();

            historyBuilder.Property(h => h.Action)
                .HasColumnName("Action")
                .HasConversion<int>()
                .IsRequired();

            historyBuilder.Property(h => h.Comment)
                .HasColumnName("Comment")
                .HasMaxLength(2000);

            historyBuilder.Property(h => h.Timestamp)
                .HasColumnName("Timestamp")
                .IsRequired();
        });

        // インデックス
        builder.HasIndex(a => a.ApplicantId)
            .HasDatabaseName("IX_aw_Applications_ApplicantId");

        builder.HasIndex(a => a.Status)
            .HasDatabaseName("IX_aw_Applications_Status");

        builder.HasIndex(a => a.Type)
            .HasDatabaseName("IX_aw_Applications_Type");

        builder.HasIndex(a => a.CreatedAt)
            .HasDatabaseName("IX_aw_Applications_CreatedAt");

        builder.HasIndex(a => a.WorkflowDefinitionId)
            .HasDatabaseName("IX_aw_Applications_WorkflowDefinitionId");
    }
}
