using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.RejectApplication;

/// <summary>
/// 申請却下コマンド
///
/// 【パターン: Feature Slice - Rejection Action】
///
/// 責務:
/// - 申請を却下
/// - 却下理由は必須
/// </summary>
public sealed record RejectApplicationCommand(
    Guid ApplicationId,
    string ApproverRole,
    string Reason) : ICommand<Result<Unit>>;
