# 10. Application層の詳細設計

[← 目次に戻る](00_README.md)

---

## 10. Application層の詳細設計

### 10.0 なぜMediatRが必要か？ - Serviceクラス直接DIとの比較

このセクションでは、3層アーキテクチャで一般的な「Serviceクラスの直接DI」と、このアーキテクチャで採用している「MediatR + Pipeline Behaviors」の違いを説明します。

---

#### **従来の3層アーキテクチャ（Serviceクラス直接DI）**

```csharp
// ===== Service層 =====
public interface IProductService
{
    Task<Product?> GetProductAsync(Guid id);
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task CreateProductAsync(CreateProductDto dto);
    Task UpdateProductAsync(UpdateProductDto dto);
    Task DeleteProductAsync(Guid id);
}

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ProductService> _logger;

    public async Task DeleteProductAsync(Guid id)
    {
        // 1. ログ出力（横断的関心事）
        _logger.LogInformation("商品削除開始: {ProductId}", id);

        // 2. トランザクション開始（横断的関心事）
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // 3. ビジネスロジック
            var product = await _repository.GetAsync(id);
            if (product == null)
            {
                _logger.LogWarning("商品が見つかりません: {ProductId}", id);
                throw new NotFoundException();
            }

            await _repository.DeleteAsync(product);
            await _dbContext.SaveChangesAsync();

            // 4. トランザクションコミット
            await transaction.CommitAsync();

            // 5. ログ出力（横断的関心事）
            _logger.LogInformation("商品削除完了: {ProductId}", id);
        }
        catch (Exception ex)
        {
            // 6. エラーハンドリング（横断的関心事）
            await transaction.RollbackAsync();
            _logger.LogError(ex, "商品削除失敗: {ProductId}", id);
            throw;
        }
    }

    public async Task CreateProductAsync(CreateProductDto dto)
    {
        // 同様に、ログ、トランザクション、エラーハンドリングを毎回実装...
    }

    // 他のメソッドも同様...
}

// ===== Controller/ViewModel =====
public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly IAuthorizationService _authService;
    private readonly ILogger<ProductsController> _logger;

    public async Task<IActionResult> Delete(Guid id)
    {
        // 認可チェック（横断的関心事）
        var authResult = await _authService.AuthorizeAsync(User, "DeleteProduct");
        if (!authResult.Succeeded)
        {
            _logger.LogWarning("認可失敗: {ProductId}", id);
            return Forbid();
        }

        // バリデーション（横断的関心事）
        if (id == Guid.Empty)
        {
            return BadRequest("IDが不正です");
        }

        await _productService.DeleteAsync(id);
        return Ok();
    }
}
```

**❌ 問題点:**

1. **横断的関心事が各メソッドに散在**
   - ログ、トランザクション、エラーハンドリングのコードが重複
   - 新しいメソッドを追加するたびにコピペ
   - メンテナンスコストが高い

2. **追加機能の実装が困難**
   - 監査ログを全メソッドに追加したい → すべてのメソッドを修正
   - メトリクス収集を追加したい → すべてのメソッドを修正
   - 冪等性保証を追加したい → すべてのメソッドを修正

3. **テストが困難**
   - ビジネスロジックと横断的関心事が混在
   - トランザクション、ログを含めたテストが必要
   - モックが複雑

4. **責務が不明確**
   - Serviceクラスが肥大化
   - 「商品サービス」なのに、ログやトランザクション管理も担当

---

#### **このアーキテクチャ（MediatR + Pipeline Behaviors）**

