using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ProductCatalog.Domain.Identity;

namespace ProductCatalog.Infrastructure.Identity;

/// <summary>
/// Identity初期データシーダー
/// </summary>
public sealed class IdentityDataSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<IdentityDataSeeder> _logger;

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
    /// 初期データをシードする
    /// </summary>
    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedAdminUserAsync();
        await SeedTestUsersAsync();
    }

    /// <summary>
    /// ロールをシードする
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
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant()
                };

                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    _logger.LogInformation("ロール '{RoleName}' を作成しました。", roleName);
                }
                else
                {
                    _logger.LogError("ロール '{RoleName}' の作成に失敗しました: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    /// <summary>
    /// 管理者ユーザーをシードする
    /// </summary>
    private async Task SeedAdminUserAsync()
    {
        const string adminEmail = "admin@example.com";
        const string adminPassword = "Admin@123456";

        var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = "システム管理者",
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(admin, Roles.Admin);
                _logger.LogInformation("管理者ユーザー '{Email}' を作成しました。", adminEmail);
            }
            else
            {
                _logger.LogError("管理者ユーザー '{Email}' の作成に失敗しました: {Errors}",
                    adminEmail, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    /// <summary>
    /// テストユーザーをシードする（開発環境用）
    /// </summary>
    private async Task SeedTestUsersAsync()
    {
        const string testUserEmail = "user@example.com";
        const string testUserPassword = "User@123456";

        var existingUser = await _userManager.FindByEmailAsync(testUserEmail);
        if (existingUser == null)
        {
            var testUser = new ApplicationUser
            {
                UserName = testUserEmail,
                Email = testUserEmail,
                EmailConfirmed = true,
                DisplayName = "テストユーザー",
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(testUser, testUserPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(testUser, Roles.User);
                _logger.LogInformation("テストユーザー '{Email}' を作成しました。", testUserEmail);
            }
            else
            {
                _logger.LogError("テストユーザー '{Email}' の作成に失敗しました: {Errors}",
                    testUserEmail, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
