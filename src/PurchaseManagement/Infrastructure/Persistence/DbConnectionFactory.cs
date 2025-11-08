using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PurchaseManagement.Shared.Application;

namespace PurchaseManagement.Infrastructure.Persistence;

/// <summary>
/// Purchase Management BC用データベース接続ファクトリ
///
/// 【パターン: Factory Implementation】
///
/// 責務:
/// - PostgreSQL接続の生成
/// - 接続文字列の管理
///
/// AI実装時の注意:
/// - IDbConnectionFactoryの実装
/// - PurchaseManagement BC専用の接続を提供
/// </summary>
public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string 'DefaultConnection' not found.");
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
