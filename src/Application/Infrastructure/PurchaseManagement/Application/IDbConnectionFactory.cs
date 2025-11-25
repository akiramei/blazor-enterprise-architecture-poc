using System.Data;

namespace PurchaseManagement.Shared.Application;

/// <summary>
/// データベース接続ファクトリインターフェース
///
/// 【パターン: Factory Pattern for Data Access】
///
/// 責務:
/// - Dapperクエリ用のデータベース接続を生成
/// - 接続文字列の管理を抽象化
///
/// AI実装時の注意:
/// - 読み取り専用クエリに使用
/// - 実装はShared.Infrastructureまたは各BC Infrastructureで提供
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// データベース接続を作成
    /// </summary>
    IDbConnection CreateConnection();
}
