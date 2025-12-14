namespace Application.Api.Auth;

/// <summary>
/// 2FA無効化リクエスト
/// セキュリティのため、パスワード再確認が必要
/// </summary>
/// <param name="Password">ユーザーのパスワード</param>
public sealed record Disable2FARequest(string Password);
