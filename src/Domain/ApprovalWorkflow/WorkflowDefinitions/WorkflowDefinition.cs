using Domain.ApprovalWorkflow.Applications;
using Shared.Kernel;

namespace Domain.ApprovalWorkflow.WorkflowDefinitions;

/// <summary>
/// ワークフロー定義（集約ルート）
///
/// 【パターン: Approval Workflow - Workflow Definition】
///
/// 責務:
/// - 申請タイプごとに承認ステップの順番を定義
/// - 各ステップの承認ロールを管理
///
/// 不変条件:
/// - ステップ番号は1から連続していること
/// - 最低1つのステップが必要
/// </summary>
public sealed class WorkflowDefinition : AggregateRoot<Guid>
{
    private readonly List<WorkflowStep> _steps = new();

    /// <summary>申請タイプ</summary>
    public ApplicationType ApplicationType { get; private set; }

    /// <summary>ワークフロー名</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>説明</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>有効フラグ</summary>
    public bool IsActive { get; private set; }

    /// <summary>作成日時</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>更新日時</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>ワークフローステップ一覧（読み取り専用）</summary>
    public IReadOnlyList<WorkflowStep> Steps => _steps.OrderBy(s => s.StepNumber).ToList().AsReadOnly();

    /// <summary>総ステップ数</summary>
    public int TotalSteps => _steps.Count;

    /// <summary>
    /// EF Core用のパラメータレスコンストラクタ
    /// </summary>
    private WorkflowDefinition() { }

    /// <summary>
    /// ワークフロー定義を作成
    /// </summary>
    public static WorkflowDefinition Create(
        ApplicationType applicationType,
        string name,
        string description,
        IEnumerable<(string Role, string Name)> steps)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("ワークフロー名は必須です");

        var stepList = steps.ToList();
        if (stepList.Count == 0)
            throw new DomainException("最低1つの承認ステップが必要です");

        var now = DateTime.UtcNow;
        var definition = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            ApplicationType = applicationType,
            Name = name,
            Description = description ?? string.Empty,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        // ステップを追加
        int stepNumber = 1;
        foreach (var (role, stepName) in stepList)
        {
            definition._steps.Add(WorkflowStep.Create(
                definition.Id, stepNumber, role, stepName));
            stepNumber++;
        }

        return definition;
    }

    /// <summary>
    /// ステップを追加
    /// </summary>
    public void AddStep(string role, string name)
    {
        var nextStepNumber = _steps.Count + 1;
        _steps.Add(WorkflowStep.Create(Id, nextStepNumber, role, name));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// ステップを削除（最後のステップのみ削除可能）
    /// </summary>
    public void RemoveLastStep()
    {
        if (_steps.Count <= 1)
            throw new DomainException("最低1つの承認ステップが必要です");

        var lastStep = _steps.OrderByDescending(s => s.StepNumber).First();
        _steps.Remove(lastStep);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// ワークフロー定義を更新
    /// </summary>
    public void Update(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("ワークフロー名は必須です");

        Name = name;
        Description = description ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// ワークフローを有効化
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// ワークフローを無効化
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 指定されたステップ番号のステップを取得
    /// </summary>
    public WorkflowStep? GetStep(int stepNumber)
    {
        return _steps.FirstOrDefault(s => s.StepNumber == stepNumber);
    }

    /// <summary>
    /// 指定されたロールが指定されたステップで承認可能かチェック
    /// </summary>
    public bool CanApproveAtStep(int stepNumber, string role)
    {
        var step = GetStep(stepNumber);
        return step != null && step.Role == role;
    }
}
