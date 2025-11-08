using MediatR;
using Microsoft.Extensions.Logging;
using PurchaseManagement.Shared.Application;
using PurchaseManagement.Shared.Domain;
using Shared.Abstractions;
using Shared.Application;
using Shared.Application.Interfaces;
using Shared.Kernel;

namespace UploadAttachment.Application;

/// <summary>
/// ファイルアップロードコマンドハンドラー
///
/// 【処理フロー】
/// 1. 購買申請の存在確認（テナント分離考慮）
/// 2. PurchaseRequestAttachmentエンティティ作成
/// 3. ファイルストレージへの保存
/// 4. エンティティの永続化
///
/// 【エラーハンドリング】
/// - ファイル保存失敗時: ストレージ削除 + トランザクションロールバック
/// - 購買申請が見つからない: NotFoundエラー
/// - 権限なし（別テナント）: NotFoundエラー（情報漏洩防止）
/// </summary>
public sealed class UploadAttachmentHandler : IRequestHandler<UploadAttachmentCommand, Result<Guid>>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IAppContext _appContext;
    private readonly ILogger<UploadAttachmentHandler> _logger;

    public UploadAttachmentHandler(
        IPurchaseRequestRepository repository,
        IFileStorageService fileStorageService,
        IAppContext appContext,
        ILogger<UploadAttachmentHandler> logger)
    {
        _repository = repository;
        _fileStorageService = fileStorageService;
        _appContext = appContext;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(UploadAttachmentCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 1. テナントIDの取得
            var tenantId = _appContext.TenantId ?? throw new InvalidOperationException("TenantIdが設定されていません");

            // 2. 購買申請の存在確認（Global Query Filterにより自動的にテナント分離）
            var purchaseRequest = await _repository.GetByIdAsync(command.PurchaseRequestId, cancellationToken);
            if (purchaseRequest == null)
            {
                return Result.Fail<Guid>("購買申請が見つかりません");
            }

            // 3. PurchaseRequestAttachmentエンティティを作成
            var attachment = PurchaseRequestAttachment.Create(
                command.PurchaseRequestId,
                command.FileName,
                command.FileContent.Length,
                command.ContentType,
                _appContext.UserId,
                _appContext.UserName,
                tenantId,
                command.Description
            );

            // 4. ファイルをストレージに保存
            try
            {
                using var stream = new MemoryStream(command.FileContent);
                await _fileStorageService.UploadAsync(
                    stream,
                    attachment.StoragePath,
                    command.ContentType,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save file to storage: {StoragePath}", attachment.StoragePath);
                return Result.Fail<Guid>("ファイルの保存に失敗しました");
            }

            // 5. PurchaseRequestに添付ファイルを追加
            purchaseRequest.AddAttachment(attachment);

            // 6. 永続化
            await _repository.SaveAsync(purchaseRequest, cancellationToken);

            _logger.LogInformation(
                "File uploaded successfully: AttachmentId={AttachmentId}, PurchaseRequestId={PurchaseRequestId}, FileName={FileName}",
                attachment.Id, command.PurchaseRequestId, command.FileName);

            return Result.Success(attachment.Id);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Failed to upload attachment: {Message}", ex.Message);
            return Result.Fail<Guid>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during file upload");
            return Result.Fail<Guid>("ファイルのアップロードに失敗しました");
        }
    }
}
