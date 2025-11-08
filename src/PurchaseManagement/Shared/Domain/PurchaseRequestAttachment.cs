using Shared.Kernel;

namespace PurchaseManagement.Shared.Domain;

/// <summary>
/// 購買申請 添付ファイル
///
/// 【パターン: Value Object + File Metadata Entity】
///
/// 責務:
/// - 購買申請に添付されたファイルのメタデータを管理
/// - ファイルの検証（サイズ、拡張子、ウイルススキャン）
/// - ファイルストレージのパスとアクセス情報を保持
///
/// 設計パターン:
/// - **Entity**: 一意なIDを持つエンティティ（ファイルごとにユニーク）
/// - **Immutable**: 作成後は変更不可（削除のみ可能）
/// - **Value Object (FileMetadata)**: ファイルの属性を値オブジェクトとして管理
///
/// セキュリティ:
/// - ファイル拡張子のホワイトリスト検証
/// - ファイルサイズの上限チェック
/// - ウイルススキャン結果の記録
///
/// AI実装時の注意:
/// - ファイルの実体は外部ストレージ（Azure Blob, S3など）に保存
/// - このエンティティはメタデータのみを保持
/// - ダウンロード時はストレージパスを使用してファイルを取得
/// </summary>
public sealed class PurchaseRequestAttachment : Entity
{
    /// <summary>
    /// 許可するファイル拡張子
    /// </summary>
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv"
    };

    /// <summary>
    /// ファイルサイズの上限（10MB）
    /// </summary>
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    /// <summary>
    /// 添付ファイルID
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// 購買申請ID（外部キー）
    /// </summary>
    public Guid PurchaseRequestId { get; private set; }

    /// <summary>
    /// 元のファイル名
    /// </summary>
    public string OriginalFileName { get; private set; } = string.Empty;

    /// <summary>
    /// ストレージ上のファイル名（UUID + 拡張子）
    /// </summary>
    public string StorageFileName { get; private set; } = string.Empty;

    /// <summary>
    /// ストレージパス（コンテナ/バケット内のパス）
    /// </summary>
    public string StoragePath { get; private set; } = string.Empty;

    /// <summary>
    /// ファイルサイズ（バイト）
    /// </summary>
    public long FileSizeBytes { get; private set; }

    /// <summary>
    /// Content Type (MIME Type)
    /// </summary>
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>
    /// ファイル拡張子
    /// </summary>
    public string FileExtension { get; private set; } = string.Empty;

    /// <summary>
    /// アップロード日時
    /// </summary>
    public DateTime UploadedAt { get; private set; }

    /// <summary>
    /// アップロードしたユーザーID
    /// </summary>
    public Guid UploadedBy { get; private set; }

    /// <summary>
    /// アップロードしたユーザー名
    /// </summary>
    public string UploadedByName { get; private set; } = string.Empty;

    /// <summary>
    /// 説明・コメント
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// ウイルススキャン済みフラグ
    /// </summary>
    public bool IsScanned { get; private set; }

    /// <summary>
    /// ウイルス検出フラグ
    /// </summary>
    public bool IsMalwareDetected { get; private set; }

    /// <summary>
    /// 削除済みフラグ（論理削除）
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// 削除日時
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    // EF Core用のプライベートコンストラクタ
    private PurchaseRequestAttachment() { }

    /// <summary>
    /// 添付ファイルを作成
    /// </summary>
    public static PurchaseRequestAttachment Create(
        Guid purchaseRequestId,
        string originalFileName,
        long fileSizeBytes,
        string contentType,
        Guid uploadedBy,
        string uploadedByName,
        string? description = null)
    {
        // ファイルサイズの検証
        if (fileSizeBytes <= 0)
            throw new ArgumentException("ファイルサイズは0より大きくなければなりません", nameof(fileSizeBytes));

        if (fileSizeBytes > MaxFileSizeBytes)
            throw new ArgumentException($"ファイルサイズは{MaxFileSizeBytes / 1024 / 1024}MB以下でなければなりません", nameof(fileSizeBytes));

        // ファイル名の検証
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("ファイル名は必須です", nameof(originalFileName));

        // 拡張子の検証
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException($"許可されていないファイル形式です。許可される拡張子: {string.Join(", ", AllowedExtensions)}", nameof(originalFileName));

        // ストレージファイル名の生成（UUID + 拡張子）
        var storageFileName = $"{Guid.NewGuid()}{extension}";

        // ストレージパスの生成（purchase-requests/{PurchaseRequestId}/{ファイル名}）
        var storagePath = $"purchase-requests/{purchaseRequestId}/{storageFileName}";

        return new PurchaseRequestAttachment
        {
            Id = Guid.NewGuid(),
            PurchaseRequestId = purchaseRequestId,
            OriginalFileName = originalFileName,
            StorageFileName = storageFileName,
            StoragePath = storagePath,
            FileSizeBytes = fileSizeBytes,
            ContentType = contentType,
            FileExtension = extension,
            UploadedAt = DateTime.UtcNow,
            UploadedBy = uploadedBy,
            UploadedByName = uploadedByName,
            Description = description,
            IsScanned = false,
            IsMalwareDetected = false,
            IsDeleted = false
        };
    }

    /// <summary>
    /// ウイルススキャン完了をマーク
    /// </summary>
    public void MarkAsScanned(bool malwareDetected)
    {
        IsScanned = true;
        IsMalwareDetected = malwareDetected;

        if (malwareDetected)
        {
            // マルウェア検出時は自動的に削除マーク
            Delete();
        }
    }

    /// <summary>
    /// 説明を更新
    /// </summary>
    public void UpdateDescription(string description)
    {
        Description = description;
    }

    /// <summary>
    /// 論理削除
    /// </summary>
    public void Delete()
    {
        if (IsDeleted)
            throw new InvalidOperationException("このファイルは既に削除されています");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
