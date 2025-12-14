using Domain.ApprovalWorkflow.Applications;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.GetWorkflowDefinitions;

/// <summary>
/// ワークフロー定義一覧取得クエリ
///
/// 【パターン: Query - Get List】
///
/// 責務:
/// - 全ワークフロー定義の一覧を取得
/// </summary>
public sealed record GetWorkflowDefinitionsQuery(
    bool? ActiveOnly = null) : IQuery<Result<List<WorkflowDefinitionDto>>>;

/// <summary>
/// ワークフロー定義DTO
/// </summary>
public sealed record WorkflowDefinitionDto(
    Guid Id,
    ApplicationType ApplicationType,
    string ApplicationTypeName,
    string Name,
    string Description,
    bool IsActive,
    int TotalSteps,
    List<WorkflowStepDto> Steps,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// ワークフローステップDTO
/// </summary>
public sealed record WorkflowStepDto(
    Guid Id,
    int StepNumber,
    string Role,
    string Name);
