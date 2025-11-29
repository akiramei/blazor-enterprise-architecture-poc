using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.ApproveApplication;

/// <summary>
/// 申請承認コマンド
///
/// 【パターン: Feature Slice - Approval Action】
///
/// 責務:
/// - 現在の承認ステップで申請を承認
/// - ワークフロー定義に基づいて次のステップに進めるか、最終承認を完了
/// </summary>
public sealed record ApproveApplicationCommand(
    Guid ApplicationId,
    string ApproverRole,
    string? Comment = null) : ICommand<Result<Unit>>;
