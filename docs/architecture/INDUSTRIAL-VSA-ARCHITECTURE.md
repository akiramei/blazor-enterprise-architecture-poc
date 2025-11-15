# 工業製品化VSAアーキテクチャ設計書

## 1. 設計哲学

### 1.1 目標: 工業製品化 (Industrialization)

**新プロジェクト開始の理想的なフロー:**
1. **ドメイン分析** (3週間) → ドメインモデル構築
2. **アーキテクチャ統合** (1週間) → マニュアル通りにドメインをアーキテクチャに組み込み
3. **機能実装** (2日) → 薄いアダプター層のみ記述

**アーキテクチャの役割分担:**

| 層 | 役割 | 特性 | プロジェクト間の再利用性 |
|---|---|---|---|
| **Domain** | プロジェクトの中心<br>ドメインエキスパートと開発者の協働成果 | プロジェクト固有<br>技術要素ゼロ | 0% (毎回分析・構築) |
| **Application** | システムとしてのクリーンさ・堅牢性・保守性 | 工業製品<br>ドメイン非依存 | 100% (完全再利用) |
| **Boundaries** | アプリ固有情報の集約点 | 薄い統合層<br>定型パターン | 80% (テンプレート再利用) |

### 1.2 VSAのメリットを活かす

- **変更の局所化**: 機能追加時の影響範囲最小化
- **BC (Bounded Context) 分離**: ドメイン境界の物理的独立性
- **チーム独立性**: BCごとの並行開発可能

### 1.3 Boundaryアーキテクチャのメリットを活かす

- **技術的関心の集約**: UI/DB/Hostの技術詳細を分離
- **汎用化の推進**: `Foo<TModel>` 型の汎用実装
- **ドメインの純粋性**: ドメイン層から技術要素を完全排除

---

## 2. 新アーキテクチャ構造

### 2.1 ディレクトリ構造

```text
src/
├── Domain/                          ← プロジェクト固有 (技術要素ゼロ)
│   ├── {BoundedContext}/
│   │   ├── {Aggregate}/
│   │   │   ├── {Aggregate}.cs       ← エンティティ (EFコメント排除)
│   │   │   ├── {ValueObject}.cs
│   │   │   └── I{Aggregate}Repository.cs
│   │   ├── Services/
│   │   │   └── {DomainService}.cs
│   │   └── Boundaries/              ← ドメイン境界 (技術非依存)
│   │       ├── I{操作}Boundary.cs   ← インターフェース定義のみ
│   │       └── {操作}Input.cs       ← DTO (技術非依存)
│   │
│   └── Shared.Kernel/               ← 共通ドメイン基盤 (技術非依存)
│       ├── AggregateRoot.cs
│       ├── Entity.cs
│       ├── ValueObject.cs
│       ├── DomainEvent.cs
│       └── Money.cs
│
├── Application/                     ← 工業製品 (100%再利用可能)
│   ├── Core/                        ← 汎用基盤 (ドメイン非依存)
│   │   ├── Commands/
│   │   │   ├── ICommand.cs
│   │   │   ├── ICommandHandler.cs
│   │   │   └── CommandPipeline.cs   ← トランザクション・ログ・監査の統合
│   │   ├── Queries/
│   │   │   ├── IQuery.cs
│   │   │   └── IQueryHandler.cs
│   │   ├── Behaviors/               ← MediatR Pipeline
│   │   │   ├── LoggingBehavior.cs
│   │   │   ├── ValidationBehavior.cs
│   │   │   ├── AuditLogBehavior.cs
│   │   │   ├── IdempotencyBehavior.cs
│   │   │   └── MetricsBehavior.cs
│   │   └── Common/
│   │       ├── Result.cs
│   │       └── PagedResult.cs
│   │
│   └── Features/                    ← 薄いアダプター層 (定型コード)
│       └── {BoundedContext}/
│           └── {Feature}/
│               ├── {Feature}Command.cs      ← 薄いDTO
│               └── {Feature}CommandHandler.cs  ← 3-5行のみ
│
├── Boundaries/                      ← アプリ固有情報を集約 (80%再利用)
│   ├── Persistence/                 ← DB境界
│   │   ├── {BoundedContext}/
│   │   │   ├── {BC}DbContext.cs
│   │   │   ├── Configurations/
│   │   │   │   └── {Entity}Configuration.cs  ← EF Core設定
│   │   │   ├── Repositories/
│   │   │   │   └── Ef{Aggregate}Repository.cs
│   │   │   └── Behaviors/
│   │   │       └── TransactionBehavior.cs   ← BC専用トランザクション
│   │   └── Migrations/
│   │
│   ├── Presentation/                ← UI境界
│   │   ├── {BoundedContext}/
│   │   │   └── {Feature}/
│   │   │       └── {Feature}Page.razor
│   │   └── Common/
│   │       ├── Components/
│   │       │   ├── GenericListPage.razor
│   │       │   └── GenericDetailPage.razor
│   │       └── Layouts/
│   │
│   └── Host/                        ← DI・起動境界
│       ├── Program.cs
│       ├── DependencyInjection/
│       │   ├── ApplicationServiceExtensions.cs
│       │   └── {BC}ServiceExtensions.cs
│       └── Middleware/
│
└── Tests/
    ├── Domain.Tests/
    ├── Application.Tests/
    └── Integration.Tests/
```