```csharp
// ===== Command定義 =====
public record DeleteProductCommand(Guid ProductId) : ICommand<Result>;

// ===== Handler: ビジネスロジックのみ！ =====
public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IProductRepository _repository;

    // ログ、トランザクション、認可等の横断的関心事は一切書かない！
    public async Task<Result> Handle(DeleteProductCommand command, CancellationToken ct)
    {
        var product = await _repository.GetAsync(new ProductId(command.ProductId), ct);
        if (product is null)
            return Result.Fail("商品が見つかりません");

        product.Delete();  // ドメインルール適用
        await _repository.SaveAsync(product, ct);

        return Result.Success();
    }
}

// ===== Validator: バリデーションのみ =====
public class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("商品IDは必須です");
    }
}

// ===== UI層/Store =====
// たった1行でCommand送信（Pipeline Behaviorsが自動適用）
var result = await _mediator.Send(new DeleteProductCommand(productId), ct);

// ===== Infrastructure層: Pipeline Behaviors（自動適用） =====
// Program.csで登録するだけで、すべてのCommand/Queryに適用
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MetricsBehavior<,>));        // 0. メトリクス収集
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));        // 1. ログ出力
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));     // 2. バリデーション
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));  // 3. 認可チェック
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));    // 4. 冪等性保証
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));        // 5. キャッシング
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditLogBehavior<,>));       // 6. 監査ログ
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));    // 7. トランザクション
```

**✅ メリット:**

1. **横断的関心事が1箇所に集約**
   - すべてのCommand/Queryに自動適用
   - コードの重複がゼロ
   - 一貫した動作が保証される

2. **追加機能の実装が容易**
   - 監査ログを追加したい → `AuditLogBehavior`を追加するだけ
   - メトリクス収集を追加したい → `MetricsBehavior`を追加するだけ
   - **既存のHandlerは一切修正不要**

3. **Handlerがビジネスロジックに集中**
   - テストが容易（ビジネスロジックのみをテスト）
   - 可読性が高い
   - 単一責任の原則を守る

4. **実行順序を制御可能**
   - Pipeline Behaviorsの登録順序で実行順序が決まる
   - 例: Validation → Authorization → Transaction の順序を保証

---

#### **Pipeline Behaviorsの動作イメージ**

```
リクエスト: DeleteProductCommand(productId)
  ↓
┌─────────────────────────────────────────────────────────┐
│ 0. MetricsBehavior                                      │
│    - 実行時間の計測開始                                   │
│  ↓                                                       │
│ 1. LoggingBehavior                                      │
│    - ログ出力: "商品削除開始: {productId}"                │
│  ↓                                                       │
│ 2. ValidationBehavior                                   │
│    - FluentValidation実行（productId != Guid.Empty）    │
│  ↓                                                       │
│ 3. AuthorizationBehavior                                │
│    - 認可チェック（DeleteProduct権限）                    │
│  ↓                                                       │
│ 4. IdempotencyBehavior                                  │
│    - 冪等性チェック（重複実行を防止）                      │
│  ↓                                                       │
│ 5. CachingBehavior                                      │
│    - （Commandなのでスキップ）                            │
│  ↓                                                       │
│ 6. AuditLogBehavior                                     │
│    - 監査ログ準備                                         │
│  ↓                                                       │
│ 7. TransactionBehavior                                  │
│    - トランザクション開始                                  │
│  ↓                                                       │
│ ┌─────────────────────────────────────────────┐         │
│ │ DeleteProductHandler (ビジネスロジック)      │         │
│ │  - 商品を取得                                │         │
│ │  - product.Delete() 実行                    │         │
│ │  - Repository.SaveAsync()                  │         │
│ └─────────────────────────────────────────────┘         │
│  ↓                                                       │
│ 7. TransactionBehavior                                  │
│    - トランザクションコミット                              │
│  ↓                                                       │
│ 6. AuditLogBehavior                                     │
│    - 監査ログ保存                                         │
│  ↓                                                       │
│ ... (逆順で終了処理)                                      │
│  ↓                                                       │
│ 0. MetricsBehavior                                      │
│    - 実行時間の計測終了、メトリクス記録                    │
└─────────────────────────────────────────────────────────┘
  ↓
レスポンス: Result.Success()
```

---

#### **比較表: ServiceクラスDI vs MediatR**

| 観点 | Serviceクラス直接DI | MediatR + Pipeline Behaviors |
|------|-------------------|------------------------------|
| **横断的関心事** | 各メソッドに散在 | 1箇所に集約、自動適用 |
| **コードの重複** | 多い（各メソッドでコピペ） | ゼロ |
| **新機能追加** | すべてのメソッドを修正 | Behavior追加のみ |
| **テスタビリティ** | 低い（横断的関心事も含む） | 高い（ビジネスロジックのみ） |
| **可読性** | 低い（ログ等が混在） | 高い（ビジネスロジックのみ） |
| **実行順序制御** | 手動（各メソッド） | 自動（登録順序） |
| **単一責任の原則** | 違反（複数の責務） | 遵守（1 Handler = 1 UseCase） |

