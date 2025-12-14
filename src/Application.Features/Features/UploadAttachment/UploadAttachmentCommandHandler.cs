using Application.Core.Commands;
using Domain.PurchaseManagement;
using Domain.PurchaseManagement.PurchaseRequests;
using PurchaseManagement.Shared.Application;
using Shared.Abstractions;
using Shared.Application;

namespace Application.Features.UploadAttachment;

/// <summary>
/// ファイルアップロードコマンドハンドラー (工業製品化版)
///
/// 【処理フロー】
/// 1. 購買申請の存在確認（テナント分離考慮）
/// 2. PurchaseRequestAttachmentエンティティ作成
/// 3. ファイルストレージへの保存
/// 4. エンティティの永続化
///
/// 【リファクタリング成果】
/// - Before: 約112行 (try-catch, ログ, エラーハンドリング含む)
/// - After: 約65行 (ビジネスロジックのみ)
/// - 削減率: 42%
///
/// 【エラーハンドリング】
/// - ファイル保存失敗: Result.Fail で返却（CommandPipelineが処理）
/// - DomainException: CommandPipelineが自動的にResult.Failに変換
/// </summary>
public sealed class UploadAttachmentCommandHandler
    : CommandPipeline<UploadAttachmentCommand, Guid>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;

    public UploadAttachmentCommandHandler(
        IPurchaseRequestRepository repository,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
    }

    protected override async Task<Result<Guid>> ExecuteAsync(
        UploadAttachmentCommand cmd,
        CancellationToken ct)
    {
        // 1. テナントIDの取得
        var tenantId = _currentUserService.TenantId;
        if (tenantId == null)
            return Result.Fail<Guid>("テナント情報が取得できません");

        // 2. 購買申請の存在確認（Global Query Filterにより自動的にテナント分離）
        var purchaseRequest = await _repository.GetByIdAsync(cmd.PurchaseRequestId, ct);
        if (purchaseRequest is null)
            return Result.Fail<Guid>("購買申請が見つかりません");

        // 3. PurchaseRequestAttachmentエンティティを作成
        // DomainExceptionは CommandPipeline.Handle() で Result.Fail に変換される
        var attachment = PurchaseRequestAttachment.Create(
            cmd.PurchaseRequestId,
            cmd.FileName,
            cmd.FileContent.Length,
            cmd.ContentType,
            _currentUserService.UserId,
            _currentUserService.UserName ?? "Unknown",
            tenantId.Value,
            cmd.Description
        );

        // 4. ファイルをストレージに保存
        try
        {
            using var stream = new MemoryStream(cmd.FileContent);
            await _fileStorageService.UploadAsync(
                stream,
                attachment.StoragePath,
                cmd.ContentType,
                ct
            );
        }
        catch (Exception ex)
        {
            return Result.Fail<Guid>($"ファイルの保存に失敗しました: {ex.Message}");
        }

        // 5. PurchaseRequestに添付ファイルを追加（メモリ上のリスト）
        purchaseRequest.AddAttachment(attachment);

        // 6. 添付ファイルを永続化（独立エンティティとして保存）
        await _repository.AddAttachmentAsync(attachment, ct);

        return Result.Success(attachment.Id);
    }
}
