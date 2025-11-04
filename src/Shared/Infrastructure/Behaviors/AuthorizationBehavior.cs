using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Shared.Application;
using Shared.Application.Interfaces;
using AuthorizeAttribute = Shared.Application.Attributes.AuthorizeAttribute;

namespace Shared.Infrastructure.Behaviors;

/// <summary>
/// 認可のPipeline Behavior
/// Command/Queryの認可属性をチェック
/// </summary>
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly ICurrentUserService _currentUser;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;

    public AuthorizationBehavior(
        ICurrentUserService currentUser,
        IAuthorizationService authorizationService,
        ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
    {
        _currentUser = currentUser;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // リクエストに必要な権限を取得
        var authorizeAttributes = request.GetType()
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .ToList();

        if (!authorizeAttributes.Any())
        {
            return await next();  // 認可不要
        }

        // 認証チェック
        if (!_currentUser.IsAuthenticated)
        {
            _logger.LogWarning("未認証ユーザーがアクセスを試みました: {RequestType}", typeof(TRequest).Name);
            return (TResponse)(object)Result.Fail("認証が必要です");
        }

        // 権限チェック
        foreach (var attribute in authorizeAttributes)
        {
            // ポリシーベースの認可
            if (!string.IsNullOrEmpty(attribute.Policy))
            {
                if (_currentUser.User == null)
                {
                    return (TResponse)(object)Result.Fail("認証が必要です");
                }

                var authorized = await _authorizationService.AuthorizeAsync(
                    _currentUser.User,
                    attribute.Policy);

                if (!authorized.Succeeded)
                {
                    _logger.LogWarning(
                        "ポリシー認可失敗: {Policy} ユーザー: {UserName}",
                        attribute.Policy,
                        _currentUser.UserName);
                    return (TResponse)(object)Result.Fail("この操作を実行する権限がありません");
                }
            }

            // ロールベースの認可
            if (!string.IsNullOrEmpty(attribute.Roles))
            {
                var roles = attribute.Roles.Split(',');
                var hasRole = roles.Any(role => _currentUser.IsInRole(role.Trim()));

                if (!hasRole)
                {
                    _logger.LogWarning(
                        "ロール認可失敗: 必要なロール={Roles} ユーザー={UserName}",
                        attribute.Roles,
                        _currentUser.UserName);
                    return (TResponse)(object)Result.Fail($"必要なロール: {attribute.Roles}");
                }
            }
        }

        return await next();
    }
}
