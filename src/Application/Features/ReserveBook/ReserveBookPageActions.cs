using Microsoft.Extensions.Logging;

namespace Application.Features.ReserveBook;

/// <summary>
/// 予約登録PageActions（UI手順オーケストレーション）
///
/// 【SPEC準拠】
/// - actor: Member（会員）
/// - scenarios.happy_path: 本検索 → 予約 → 完了表示
///
/// 【カタログパターン: layer-pageactions】
/// - UI手順のオーケストレーション
/// - トースト通知、画面遷移
/// - Storeへの委譲
/// </summary>
public sealed class ReserveBookPageActions
{
    private readonly ReserveBookStore _store;
    private readonly ILogger<ReserveBookPageActions> _logger;

    public ReserveBookPageActions(
        ReserveBookStore store,
        ILogger<ReserveBookPageActions> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// 会員選択時
    /// </summary>
    public async Task OnMemberSelectedAsync(Guid memberId)
    {
        _logger.LogInformation("会員選択: {MemberId}", memberId);
        await _store.SetMemberIdAsync(memberId);
    }

    /// <summary>
    /// 本選択時
    /// </summary>
    public async Task OnBookSelectedAsync(Guid bookCopyId)
    {
        _logger.LogInformation("本選択: {BookCopyId}", bookCopyId);
        await _store.SetBookCopyIdAsync(bookCopyId);
    }

    /// <summary>
    /// 予約ボタンクリック時
    /// </summary>
    public async Task OnReserveClickedAsync(CancellationToken ct = default)
    {
        _logger.LogInformation(
            "予約実行: 会員={MemberId}, 蔵書={BookCopyId}",
            _store.State.MemberId,
            _store.State.BookCopyId);

        var result = await _store.ReserveAsync(ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("予約成功: ReservationId={ReservationId}", result.Value);
            // トースト通知は UI側で State.SuccessMessage を表示
        }
        else
        {
            _logger.LogWarning("予約失敗: {Error}", result.Error);
            // エラー表示は UI側で State.ErrorMessage を表示
        }
    }

    /// <summary>
    /// リセットボタンクリック時
    /// </summary>
    public async Task OnResetClickedAsync()
    {
        _logger.LogInformation("フォームリセット");
        await _store.ResetAsync();
    }
}
