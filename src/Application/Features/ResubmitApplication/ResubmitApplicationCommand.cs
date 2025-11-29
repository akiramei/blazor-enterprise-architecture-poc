using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.ResubmitApplication;

/// <summary>
/// 申請再提出コマンド
///
/// 【パターン: Feature Slice - Resubmit Action】
///
/// 責務:
/// - 差し戻された申請を再提出
/// - 申請者のみが再提出可能
/// </summary>
public sealed record ResubmitApplicationCommand(
    Guid ApplicationId) : ICommand<Result<Unit>>;
