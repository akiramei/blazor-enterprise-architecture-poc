# 業務アプリ頻出パターン実装ロードマップ

## 📋 概要

このドキュメントは、VSASampleプロジェクトに「業務アプリケーションでよく遭遇するパターン」を追加するための実装ロードマップです。

**現状:** 主要な垂直スライス（CRUD、検索、CSV入出力、バルク操作、Store/Action、Outboxなど）は実装済み

**課題:** 実務で頻出する以下のパターンが未実装または文書化が不十分
1. 承認フロー・ワークフロー系
2. 集計・レポート系クエリ
3. 非同期処理・バッチ
4. ファイルアップロード・添付管理
5. 細粒度権限・マルチテナント

---

## 🎯 実装方針

### VSA原則の厳格な遵守

すべての新機能は、以下のVSA構造に従って実装します：

```
src/
└── {BoundedContext}/              # BC境界
    ├── Features/                  # 機能群
    │   └── {FeatureName}/         # 個別機能スライス
    │       ├── Application/       # Command/Handler/Validator
    │       └── UI/                # API/Components
    └── Shared/                    # BC内共通コード
        ├── Domain/
        ├── Infrastructure/
        └── UI/
```

**絶対禁止:**
- ❌ `src/`直下にレイヤー名プロジェクト（`*.Application`, `*.Domain`等）を作成
- ❌ 機能スライス間の直接依存

**必須:**
- ✅ 各機能は独立したスライス内で完結
- ✅ 共通コードは`{BC}/Shared/`または`Shared/`（グローバル）に配置
- ✅ 1機能追加 = 1スライスフォルダ内で完結

---

## 📦 パターン1: 承認フロー・ワークフロー系

### 目的

多段階承認や状態遷移（申請中→承認待ち→承認済み→却下）を伴う機能の実装パターンを提供します。
B2B業務アプリケーションで頻繁に必要となる、稟議・購買申請・経費精算などのワークフローに対応します。

### 採用技術・パターン

- **State Machine Pattern**: 状態遷移ロジックのカプセル化
- **Saga Pattern（疑似）**: 複数段階の処理と補償トランザクション
- **Domain Event**: 承認・却下時のイベント駆動処理
- **Outbox Pattern**: 承認通知メールの確実な配信

### 実装する新規BC

#### BC: `PurchaseManagement`（購買管理）

**理由:** 商品カタログとは異なるビジネスコンテキストであり、独立したBCとして切り出す

**構造:**
```
src/PurchaseManagement/
├── Features/
│   ├── SubmitPurchaseRequest/          # 購買申請提出
│   ├── ApprovePurchaseRequest/         # 購買申請承認
│   ├── RejectPurchaseRequest/          # 購買申請却下
│   ├── CancelPurchaseRequest/          # 購買申請キャンセル
│   ├── GetPurchaseRequestById/         # 申請詳細取得
│   └── GetPendingApprovals/            # 承認待ち一覧取得
│
└── Shared/
    ├── Domain/
    │   └── PurchaseRequests/
    │       ├── PurchaseRequest.cs              # 集約ルート
    │       ├── PurchaseRequestStatus.cs        # 状態列挙型
    │       ├── PurchaseRequestItem.cs          # 明細
    │       ├── ApprovalStep.cs                 # 承認ステップ
    │       ├── ApprovalFlow.cs                 # 承認フロー定義
    │       ├── StateMachine/
    │       │   ├── PurchaseRequestStateMachine.cs
    │       │   └── IStateMachine.cs
    │       └── Events/
    │           ├── PurchaseRequestSubmittedEvent.cs
    │           ├── PurchaseRequestApprovedEvent.cs
    │           └── PurchaseRequestRejectedEvent.cs
    │
    ├── Infrastructure/
    │   └── Persistence/
    │       ├── Configurations/
    │       │   ├── PurchaseRequestConfiguration.cs
    │       │   └── ApprovalStepConfiguration.cs
    │       └── Repositories/
    │           └── PurchaseRequestRepository.cs
    │
    └── UI/
        └── Store/
            └── PurchaseRequestsStore.cs
```

### コア実装: State Machine Pattern

#### PurchaseRequestStatus（状態定義）

```csharp
public enum PurchaseRequestStatus
{
    Draft = 0,              // 下書き
    Submitted = 1,          // 申請中
    PendingApproval = 2,    // 承認待ち（1次）
    PendingFinalApproval = 3, // 承認待ち（最終）
    Approved = 4,           // 承認済み
    Rejected = 5,           // 却下
    Cancelled = 6           // キャンセル
}
```

#### PurchaseRequestStateMachine（状態遷移ロジック）

```csharp
public class PurchaseRequestStateMachine : IStateMachine<PurchaseRequestStatus>
{
    private static readonly Dictionary<PurchaseRequestStatus, List<PurchaseRequestStatus>> _allowedTransitions = new()
    {
        { PurchaseRequestStatus.Draft, new() { PurchaseRequestStatus.Submitted } },
        { PurchaseRequestStatus.Submitted, new() { PurchaseRequestStatus.PendingApproval, PurchaseRequestStatus.Cancelled } },
        { PurchaseRequestStatus.PendingApproval, new() { PurchaseRequestStatus.PendingFinalApproval, PurchaseRequestStatus.Rejected, PurchaseRequestStatus.Cancelled } },
        { PurchaseRequestStatus.PendingFinalApproval, new() { PurchaseRequestStatus.Approved, PurchaseRequestStatus.Rejected } },
        { PurchaseRequestStatus.Approved, new() { } }, // 終端状態
        { PurchaseRequestStatus.Rejected, new() { } }, // 終端状態
        { PurchaseRequestStatus.Cancelled, new() { } } // 終端状態
    };

    public bool CanTransition(PurchaseRequestStatus from, PurchaseRequestStatus to)
    {
        return _allowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public void ValidateTransition(PurchaseRequestStatus from, PurchaseRequestStatus to)
    {
        if (!CanTransition(from, to))
            throw new InvalidStateTransitionException($"Cannot transition from {from} to {to}");
    }
}
```