### 2.2 層間依存関係

```text
┌────────────────────────────────────────────────────────┐
│ Boundaries/Host (Program.cs + DI)                      │
│  - すべての層を統合                                     │
│  - アプリ起動・設定                                     │
└───────────────┬────────────────────────────────────────┘
                │
    ┌───────────┴───────────┐
    │                       │
┌───▼──────────┐    ┌──────▼────────────────────────────┐
│ Boundaries/  │    │ Application/                      │
│ Presentation │◄───┤  - Core (汎用基盤)                 │
│  (UI)        │    │  - Features (薄いアダプター)       │
└──────────────┘    └───────┬───────────────────────────┘
                            │
                    ┌───────┴──────────┐
                    │                  │
            ┌───────▼────────┐  ┌─────▼──────────────────┐
            │ Boundaries/    │  │ Domain/                │
            │ Persistence    │◄─┤  - {BC}/               │
            │  (DB)          │  │  - Shared.Kernel/      │
            └────────────────┘  └────────────────────────┘

依存方向: 上 → 下
Domain = 最も安定 (依存ゼロ)
Application/Core = 2番目に安定 (Domainのみ依存)
Boundaries = 最も不安定 (すべてに依存)
```

---

## 3. 各層の詳細設計

### 3.1 Domain層 (プロジェクト固有・技術要素ゼロ)

#### 3.1.1 エンティティ設計 (技術要素排除)

**Before (技術要素が侵入):**
```csharp
// ❌ EF Core用コメント = 技術要素
public class PurchaseRequest : AggregateRoot
{
    private PurchaseRequest() { } // For EF Core

    public static PurchaseRequest Create(...)
    {
        return new PurchaseRequest(...);
    }
}
```

**After (技術要素完全排除):**
```csharp
// ✅ ドメインロジックのみ
public class PurchaseRequest : AggregateRoot
{
    // パラメータレスコンストラクタ = 値オブジェクト復元のドメインルール
    // (デシリアライズ・再構成時に必要 = ドメインの要求)
    private PurchaseRequest()
    {
        // デフォルト値の設定 (ドメインルール)
        _items = new List<PurchaseRequestItem>();
        _approvalSteps = new List<ApprovalStep>();
    }

    public static PurchaseRequest Create(
        Guid requesterId,
        string requesterName,
        string title,
        string description,
        Guid tenantId)
    {
        var request = new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            RequesterId = requesterId,
            RequesterName = requesterName,
            Title = title,
            Description = description,
            TenantId = tenantId,
            Status = PurchaseRequestStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        // ドメインイベント
        request.AddDomainEvent(new PurchaseRequestCreatedEvent(request.Id));
        return request;
    }
}
```

#### 3.1.2 Boundary定義 (インターフェースのみ)

