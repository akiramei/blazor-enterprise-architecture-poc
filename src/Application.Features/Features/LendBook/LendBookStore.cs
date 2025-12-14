using System.Collections.Immutable;
using MediatR;
using Shared.Application;

namespace Application.Features.LendBook;

/// <summary>
/// 貸出登録Store（状態管理 + I/O）
///
/// 【SPEC準拠】
/// - boundary.output.success: 貸出ID + 返却期限メッセージ
/// - boundary.output.errors: エラーコード別メッセージ
///
/// 【カタログパターン: layer-store】
/// - 状態管理（フォーム入力、ローディング、エラー）
/// - MediatR経由でCommand送信
/// - OnChangeAsync で変更通知
/// </summary>
public sealed class LendBookStore
{
    private readonly IMediator _mediator;
    private LendBookState _state = LendBookState.Empty;

    public LendBookStore(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 現在の状態
    /// </summary>
    public LendBookState State => _state;

    /// <summary>
    /// 状態変更通知イベント
    /// </summary>
    public event Func<Task>? OnChangeAsync;

    /// <summary>
    /// 会員バーコードを設定
    /// </summary>
    public async Task SetMemberBarcodeAsync(string barcode)
    {
        _state = _state with { MemberBarcode = barcode };
        await NotifyStateChangedAsync();
    }

    /// <summary>
    /// 蔵書バーコードを設定
    /// </summary>
    public async Task SetBookCopyBarcodeAsync(string barcode)
    {
        _state = _state with { BookCopyBarcode = barcode };
        await NotifyStateChangedAsync();
    }

    /// <summary>
    /// 貸出を実行
    /// </summary>
    public async Task<Result<Guid>> LendAsync(CancellationToken ct = default)
    {
        _state = _state with { IsLoading = true, ErrorMessage = null };
        await NotifyStateChangedAsync();

        try
        {
            var command = new LendBookCommand
            {
                MemberBarcode = _state.MemberBarcode,
                BookCopyBarcode = _state.BookCopyBarcode
            };

            var result = await _mediator.Send(command, ct);

            if (result.IsSuccess)
            {
                _state = _state with
                {
                    IsLoading = false,
                    LastLoanId = result.Value,
                    SuccessMessage = $"貸出を登録しました（返却期限: {DateTime.Today.AddDays(14):yyyy/MM/dd}）",
                    MemberBarcode = string.Empty,
                    BookCopyBarcode = string.Empty
                };
            }
            else
            {
                _state = _state with
                {
                    IsLoading = false,
                    ErrorMessage = result.Error
                };
            }

            await NotifyStateChangedAsync();
            return result;
        }
        catch (Exception ex)
        {
            _state = _state with
            {
                IsLoading = false,
                ErrorMessage = ex.Message
            };
            await NotifyStateChangedAsync();
            return Result.Fail<Guid>(ex.Message);
        }
    }

    /// <summary>
    /// フォームをリセット
    /// </summary>
    public async Task ResetAsync()
    {
        _state = LendBookState.Empty;
        await NotifyStateChangedAsync();
    }

    private async Task NotifyStateChangedAsync()
    {
        if (OnChangeAsync != null)
        {
            await OnChangeAsync.Invoke();
        }
    }
}

/// <summary>
/// 貸出登録状態（不変オブジェクト）
/// </summary>
public sealed record LendBookState
{
    public string MemberBarcode { get; init; } = string.Empty;
    public string BookCopyBarcode { get; init; } = string.Empty;
    public bool IsLoading { get; init; }
    public string? ErrorMessage { get; init; }
    public string? SuccessMessage { get; init; }
    public Guid? LastLoanId { get; init; }

    public static LendBookState Empty => new();

    public bool CanSubmit => !IsLoading
        && !string.IsNullOrWhiteSpace(MemberBarcode)
        && !string.IsNullOrWhiteSpace(BookCopyBarcode);
}
