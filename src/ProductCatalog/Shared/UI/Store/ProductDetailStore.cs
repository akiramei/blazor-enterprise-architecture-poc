using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application;
using ProductCatalog.Shared.Application.DTOs;
using GetProductById.Application;

namespace ProductCatalog.Shared.UI.Store;

/// <summary>
/// 商品詳細の状態管理とI/O実行
/// </summary>
public sealed class ProductDetailStore : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductDetailStore> _logger;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _cts;

    private ProductDetailState _state = ProductDetailState.Empty;

    public event Func<Task>? OnChangeAsync;

    public ProductDetailStore(
        IServiceScopeFactory scopeFactory,
        ILogger<ProductDetailStore> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public ProductDetailState GetState() => _state;

    public async Task LoadAsync(Guid productId, CancellationToken ct = default)
    {
        _cts?.Cancel();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        await _gate.WaitAsync(_cts.Token);
        try
        {
            await SetStateAsync(_state with { IsLoading = true, ErrorMessage = null });

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var query = new GetProductByIdQuery(productId);
            var result = await mediator.Send(query, _cts.Token);

            if (result.IsSuccess)
            {
                await SetStateAsync(_state with
                {
                    IsLoading = false,
                    Product = result.Value,
                    ErrorMessage = null
                });
            }
            else
            {
                await SetStateAsync(_state with
                {
                    IsLoading = false,
                    ErrorMessage = result.Error
                });
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("商品詳細の読み込みがキャンセルされました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "商品詳細の読み込みに失敗しました");
            await SetStateAsync(_state with
            {
                IsLoading = false,
                ErrorMessage = "データの読み込みに失敗しました"
            });
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SetStateAsync(ProductDetailState newState)
    {
        _state = newState;

        if (OnChangeAsync is null) return;

        foreach (var handler in OnChangeAsync.GetInvocationList().Cast<Func<Task>>())
        {
            try
            {
                await handler();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "状態変更通知中にエラーが発生しました");
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _gate.Dispose();
    }
}
