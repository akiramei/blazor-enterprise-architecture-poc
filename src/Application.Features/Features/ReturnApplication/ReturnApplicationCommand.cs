using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.ReturnApplication;

/// <summary>
/// 申請差し戻しコマンド
///
/// 【パターン: Feature Slice - Return Action】
///
/// 責務:
/// - 申請を差し戻し
/// - 差し戻し理由は必須
/// - 差し戻し後、申請者は再提出可能
/// </summary>
public sealed record ReturnApplicationCommand(
    Guid ApplicationId,
    string ApproverRole,
    string Reason) : ICommand<Result<Unit>>;
