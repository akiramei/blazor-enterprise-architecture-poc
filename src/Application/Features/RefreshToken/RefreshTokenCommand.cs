using MediatR;
using Shared.Application;
using Shared.Application.Interfaces;


namespace Application.Features.RefreshToken;

/// <summary>
/// Refresh Tokenコマンド
///
/// 【Phase 3改善】
/// - AuthController.RefreshToken()からビジネスロジックを分離
/// - CQRS/MediatRパターン適用
/// - Refresh Token Rotation実装
///
/// 【責務】
/// - Access Token検証
/// - Refresh Token検証
/// - 古いRefresh Tokenの無効化
/// - 新しいトークンペア生成
/// </summary>
public class RefreshTokenCommand : ICommand<Result<RefreshTokenResult>>
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
}

/// <summary>
/// Refresh Token結果
/// </summary>
public record RefreshTokenResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);
