using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Shared.Domain.Identity;
using System.Security.Claims;

namespace Shared.Infrastructure.Platform;

/// <summary>
/// Identity データシーダー
/// 初回起動時にデフォルトのロールとユーザーを作成
///
/// 【マルチテナント対応】
/// - テストテナント（Tenant A, Tenant B）のマスターデータを定義
/// - ユーザー作成時にTenantId Claimを追加
/// - テナント間のデータ分離をテスト可能にする
/// </summary>
public sealed class IdentityDataSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<IdentityDataSeeder> _logger;

    /// <summary>
    /// テストテナントA（デフォルトテナント）
    /// </summary>
    private static readonly Guid TenantAId = new Guid("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// テストテナントB（マルチテナント動作確認用）
    /// </summary>
    private static readonly Guid TenantBId = new Guid("22222222-2222-2222-2222-222222222222");

    public IdentityDataSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<IdentityDataSeeder> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// ロールとユーザーをシードする
    /// </summary>
    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedUsersAsync();
    }

    /// <summary>
    /// デフォルトのロールを作成
    /// </summary>
    private async Task SeedRolesAsync()
    {
        foreach (var roleName in Roles.All)
        {
            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                var role = new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant()
                };

                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Role '{RoleName}' created successfully", roleName);
                }
                else
                {
                    _logger.LogError("Failed to create role '{RoleName}': {Errors}",
                        roleName,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    /// <summary>
    /// デフォルトのユーザーを作成
    /// </summary>
    private async Task SeedUsersAsync()
    {
        // 管理者ユーザーの作成（Tenant A所属）
        const string adminEmail = "admin@example.com";
        const string adminPassword = "Admin@123";
        const string adminDisplayName = "システム管理者";

        await CreateUserIfNotExistsAsync(
            adminEmail,
            adminPassword,
            adminDisplayName,
            Roles.Admin,
            TenantAId);

        // 一般ユーザーの作成（Tenant A所属）
        const string userEmail = "user@example.com";
        const string userPassword = "User@123";
        const string userDisplayName = "一般ユーザー";

        await CreateUserIfNotExistsAsync(
            userEmail,
            userPassword,
            userDisplayName,
            Roles.User,
            TenantAId);

        // Tenant B所属の一般ユーザー（マルチテナントテスト用）
        const string userBEmail = "userb@example.com";
        const string userBPassword = "UserB@123";
        const string userBDisplayName = "テナントBユーザー";

        await CreateUserIfNotExistsAsync(
            userBEmail,
            userBPassword,
            userBDisplayName,
            Roles.User,
            TenantBId);
    }

    /// <summary>
    /// ユーザーが存在しない場合に作成
    /// </summary>
    private async Task CreateUserIfNotExistsAsync(
        string email,
        string password,
        string displayName,
        string role,
        Guid tenantId)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            _logger.LogInformation("User '{Email}' already exists", email);

            // Check if user has TenantId claim and add if missing
            var claims = await _userManager.GetClaimsAsync(existingUser);
            if (!claims.Any(c => c.Type == "TenantId"))
            {
                var tenantClaim = new Claim("TenantId", tenantId.ToString());
                var claimResult = await _userManager.AddClaimAsync(existingUser, tenantClaim);
                if (claimResult.Succeeded)
                {
                    _logger.LogInformation("TenantId claim '{TenantId}' added to existing user '{Email}'", tenantId, email);
                }
                else
                {
                    _logger.LogError("Failed to add TenantId claim to existing user '{Email}': {Errors}",
                        email,
                        string.Join(", ", claimResult.Errors.Select(e => e.Description)));
                }
            }
            return;
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            _logger.LogInformation("User '{Email}' created successfully", email);

            // ロールを割り当て
            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (roleResult.Succeeded)
            {
                _logger.LogInformation("Role '{Role}' assigned to user '{Email}'", role, email);
            }
            else
            {
                _logger.LogError("Failed to assign role '{Role}' to user '{Email}': {Errors}",
                    role,
                    email,
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }

            // TenantId Claimを追加
            var tenantClaim = new Claim("TenantId", tenantId.ToString());
            var claimResult = await _userManager.AddClaimAsync(user, tenantClaim);
            if (claimResult.Succeeded)
            {
                _logger.LogInformation("TenantId claim '{TenantId}' assigned to user '{Email}'", tenantId, email);
            }
            else
            {
                _logger.LogError("Failed to assign TenantId claim to user '{Email}': {Errors}",
                    email,
                    string.Join(", ", claimResult.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            _logger.LogError("Failed to create user '{Email}': {Errors}",
                email,
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