#### PurchaseRequest（集約ルート）

```csharp
public class PurchaseRequest : AggregateRoot<Guid>
{
    private readonly PurchaseRequestStateMachine _stateMachine = new();
    private readonly List<ApprovalStep> _approvalSteps = new();
    private readonly List<PurchaseRequestItem> _items = new();

    public string RequestNumber { get; private set; } = string.Empty; // 申請番号
    public Guid RequesterId { get; private set; }                     // 申請者ID
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public PurchaseRequestStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }

    public IReadOnlyList<ApprovalStep> ApprovalSteps => _approvalSteps.AsReadOnly();
    public IReadOnlyList<PurchaseRequestItem> Items => _items.AsReadOnly();

    public Money TotalAmount => new(_items.Sum(i => i.Amount.Value));

    // ファクトリメソッド
    public static PurchaseRequest Create(Guid requesterId, string title, string description)
    {
        var request = new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = GenerateRequestNumber(),
            RequesterId = requesterId,
            Title = title,
            Description = description,
            Status = PurchaseRequestStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        return request;
    }

    // 明細追加
    public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (Status != PurchaseRequestStatus.Draft)
            throw new DomainException("明細の追加は下書き状態でのみ可能です");

        var item = new PurchaseRequestItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName,
            UnitPrice = new Money(unitPrice),
            Quantity = quantity,
            Amount = new Money(unitPrice * quantity)
        };

        _items.Add(item);
    }

    // 申請提出
    public void Submit(ApprovalFlow approvalFlow)
    {
        _stateMachine.ValidateTransition(Status, PurchaseRequestStatus.Submitted);

        if (_items.Count == 0)
            throw new DomainException("明細が1件もありません");

        // 承認フローを設定
        foreach (var step in approvalFlow.Steps)
        {
            _approvalSteps.Add(new ApprovalStep
            {
                Id = Guid.NewGuid(),
                StepNumber = step.StepNumber,
                ApproverId = step.ApproverId,
                ApproverName = step.ApproverName,
                Status = ApprovalStepStatus.Pending
            });
        }

        Status = PurchaseRequestStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;

        AddDomainEvent(new PurchaseRequestSubmittedEvent(Id, RequesterId, TotalAmount.Value));
    }

    // 承認
    public void Approve(Guid approverId, string comment)
    {
        // 現在の承認ステップを取得
        var currentStep = _approvalSteps.FirstOrDefault(s => s.Status == ApprovalStepStatus.Pending);
        if (currentStep is null)
            throw new DomainException("承認待ちのステップがありません");

        if (currentStep.ApproverId != approverId)
            throw new DomainException("このステップの承認者ではありません");

        // 承認ステップを完了
        currentStep.Approve(comment);

        // 次のステップがあるか確認
        var nextStep = _approvalSteps.FirstOrDefault(s => s.StepNumber == currentStep.StepNumber + 1);
        if (nextStep is not null)
        {
            // 次の承認ステップへ
            _stateMachine.ValidateTransition(Status, PurchaseRequestStatus.PendingApproval);
            Status = PurchaseRequestStatus.PendingApproval;
        }
        else
        {
            // 最終承認完了
            _stateMachine.ValidateTransition(Status, PurchaseRequestStatus.Approved);
            Status = PurchaseRequestStatus.Approved;
            ApprovedAt = DateTime.UtcNow;

            AddDomainEvent(new PurchaseRequestApprovedEvent(Id, RequesterId, approverId, TotalAmount.Value));
        }
    }

    // 却下
    public void Reject(Guid approverId, string reason)
    {
        var currentStep = _approvalSteps.FirstOrDefault(s => s.Status == ApprovalStepStatus.Pending);
        if (currentStep is null)
            throw new DomainException("承認待ちのステップがありません");

        if (currentStep.ApproverId != approverId)
            throw new DomainException("このステップの承認者ではありません");

        currentStep.Reject(reason);

        _stateMachine.ValidateTransition(Status, PurchaseRequestStatus.Rejected);
        Status = PurchaseRequestStatus.Rejected;

        AddDomainEvent(new PurchaseRequestRejectedEvent(Id, RequesterId, approverId, reason));
    }

    private static string GenerateRequestNumber()
    {
        return $"PR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }
}
```

#### ApprovalStep（承認ステップ）

```csharp
public class ApprovalStep : Entity<Guid>
{
    public int StepNumber { get; init; }
    public Guid ApproverId { get; init; }
    public string ApproverName { get; init; } = string.Empty;
    public ApprovalStepStatus Status { get; private set; }
    public string? Comment { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }

    public void Approve(string comment)
    {
        if (Status != ApprovalStepStatus.Pending)
            throw new DomainException("このステップは既に処理されています");

        Status = ApprovalStepStatus.Approved;
        Comment = comment;
        ApprovedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        if (Status != ApprovalStepStatus.Pending)
            throw new DomainException("このステップは既に処理されています");

        Status = ApprovalStepStatus.Rejected;
        Comment = reason;
        RejectedAt = DateTime.UtcNow;
    }
}

public enum ApprovalStepStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
```

### 機能スライス実装例

#### 1. SubmitPurchaseRequest（申請提出）

