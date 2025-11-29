using Domain.ApprovalWorkflow.Applications;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.GetApplicationDetail;

/// <summary>
/// 申請詳細取得クエリ
///
/// 【パターン: Query - Get By Id】
///
/// 責務:
/// - 申請の詳細情報を取得
/// - 承認履歴も含む
/// </summary>
public sealed record GetApplicationDetailQuery(
    Guid ApplicationId) : IQuery<Result<ApplicationDetailDto>>;

/// <summary>
/// 申請詳細DTO
/// </summary>
public sealed record ApplicationDetailDto(
    Guid Id,
    Guid ApplicantId,
    ApplicationType Type,
    string TypeName,
    string Content,
    ApplicationStatus Status,
    string StatusName,
    int CurrentStepNumber,
    int TotalSteps,
    Guid? WorkflowDefinitionId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    DateTime? ReturnedAt,
    List<ApprovalHistoryItemDto> ApprovalHistory,
    // Boundary情報：UIでボタン表示制御に使用
    bool CanEdit,
    bool CanSubmit,
    bool CanResubmit);

/// <summary>
/// 承認履歴項目DTO
/// </summary>
public sealed record ApprovalHistoryItemDto(
    Guid Id,
    int StepNumber,
    Guid ApproverId,
    ApprovalAction Action,
    string ActionName,
    string? Comment,
    DateTime Timestamp);
