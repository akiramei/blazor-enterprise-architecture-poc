using System.Data;
using PurchaseManagement.Shared.Application;

namespace PurchaseManagement.Web.IntegrationTests.TestDoubles;

/// <summary>
/// Throwing Connection Factory（Fast テスト用誤使用防止）
///
/// Dapper経路を使わないFastテストで、誤ってDapperハンドラが呼ばれた場合に例外を投げる
/// </summary>
public sealed class ThrowingConnectionFactory : IDbConnectionFactory
{
    public IDbConnection CreateConnection()
    {
        throw new InvalidOperationException(
            "Dapper経路はFastテストで使用禁止です。EF Coreハンドラに置き換えてください。" +
            "もし意図的にDapperを使いたい場合は、Realテスト（Testcontainers）を使用してください。");
    }
}