```csharp
// Command
[Authorize(Roles = "User,Manager")]
public record SubmitPurchaseRequestCommand : ICommand<Result<Guid>>
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required List<PurchaseRequestItemDto> Items { get; init; }
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}

public record PurchaseRequestItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity
);

// Handler
public class SubmitPurchaseRequestHandler : ICommandHandler<SubmitPurchaseRequestCommand, Result<Guid>>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly IApprovalFlowService _approvalFlowService;
    private readonly ICurrentUserService _currentUserService;

    public async Task<Result<Guid>> Handle(SubmitPurchaseRequestCommand cmd, CancellationToken ct)
    {
        // 1. 購買申請を作成
        var request = PurchaseRequest.Create(
            _currentUserService.UserId!.Value,
            cmd.Title,
            cmd.Description
        );

        // 2. 明細を追加
        foreach (var item in cmd.Items)
        {
            request.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);
        }

        // 3. 承認フローを決定（金額に応じて自動判定）
        var approvalFlow = await _approvalFlowService.DetermineFlowAsync(request.TotalAmount.Value, ct);

        // 4. 申請提出
        request.Submit(approvalFlow);

        // 5. 永続化
        await _repository.SaveAsync(request, ct);

        return Result.Success(request.Id);
    }
}
```

#### 2. ApprovePurchaseRequest（承認）

```csharp
// Command
[Authorize(Roles = "Manager,Director")]
public record ApprovePurchaseRequestCommand : ICommand<Result>
{
    public required Guid RequestId { get; init; }
    public required string Comment { get; init; }
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}

// Handler
public class ApprovePurchaseRequestHandler : ICommandHandler<ApprovePurchaseRequestCommand, Result>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public async Task<Result> Handle(ApprovePurchaseRequestCommand cmd, CancellationToken ct)
    {
        var request = await _repository.GetByIdAsync(cmd.RequestId, ct);
        if (request is null)
            return Result.Failure("購買申請が見つかりません");

        try
        {
            request.Approve(_currentUserService.UserId!.Value, cmd.Comment);
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }

        await _repository.SaveAsync(request, ct);

        return Result.Success();
    }
}
```

---

## 📊 パターン2: 集計・レポート系クエリ

### 目的

リアルタイム集計、ダッシュボード表示、複雑な検索条件での絞り込みなど、
業務アプリケーションで頻繁に必要となる高度なクエリパターンを提供します。

### 採用技術

- **Dapper + Raw SQL**: 複雑な結合・集計クエリの最適化
- **PostgreSQL Materialized View**: 事前集計による高速化
- **Dynamic Query Builder**: 柔軟な検索条件の組み立て
- **Specification Pattern**: 再利用可能なクエリ条件

### 実装する機能スライス

#### BC: `ProductCatalog`（既存BCに追加）

**理由:** 商品カタログに関連する集計・レポート機能なので、既存BCに追加

```
src/ProductCatalog/Features/
├── GetProductSalesReport/              # 商品売上レポート
├── GetInventoryTurnoverReport/         # 在庫回転率レポート
├── GetTopSellingProducts/              # 売れ筋商品ランキング
├── GetLowStockProducts/                # 在庫僅少商品一覧
└── SearchProductsAdvanced/             # 高度な商品検索（複合条件）
```

### コア実装: 集計クエリ

#### GetProductSalesReport（商品売上レポート）

```csharp
// Query
public record GetProductSalesReportQuery : IQuery<Result<ProductSalesReportDto>>, ICacheableQuery
{
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public Guid? CategoryId { get; init; }
    public string? ProductNameFilter { get; init; }

    public string GetCacheKey() => $"product-sales-report:{StartDate:yyyyMMdd}:{EndDate:yyyyMMdd}:{CategoryId}:{ProductNameFilter}";
    public int CacheDurationMinutes => 30; // 30分キャッシュ
}

// DTO
public record ProductSalesReportDto
{
    public required List<ProductSalesItem> Items { get; init; }
    public required SalesSummary Summary { get; init; }
}

public record ProductSalesItem
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal TotalSales { get; init; }
    public int TotalQuantity { get; init; }
    public decimal AverageUnitPrice { get; init; }
    public int OrderCount { get; init; }
}

public record SalesSummary
{
    public decimal TotalRevenue { get; init; }
    public int TotalOrders { get; init; }
    public int TotalProducts { get; init; }
    public decimal AverageOrderValue { get; init; }
}

// Handler（Dapper使用）
public class GetProductSalesReportHandler : IQueryHandler<GetProductSalesReportQuery, Result<ProductSalesReportDto>>
{
    private readonly IDbConnection _connection;

    public async Task<Result<ProductSalesReportDto>> Handle(GetProductSalesReportQuery query, CancellationToken ct)
    {
        // 動的にWHERE句を構築
        var whereConditions = new List<string> { "o.OrderDate BETWEEN @StartDate AND @EndDate" };
        var parameters = new DynamicParameters();
        parameters.Add("StartDate", query.StartDate);
        parameters.Add("EndDate", query.EndDate);

        if (query.CategoryId.HasValue)
        {
            whereConditions.Add("p.CategoryId = @CategoryId");
            parameters.Add("CategoryId", query.CategoryId.Value);
        }

        if (!string.IsNullOrEmpty(query.ProductNameFilter))
        {
            whereConditions.Add("p.Name LIKE @ProductNameFilter");
            parameters.Add("ProductNameFilter", $"%{query.ProductNameFilter}%");
        }

        var whereClause = string.Join(" AND ", whereConditions);

        // 集計クエリ（PostgreSQL最適化版）
        var sql = $@"
            SELECT
                p.Id AS ProductId,
                p.Name AS ProductName,
                SUM(oi.Quantity * oi.UnitPrice) AS TotalSales,
                SUM(oi.Quantity) AS TotalQuantity,
                AVG(oi.UnitPrice) AS AverageUnitPrice,
                COUNT(DISTINCT o.Id) AS OrderCount
            FROM
                Products p
                INNER JOIN OrderItems oi ON p.Id = oi.ProductId
                INNER JOIN Orders o ON oi.OrderId = o.Id
            WHERE
                {whereClause}
            GROUP BY
                p.Id, p.Name
            ORDER BY
                TotalSales DESC
            LIMIT 100;
        ";

        var items = await _connection.QueryAsync<ProductSalesItem>(sql, parameters);

        // サマリー計算
        var summary = new SalesSummary
        {
            TotalRevenue = items.Sum(i => i.TotalSales),
            TotalOrders = items.Sum(i => i.OrderCount),
            TotalProducts = items.Count(),
            AverageOrderValue = items.Any() ? items.Sum(i => i.TotalSales) / items.Sum(i => i.OrderCount) : 0
        };

        var report = new ProductSalesReportDto
        {
            Items = items.ToList(),
            Summary = summary
        };

        return Result.Success(report);
    }
}
```

