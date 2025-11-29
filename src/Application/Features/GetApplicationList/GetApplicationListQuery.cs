using Domain.ApprovalWorkflow.Applications;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.GetApplicationList;

/// <summary>
/// 申請一覧取得クエリ
///
/// 【パターン: Query - Get List】
///
/// 責務:
/// - 申請者の申請一覧を取得
/// - ステータスでフィルタリング可能
/// </summary>
public sealed record GetApplicationListQuery(
    ApplicationStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<Result<List<ApplicationListItemDto>>>;

/// <summary>
/// 申請一覧項目DTO
/// </summary>
public sealed record ApplicationListItemDto(
    Guid Id,
    ApplicationType Type,
    string TypeName,
    ApplicationStatus Status,
    string StatusName,
    int CurrentStepNumber,
    DateTime CreatedAt,
    DateTime? SubmittedAt);
