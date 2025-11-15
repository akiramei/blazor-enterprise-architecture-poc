using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application;
using ProductCatalog.Shared.Application.DTOs;
using Application.Features.ProductCatalog.GetProductById;

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
        _logger.LogInformation("[Store] LoadAsync started for ProductId: {ProductId}", productId);

        _cts?.Cancel();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        await _gate.WaitAsync(_cts.Token);
        try
        {
            _logger.LogInformation("[Store] Setting IsLoading = true");
            await SetStateAsync(_state with { IsLoading = true, ErrorMessage = null });

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var query = new GetProductByIdQuery(productId);
            _logger.LogInformation("[Store] Sending GetProductByIdQuery to MediatR");
            var result = await mediator.Send(query, _cts.Token);

            _logger.LogInformation("[Store] MediatR result received. IsSuccess: {IsSuccess}, Error: {Error}, Value: {Value}",
                result.IsSuccess, result.Error ?? "(null)", result.Value != null ? "NOT NULL" : "NULL");

            if (result.IsSuccess)
            {
                _logger.LogInformation("[Store] Query succeeded. Product: {ProductName}, State.Product will be: {HasProduct}",
                    result.Value?.Name, result.Value != null ? "NOT NULL" : "NULL");
                await SetStateAsync(_state with
                {
                    IsLoading = false,
                    Product = result.Value,
                    ErrorMessage = null
                });
                _logger.LogInformation("[Store] State updated. State.Product is now: {HasProduct}",
                    _state.Product != null ? "NOT NULL" : "NULL");
            }
            else
            {
                _logger.LogWarning("[Store] Query failed with error: {Error}", result.Error);
                await SetStateAsync(_state with
                {
                    IsLoading = false,
                    ErrorMessage = result.Error
                });
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("[Store] 商品詳細の読み込みがキャンセルされました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Store] 商品詳細の読み込みに失敗しました");
            await SetStateAsync(_state with
            {
                IsLoading = false,
                ErrorMessage = "データの読み込みに失敗しました"
            });
        }
        finally
        {
            _gate.Release();
            _logger.LogInformation("[Store] LoadAsync completed");
        }
    }

    private async Task SetStateAsync(ProductDetailState newState)
    {
        _state = newState;

        var handlerCount = OnChangeAsync?.GetInvocationList().Length ?? 0;
        _logger.LogInformation("[Store] SetStateAsync called. OnChangeAsync handlers: {HandlerCount}, Product: {HasProduct}",
            handlerCount, newState.Product != null ? "NOT NULL" : "NULL");

        if (OnChangeAsync is null) return;

        foreach (var handler in OnChangeAsync.GetInvocationList().Cast<Func<Task>>())
        {
            try
            {
                _logger.LogInformation("[Store] Invoking OnChangeAsync handler");
                await handler();
                _logger.LogInformation("[Store] OnChangeAsync handler completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Store] 状態変更通知中にエラーが発生しました");
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