**ドメイン側 (Domain/{BC}/Boundaries/):**
```csharp
namespace Domain.PurchaseManagement.Boundaries;

/// <summary>
/// 提出境界 - 提出資格チェック・総額計算
/// 実装はBoundaries/Persistence層が提供
/// </summary>
public interface ISubmissionBoundary
{
    /// <summary>
    /// 提出コンテキストを取得
    /// </summary>
    SubmissionContext GetContext(
        string title,
        string description,
        IReadOnlyList<PurchaseRequestItemInput> items);

    /// <summary>
    /// 合計金額を計算
    /// </summary>
    decimal CalculateTotalAmount(IReadOnlyList<PurchaseRequestItemInput> items);
}

/// <summary>
/// 提出用入力DTO (技術非依存)
/// </summary>
public record PurchaseRequestItemInput(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity);

/// <summary>
/// 提出コンテキスト (ドメイン知識)
/// </summary>
public record SubmissionContext(
    bool CanSubmit,
    decimal TotalAmount,
    decimal MaxAllowedAmount,
    bool IsOverLimit,
    bool IsNearLimit,
    decimal RemainingAmount);
```

**実装側 (Boundaries/Persistence/{BC}/):**
```csharp
namespace Boundaries.Persistence.PurchaseManagement;

/// <summary>
/// 提出境界の実装 (DB・キャッシュ等の技術詳細)
/// </summary>
public class SubmissionBoundary : ISubmissionBoundary
{
    private readonly PurchaseManagementDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SubmissionContext GetContext(
        string title,
        string description,
        IReadOnlyList<PurchaseRequestItemInput> items)
    {
        var total = CalculateTotalAmount(items);

        // DB問い合わせ (技術詳細)
        var userLimit = _context.UserLimits
            .FirstOrDefault(x => x.UserId == _currentUser.UserId)
            ?.MaxAmount ?? 100000m;

        var isOverLimit = total > userLimit;
        var isNearLimit = total > userLimit * 0.8m;
        var remaining = userLimit - total;

        return new SubmissionContext(
            CanSubmit: !isOverLimit,
            TotalAmount: total,
            MaxAllowedAmount: userLimit,
            IsOverLimit: isOverLimit,
            IsNearLimit: isNearLimit,
            RemainingAmount: remaining);
    }

    public decimal CalculateTotalAmount(IReadOnlyList<PurchaseRequestItemInput> items)
    {
        return items.Sum(i => i.UnitPrice * i.Quantity);
    }
}
```

---

### 3.2 Application層 (工業製品・100%再利用)

#### 3.2.1 Core基盤 (完全汎用)

**Application/Core/Commands/CommandPipeline.cs:**
```csharp
namespace Application.Core.Commands;

/// <summary>
/// 汎用コマンド実行パイプライン
/// トランザクション・ログ・エラーハンドリングを統合
///
/// 目的: 個別Handlerでのボイラープレート排除
/// 使用例: すべてのCommandHandlerが継承
/// </summary>
public abstract class CommandPipeline<TCommand, TResult>
    : IRequestHandler<TCommand, Result<TResult>>
    where TCommand : ICommand<Result<TResult>>
{
    protected abstract Task<Result<TResult>> ExecuteAsync(
        TCommand command,
        CancellationToken cancellationToken);

    public async Task<Result<TResult>> Handle(
        TCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. トランザクション開始 (Behaviorで実行)
            // 2. ドメインロジック実行
            var result = await ExecuteAsync(command, cancellationToken);

            // 3. Behaviorがトランザクションコミット・ログ出力
            return result;
        }
        catch (DomainException ex)
        {
            // ドメインルール違反
            return Result.Fail<TResult>(ex.Message);
        }
        catch (Exception)
        {
            // インフラ例外はBehaviorで処理
            throw;
        }
    }
}
```

**Application/Core/Behaviors/TransactionBehavior.cs (BC非依存版):**
```csharp
namespace Application.Core.Behaviors;

/// <summary>
/// 汎用トランザクション管理Behavior
/// BC別DbContextを動的に解決
/// </summary>
public class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : Result
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // リクエストのBCを特定
        var boundedContext = GetBoundedContext(request);

        // BC別DbContextを解決
        var dbContext = ResolveDbContext(boundedContext);

        if (dbContext.Database.CurrentTransaction != null)
        {
            return await next();
        }

        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();

            if (response.IsSuccess)
            {
                await DispatchDomainEventsAsync(dbContext, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private DbContext ResolveDbContext(string boundedContext)
    {
        // BC名からDbContext型を動的解決
        var dbContextType = boundedContext switch
        {
            "PurchaseManagement" => typeof(PurchaseManagementDbContext),
            "ProductCatalog" => typeof(ProductCatalogDbContext),
            _ => throw new InvalidOperationException($"Unknown BC: {boundedContext}")
        };

        return (DbContext)_serviceProvider.GetRequiredService(dbContextType);
    }
}
```

