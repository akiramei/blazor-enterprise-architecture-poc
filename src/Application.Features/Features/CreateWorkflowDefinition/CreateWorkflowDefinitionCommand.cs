using Domain.ApprovalWorkflow.Applications;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.CreateWorkflowDefinition;

/// <summary>
/// ワークフロー定義作成コマンド
///
/// 【パターン: Feature Slice - Create Entity】
///
/// 責務:
/// - 新しいワークフロー定義を作成
/// - 承認ステップを設定
/// </summary>
public sealed record CreateWorkflowDefinitionCommand(
    ApplicationType ApplicationType,
    string Name,
    string Description,
    List<WorkflowStepInput> Steps) : ICommand<Result<Guid>>;

/// <summary>
/// ワークフローステップ入力
/// </summary>
public sealed record WorkflowStepInput(
    string Role,
    string Name);