---

#### **いつMediatRを使うべきか？**

**✅ MediatRが有利なケース:**
- チーム開発（5名以上）
- 長期保守（3年以上）
- エンタープライズ要件（監査ログ、メトリクス、認可等）
- 複雑なビジネスロジック
- 横断的関心事の統一が重要

**⚠️ Serviceクラス直接DIで十分なケース:**
- 小規模プロトタイプ（< 5画面）
- 単一開発者
- 短期プロジェクト（< 6ヶ月）
- 横断的関心事がほとんどない

---

#### **まとめ**

MediatRとPipeline Behaviorsを使うことで、**横断的関心事を宣言的に適用**できます。

**Before（Serviceクラス）:**
- 各メソッドにログ、トランザクション、認可等を手動で実装
- コードの重複が多い
- 新機能追加が困難

**After（MediatR）:**
- Handlerはビジネスロジックのみに集中
- 横断的関心事はPipeline Behaviorsが自動適用
- 新機能追加が容易（既存Handlerは修正不要）

このアプローチにより、**保守性、テスタビリティ、拡張性**が大幅に向上します。

---

### 10.1 Command/Query定義

#### **マーカーインターフェース**

```csharp
/// <summary>
/// Commandマーカー(書き込み)
/// </summary>
public interface ICommand<TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Queryマーカー(読み取り)
/// </summary>
public interface IQuery<TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// キャッシュ可能なQuery
/// </summary>
public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan CacheDuration { get; }
}
```

#### **Commandの実装例**

```csharp
/// <summary>
/// 商品削除Command
/// </summary>
public sealed record DeleteProductCommand(Guid ProductId) : ICommand<Result>
{
    /// <summary>
    /// 冪等性キー(重複実行防止)
    /// </summary>
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}

/// <summary>
/// バリデーター
/// </summary>
public sealed class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("商品IDは必須です");
    }
}

/// <summary>
/// ハンドラー
/// </summary>
public sealed class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<DeleteProductHandler> _logger;
    
    public DeleteProductHandler(
        IProductRepository repository,
        ILogger<DeleteProductHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<Result> Handle(DeleteProductCommand command, CancellationToken ct)
    {
        // 1. 集約を取得
        var product = await _repository.GetAsync(new ProductId(command.ProductId), ct);
        
        if (product is null)
        {
            return Result.Fail("商品が見つかりません");
        }
        
        // 2. ドメインロジックを実行
        try
        {
            product.Delete();  // ビジネスルール検証
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "商品削除がドメインルールにより拒否されました: {ProductId}", command.ProductId);
            return Result.Fail(ex.Message);
        }
        
        // 3. 永続化(TransactionBehaviorがCommit)
        await _repository.SaveAsync(product, ct);
        
        _logger.LogInformation("商品を削除しました: {ProductId}", command.ProductId);
        
        return Result.Success();
    }
}
```

#### **Queryの実装例**

```csharp
/// <summary>
/// 商品一覧取得Query
/// </summary>
public sealed record GetProductsQuery(int Page, int PageSize) : IQuery<Result<PagedList<ProductDto>>>, ICacheableQuery
{
    public string CacheKey => $"products_list_{Page}_{PageSize}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
}

/// <summary>
/// ハンドラー
/// </summary>
public sealed class GetProductsHandler : IRequestHandler<GetProductsQuery, Result<PagedList<ProductDto>>>
{
    private readonly IProductReadDao _readDao;
    private readonly ILogger<GetProductsHandler> _logger;
    
    public GetProductsHandler(
        IProductReadDao readDao,
        ILogger<GetProductsHandler> logger)
    {
        _readDao = readDao;
        _logger = logger;
    }
    
    public async Task<Result<PagedList<ProductDto>>> Handle(GetProductsQuery query, CancellationToken ct)
    {
        // Read側の最適化されたDAOを使用
        var products = await _readDao.GetProductListAsync(
            page: query.Page,
            pageSize: query.PageSize,
            ct: ct);
        
        var totalCount = await _readDao.GetTotalCountAsync(ct);
        
        var pagedList = new PagedList<ProductDto>(
            items: products,
            totalCount: totalCount,
            page: query.Page,
            pageSize: query.PageSize);
        
        return Result.Success(pagedList);
    }
}
```

