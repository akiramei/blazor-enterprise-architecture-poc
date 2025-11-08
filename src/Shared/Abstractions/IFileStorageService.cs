namespace Shared.Abstractions;

/// <summary>
/// ファイルストレージサービス インターフェース
///
/// 【パターン: Storage Abstraction Layer】
///
/// 責務:
/// - ファイルのアップロード・ダウンロード・削除操作を抽象化
/// - 複数のストレージバックエンド（Azure Blob, AWS S3, ローカルファイル）に対応
/// - ストレージ固有の実装詳細をアプリケーション層から隠蔽
///
/// 実装例:
/// - **LocalFileStorageService**: ローカルディスクにファイルを保存（開発環境用）
/// - **AzureBlobStorageService**: Azure Blob Storageに保存（本番環境推奨）
/// - **S3StorageService**: AWS S3に保存（本番環境推奨）
///
/// AI実装時の注意:
/// - 本番環境では必ずクラウドストレージ（Azure Blob, S3）を使用
/// - ローカルファイルストレージは開発・テスト環境のみ
/// - ファイル名はUUIDを使用してユニーク性を保証
/// - セキュリティ: 署名付きURL (SAS, Presigned URL) を使用してアクセス制御
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// ファイルをアップロード
    /// </summary>
    /// <param name="stream">ファイルストリーム</param>
    /// <param name="storagePath">ストレージパス（例: "purchase-requests/123/file.pdf"）</param>
    /// <param name="contentType">Content Type (MIME Type)</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>アップロード成功したストレージパス</returns>
    Task<string> UploadAsync(
        Stream stream,
        string storagePath,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ファイルをダウンロード
    /// </summary>
    /// <param name="storagePath">ストレージパス</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>ファイルストリーム</returns>
    Task<Stream> DownloadAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ファイルを削除
    /// </summary>
    /// <param name="storagePath">ストレージパス</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ファイルが存在するかチェック
    /// </summary>
    /// <param name="storagePath">ストレージパス</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>存在する場合はtrue</returns>
    Task<bool> ExistsAsync(
        string storagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 署名付きダウンロードURLを生成（時間制限付き）
    /// </summary>
    /// <param name="storagePath">ストレージパス</param>
    /// <param name="expiresIn">有効期限（デフォルト: 1時間）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>署名付きURL</returns>
    Task<string> GetDownloadUrlAsync(
        string storagePath,
        TimeSpan? expiresIn = null,
        CancellationToken cancellationToken = default);
}