#### Materialized View活用パターン

```sql
-- PostgreSQL Materialized View定義
CREATE MATERIALIZED VIEW product_sales_summary AS
SELECT
    p.id AS product_id,
    p.name AS product_name,
    DATE_TRUNC('day', o.order_date) AS order_date,
    SUM(oi.quantity * oi.unit_price) AS daily_sales,
    SUM(oi.quantity) AS daily_quantity,
    COUNT(DISTINCT o.id) AS daily_orders
FROM
    products p
    INNER JOIN order_items oi ON p.id = oi.product_id
    INNER JOIN orders o ON oi.order_id = o.id
WHERE
    o.order_date >= CURRENT_DATE - INTERVAL '90 days'
GROUP BY
    p.id, p.name, DATE_TRUNC('day', o.order_date);

-- インデックス作成
CREATE INDEX idx_product_sales_summary_date ON product_sales_summary(order_date);
CREATE INDEX idx_product_sales_summary_product ON product_sales_summary(product_id);

-- 定期更新（夜間バッチで実行）
REFRESH MATERIALIZED VIEW CONCURRENTLY product_sales_summary;
```

```csharp
// Materialized Viewを使用した高速クエリ
public async Task<Result<ProductSalesReportDto>> HandleUsingMaterializedView(
    GetProductSalesReportQuery query,
    CancellationToken ct)
{
    var sql = @"
        SELECT
            product_id AS ProductId,
            product_name AS ProductName,
            SUM(daily_sales) AS TotalSales,
            SUM(daily_quantity) AS TotalQuantity,
            SUM(daily_sales) / NULLIF(SUM(daily_quantity), 0) AS AverageUnitPrice,
            SUM(daily_orders) AS OrderCount
        FROM
            product_sales_summary
        WHERE
            order_date BETWEEN @StartDate AND @EndDate
        GROUP BY
            product_id, product_name
        ORDER BY
            TotalSales DESC;
    ";

    var parameters = new { query.StartDate, query.EndDate };
    var items = await _connection.QueryAsync<ProductSalesItem>(sql, parameters);

    // 以下、サマリー計算は同じ...
}
```

#### Specification Patternによる再利用可能なクエリ条件

```csharp
// Specification抽象クラス
public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public Func<T, bool> ToFunc() => ToExpression().Compile();

    public Specification<T> And(Specification<T> other)
    {
        return new AndSpecification<T>(this, other);
    }

    public Specification<T> Or(Specification<T> other)
    {
        return new OrSpecification<T>(this, other);
    }
}

// 具体的なSpecification
public class ProductNameContainsSpecification : Specification<Product>
{
    private readonly string _name;

    public ProductNameContainsSpecification(string name)
    {
        _name = name;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product => product.Name.Contains(_name);
    }
}

public class ProductPriceRangeSpecification : Specification<Product>
{
    private readonly decimal? _minPrice;
    private readonly decimal? _maxPrice;

    public ProductPriceRangeSpecification(decimal? minPrice, decimal? maxPrice)
    {
        _minPrice = minPrice;
        _maxPrice = maxPrice;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product =>
            (!_minPrice.HasValue || product.Price.Value >= _minPrice.Value) &&
            (!_maxPrice.HasValue || product.Price.Value <= _maxPrice.Value);
    }
}

// 使用例
public async Task<IEnumerable<Product>> SearchProducts(string nameFilter, decimal? minPrice, decimal? maxPrice)
{
    var spec = new ProductNameContainsSpecification(nameFilter)
        .And(new ProductPriceRangeSpecification(minPrice, maxPrice));

    return await _repository.FindAsync(spec.ToExpression());
}
```

---

## ⏱️ パターン3: 非同期処理・バッチ

### 目的

夜間集計、定期レポート生成、大量データ処理など、バックグラウンドで実行すべき処理のパターンを提供します。

### 採用技術

**Hangfire**を採用（理由）:
- ダッシュボードUI標準搭載（ジョブ監視が容易）
- PostgreSQL永続化対応
- リトライ・スケジュール設定が容易
- ASP.NET Core統合が成熟

### 実装する共通インフラ

**配置:** `Shared/Infrastructure/BackgroundJobs/`

```
Shared/Infrastructure/BackgroundJobs/
├── IBackgroundJobService.cs          # 抽象化インターフェース
├── HangfireJobService.cs             # Hangfire実装
├── HangfireConfiguration.cs          # DI登録拡張
└── Attributes/
    └── IdempotentJobAttribute.cs     # ジョブの冪等性保証
```

#### IBackgroundJobService（抽象化）