### 10.2 Pipeline Behaviors

#### **実行順序**

```csharp
// Program.csでの登録順序が重要
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));         // 1
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));      // 2
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));   // 3
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));     // 4
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));         // 5 (Query)
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));     // 6 (Command)

// 実行フロー:
// Logging → Validation → Authorization → Idempotency → Cache/Transaction → Handler
```

#### **1. LoggingBehavior**

```csharp
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid();
        
        _logger.LogInformation(
            "処理開始: {RequestName} {@Request} [RequestId: {RequestId}]",
            requestName,
            request,
            requestId);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var response = await next();
            
            stopwatch.Stop();
            
            _logger.LogInformation(
                "処理完了: {RequestName} [RequestId: {RequestId}] 実行時間: {ElapsedMs}ms",
                requestName,
                requestId,
                stopwatch.ElapsedMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex,
                "処理失敗: {RequestName} [RequestId: {RequestId}] 実行時間: {ElapsedMs}ms",
                requestName,
                requestId,
                stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }
}
```

#### **2. ValidationBehavior**

```csharp
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }
        
        var context = new ValidationContext<TRequest>(request);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();
        
        if (failures.Any())
        {
            var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
            
            // Result型にエラーを設定して返す
            return (TResponse)(object)Result.Fail(errorMessage);
        }
        
        return await next();
    }
}
```

#### **3. AuthorizationBehavior**

```csharp
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly ICurrentUserService _currentUser;
    private readonly IAuthorizationService _authorizationService;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // リクエストに必要な権限を取得
        var authorizeAttributes = request.GetType()
            .GetCustomAttributes<AuthorizeAttribute>()
            .ToList();
        
        if (!authorizeAttributes.Any())
        {
            return await next();  // 認可不要
        }
        
        // 認証チェック
        if (!_currentUser.IsAuthenticated)
        {
            return (TResponse)(object)Result.Fail("認証が必要です");
        }
        
        // 権限チェック
        foreach (var attribute in authorizeAttributes)
        {
            if (!string.IsNullOrEmpty(attribute.Policy))
            {
                var authorized = await _authorizationService.AuthorizeAsync(
                    _currentUser.User!,
                    attribute.Policy);
                
                if (!authorized.Succeeded)
                {
                    return (TResponse)(object)Result.Fail("この操作を実行する権限がありません");
                }
            }
            
            if (!string.IsNullOrEmpty(attribute.Roles))
            {
                var roles = attribute.Roles.Split(',');
                var hasRole = roles.Any(role => _currentUser.IsInRole(role.Trim()));
                
                if (!hasRole)
                {
                    return (TResponse)(object)Result.Fail($"必要なロール: {attribute.Roles}");
                }
            }
        }
        
        return await next();
    }
}

// 使用例
[Authorize(Roles = "Admin")]
public sealed record DeleteProductCommand(Guid ProductId) : ICommand<Result>;
```

#### **4. IdempotencyBehavior**

```csharp
public sealed class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : Result
{
    private readonly IIdempotencyStore _store;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Commandからキーを取得
        var idempotencyKey = GetIdempotencyKey(request);
        
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            return await next();  // キーがない場合はスキップ
        }
        
        var commandType = typeof(TRequest).Name;
        
        // 既に処理済みかチェック
        var existingRecord = await _store.GetAsync(idempotencyKey, cancellationToken);
        
        if (existingRecord != null)
        {
            _logger.LogInformation(
                "冪等性により既存の結果を返します: {CommandType} [Key: {IdempotencyKey}]",
                commandType,
                idempotencyKey);
            
            return existingRecord.GetResult<TResponse>();
        }
        
        // 新規処理を実行
        var response = await next();
        
        // 成功した場合のみ記録
        if (response.IsSuccess)
        {
            var record = IdempotencyRecord.Create(idempotencyKey, commandType, response);
            await _store.SaveAsync(record, cancellationToken);
            
            _logger.LogInformation(
                "冪等性レコードを保存しました: {CommandType} [Key: {IdempotencyKey}]",
                commandType,
                idempotencyKey);
        }
        
        return response;
    }
    
    private string? GetIdempotencyKey(TRequest request)
    {
        var property = typeof(TRequest).GetProperty("IdempotencyKey");
        return property?.GetValue(request) as string;
    }
}
```

