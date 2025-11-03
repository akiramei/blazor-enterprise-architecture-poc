using System.Collections.Concurrent;
using System.Collections.Immutable;
using MediatR;
using ProductCatalog.Application.Common;
using ProductCatalog.Application.Features.Products.GetProducts;
using ProductCatalog.Application.Features.Products.DeleteProduct;
using ProductCatalog.Application.Features.Products.BulkDeleteProducts;

namespace ProductCatalog.Web.Features.Products.Store;

/// <summary>
/// 商品一覧の状態管理とI/O実行（並行制御強化版）
/// </summary>
public sealed class ProductsStore : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductsStore> _logger;

    // 並行制御用
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ConcurrentDictionary<string, Task> _inflightRequests = new();
    private CancellationTokenSource? _cts;

    // バージョン管理（連打対策）
    private long _version;

    // 状態（不変）
    private ProductsState _state = ProductsState.Empty;

    public event Func<Task>? OnChangeAsync;

    public ProductsStore(
        IServiceScopeFactory scopeFactory,
        ILogger<ProductsStore> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public ProductsState GetState() => _state;

    /// <summary>
    /// 同一キーの読み込みは合流し、結果を共有（single-flight）
    /// </summary>
    public Task LoadAsync(CancellationToken ct = default)
    {
        const string key = "products-load";  // 固有キー

        // single-flight: 同時リクエストは1つに合流
        return _inflightRequests.GetOrAdd(key, _ => LoadInternalAsync(ct))
            .ContinueWith(t =>
            {
                _inflightRequests.TryRemove(key, out _);  // クリーンアップ
                return t;
            }, ct, TaskContinuationOptions.None, TaskScheduler.Default)
            .Unwrap();
    }

    /// <summary>
    /// 実際の読み込み処理（versioning併用）
    /// </summary>
    private async Task LoadInternalAsync(CancellationToken ct)
    {
        // 現在の実行のバージョンを記録
        var currentVersion = Interlocked.Increment(ref _version);

        // 既存の処理をキャンセル
        _cts?.Cancel();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        await _gate.WaitAsync(_cts.Token);
        try
        {
            // versioning: 古い実行は結果を破棄
            if (currentVersion != _version)
            {
                _logger.LogDebug("古い実行をスキップ: Version {Current} != {Latest}",
                    currentVersion, _version);
                return;
            }

            await SetStateAsync(_state with { IsLoading = true, ErrorMessage = null });

            // 実際のDB読み込み（重い処理）
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(new GetProductsQuery(), _cts.Token);

            // 最新版のみ反映
            if (currentVersion == _version && result.IsSuccess)
            {
                await SetStateAsync(_state with
                {
                    IsLoading = false,
                    Products = result.Value?.ToImmutableList() ?? ImmutableList<Application.Products.DTOs.ProductDto>.Empty,
                    ErrorMessage = null
                });
            }
            else if (result.IsSuccess == false)
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
            _logger.LogDebug("読み込み処理がキャンセルされました: Version {Version}", currentVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "商品一覧の読み込みに失敗しました");
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

    public async Task<bool> DeleteAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new DeleteProductCommand(productId);
            var result = await mediator.Send(command, ct);

            if (result.IsSuccess)
            {
                await LoadAsync(ct);
                return true;
            }

            await SetStateAsync(_state with { ErrorMessage = result.Error });
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "商品削除に失敗しました: {ProductId}", productId);
            await SetStateAsync(_state with { ErrorMessage = "削除処理に失敗しました" });
            return false;
        }
    }

    public async Task<BulkOperationResult> BulkDeleteAsync(IEnumerable<Guid> productIds, CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new BulkDeleteProductsCommand(productIds);
            var result = await mediator.Send(command, ct);

            if (result.IsSuccess)
            {
                await LoadAsync(ct);
                return result.Value!;
            }

            await SetStateAsync(_state with { ErrorMessage = result.Error });
            return BulkOperationResult.AllFailed(
                productIds.Count(),
                new[] { result.Error ?? "一括削除に失敗しました" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "一括削除に失敗しました");
            await SetStateAsync(_state with { ErrorMessage = "一括削除処理に失敗しました" });
            return BulkOperationResult.AllFailed(
                productIds.Count(),
                new[] { ex.Message });
        }
    }

    private async Task SetStateAsync(ProductsState newState)
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