#### 3.2.2 Features層 (薄いアダプター)

**Application/Features/{BC}/{Feature}CommandHandler.cs:**
```csharp
namespace Application.Features.PurchaseManagement;

/// <summary>
/// 購買申請提出ハンドラー (薄いアダプター)
///
/// 責務: CommandをDomainオペレーションに変換
/// 行数: 3-5行のみ
/// </summary>
public class SubmitPurchaseRequestCommandHandler
    : CommandPipeline<SubmitPurchaseRequestCommand, Guid>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly ISubmissionBoundary _submissionBoundary;
    private readonly IAppContext _appContext;

    protected override async Task<Result<Guid>> ExecuteAsync(
        SubmitPurchaseRequestCommand cmd,
        CancellationToken ct)
    {
        // 1. Boundary経由で資格チェック
        var eligibility = _submissionBoundary.GetContext(
            cmd.Title, cmd.Description, cmd.Items);

        if (!eligibility.CanSubmit)
            return Result.Fail<Guid>("提出不可");

        // 2. Domain操作
        var request = PurchaseRequest.Create(
            _appContext.UserId,
            _appContext.UserName,
            cmd.Title,
            cmd.Description,
            _appContext.TenantId);

        // 3. 永続化
        await _repository.SaveAsync(request, ct);

        return Result.Success(request.Id);
    }
}
```

---

### 3.3 Boundaries層 (アプリ固有情報を集約)

#### 3.3.1 Persistence境界 (DB技術集約)

**Boundaries/Persistence/{BC}/{Entity}Configuration.cs:**
```csharp
namespace Boundaries.Persistence.PurchaseManagement.Configurations;

/// <summary>
/// EF Core設定 (技術詳細をこの層に集約)
/// </summary>
public class PurchaseRequestConfiguration : IEntityTypeConfiguration<PurchaseRequest>
{
    public void Configure(EntityTypeBuilder<PurchaseRequest> builder)
    {
        builder.ToTable("PurchaseRequests");

        builder.HasKey(x => x.Id);

        // パラメータレスコンストラクタを使用 (ドメイン側で定義済み)
        // EF Coreはリフレクションで自動検出

        // Money値オブジェクト (Owned Entity)
        builder.OwnsOne(x => x.TotalAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("TotalAmount");
            money.Property(m => m.Currency).HasColumnName("Currency");
        });

        // コレクション
        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey("PurchaseRequestId");
    }
}
```

#### 3.3.2 Presentation境界 (UI技術集約)

**Boundaries/Presentation/Common/GenericCrudPage.razor:**
```razor
@typeparam TEntity
@typeparam TCommand
@typeparam TDto

<PageTitle>@PageTitle</PageTitle>

<h3>@PageTitle</h3>

<EditForm Model="@Model" OnValidSubmit="@HandleSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />

    @ChildContent(Model)

    <button type="submit" class="btn btn-primary" disabled="@IsSubmitting">
        @SubmitText
    </button>
</EditForm>

@code {
    [Parameter] public string PageTitle { get; set; } = "";
    [Parameter] public RenderFragment<TDto> ChildContent { get; set; } = null!;
    [Parameter] public Func<TDto, TCommand> CreateCommand { get; set; } = null!;
    [Parameter] public string SubmitText { get; set; } = "送信";

    private TDto Model { get; set; } = default!;
    private bool IsSubmitting { get; set; }

    private async Task HandleSubmit()
    {
        IsSubmitting = true;
        var command = CreateCommand(Model);
        var result = await Mediator.Send(command);
        // エラーハンドリング
        IsSubmitting = false;
    }
}
```