#### **5. CachingBehavior(Query専用)**

```csharp
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>, ICacheableQuery
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cacheKey = request.CacheKey;
        
        // キャッシュから取得
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        
        if (!string.IsNullOrEmpty(cachedData))
        {
            _logger.LogDebug("キャッシュヒット: {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<TResponse>(cachedData)!;
        }
        
        // キャッシュミス: Queryを実行
        _logger.LogDebug("キャッシュミス: {CacheKey}", cacheKey);
        var response = await next();
        
        // キャッシュに保存
        var serialized = JsonSerializer.Serialize(response);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = request.CacheDuration
        };
        
        await _cache.SetStringAsync(cacheKey, serialized, options, cancellationToken);
        
        return response;
    }
}
```

#### **6. TransactionBehavior(Command専用)**

```csharp
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : Result
{
    private readonly AppDbContext _context;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // ネストされたトランザクションを防ぐため、既存トランザクションがあればスキップ
        if (_context.Database.CurrentTransaction != null)
        {
            return await next();
        }
        
        var commandName = typeof(TRequest).Name;
        
        _logger.LogDebug("トランザクション開始: {CommandName}", commandName);
        
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var response = await next();
            
            if (response.IsSuccess)
            {
                // ドメインイベントをディスパッチ
                await DispatchDomainEventsAsync(cancellationToken);
                
                // Commit
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                
                _logger.LogDebug("トランザクションコミット: {CommandName}", commandName);
            }
            else
            {
                // ビジネスルール違反の場合もロールバック
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogDebug("トランザクションロールバック(ビジネスルール違反): {CommandName}", commandName);
            }
            
            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "トランザクションロールバック(例外): {CommandName}", commandName);
            throw;
        }
    }
    
    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        var domainEntities = _context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(x => x.Entity.GetDomainEvents().Any())
            .ToList();
        
        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.GetDomainEvents())
            .ToList();
        
        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());
        
        foreach (var domainEvent in domainEvents)
        {
            await PublishDomainEventAsync(domainEvent, ct);
        }
    }
}
```

### 10.4 Pipeline登録とBehavior順序規約 (v2.1改善)

#### 10.4.1 Query Pipeline順序とキャッシュ安全性

**CRITICAL**: キャッシュ誤配信を防ぐため、Pipeline順序とキーの規約を厳守してください。

```csharp
// Program.cs - Pipeline Behaviors 登録順序(この順序厳守)

var builder = WebApplication.CreateBuilder(args);

// MediatR 登録
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

// ✅ Query Pipeline順序(CRITICAL):
// 1) Logging → 2) Validation → 3) Authorization → 4) Caching → 5) Handler

// Pipeline Behaviors の登録(順序重要)
// 全てのリクエスト(Query + Command)に適用
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));           // 1
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));        // 2
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));     // 3

// Query のみに適用(Caching)
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));           // 4

// Command のみに適用(Idempotency, Transaction)
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehaviorForCommands<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehaviorForCommands<,>));

var app = builder.Build();
```

**順序が重要な理由**:

| 順序 | Behavior | 理由 |
|------|----------|------|
| 1 | Logging | 全リクエストをトレース可能に |
| 2 | Validation | 無効なリクエストもログに残す |
| 3 | **Authorization** | **権限チェック後にキャッシュ** ← 重要 |
| 4 | Caching | 認可済みデータのみキャッシュ |
| 5 | Handler | 実際の処理 |

**❌ 誤った順序の危険性**:

```csharp
// ❌ BAD: キャッシュが認可より先
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));      // 3
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>)); // 4
// → 権限のないユーザーがキャッシュから取得できてしまう危険性
```

#### 10.4.2 キャッシュキーの安全性規約

**CRITICAL**: キーに必ずユーザー/テナント情報を含めて誤配信を防ぐ

