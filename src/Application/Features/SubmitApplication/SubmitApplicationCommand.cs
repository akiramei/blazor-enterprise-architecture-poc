using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.SubmitApplication;

/// <summary>
/// 申請提出コマンド
///
/// 【パターン: Feature Slice - State Transition】
///
/// 責務:
/// - 下書き状態の申請を提出
/// - ワークフロー定義に基づいて承認フローを開始
/// </summary>
public sealed record SubmitApplicationCommand(
    Guid ApplicationId) : ICommand<Result<Unit>>;
