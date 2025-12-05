using MediatR;
using Shared.Application;

namespace Application.Features.ReserveBook;

/// <summary>
/// 予約登録Store（状態管理 + I/O）
///
/// 【SPEC準拠】
/// - boundary.output.success: 予約確認メッセージ + 順位
/// - boundary.output.errors: エラーコード別メッセージ
///
/// 【カタログパターン: layer-store】
/// - 状態管理（フォーム入力、ローディング、エラー）
/// - MediatR経由でCommand送信
/// - OnChangeAsync で変更通知
/// </summary>
public sealed class ReserveBookStore
{
    private readonly IMediator _mediator;
    private ReserveBookState _state = ReserveBookState.Empty;

    public ReserveBookStore(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 現在の状態
    /// </summary>
    public ReserveBookState State => _state;

    /// <summary>
    /// 状態変更通知イベント
    /// </summary>
    public event Func<Task>? OnChangeAsync;

    /// <summary>
    /// 会員IDを設定
    /// </summary>
    public async Task SetMemberIdAsync(Guid memberId)
    {
        _state = _state with { MemberId = memberId };
        await NotifyStateChangedAsync();
    }

    /// <summary>
    /// 蔵書IDを設定
    /// </summary>
    public async Task SetBookCopyIdAsync(Guid bookCopyId)
    {
        _state = _state with { BookCopyId = bookCopyId };
        await NotifyStateChangedAsync();
    }

    /// <summary>
    /// 予約を実行
    /// </summary>
    public async Task<Result<Guid>> ReserveAsync(CancellationToken ct = default)
    {
        _state = _state with { IsLoading = true, ErrorMessage = null };
        await NotifyStateChangedAsync();

        try
        {
            var command = new ReserveBookCommand
            {
                MemberId = _state.MemberId!.Value,
                BookCopyId = _state.BookCopyId!.Value
            };

            var result = await _mediator.Send(command, ct);

            if (result.IsSuccess)
            {
                _state = _state with
                {
                    IsLoading = false,
                    LastReservationId = result.Value,
                    SuccessMessage = "予約を受け付けました。順番が来たらメールでお知らせします。",
                    MemberId = null,
                    BookCopyId = null
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
        _state = ReserveBookState.Empty;
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
/// 予約登録状態（不変オブジェクト）
/// </summary>
public sealed record ReserveBookState
{
    public Guid? MemberId { get; init; }
    public Guid? BookCopyId { get; init; }
    public bool IsLoading { get; init; }
    public string? ErrorMessage { get; init; }
    public string? SuccessMessage { get; init; }
    public Guid? LastReservationId { get; init; }

    public static ReserveBookState Empty => new();

    public bool CanSubmit => !IsLoading
        && MemberId.HasValue
        && BookCopyId.HasValue;
}