```csharp
/// <summary>
/// キャッシュ誤配信を防ぐ改善版CachingBehavior
/// </summary>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>, ICacheable
{
    private readonly IMemoryCache _cache;
    private readonly ICurrentUserService _currentUser;  // ✅ 必須依存
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
    
    public CachingBehavior(
        IMemoryCache cache,
        ICurrentUserService currentUser,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _currentUser = currentUser;
        _logger = logger;
    }
    
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken ct)
    {
        // ✅ CRITICAL: キーに必ずユーザー/テナント情報を含める
        var userSegment = _currentUser.UserId.ToString("N");
        var tenantSegment = _currentUser.TenantId?.ToString("N") ?? "default";
        var requestSegment = request.GetCacheKey();
        
        var cacheKey = $"{typeof(TRequest).Name}:{tenantSegment}:{userSegment}:{requestSegment}";
        //                                        ^^^^^^^^^^^^^^^^^ ^^^^^^^^^^^^^^
        //                                        テナント分離      ユーザー分離
        
        if (_cache.TryGetValue(cacheKey, out TResponse? cached))
        {
            _logger.LogDebug("Cache hit: {Key}", cacheKey);
            return cached!;
        }
        
        _logger.LogDebug("Cache miss: {Key}", cacheKey);
        var response = await next();
        
        _cache.Set(
            cacheKey, 
            response, 
            TimeSpan.FromMinutes(request.CacheDuration));
        
        return response;
    }
}

// ✅ 使用例(正しいキー設計)
public record GetProductQuery(Guid Id) : IQuery<ProductDto>, ICacheable
{
    // ❌ 悪い例: "Product:123" → 全ユーザーで共有される
    // ✅ 良い例: Behaviorが自動的に "GetProductQuery:tenant456:user789:Product:123" に拡張
    public string GetCacheKey() => $"Product:{Id}";
    public int CacheDuration => 5;  // 分
}
```

**マルチテナント対応のベストプラクティス**:

```csharp
// ICurrentUserService の実装例
public interface ICurrentUserService
{
    Guid UserId { get; }
    Guid? TenantId { get; }  // マルチテナントの場合
    string UserName { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

// キャッシュキー規約チェックリスト
// ✅ テナントIDを含める(マルチテナント環境)
// ✅ ユーザーIDを含める(ユーザー固有データ)
// ✅ リクエスト型名を含める(型の衝突防止)
// ✅ パラメータを含める(クエリパラメータ)
// ❌ 機密情報を含めない(ログに出力される)
```

#### 10.4.3 Idempotency-Keyのエンドツーエンド伝播 (v2.1改善)

**UI→Store→Commandでキーを伝播し、重複Submit源流で止める**

```csharp
// Step 1: PageActionsでキー生成(入口)
public class ProductActions
{
    private readonly ProductsStore _store;
    
    public async Task SaveAsync(ProductDto input, CancellationToken ct = default)
    {
        // ✅ 冪等性キーを生成(重複Submit源流で止める)
        var idempotencyKey = Guid.NewGuid().ToString("N");
        
        await _store.SaveAsync(input, idempotencyKey, ct);
    }
}

// Step 2: Storeで伝播
public class ProductStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    
    public Task SaveAsync(ProductDto dto, string idempotencyKey, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        return mediator.Send(
            new SaveProductCommand(dto) { IdempotencyKey = idempotencyKey },  // ✅ 伝播
            ct);
    }
}

// Step 3: Commandに含める
public record SaveProductCommand(ProductDto Data) : ICommand<Result>
{
    public string IdempotencyKey { get; init; } = default!;  // ✅ 必須プロパティ
}

// Step 4: IdempotencyBehaviorで判定
public class IdempotencyBehaviorForCommands<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IIdempotencyStore _idempotencyStore;  // Redis or Table
    private readonly ILogger<IdempotencyBehaviorForCommands<TRequest, TResponse>> _logger;
    
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken ct)
    {
        if (request is not IHasIdempotencyKey keyed || string.IsNullOrEmpty(keyed.IdempotencyKey))
            return await next();
        
        // ✅ 既に処理済みならキャッシュされた結果を返す
        var cached = await _idempotencyStore.GetAsync<TResponse>(keyed.IdempotencyKey, ct);
        if (cached is not null)
        {
            _logger.LogInformation("Idempotency hit: {Key}", keyed.IdempotencyKey);
            return cached;
        }
        
        var response = await next();
        
        // ✅ 結果をキャッシュ(24時間保持)
        if (response is Result { IsSuccess: true })
        {
            await _idempotencyStore.SetAsync(
                keyed.IdempotencyKey, 
                response, 
                TimeSpan.FromHours(24), 
                ct);
        }
        
        return response;
    }
}

// マーカーインターフェース
public interface IHasIdempotencyKey
{
    string IdempotencyKey { get; }
}

// Commandの実装例
public record SaveProductCommand(ProductDto Data) : ICommand<Result>, IHasIdempotencyKey
{
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString("N");
}
```

