using Domain.ApprovalWorkflow.Applications;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.GetPendingApplications;

/// <summary>
/// 承認待ち申請一覧取得クエリ
///
/// 【パターン: Query - Get Pending List】
///
/// 責務:
/// - 指定されたロールの承認待ち申請一覧を取得
/// </summary>
public sealed record GetPendingApplicationsQuery(
    string ApproverRole,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<Result<List<PendingApplicationDto>>>;

/// <summary>
/// 承認待ち申請DTO
/// </summary>
public sealed record PendingApplicationDto(
    Guid Id,
    Guid ApplicantId,
    ApplicationType Type,
    string TypeName,
    string ContentPreview,
    ApplicationStatus Status,
    string StatusName,
    int CurrentStepNumber,
    string CurrentStepRole,
    DateTime CreatedAt,
    DateTime? SubmittedAt);
