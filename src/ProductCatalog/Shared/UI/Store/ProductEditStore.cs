using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application;
using ProductCatalog.Shared.Application.DTOs;
using UpdateProduct.Application;
using GetProductById.Application;

namespace ProductCatalog.Shared.UI.Store;

/// <summary>
/// 商品編集の状態管理とI/O実行
/// </summary>
public sealed class ProductEditStore : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductEditStore> _logger;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _cts;

    private ProductEditState _state = ProductEditState.Empty;

    public event Func<Task>? OnChangeAsync;

    public ProductEditStore(
        IServiceScopeFactory scopeFactory,
        ILogger<ProductEditStore> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public ProductEditState GetState() => _state;

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
            _logger.LogDebug("商品データの読み込みがキャンセルされました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "商品データの読み込みに失敗しました");
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

    public async Task<bool> SaveAsync(
        Guid productId,
        string name,
        string description,
        decimal price,
        int stock,
        long version,
        CancellationToken ct = default)
    {
        _cts?.Cancel();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        await _gate.WaitAsync(_cts.Token);
        try
        {
            await SetStateAsync(_state with { IsSaving = true, ErrorMessage = null, SuccessMessage = null });

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new UpdateProductCommand(productId, name, description, price, stock, version);
            var result = await mediator.Send(command, _cts.Token);

            if (result.IsSuccess)
            {
                await SetStateAsync(_state with
                {
                    IsSaving = false,
                    SuccessMessage = "商品を更新しました",
                    ErrorMessage = null
                });

                // 最新データを再読み込み
                await LoadAsync(productId, ct);
                return true;
            }
            else
            {
                await SetStateAsync(_state with
                {
                    IsSaving = false,
                    ErrorMessage = result.Error
                });
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("商品更新がキャンセルされました");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "商品更新に失敗しました");
            await SetStateAsync(_state with
            {
                IsSaving = false,
                ErrorMessage = "更新処理に失敗しました"
            });
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SetStateAsync(ProductEditState newState)
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
