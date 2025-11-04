using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application;
using SearchProducts.Application;
using ProductCatalog.Shared.Domain.Products;

namespace ProductCatalog.Shared.UI.Store;

/// <summary>
/// 商品検索の状態管理とI/O実行
/// </summary>
public sealed class ProductSearchStore : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductSearchStore> _logger;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _cts;

    private ProductSearchState _state = ProductSearchState.Empty;

    public event Func<Task>? OnChangeAsync;

    public ProductSearchStore(
        IServiceScopeFactory scopeFactory,
        ILogger<ProductSearchStore> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public ProductSearchState GetState() => _state;

    public async Task SearchAsync(
        string? nameFilter = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int? status = null,
        int page = 1,
        int pageSize = 10,
        string orderBy = "Name",
        bool isDescending = false,
        CancellationToken ct = default)
    {
        _cts?.Cancel();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        await _gate.WaitAsync(_cts.Token);
        try
        {
            await SetStateAsync(_state with
            {
                IsSearching = true,
                ErrorMessage = null,
                NameFilter = nameFilter,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Status = status,
                CurrentPage = page,
                PageSize = pageSize,
                OrderBy = orderBy,
                IsDescending = isDescending
            });

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Convert status int to ProductStatus enum if provided
            ProductStatus? statusEnum = status.HasValue
                ? (ProductStatus)status.Value
                : null;

            var query = new SearchProductsQuery(
                nameFilter,
                minPrice,
                maxPrice,
                statusEnum,
                page,
                pageSize,
                orderBy,
                isDescending);

            var result = await mediator.Send(query, _cts.Token);

            if (result.IsSuccess)
            {
                await SetStateAsync(_state with
                {
                    IsSearching = false,
                    SearchResult = result.Value,
                    ErrorMessage = null
                });
            }
            else
            {
                await SetStateAsync(_state with
                {
                    IsSearching = false,
                    ErrorMessage = result.Error
                });
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("商品検索がキャンセルされました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "商品検索に失敗しました");
            await SetStateAsync(_state with
            {
                IsSearching = false,
                ErrorMessage = "検索処理に失敗しました"
            });
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SetStateAsync(ProductSearchState newState)
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
