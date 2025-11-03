using System.Security.Claims;

namespace ProductCatalog.Application.Common.Interfaces;

/// <summary>
/// アプリケーションコンテキスト - 横断的なリクエストスコープ情報へのアクセスを提供
///
/// 【パターン: AppContext/Kernel】
///
/// 使用シナリオ:
/// - 現在のユーザー情報の取得
/// - リクエスト追跡情報の取得（Correlation ID, Request ID）
/// - リクエストメタデータの取得（開始時刻、環境情報等）
///
/// 設計ガイド:
/// - すべてのCommand/QueryHandlerでIAppContextを使用可能
/// - リクエストスコープでDI登録（Scoped）
/// - Lazy初期化によるパフォーマンス最適化
/// - HttpContextへの直接依存を避け、テスト容易性を確保
///
/// 移行戦略:
/// - 既存のICurrentUserServiceとICorrelationIdAccessorは互換性維持
/// - 新規実装ではIAppContextを優先使用
/// - 将来的にICurrentUserService/ICorrelationIdAccessorはIAppContextのファサードとして実装可能
/// </summary>
public interface IAppContext
{
    // ===================================
    // ユーザー情報（ICurrentUserServiceから統合）
    // ===================================

    /// <summary>
    /// 現在のユーザーID
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// 現在のユーザー名
    /// </summary>
    string UserName { get; }

    /// <summary>
    /// 現在のテナントID（マルチテナント対応）
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// 認証状態
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// ClaimsPrincipal
    /// </summary>
    ClaimsPrincipal User { get; }

    /// <summary>
    /// 指定されたロールに所属しているかを判定
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// 複数のロールのいずれかに所属しているかを判定
    /// </summary>
    bool IsInAnyRole(params string[] roles);

    // ===================================
    // リクエスト追跡情報（ICorrelationIdAccessorから統合）
    // ===================================

    /// <summary>
    /// Correlation ID - 分散トレーシング用
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Request ID - 個別リクエスト識別用
    /// </summary>
    Guid RequestId { get; }

    // ===================================
    // リクエストメタデータ
    // ===================================

    /// <summary>
    /// リクエスト開始時刻（UTC）
    /// </summary>
    DateTime RequestStartTimeUtc { get; }

    /// <summary>
    /// リクエストパス（例: /api/products）
    /// </summary>
    string RequestPath { get; }

    /// <summary>
    /// HTTPメソッド（例: GET, POST）
    /// </summary>
    string HttpMethod { get; }

    /// <summary>
    /// クライアントIPアドレス
    /// </summary>
    string? ClientIpAddress { get; }

    /// <summary>
    /// User-Agentヘッダー
    /// </summary>
    string? UserAgent { get; }

    // ===================================
    // 環境情報
    // ===================================

    /// <summary>
    /// 環境名（Development, Staging, Production等）
    /// </summary>
    string EnvironmentName { get; }

    /// <summary>
    /// ホスト名
    /// </summary>
    string HostName { get; }

    /// <summary>
    /// アプリケーション名
    /// </summary>
    string ApplicationName { get; }
}