```csharp
public interface IBackgroundJobService
{
    /// <summary>
    /// ジョブをキューに追加（即座に実行）
    /// </summary>
    string Enqueue<TCommand>(TCommand command) where TCommand : ICommand;

    /// <summary>
    /// 指定時刻にジョブをスケジュール
    /// </summary>
    string Schedule<TCommand>(TCommand command, DateTime scheduleAt) where TCommand : ICommand;

    /// <summary>
    /// 定期実行ジョブを登録
    /// </summary>
    void AddOrUpdateRecurringJob<TCommand>(string jobId, TCommand command, string cronExpression)
        where TCommand : ICommand;

    /// <summary>
    /// ジョブをキャンセル
    /// </summary>
    bool Cancel(string jobId);

    /// <summary>
    /// ジョブのステータスを取得
    /// </summary>
    JobStatus GetJobStatus(string jobId);
}

public record JobStatus(string State, DateTime? StartedAt, DateTime? CompletedAt, string? ErrorMessage);
```

### 実装する機能スライス

#### BC: `ProductCatalog`（既存BCに追加）

```
src/ProductCatalog/Features/
├── GenerateNightlyProductReport/      # 夜間商品レポート生成
├── ArchiveInactiveProducts/           # 非アクティブ商品アーカイブ
├── SyncProductCatalogToExternal/      # 外部システムへの商品同期
└── CleanupExpiredProductImages/       # 期限切れ画像クリーンアップ
```

#### GenerateNightlyProductReport（夜間レポート生成）

```csharp
// Command
[Authorize(Roles = "System")]
public record GenerateNightlyProductReportCommand : ICommand<Result<string>>
{
    public required DateTime ReportDate { get; init; }
    public required string OutputPath { get; init; }
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}

// Handler
public class GenerateNightlyProductReportHandler : ICommandHandler<GenerateNightlyProductReportCommand, Result<string>>
{
    private readonly IProductRepository _repository;
    private readonly IReportGenerator _reportGenerator;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<GenerateNightlyProductReportHandler> _logger;

    public async Task<Result<string>> Handle(GenerateNightlyProductReportCommand cmd, CancellationToken ct)
    {
        _logger.LogInformation("Starting nightly product report generation for {ReportDate}", cmd.ReportDate);

        try
        {
            // 1. データ取得
            var products = await _repository.GetAllAsync(ct);

            // 2. レポート生成
            var report = _reportGenerator.GenerateInventoryReport(products, cmd.ReportDate);

            // 3. ファイル保存
            var fileName = $"product-report-{cmd.ReportDate:yyyyMMdd}.pdf";
            var filePath = Path.Combine(cmd.OutputPath, fileName);

            using var reportStream = report.ToStream();
            var fileUrl = await _fileStorage.UploadAsync(filePath, reportStream, "application/pdf", ct);

            _logger.LogInformation("Nightly product report generated successfully: {FileUrl}", fileUrl);

            return Result.Success(fileUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate nightly product report");
            return Result.Failure($"レポート生成に失敗しました: {ex.Message}");
        }
    }
}

// Hangfire Job登録（Program.cs）
public static class BackgroundJobsConfiguration
{
    public static void ConfigureRecurringJobs(this IApplicationBuilder app)
    {
        var jobService = app.ApplicationServices.GetRequiredService<IBackgroundJobService>();

        // 毎日深夜2時に実行
        jobService.AddOrUpdateRecurringJob(
            "nightly-product-report",
            new GenerateNightlyProductReportCommand
            {
                ReportDate = DateTime.Today,
                OutputPath = "/reports/nightly"
            },
            Cron.Daily(2)
        );

        // 毎週月曜日朝6時に実行
        jobService.AddOrUpdateRecurringJob(
            "weekly-inventory-check",
            new ArchiveInactiveProductsCommand
            {
                InactiveDays = 90
            },
            Cron.Weekly(DayOfWeek.Monday, 6)
        );
    }
}
```

---

## 📁 パターン4: ファイルアップロード・添付管理

### 目的

PDF、画像、Excel等のファイルアップロード、保存、ダウンロードを行う汎用的なパターンを提供します。
ストレージ先（ローカル/Azure Blob/AWS S3）を抽象化し、環境ごとの切り替えを容易にします。

### 実装する共通インフラ

**配置:** `Shared/Infrastructure/Storage/`

```
Shared/Infrastructure/Storage/
├── IFileStorageService.cs               # 抽象化インターフェース
├── LocalFileStorageService.cs           # ローカルファイルシステム実装
├── AzureBlobStorageService.cs           # Azure Blob実装
├── AwsS3StorageService.cs               # AWS S3実装
├── FileMetadata.cs                      # ファイルメタデータモデル
└── ImageProcessing/
    ├── IImageProcessor.cs
    └── ImageSharpProcessor.cs           # ImageSharp実装
```

#### IFileStorageService（抽象化）

```csharp
public interface IFileStorageService
{
    /// <summary>
    /// ファイルをアップロード
    /// </summary>
    Task<string> UploadAsync(string path, Stream content, string contentType, CancellationToken ct);

    /// <summary>
    /// ファイルをダウンロード
    /// </summary>
    Task<Stream> DownloadAsync(string path, CancellationToken ct);

    /// <summary>
    /// 署名付きURLを生成（一時的なアクセス許可）
    /// </summary>
    Task<string> GeneratePresignedUrlAsync(string path, TimeSpan expiration, CancellationToken ct);

    /// <summary>
    /// ファイルを削除
    /// </summary>
    Task DeleteAsync(string path, CancellationToken ct);

    /// <summary>
    /// ファイルの存在確認
    /// </summary>
    Task<bool> ExistsAsync(string path, CancellationToken ct);

    /// <summary>
    /// ファイルのメタデータを取得
    /// </summary>
    Task<FileMetadata?> GetMetadataAsync(string path, CancellationToken ct);
}

public record FileMetadata(
    string Path,
    long SizeBytes,
    string ContentType,
    DateTime CreatedAt,
    DateTime? LastModifiedAt
);
```

