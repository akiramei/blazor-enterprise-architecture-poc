namespace Shared.Abstractions.Platform;

/// <summary>
/// Outbox Reader インターフェース（Platform責務）
///
/// 【トランザクショナルOutboxパターン - 読み取り側】
///
/// 責務:
/// - 各BCのOutboxから未処理メッセージを読み取り
/// - 処理結果（成功/失敗）の更新
/// - 複数BC対応（各BCがIOutboxReader実装を提供）
///
/// 設計:
/// - **論理所有**: Platform（OutboxBackgroundServiceが利用）
/// - **物理実装**: 各BC（例: ProductCatalogOutboxReader）
/// - **分離**: 書き込み（TransactionBehavior）と読み取り（IOutboxReader）の責務分離
///
/// 実装パターン:
/// - ProductCatalogOutboxReader: ProductCatalogDbContextから読み取り
/// - OrderOutboxReader: OrderDbContextから読み取り（将来）
/// - InventoryOutboxReader: InventoryDbContextから読み取り（将来）
///
/// OutboxBackgroundServiceは IEnumerable<IOutboxReader> を受け取り、
/// 全BCのOutboxを巡回してディスパッチします。
/// </summary>
public interface IOutboxReader
{
    /// <summary>
    /// 未処理のOutboxメッセージを取得
    /// </summary>
    /// <param name="take">取得件数上限</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>未処理メッセージのリスト</returns>
    Task<IReadOnlyList<OutboxMessage>> DequeueAsync(int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// メッセージを成功としてマーク
    /// </summary>
    /// <param name="id">メッセージID</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task MarkSucceededAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// メッセージを失敗としてマーク
    /// </summary>
    /// <param name="id">メッセージID</param>
    /// <param name="error">エラーメッセージ</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default);
}
