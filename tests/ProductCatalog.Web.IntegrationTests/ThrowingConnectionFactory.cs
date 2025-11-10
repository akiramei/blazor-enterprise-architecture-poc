using PurchaseManagement.Shared.Application;
using System.Data;

namespace ProductCatalog.Web.IntegrationTests;

/// <summary>
/// Dapper誤使用を防ぐためのConnectionFactory
/// テスト環境でDapperハンドラが使用された場合に例外をスローする
/// </summary>
public class ThrowingConnectionFactory : IDbConnectionFactory
{
    public IDbConnection CreateConnection()
    {
        throw new InvalidOperationException(
            "❌ Dapper handler detected in test! " +
            "Replace with EF Core handler for fast in-memory testing.");
    }
}