### 実装する機能スライス

#### BC: `ProductCatalog`（既存BCに追加）

```
src/ProductCatalog/Features/
├── UploadProductImage/                 # 商品画像アップロード
├── DeleteProductImage/                 # 商品画像削除
├── DownloadProductAttachment/          # 商品添付ファイルダウンロード
└── UploadProductDocument/              # 商品ドキュメント（PDF等）アップロード
```

#### UploadProductImage（商品画像アップロード）

```csharp
// Command
[Authorize(Roles = "Manager,Admin")]
public record UploadProductImageCommand : ICommand<Result<UploadedFileInfo>>
{
    public required Guid ProductId { get; init; }
    public required Stream FileStream { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long FileSizeBytes { get; init; }
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}

public record UploadedFileInfo(
    string FileId,
    string FileUrl,
    string ThumbnailUrl
);

// Validator
public class UploadProductImageValidator : AbstractValidator<UploadProductImageCommand>
{
    public UploadProductImageValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType)
            .Must(ct => ct == "image/jpeg" || ct == "image/png" || ct == "image/webp")
            .WithMessage("JPEG、PNG、WebP形式の画像のみアップロード可能です");
        RuleFor(x => x.FileSizeBytes)
            .LessThanOrEqualTo(5 * 1024 * 1024)
            .WithMessage("ファイルサイズは5MBまでです");
    }
}

// Handler
public class UploadProductImageHandler : ICommandHandler<UploadProductImageCommand, Result<UploadedFileInfo>>
{
    private readonly IProductRepository _productRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly IImageProcessor _imageProcessor;
    private readonly ILogger<UploadProductImageHandler> _logger;

    public async Task<Result<UploadedFileInfo>> Handle(UploadProductImageCommand cmd, CancellationToken ct)
    {
        // 1. 商品の存在確認
        var product = await _productRepository.GetByIdAsync(cmd.ProductId, ct);
        if (product is null)
            return Result<UploadedFileInfo>.Failure("商品が見つかりません");

        try
        {
            // 2. 画像リサイズ（オリジナル + サムネイル）
            cmd.FileStream.Position = 0;
            using var thumbnail = await _imageProcessor.ResizeAsync(cmd.FileStream, 200, 200, ct);

            // 3. ストレージに保存
            var fileId = Guid.NewGuid().ToString();
            var extension = Path.GetExtension(cmd.FileName);

            cmd.FileStream.Position = 0;
            var originalUrl = await _fileStorage.UploadAsync(
                $"products/{cmd.ProductId}/images/{fileId}{extension}",
                cmd.FileStream,
                cmd.ContentType,
                ct
            );

            thumbnail.Position = 0;
            var thumbnailUrl = await _fileStorage.UploadAsync(
                $"products/{cmd.ProductId}/images/{fileId}_thumb.jpg",
                thumbnail,
                "image/jpeg",
                ct
            );

            // 4. Product集約に画像情報を追加
            product.AddImage(fileId, originalUrl, thumbnailUrl, cmd.FileName, cmd.FileSizeBytes);
            await _productRepository.UpdateAsync(product, ct);

            _logger.LogInformation("Product image uploaded successfully: ProductId={ProductId}, FileId={FileId}", cmd.ProductId, fileId);

            return Result<UploadedFileInfo>.Success(new UploadedFileInfo(fileId, originalUrl, thumbnailUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload product image: ProductId={ProductId}", cmd.ProductId);
            return Result<UploadedFileInfo>.Failure($"画像のアップロードに失敗しました: {ex.Message}");
        }
    }
}
```

#### Product集約の拡張（画像管理）

```csharp
public class Product : AggregateRoot<Guid>
{
    // 既存プロパティ...

    private readonly List<ProductImage> _images = new();
    public IReadOnlyList<ProductImage> Images => _images.AsReadOnly();

    public void AddImage(string fileId, string url, string thumbnailUrl, string fileName, long fileSizeBytes)
    {
        // ビジネスルール: 商品画像は最大10枚まで
        if (_images.Count >= 10)
            throw new DomainException("商品画像は最大10枚までです");

        var image = new ProductImage
        {
            Id = Guid.NewGuid(),
            FileId = fileId,
            Url = url,
            ThumbnailUrl = thumbnailUrl,
            FileName = fileName,
            FileSizeBytes = fileSizeBytes,
            UploadedAt = DateTime.UtcNow
        };

        _images.Add(image);

        AddDomainEvent(new ProductImageAddedEvent(Id, image.Id, fileId));
    }

    public void RemoveImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
            throw new DomainException("画像が見つかりません");

        _images.Remove(image);

        AddDomainEvent(new ProductImageRemovedEvent(Id, imageId, image.FileId));
    }
}

public class ProductImage : Entity<Guid>
{
    public required string FileId { get; init; }
    public required string Url { get; init; }
    public required string ThumbnailUrl { get; init; }
    public required string FileName { get; init; }
    public required long FileSizeBytes { get; init; }
    public DateTime UploadedAt { get; init; }
}
```

---

## 🔐 パターン5: 細粒度権限・マルチテナント

### 目的

ロールベース認可から、より細かい権限管理（Permission-based）とマルチテナント分離を実現します。
企業向けSaaSでの典型的な要件に対応します。

### 実装アプローチ

#### 1. 権限クレームベース認可

**現状（ロールベース）:**
```csharp
[Authorize(Roles = "Admin")]
public class DeleteProductCommand : ICommand { }
```

