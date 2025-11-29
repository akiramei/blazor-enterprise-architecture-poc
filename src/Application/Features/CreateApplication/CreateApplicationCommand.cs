using Domain.ApprovalWorkflow.Applications;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.CreateApplication;

/// <summary>
/// 申請作成コマンド（下書き状態で作成）
///
/// 【パターン: Feature Slice - Create Entity】
///
/// 責務:
/// - 申請を下書き状態で作成
/// - 申請者IDは現在のユーザーIDから自動設定
/// </summary>
public sealed record CreateApplicationCommand(
    ApplicationType Type,
    string Content) : ICommand<Result<Guid>>;
