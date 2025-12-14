using Microsoft.Extensions.Logging;

namespace Application.Features.LendBook;

/// <summary>
/// 貸出登録PageActions（UI手順オーケストレーション）
///
/// 【SPEC準拠】
/// - actor: Librarian（職員）
/// - scenarios.happy_path: バーコード入力 → 貸出登録 → 完了表示
///
/// 【カタログパターン: layer-pageactions】
/// - UI手順のオーケストレーション
/// - トースト通知、画面遷移
/// - Storeへの委譲
/// </summary>
public sealed class LendBookPageActions
{
    private readonly LendBookStore _store;
    private readonly ILogger<LendBookPageActions> _logger;

    public LendBookPageActions(
        LendBookStore store,
        ILogger<LendBookPageActions> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// 会員バーコードスキャン時
    /// </summary>
    public async Task OnMemberBarcodeScannedAsync(string barcode)
    {
        _logger.LogInformation("会員バーコードスキャン: {Barcode}", barcode);
        await _store.SetMemberBarcodeAsync(barcode);
    }

    /// <summary>
    /// 蔵書バーコードスキャン時
    /// </summary>
    public async Task OnBookCopyBarcodeScannedAsync(string barcode)
    {
        _logger.LogInformation("蔵書バーコードスキャン: {Barcode}", barcode);
        await _store.SetBookCopyBarcodeAsync(barcode);
    }

    /// <summary>
    /// 貸出ボタンクリック時
    /// </summary>
    public async Task OnLendClickedAsync(CancellationToken ct = default)
    {
        _logger.LogInformation(
            "貸出実行: 会員={Member}, 蔵書={BookCopy}",
            _store.State.MemberBarcode,
            _store.State.BookCopyBarcode);

        var result = await _store.LendAsync(ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("貸出成功: LoanId={LoanId}", result.Value);
            // トースト通知は UI側で State.SuccessMessage を表示
        }
        else
        {
            _logger.LogWarning("貸出失敗: {Error}", result.Error);
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