**改善後（権限クレームベース）:**
```csharp
[Authorize(Permissions = "products:delete")]
public class DeleteProductCommand : ICommand { }
```

#### 2. マルチテナント分離

**戦略:** データベースレベルのテナント分離（共有DB + TenantIdフィルタ）

### 実装する新規BC

#### BC: `IdentityManagement`（新規BC）

```
src/IdentityManagement/
├── Features/
│   ├── AssignPermissionsToUser/        # ユーザー権限割り当て
│   ├── RevokePermissionsFromUser/      # ユーザー権限取り消し
│   ├── GetUserPermissions/             # ユーザー権限取得
│   ├── CreateTenant/                   # テナント作成
│   ├── UpdateTenant/                   # テナント更新
│   └── GetTenantById/                  # テナント詳細取得
│
└── Shared/
    ├── Domain/
    │   ├── Permissions/
    │   │   ├── Permission.cs
    │   │   ├── PermissionConstants.cs
    │   │   └── UserPermission.cs
    │   └── Tenants/
    │       ├── Tenant.cs
    │       ├── ITenantContext.cs
    │       └── TenantSettings.cs
    │
    ├── Infrastructure/
    │   ├── Persistence/
    │   │   ├── IdentityDbContext.cs
    │   │   └── Repositories/
    │   │       ├── PermissionRepository.cs
    │   │       └── TenantRepository.cs
    │   └── Services/
    │       └── CurrentTenantService.cs
    │
    └── UI/
        └── Store/
            └── PermissionsStore.cs
```

### コア実装: Permission（権限エンティティ）

```csharp
public class Permission : Entity<Guid>
{
    public required string Name { get; init; }           // 例: "products:delete"
    public required string Description { get; init; }    // 例: "商品を削除する権限"
    public required string Category { get; init; }       // 例: "ProductCatalog"
    public bool IsSystemPermission { get; init; }        // システム権限（削除不可）

    private readonly List<UserPermission> _userPermissions = new();
    public IReadOnlyList<UserPermission> UserPermissions => _userPermissions.AsReadOnly();
}

public class UserPermission : Entity<Guid>
{
    public required Guid UserId { get; init; }
    public required Guid PermissionId { get; init; }
    public required Guid TenantId { get; init; }         // テナント分離
    public DateTime GrantedAt { get; init; }
    public required string GrantedBy { get; init; }      // 付与者（監査用）

    public Permission Permission { get; init; } = null!;
}
```

### PermissionConstants（権限定数）

```csharp
public static class PermissionConstants
{
    // ProductCatalog権限
    public const string ProductsView = "products:view";
    public const string ProductsCreate = "products:create";
    public const string ProductsUpdate = "products:update";
    public const string ProductsDelete = "products:delete";
    public const string ProductsExport = "products:export";
    public const string ProductsImport = "products:import";

    // PurchaseManagement権限
    public const string PurchaseRequestsView = "purchase-requests:view";
    public const string PurchaseRequestsCreate = "purchase-requests:create";
    public const string PurchaseRequestsApprove = "purchase-requests:approve";
    public const string PurchaseRequestsReject = "purchase-requests:reject";

    // IdentityManagement権限
    public const string UsersView = "users:view";
    public const string UsersCreate = "users:create";
    public const string UsersUpdate = "users:update";
    public const string UsersDelete = "users:delete";
    public const string PermissionsManage = "permissions:manage";

    // テナント管理権限（システム管理者専用）
    public const string TenantsView = "tenants:view";
    public const string TenantsCreate = "tenants:create";
    public const string TenantsUpdate = "tenants:update";

    public static IEnumerable<Permission> GetDefaultPermissions()
    {
        return new[]
        {
            new Permission
            {
                Id = Guid.NewGuid(),
                Name = ProductsView,
                Description = "商品を閲覧する権限",
                Category = "ProductCatalog",
                IsSystemPermission = true
            },
            new Permission
            {
                Id = Guid.NewGuid(),
                Name = ProductsDelete,
                Description = "商品を削除する権限",
                Category = "ProductCatalog",
                IsSystemPermission = true
            },
            // その他の権限...
        };
    }
}
```

### AuthorizationBehavior拡張（権限チェック）

```csharp
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionRepository _permissionRepository;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var authorizeAttributes = request.GetType().GetCustomAttributes<AuthorizeAttribute>().ToArray();
        if (!authorizeAttributes.Any())
            return await next();

        var user = _currentUserService.User;
        if (user is null)
            throw new UnauthorizedAccessException("認証されていません");

        foreach (var attribute in authorizeAttributes)
        {
            // 1. ロールチェック（既存機能）
            if (attribute.Roles?.Length > 0)
            {
                var hasRole = attribute.Roles.Any(role => user.IsInRole(role));
                if (!hasRole)
                    throw new ForbiddenAccessException($"必要なロール権限がありません: {string.Join(", ", attribute.Roles)}");
            }

            // 2. 権限チェック（新規追加）
            if (attribute.Permissions?.Length > 0)
            {
                var userPermissions = await _permissionRepository.GetUserPermissionsAsync(user.Id, ct);
                var hasPermission = attribute.Permissions.Any(perm => userPermissions.Contains(perm));

                if (!hasPermission)
                {
                    _logger.LogWarning(
                        "User {UserId} attempted to access {Resource} without permission {Permissions}",
                        user.Id, typeof(TRequest).Name, string.Join(", ", attribute.Permissions));

                    throw new ForbiddenAccessException($"必要な権限がありません: {string.Join(", ", attribute.Permissions)}");
                }
            }
        }

        return await next();
    }
}
```

### マルチテナント実装

#### ITenantContext（現在のテナント情報）