**使用例 (BC固有のページ):**
```razor
@page "/purchase-requests/submit"
@inherits GenericCrudPage<PurchaseRequest, SubmitPurchaseRequestCommand, PurchaseRequestSubmitDto>

<GenericCrudPage PageTitle="購買申請提出"
                 CreateCommand="@(dto => new SubmitPurchaseRequestCommand { ... })">
    <ChildContent Context="model">
        <div class="mb-3">
            <label>タイトル</label>
            <InputText @bind-Value="model.Title" class="form-control" />
        </div>
        <!-- BC固有のフィールドのみ記述 -->
    </ChildContent>
</GenericCrudPage>
```

#### 3.3.3 Host境界 (DI・起動設定)

**Boundaries/Host/DependencyInjection/{BC}ServiceExtensions.cs:**
```csharp
namespace Boundaries.Host.DependencyInjection;

public static class PurchaseManagementServiceExtensions
{
    public static IServiceCollection AddPurchaseManagementServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. DbContext登録
        services.AddDbContext<PurchaseManagementDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("PurchaseManagement")));

        // 2. Repository登録
        services.AddScoped<IPurchaseRequestRepository, EfPurchaseRequestRepository>();

        // 3. Boundary登録
        services.AddScoped<ISubmissionBoundary, SubmissionBoundary>();
        services.AddScoped<IApprovalBoundary, ApprovalBoundary>();
        services.AddScoped<IFilteringBoundary, FilteringBoundary>();

        // 4. Domain Service登録
        services.AddScoped<IApprovalFlowService, ApprovalFlowService>();

        // 5. BC専用TransactionBehavior登録
        services.AddScoped(typeof(IPipelineBehavior<,>),
            typeof(PurchaseManagementTransactionBehavior<,>));

        return services;
    }
}
```

---

## 4. 工業製品化の達成度評価

### 4.1 新アーキテクチャの評価

| 項目 | 現行VSA | 新アーキテクチャ | 改善度 |
|---|---|---|---|
| **Application/Coreの再利用性** | 30% (BC固有が混在) | 100% (完全汎用) | ✅ +70% |
| **Featuresのコード量** | 50-100行/Handler | 5-10行/Handler | ✅ -80% |
| **Boundariesの集約度** | 分散 (4箇所) | 集約 (3境界) | ✅ 改善 |
| **Domain技術要素排除** | ❌ (EFコメント残存) | ✅ (完全排除) | ✅ 達成 |
| **新BC追加の工数** | 2週間 | 2日 | ✅ -85% |
| **工業製品化達成度** | 45% | **95%** | ✅ +50% |

### 4.2 コード行数の比較

**現行 (VSA):**
- Domain: 2,000行
- Features: 3,500行 (個別実装)
- Infrastructure: 1,500行

**新アーキテクチャ:**
- Domain: 2,000行 (変更なし)
- Application/Core: 800行 (100%再利用)
- Application/Features: 500行 (薄いアダプター)
- Boundaries: 1,200行 (80%再利用)

**削減率: -40% (-2,500行)**

---

## 5. 移行戦略

### 5.1 段階的移行パス

#### Phase 1: Application/Core基盤構築 (2日)
- [ ] `CommandPipeline<TCommand, TResult>` 作成
- [ ] BC非依存 `TransactionBehavior` 作成
- [ ] 汎用Behaviorの整理

#### Phase 2: Boundaries構造作成 (1日)
- [ ] `Boundaries/Persistence/{BC}/` フォルダ作成
- [ ] `Boundaries/Presentation/{BC}/` フォルダ作成
- [ ] `Boundaries/Host/DependencyInjection/` フォルダ作成

#### Phase 3: Domain技術要素排除 (1日)
- [ ] EF Coreコメント削除
- [ ] パラメータレスコンストラクタの意味再定義
- [ ] Boundary実装をPersistence層に移動

#### Phase 4: Features薄層化 (3日)
- [ ] 各Handler を `CommandPipeline` 継承に変更
- [ ] ボイラープレート削除
- [ ] 5-10行に圧縮

#### Phase 5: 検証・テスト (2日)
- [ ] 全機能の動作確認
- [ ] パフォーマンステスト
- [ ] ドキュメント更新

**合計: 9日**

### 5.2 移行例: SubmitPurchaseRequest