**IdempotencyStoreの実装例(Redis)**:

```csharp
public class RedisIdempotencyStore : IIdempotencyStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisIdempotencyStore> _logger;
    
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var json = await db.StringGetAsync($"idempotency:{key}");
        
        if (json.IsNullOrEmpty)
            return default;
        
        return JsonSerializer.Deserialize<T>(json!);
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(value);
        
        await db.StringSetAsync($"idempotency:{key}", json, expiry);
        _logger.LogInformation("Idempotency record saved: {Key}, Expiry: {Expiry}", key, expiry);
    }
}
```

**重複防止の流れ**:

```
[User Double-Click]
      ↓
[PageActions: 同一キー生成]  ← 連打でも同じキー
      ↓
[Store: キー伝播]
      ↓
[IdempotencyBehavior: 重複判定]  ← 2回目以降はキャッシュ返却
      ↓
[Handler: 1回だけ実行]
```

**実装チェックリスト**:

- [ ] PageActionsで冪等性キーを生成
- [ ] Storeメソッドにキーパラメータを追加
- [ ] Commandにキープロパティを追加
- [ ] IHasIdempotencyKeyを実装
- [ ] IdempotencyBehaviorを登録(TransactionBehaviorより前)
- [ ] RedisまたはDB テーブルでキャッシュを実装
- [ ] 適切な有効期限を設定(24時間推奨)

---

## ⚠️ 注意: PurchaseManagement BCの実装例について

**PurchaseManagement BC**には承認ワークフロー、ダッシュボード、ファイルアップロード等の実装が含まれていますが、現在以下の**重大な問題**があるため、参照カタログとしては**使用しないでください**：

### 確認された問題

1. **SQL/スキーマの不整合**
   - `GetPendingApprovals`, `GetDashboardStatistics` が存在しないテーブル名・列名を参照
   - `pm_PurchaseRequests`, `TotalAmount`, `IsPending` 等は実際のスキーマに存在しない
   - 計算プロパティをDB列として扱っている

2. **マルチテナント制御の欠如**
   - `GetPurchaseRequestById` でGlobal Query Filterを迂回
   - 任意のGUIDで他テナントのデータが閲覧可能（**セキュリティ脆弱性**）

3. **認可制御の欠如**
   - `PurchaseRequestDetail.razor` で承認者かどうかの確認なし
   - 誰でも任意の申請を承認・却下可能（**セキュリティ脆弱性**）

4. **入力検証の欠如**
   - `PurchaseRequestSubmit.razor` で`Guid.Parse()`を直接実行（クラッシュリスク）

### 修正が必要な箇所

| ファイル | 問題 | 修正内容 |
|---------|------|---------|
| `GetPendingApprovalsHandler.cs:63-88` | 存在しないテーブル・列参照 | 実際のスキーマに合わせる |
| `GetDashboardStatisticsHandler.cs` | 同上 + Task.WhenAll誤用 | スキーマ修正、順次実行に変更 |
| `GetPurchaseRequestByIdHandler.cs:90-125` | TenantIdチェック迂回 | EF Coreリポジトリ経由に変更 |
| `PurchaseRequestDetail.razor:220-384` | 承認者確認なし | `ICurrentUserService.UserId`と突き合わせ |
| `PurchaseRequestSubmit.razor` | 入力検証なし | `Guid.TryParse()`に変更 |

これらの問題が修正されるまで、**ProductCatalog BCのパターンのみを参照**してください。

---

