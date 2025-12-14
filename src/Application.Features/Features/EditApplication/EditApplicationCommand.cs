using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.EditApplication;

/// <summary>
/// 申請編集コマンド（下書き状態のみ編集可能）
///
/// 【パターン: Feature Slice - Update Entity】
///
/// 責務:
/// - 申請内容の編集
/// - 下書き状態の申請のみ編集可能
/// - 編集者は申請者本人のみ
/// </summary>
public sealed record EditApplicationCommand(
    Guid ApplicationId,
    string Content) : ICommand<Result<Unit>>;
