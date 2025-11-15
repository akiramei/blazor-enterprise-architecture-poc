using Shared.Application;
using Shared.Application.Interfaces;

namespace Application.Features.UploadAttachment;

/// <summary>
/// 購買申請に添付ファイルをアップロードするコマンド
/// </summary>
public class UploadAttachmentCommand : ICommand<Result<Guid>>
{
    /// <summary>
    /// 購買申請ID
    /// </summary>
    public Guid PurchaseRequestId { get; init; }

    /// <summary>
    /// ファイル名（元のファイル名）
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// ファイルの内容（バイナリデータ）
    /// </summary>
    public byte[] FileContent { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// Content Type (MIME Type)
    /// </summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>
    /// 説明・コメント（任意）
    /// </summary>
    public string? Description { get; init; }
}
