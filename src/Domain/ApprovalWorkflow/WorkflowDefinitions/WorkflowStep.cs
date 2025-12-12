using Shared.Kernel;

namespace Domain.ApprovalWorkflow.WorkflowDefinitions;

/// <summary>
/// ワークフローステップ（エンティティ）
///
/// 【パターン: Approval Workflow - Workflow Step】
///
/// 責務:
/// - 承認ステップの定義
/// - このステップで承認できるロール（役割）の指定
///
/// 例:
/// - ステップ1: Manager（上長）が承認
/// - ステップ2: Finance（経理）が承認
/// </summary>
public sealed class WorkflowStep : Entity
{
    /// <summary>ステップID</summary>
    public Guid Id { get; private set; }

    /// <summary>ワークフロー定義ID</summary>
    public Guid WorkflowDefinitionId { get; private set; }

    /// <summary>ステップ番号（1, 2, 3...）</summary>
    public int StepNumber { get; private set; }

    /// <summary>このステップで承認できるロール</summary>
    public string Role { get; private set; } = string.Empty;

    /// <summary>ステップ名（任意）</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// EF Core用のパラメータレスコンストラクタ
    /// </summary>
    private WorkflowStep() { }

    /// <summary>
    /// ワークフローステップを作成
    /// </summary>
    public static WorkflowStep Create(
        Guid workflowDefinitionId,
        int stepNumber,
        string role,
        string name)
    {
        if (stepNumber <= 0)
            throw new DomainException("ステップ番号は1以上である必要があります");

        if (string.IsNullOrWhiteSpace(role))
            throw new DomainException("ロールは必須です");

        return new WorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowDefinitionId,
            StepNumber = stepNumber,
            Role = role,
            Name = string.IsNullOrWhiteSpace(name) ? $"ステップ{stepNumber}" : name
        };
    }

    /// <summary>
    /// ステップ名を更新
    /// </summary>
    public void UpdateName(string name)
    {
        Name = name;
    }

    /// <summary>
    /// ロールを更新
    /// </summary>
    public void UpdateRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new DomainException("ロールは必須です");

        Role = role;
    }
}
