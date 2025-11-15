using MediatR;
using Shared.Application;

namespace UploadAttachment.Application;

/// <summary>
/// 購買申請に添付ファイルをアップロードするコマンド
///
/// 【VSA Feature: UploadAttachment】
///
/// 責務:
/// - 購買申請へのファイル添付
/// - ファイルメタデータの検証
/// - ストレージへのファイル保存
/// - PurchaseRequestAttachmentエンティティの作成
///
/// セキュリティ:
/// - ファイルサイズ制限（10MB）
/// - 許可された拡張子のみ受付
/// - テナント分離（TenantIdによる）
/// - アップロードユーザーの記録
///
/// 設計:
/// - IFormFileは受け取らず、byte[]とメタデータを受け取る
/// - API層でIFormFileからbyte[]に変換
/// - テスタビリティとレイヤー分離のため
/// </summary>
public sealed record UploadAttachmentCommand : IRequest<Result<Guid>>
{
    /// <summary>
    /// 購買申請ID
    /// </summary>
    public required Guid PurchaseRequestId { get; init; }

    /// <summary>
    /// ファイル名（元のファイル名）
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// ファイルの内容（バイナリデータ）
    /// </summary>
    public required byte[] FileContent { get; init; }

    /// <summary>
    /// Content Type (MIME Type)
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// 説明・コメント（任意）
    /// </summary>
    public string? Description { get; init; }
}