```csharp
public interface ITenantContext
{
    Guid? TenantId { get; }
    string? TenantName { get; }
}

public class CurrentTenantService : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Guid? TenantId
    {
        get
        {
            var tenantIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("tenant_id");
            return tenantIdClaim != null ? Guid.Parse(tenantIdClaim.Value) : null;
        }
    }

    public string? TenantName =>
        _httpContextAccessor.HttpContext?.User.FindFirst("tenant_name")?.Value;
}
```

#### EF Core Global Query Filter（自動テナント分離）

```csharp
public class ProductDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 全エンティティにTenantIdを追加
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Global Query Filter: 現在のテナントのデータのみ取得
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var tenantIdProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
                var tenantIdValue = Expression.Constant(_tenantContext.TenantId);
                var filter = Expression.Equal(tenantIdProperty, tenantIdValue);
                var lambda = Expression.Lambda(filter, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);

                // Index追加（パフォーマンス最適化）
                modelBuilder.Entity(entityType.ClrType).HasIndex(nameof(ITenantEntity.TenantId));
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // 新規エンティティに自動的にTenantIdを設定
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                entry.Entity.TenantId = _tenantContext.TenantId
                    ?? throw new InvalidOperationException("現在のコンテキストにTenantIdが設定されていません");
            }
        }

        return base.SaveChangesAsync(ct);
    }
}

public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
```

---

## 📅 実装スケジュール

### フェーズ1: 承認フロー・ワークフロー（3週間）

**Week 1:**
- 新規BC `PurchaseManagement` 作成
- State Machine Pattern実装
- `PurchaseRequest`集約実装
- `SubmitPurchaseRequest`スライス実装

**Week 2:**
- `ApprovePurchaseRequest`スライス実装
- `RejectPurchaseRequest`スライス実装
- `GetPendingApprovals`スライス実装
- Unit/Integration Test作成

**Week 3:**
- Blazor UI実装（申請・承認画面）
- ドキュメント作成
- E2Eテスト実装

### フェーズ2: 集計・レポート系クエリ（2週間）

**Week 4:**
- `GetProductSalesReport`スライス実装
- `GetTopSellingProducts`スライス実装
- Dapper + Raw SQL最適化

**Week 5:**
- PostgreSQL Materialized View設定
- Specification Pattern実装
- `SearchProductsAdvanced`スライス実装
- ドキュメント作成

### フェーズ3: 非同期処理・バッチ（2週間）

**Week 6:**
- Hangfire基盤セットアップ
- `IBackgroundJobService`インターフェース実装
- `GenerateNightlyProductReport`スライス実装

**Week 7:**
- `ArchiveInactiveProducts`スライス実装
- Hangfireダッシュボード設定
- ドキュメント作成

### フェーズ4: ファイルアップロード・添付管理（2週間）

**Week 8:**
- `IFileStorageService`インターフェース実装
- `LocalFileStorageService`実装
- `IImageProcessor`実装（ImageSharp）

**Week 9:**
- `UploadProductImage`スライス実装
- `DownloadProductAttachment`スライス実装
- `AzureBlobStorageService`実装
- Product集約拡張（画像管理）
- Blazor UIコンポーネント実装

### フェーズ5: 細粒度権限・マルチテナント（3週間）

**Week 10:**
- 新規BC `IdentityManagement` 作成
- Permission/Tenantドメインモデル実装
- `ITenantContext`/`CurrentTenantService`実装

**Week 11:**
- EF Core Global Query Filter設定
- `AuthorizationBehavior`拡張（権限チェック）
- `AssignPermissionsToUser`スライス実装
- `GetUserPermissions`スライス実装

**Week 12:**
- `CreateTenant`スライス実装
- 既存Product集約のマルチテナント対応
- 権限管理UI実装
- ドキュメント作成

### フェーズ6: ドキュメント整備（1週間）

**Week 13:**
- 各パターンの詳細ドキュメント作成
- サンプルコード集（コピー&ペースト可能）
- トラブルシューティングガイド
- パフォーマンスチューニングガイド

**合計見積もり:** 約13週間（3ヶ月）

---

## ✅ 完了基準

各パターンは以下を満たした時点で「実装完了」とみなします：

### 1. 機能要件
- ✅ すべての機能スライスが正しく動作
- ✅ VSA原則に準拠した構造（検証スクリプトでチェック）
- ✅ ビジネスルールが適切にDomain層に配置

### 2. 品質要件
- ✅ Unit Testのカバレッジ80%以上
- ✅ Integration Test実装（主要シナリオ）
- ✅ E2E Test実装（重要な業務フロー）
- ✅ ビルドエラー・警告なし

### 3. ドキュメント
- ✅ 実装ガイド（コード例付き）
- ✅ アーキテクチャ図（Mermaid）
- ✅ トラブルシューティング
- ✅ パフォーマンス考慮事項

### 4. レビュー
- ✅ コードレビュー完了
- ✅ アーキテクチャレビュー完了（VSA準拠確認）
- ✅ セキュリティレビュー完了（特にマルチテナント）

---

## 📚 関連ドキュメント

- [VSA厳格ルール](../architecture/VSA-STRICT-RULES.md) - VSA構造の原則
- [パターンカタログ一覧](../blazor-guide-package/docs/05_パターンカタログ一覧.md) - 既存パターン
- [Domain層の詳細設計](../blazor-guide-package/docs/11_Domain層の詳細設計.md) - ドメインモデル設計
- [Application層の詳細設計](../blazor-guide-package/docs/10_Application層の詳細設計.md) - Command/Query実装

---

**作成日:** 2025-11-07
**最終更新:** 2025-11-07
**ステータス:** ✅ ロードマップ策定完了
