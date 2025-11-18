using Microsoft.Extensions.Configuration;

namespace ProductCatalog.Web.IntegrationTests;

/// <summary>
/// テスト用設定を管理するクラス
/// 環境変数 > appsettings.Test.json > appsettings.json の優先順位で設定を読み込む
/// </summary>
public sealed class TestConfiguration
{
    private static readonly Lazy<IConfiguration> _configuration = new(() => BuildConfiguration());

    /// <summary>
    /// テスト用のIConfigurationインスタンスを取得
    /// </summary>
    public static IConfiguration Configuration => _configuration.Value;

    /// <summary>
    /// テスト用認証情報
    /// </summary>
    public sealed class TestCredentials
    {
        /// <summary>
        /// 管理者ユーザーのメールアドレス
        /// 環境変数: TEST_ADMIN_EMAIL
        /// </summary>
        public string AdminEmail { get; init; } = string.Empty;

        /// <summary>
        /// 管理者ユーザーのパスワード
        /// 環境変数: TEST_ADMIN_PASSWORD
        /// </summary>
        public string AdminPassword { get; init; } = string.Empty;

        /// <summary>
        /// 一般ユーザーのメールアドレス
        /// 環境変数: TEST_USER_EMAIL
        /// </summary>
        public string UserEmail { get; init; } = string.Empty;

        /// <summary>
        /// 一般ユーザーのパスワード
        /// 環境変数: TEST_USER_PASSWORD
        /// </summary>
        public string UserPassword { get; init; } = string.Empty;

        /// <summary>
        /// 設定値を検証し、必須項目が存在しない場合は例外をスロー
        /// </summary>
        public void Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(AdminEmail))
                errors.Add("TEST_ADMIN_EMAIL is required. Set it via environment variable or appsettings.Test.json");

            if (string.IsNullOrWhiteSpace(AdminPassword))
                errors.Add("TEST_ADMIN_PASSWORD is required. Set it via environment variable or appsettings.Test.json");

            if (string.IsNullOrWhiteSpace(UserEmail))
                errors.Add("TEST_USER_EMAIL is required. Set it via environment variable or appsettings.Test.json");

            if (string.IsNullOrWhiteSpace(UserPassword))
                errors.Add("TEST_USER_PASSWORD is required. Set it via environment variable or appsettings.Test.json");

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Test credentials are not configured properly:\n{string.Join("\n", errors)}");
            }
        }
    }

    /// <summary>
    /// テスト用認証情報を取得（環境変数優先）
    /// </summary>
    public static TestCredentials GetTestCredentials()
    {
        var credentials = new TestCredentials
        {
            AdminEmail = GetConfigurationValue("TestCredentials:AdminEmail", "TEST_ADMIN_EMAIL"),
            AdminPassword = GetConfigurationValue("TestCredentials:AdminPassword", "TEST_ADMIN_PASSWORD"),
            UserEmail = GetConfigurationValue("TestCredentials:UserEmail", "TEST_USER_EMAIL"),
            UserPassword = GetConfigurationValue("TestCredentials:UserPassword", "TEST_USER_PASSWORD")
        };

        // 設定値を検証
        credentials.Validate();

        return credentials;
    }

    /// <summary>
    /// 設定値を取得（環境変数 > IConfiguration の順で優先）
    /// </summary>
    private static string GetConfigurationValue(string configKey, string envKey)
    {
        // 環境変数を優先
        var envValue = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue;
        }

        // IConfigurationから取得
        return Configuration[configKey] ?? string.Empty;
    }

    /// <summary>
    /// IConfigurationを構築
    /// </summary>
    private static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Test.json", optional: true, reloadOnChange: false)
            // ローカル開発用（Gitにコミットしない）
            .AddJsonFile("appsettings.Test.Local.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