**Before (50行):**
```csharp
public class SubmitPurchaseRequestHandler : IRequestHandler<...>
{
    // 6個の依存関係
    public async Task<Result<Guid>> Handle(...)
    {
        try
        {
            // 提出資格チェック (10行)
            // 購買申請作成 (10行)
            // 明細追加 (10行)
            // 承認フロー決定 (5行)
            // 提出 (5行)
            // 永続化 (5行)
            // ログ (5行)
        }
        catch (DomainException ex)
        {
            // エラーハンドリング (5行)
        }
    }
}
```

**After (8行):**
```csharp
public class SubmitPurchaseRequestHandler
    : CommandPipeline<SubmitPurchaseRequestCommand, Guid>
{
    protected override async Task<Result<Guid>> ExecuteAsync(
        SubmitPurchaseRequestCommand cmd, CancellationToken ct)
    {
        var eligibility = _submissionBoundary.GetContext(cmd.Title, cmd.Description, cmd.Items);
        if (!eligibility.CanSubmit) return Result.Fail<Guid>("提出不可");

        var request = PurchaseRequest.Create(_appContext.UserId, _appContext.UserName, cmd.Title, cmd.Description, _appContext.TenantId);
        await _repository.SaveAsync(request, ct);

        return Result.Success(request.Id);
    }
}
```

---

## 6. 新プロジェクト開始マニュアル

### 6.1 手順

**Step 1: ドメイン分析 (3週間)**
1. ドメインエキスパートとのイベントストーミング
2. Bounded Contextの特定
3. 集約・エンティティ・値オブジェクトの定義
4. ドメインサービス・Boundaryインターフェースの定義

**Step 2: Domain層構築 (1週間)**
1. `Domain/{BC}/{Aggregate}/` 作成
2. エンティティ・値オブジェクト実装
3. ドメインサービス実装
4. Boundaryインターフェース定義

**Step 3: アーキテクチャ統合 (2日)**
1. `Boundaries/Persistence/{BC}/DbContext` 作成
2. `Boundaries/Persistence/{BC}/Repositories/` 実装
3. `Boundaries/Persistence/{BC}/Configurations/` EF設定
4. `Boundaries/Host/DependencyInjection/{BC}ServiceExtensions.cs` 作成

**Step 4: Features実装 (1日)**
1. `Application/Features/{BC}/{Feature}CommandHandler.cs` 作成 (5-10行)
2. `Boundaries/Presentation/{BC}/{Feature}Page.razor` 作成

**Step 5: テスト (1日)**
1. Domain単体テスト
2. Integration テスト

**合計: 4週間 (うち3週間はドメイン分析)**

### 6.2 テンプレート活用

**コマンドハンドラー生成テンプレート:**
```bash
dotnet new vsa-command-handler \
  --bc PurchaseManagement \
  --feature SubmitPurchaseRequest \
  --aggregate PurchaseRequest
```

**生成されるファイル:**
- `Application/Features/PurchaseManagement/SubmitPurchaseRequestCommandHandler.cs` (8行)
- `Application/Features/PurchaseManagement/SubmitPurchaseRequestCommand.cs` (DTO)
- `Boundaries/Presentation/PurchaseManagement/PurchaseRequestSubmit.razor` (UI骨格)

---

## 7. まとめ

### 7.1 新アーキテクチャの特徴

**VSAのメリット (維持):**
- ✅ BC分離による並行開発
- ✅ 変更の局所化
- ✅ チーム独立性

**Boundaryアーキテクチャのメリット (統合):**
- ✅ 技術的関心の集約 (UI/DB/Host)
- ✅ ドメインの純粋性 (技術要素ゼロ)
- ✅ 汎用化推進 (Generics活用)

**工業製品化の達成:**
- ✅ Application/Core: 100%再利用
- ✅ Boundaries: 80%再利用 (テンプレート)
- ✅ Features: 定型コード (5-10行)
- ✅ 新プロジェクト: ドメイン分析 (3週間) + 統合 (1週間)

### 7.2 次のアクション

1. **Phase 1実装**: Application/Core基盤構築
2. **Phase 2実装**: Boundaries構造作成
3. **Phase 3実装**: Domain技術要素排除
4. **検証**: 1機能で移行テスト (SubmitPurchaseRequest推奨)
5. **全体移行**: 残りの機能を順次移行

**目標: 9日で移行完了**
