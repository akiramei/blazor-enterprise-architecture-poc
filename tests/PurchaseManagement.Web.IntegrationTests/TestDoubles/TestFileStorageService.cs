using Shared.Abstractions;

namespace PurchaseManagement.Web.IntegrationTests.TestDoubles;

/// <summary>
/// テスト用ファイルストレージサービス
///
/// インメモリでファイルを保存し、テストで動作検証を可能にする
/// </summary>
public sealed class TestFileStorageService : IFileStorageService
{
    private readonly Dictionary<string, (byte[] Content, string ContentType)> _storage = new();

    public Task<string> UploadAsync(
        Stream stream,
        string storagePath,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        _storage[storagePath] = (memoryStream.ToArray(), contentType);
        return Task.FromResult(storagePath);
    }

    public Task<Stream> DownloadAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        if (!_storage.TryGetValue(storagePath, out var file))
        {
            throw new FileNotFoundException($"File not found: {storagePath}");
        }

        return Task.FromResult<Stream>(new MemoryStream(file.Content));
    }

    public Task DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        _storage.Remove(storagePath);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_storage.ContainsKey(storagePath));
    }

    public Task<string> GetDownloadUrlAsync(
        string storagePath,
        TimeSpan? expiresIn = null,
        CancellationToken cancellationToken = default)
    {
        // テスト用のダミーURL
        return Task.FromResult($"https://test.storage.local/{storagePath}?sig=testsig");
    }
}
