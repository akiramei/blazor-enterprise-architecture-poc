using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Abstractions;

namespace Shared.Infrastructure.Platform;

/// <summary>
/// ローカルファイルストレージサービス
///
/// 【パターン: Local File System Storage】
///
/// 責務:
/// - ローカルディスクにファイルを保存・取得・削除
/// - 開発環境およびテスト環境でのファイル管理
///
/// ストレージパス:
/// - ルートディレクトリ: appsettings.json の "FileStorage:RootPath" で設定
/// - デフォルト: "./uploads"
/// - 実際のファイルパス: {RootPath}/{storagePath}
///
/// セキュリティ:
/// - パストラバーサル攻撃を防ぐため、ストレージパスを正規化
/// - ルートディレクトリ外へのアクセスを禁止
///
/// AI実装時の注意:
/// - **本番環境では使用しない**（クラウドストレージを使用すること）
/// - テスト環境では ./uploads フォルダを .gitignore に追加
/// - ファイル名はUUIDを使用（PurchaseRequestAttachment.Create()で自動生成）
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(
        IConfiguration configuration,
        ILogger<LocalFileStorageService> logger)
    {
        _rootPath = configuration["FileStorage:RootPath"] ?? "./uploads";
        _logger = logger;

        // ルートディレクトリが存在しない場合は作成
        if (!Directory.Exists(_rootPath))
        {
            Directory.CreateDirectory(_rootPath);
            _logger.LogInformation("ファイルストレージのルートディレクトリを作成しました: {RootPath}", _rootPath);
        }
    }

    public async Task<string> UploadAsync(
        Stream stream,
        string storagePath,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath);

        // ディレクトリが存在しない場合は作成
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // ファイルを保存
        using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        _logger.LogInformation("ファイルをアップロードしました: {StoragePath} ({Size} bytes)", storagePath, stream.Length);
        return storagePath;
    }

    public Task<Stream> DownloadAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"ファイルが見つかりません: {storagePath}");
        }

        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        _logger.LogInformation("ファイルをダウンロードしました: {StoragePath}", storagePath);

        return Task.FromResult<Stream>(fileStream);
    }

    public Task DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("ファイルを削除しました: {StoragePath}", storagePath);
        }
        else
        {
            _logger.LogWarning("削除対象のファイルが見つかりません: {StoragePath}", storagePath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath);
        var exists = File.Exists(fullPath);
        return Task.FromResult(exists);
    }

    public Task<string> GetDownloadUrlAsync(
        string storagePath,
        TimeSpan? expiresIn = null,
        CancellationToken cancellationToken = default)
    {
        // ローカルファイルストレージの場合、署名付きURLの代わりに相対パスを返す
        // 本番環境（Azure Blob, S3）では実際の署名付きURLを生成する
        var url = $"/api/v1/files/download?path={Uri.EscapeDataString(storagePath)}";
        return Task.FromResult(url);
    }

    /// <summary>
    /// ストレージパスから完全なファイルパスを取得
    /// （パストラバーサル攻撃を防ぐため正規化）
    /// </summary>
    private string GetFullPath(string storagePath)
    {
        // パストラバーサル攻撃を防ぐため、".." や絶対パスを禁止
        if (storagePath.Contains("..") || Path.IsPathRooted(storagePath))
        {
            throw new ArgumentException("無効なストレージパスです", nameof(storagePath));
        }

        var fullPath = Path.Combine(_rootPath, storagePath);

        // 正規化した後、ルートディレクトリ外へのアクセスをチェック
        var normalizedFullPath = Path.GetFullPath(fullPath);
        var normalizedRootPath = Path.GetFullPath(_rootPath);

        if (!normalizedFullPath.StartsWith(normalizedRootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("ルートディレクトリ外へのアクセスは禁止されています");
        }

        return normalizedFullPath;
    }
}
