using Domain.ApprovalWorkflow.Applications;

namespace Domain.ApprovalWorkflow.WorkflowDefinitions;

/// <summary>
/// ワークフロー定義リポジトリインターフェース
/// </summary>
public interface IWorkflowDefinitionRepository
{
    /// <summary>
    /// IDでワークフロー定義を取得
    /// </summary>
    Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// IDでワークフロー定義を取得（ステップを含む）
    /// </summary>
    Task<WorkflowDefinition?> GetByIdWithStepsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 申請タイプでアクティブなワークフロー定義を取得
    /// </summary>
    Task<WorkflowDefinition?> GetByApplicationTypeAsync(ApplicationType applicationType, CancellationToken ct = default);

    /// <summary>
    /// 全ワークフロー定義を取得
    /// </summary>
    Task<IReadOnlyList<WorkflowDefinition>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// ワークフロー定義を追加
    /// </summary>
    Task AddAsync(WorkflowDefinition definition, CancellationToken ct = default);

    /// <summary>
    /// ワークフロー定義を更新
    /// </summary>
    Task UpdateAsync(WorkflowDefinition definition, CancellationToken ct = default);
}
