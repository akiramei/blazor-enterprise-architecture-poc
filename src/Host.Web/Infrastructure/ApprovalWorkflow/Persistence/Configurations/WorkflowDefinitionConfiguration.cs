using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.ApprovalWorkflow.WorkflowDefinitions;

namespace ApprovalWorkflow.Infrastructure.Persistence.Configurations;

/// <summary>
/// WorkflowDefinition エンティティのEF Core設定
///
/// 【パターン: EF Core Configuration】
///
/// 責務:
/// - Domainエンティティのプライベートフィールドをマッピング
/// - AggregateRootの子エンティティ（WorkflowStep）をマッピング
/// </summary>
public sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("aw_WorkflowDefinitions");

        // 公開プロパティを無視（privateフィールドを直接マッピングするため）
        builder.Ignore(w => w.Steps);
        builder.Ignore(w => w.TotalSteps);
        builder.Ignore(w => w.DomainEvents);

        // 主キー
        builder.HasKey(w => w.Id);

        // 申請タイプ（Enum → int）
        builder.Property(w => w.ApplicationType)
            .HasColumnName("ApplicationType")
            .HasConversion<int>()
            .IsRequired();

        // ワークフロー名
        builder.Property(w => w.Name)
            .HasColumnName("Name")
            .HasMaxLength(200)
            .IsRequired();

        // 説明
        builder.Property(w => w.Description)
            .HasColumnName("Description")
            .HasMaxLength(2000);

        // 有効フラグ
        builder.Property(w => w.IsActive)
            .HasColumnName("IsActive")
            .IsRequired();

        // 日時フィールド
        builder.Property(w => w.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(w => w.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .IsRequired();

        // 子エンティティ: WorkflowStep（親子関係）
        builder.OwnsMany(w => w.Steps, stepBuilder =>
        {
            stepBuilder.ToTable("aw_WorkflowSteps");

            stepBuilder.WithOwner().HasForeignKey("WorkflowDefinitionId");

            stepBuilder.HasKey(s => s.Id);

            // Ignore Entity base class properties
            stepBuilder.Ignore(s => s.DomainEvents);

            stepBuilder.Property(s => s.StepNumber)
                .HasColumnName("StepNumber")
                .IsRequired();

            stepBuilder.Property(s => s.Role)
                .HasColumnName("Role")
                .HasMaxLength(100)
                .IsRequired();

            stepBuilder.Property(s => s.Name)
                .HasColumnName("Name")
                .HasMaxLength(200)
                .IsRequired();

            // インデックス
            stepBuilder.HasIndex("WorkflowDefinitionId", "StepNumber")
                .IsUnique()
                .HasDatabaseName("IX_aw_WorkflowSteps_DefinitionId_StepNumber");
        });

        // インデックス
        builder.HasIndex(w => w.ApplicationType)
            .HasDatabaseName("IX_aw_WorkflowDefinitions_ApplicationType");

        builder.HasIndex(w => w.IsActive)
            .HasDatabaseName("IX_aw_WorkflowDefinitions_IsActive");

        // ユニーク制約: 同じ申請タイプに対してアクティブなワークフロー定義は1つのみ
        builder.HasIndex(w => new { w.ApplicationType, w.IsActive })
            .HasFilter("[IsActive] = 1")
            .IsUnique()
            .HasDatabaseName("IX_aw_WorkflowDefinitions_ApplicationType_Active");
    }
}
