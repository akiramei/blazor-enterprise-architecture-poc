# VSAにおける横断的関心事（Cross-cutting Concerns）設計ガイド

## 概要

このドキュメントでは、Vertical Slice Architecture (VSA) における横断的関心事の設計原則と実装パターンを説明します。

## 目次

1. [横断的関心事とは](#横断的関心事とは)
2. [実装されている横断的機能](#実装されている横断的機能)
3. [AppContext - 統合コンテキスト](#appcontext---統合コンテキスト)
4. [監査ログ（Audit Log）](#監査ログaudit-log)
5. [メトリクス収集](#メトリクス収集)
6. [Pipeline Behaviors](#pipeline-behaviors)
7. [ベストプラクティス](#ベストプラクティス)

---

## 横断的関心事とは

横断的関心事（Cross-cutting Concerns）とは、アプリケーション全体に影響を与える機能要件のことです。

### 主な横断的関心事

- **認証・認可**: ユーザー識別、権限チェック
- **ログ**ing: システム動作の記録
- **監査ログ**: ユーザーアクション・データ変更の記録
- **メトリクス**: パフォーマンス・ビジネス指標の収集
- **エラーハンドリング**: 例外処理
- **トランザクション**: データ整合性の保証
- **キャッシング**: パフォーマンス最適化
- **バリデーション**: 入力検証
- **冪等性**: 重複実行の防止

### VSAにおける実装パターン

VSAでは、以下の3つのパターンで横断的関心事を実装します：

1. **Pipeline Behavior** (推奨): MediatRのパイプラインを利用した自動適用
2. **Middleware**: ASP.NET CoreのHTTPパイプライン
3. **Service Injection**: 明示的なサービス注入

---

## 実装されている横断的機能

### Pipeline Behaviors

```csharp
// Program.cs での登録順序（重要！）
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MetricsBehavior<,>));        // 0. メトリクス収集
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));        // 1. ログ出力
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));     // 2. バリデーション
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>)); // 3. 認可
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));   // 4. 冪等性（Command）
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));       // 5. キャッシング（Query）
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditLogBehavior<,>));      // 6. 監査ログ（Command）
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));   // 7. トランザクション（Command）
```

### Middlewares

```csharp
app.UseMiddleware<CorrelationIdMiddleware>();           // Correlation ID付与
app.UseMiddleware<GlobalExceptionHandlerMiddleware>(); // 例外ハンドリング
app.UseSerilogRequestLogging();                         // HTTPリクエストログ
```

---

## AppContext - 統合コンテキスト

### 概要

`IAppContext` は、リクエストスコープの情報を一元管理する統合コンテキストです。

### 提供する情報

#### ユーザー情報
- `UserId`: 現在のユーザーID
- `UserName`: ユーザー名
- `TenantId`: テナントID（マルチテナント対応）
- `IsAuthenticated`: 認証状態
- `IsInRole(role)`: ロール判定

#### リクエスト追跡情報
- `CorrelationId`: 分散トレーシング用ID
- `RequestId`: リクエスト識別ID

#### リクエストメタデータ
- `RequestStartTimeUtc`: リクエスト開始時刻
- `RequestPath`: リクエストパス
- `HttpMethod`: HTTPメソッド
- `ClientIpAddress`: クライアントIP
- `UserAgent`: User-Agent

#### 環境情報
- `EnvironmentName`: 環境名（Development, Production等）
- `HostName`: ホスト名
- `ApplicationName`: アプリケーション名

### 使用例

```csharp
public class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IAppContext _appContext;
    private readonly IProductRepository _repository;

    public CreateProductHandler(
        IAppContext appContext,
        IProductRepository repository)
    {
        _appContext = appContext;
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        // 現在のユーザー情報を取得
        var userId = _appContext.UserId;
        var userName = _appContext.UserName;

        // 管理者権限チェック
        if (!_appContext.IsInRole("Admin"))
        {
            return Result<Guid>.Failure("管理者権限が必要です");
        }

        // ロギングにCorrelation IDを使用
        _logger.LogInformation(
            "商品作成: {UserName} [CorrelationId: {CorrelationId}]",
            userName,
            _appContext.CorrelationId);

        // 商品作成処理...
        var product = Product.Create(request.Name, request.Description, ...);
        await _repository.AddAsync(product, cancellationToken);

        return Result<Guid>.Success(product.Id.Value);
    }
}
```

### 既存サービスとの互換性

`IAppContext` は以下の既存サービスを統合していますが、後方互換性のため既存サービスも残されています：

- `ICurrentUserService`: ユーザー情報（互換性維持）
- `ICorrelationIdAccessor`: Correlation ID（互換性維持）

**推奨**: 新規実装では `IAppContext` を使用してください。

---

## 監査ログ（Audit Log）

### 概要

監査ログは、ユーザーアクションとデータ変更履歴を自動記録する機能です。

### 使用シナリオ

- コンプライアンス対応
- セキュリティ監査
- データ変更履歴の追跡
- トラブルシューティング

### 実装方法

#### 1. Commandに`IAuditableCommand`を実装

```csharp
public record DeleteProductCommand(Guid ProductId)
    : ICommand<Result>, IAuditableCommand
{
    public AuditInfo GetAuditInfo()
    {
        return new AuditInfo(
            Action: "DeleteProduct",
            EntityType: "Product",
            EntityId: ProductId.ToString());
    }
}
```

#### 2. 自動記録

`AuditLogBehavior` が自動的に以下を記録します：

- ユーザー情報（UserId, UserName, TenantId）
- アクション情報（Action, EntityType, EntityId）
- リクエスト情報（CorrelationId, RequestId, Path, Method）
- クライアント情報（IPアドレス, User-Agent）
- タイムスタンプ（UTC）

#### 3. 監査ログの取得

```csharp
public class AuditLogQueryHandler
{
    private readonly IAuditLogRepository _auditLogRepository;

    public async Task<IEnumerable<AuditLog>> GetProductAuditLogs(Guid productId)
    {
        return await _auditLogRepository.GetByEntityAsync(
            entityType: "Product",
            entityId: productId.ToString());
    }
}
```

### データベーススキーマ

```sql
CREATE TABLE AuditLogs (
    Id UUID PRIMARY KEY,
    UserId UUID NOT NULL,
    UserName VARCHAR(256) NOT NULL,
    TenantId UUID NULL,
    Action VARCHAR(100) NOT NULL,
    EntityType VARCHAR(100) NOT NULL,
    EntityId VARCHAR(100) NOT NULL,
    OldValues TEXT NULL,
    NewValues TEXT NULL,
    CorrelationId VARCHAR(100) NOT NULL,
    RequestId UUID NOT NULL,
    RequestPath VARCHAR(500) NULL,
    HttpMethod VARCHAR(10) NULL,
    ClientIpAddress VARCHAR(50) NULL,
    UserAgent VARCHAR(500) NULL,
    TimestampUtc TIMESTAMP NOT NULL,

    INDEX IX_AuditLogs_Entity (EntityType, EntityId),
    INDEX IX_AuditLogs_UserId (UserId),
    INDEX IX_AuditLogs_Timestamp (TimestampUtc),
    INDEX IX_AuditLogs_CorrelationId (CorrelationId)
);
```

### ベストプラクティス

1. **重要なCommandのみ監査対象にする**
   - Create, Update, Delete操作
   - 管理者権限が必要な操作

2. **機密情報はOldValues/NewValuesに含めない**
   - パスワード、クレジットカード番号等

3. **監査ログは削除しない**
   - イミュータブルな記録として保持

---

## メトリクス収集

### 概要

OpenTelemetryを使用したメトリクス収集機能です。パフォーマンス監視とビジネス指標の追跡を行います。

### 収集されるメトリクス

#### パフォーマンスメトリクス（自動）

```csharp
// リクエスト実行時間（ミリ秒）
product_catalog.request.duration
    - タグ: request_type, status

// リクエスト実行回数
product_catalog.request.count
    - タグ: request_type, status

// エラー数
product_catalog.request.errors
    - タグ: request_type
```

#### ビジネスメトリクス（手動）

```csharp
// 商品作成数
product_catalog.product.created

// 商品更新数
product_catalog.product.updated

// 商品削除数
product_catalog.product.deleted
```

### メトリクスの利用

#### 自動収集（MetricsBehavior）

すべてのCommand/Queryの実行時間と成功/失敗が自動記録されます。

#### 手動記録

```csharp
public class CreateProductHandler
{
    private readonly ApplicationMetrics _metrics;

    public async Task<Result<Guid>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        // 商品作成処理...
        var product = await _repository.AddAsync(...);

        // ビジネスメトリクスを記録
        _metrics.ProductCreated.Add(1);

        return Result<Guid>.Success(product.Id.Value);
    }
}
```

### エクスポーター設定

#### 開発環境: Console Exporter

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("ProductCatalog")
            .AddConsoleExporter(); // コンソールに出力
    });
```

#### 本番環境: Prometheus Exporter

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("ProductCatalog")
            .AddPrometheusExporter(); // Prometheusエンドポイント公開
    });

// エンドポイント設定
app.MapPrometheusScrapingEndpoint(); // /metrics
```

### Grafanaダッシュボード例

```promql
# 平均レスポンスタイム
rate(product_catalog_request_duration_sum[5m])
  / rate(product_catalog_request_duration_count[5m])

# エラー率
rate(product_catalog_request_errors[5m])
  / rate(product_catalog_request_count[5m])

# 商品作成数（1時間あたり）
rate(product_catalog_product_created[1h])
```

---

## Pipeline Behaviors

### 実行順序の重要性

Pipeline Behaviorsは登録順に実行されるため、順序が重要です。

```
リクエスト
  ↓
MetricsBehavior (全体の実行時間を計測)
  ↓
LoggingBehavior (ログ出力開始)
  ↓
ValidationBehavior (バリデーション)
  ↓
AuthorizationBehavior (認可チェック)
  ↓
IdempotencyBehavior (冪等性チェック)
  ↓
CachingBehavior (キャッシュから取得)
  ↓
AuditLogBehavior (監査ログ準備)
  ↓
TransactionBehavior (トランザクション開始)
  ↓
Handler (ビジネスロジック実行)
  ↓
TransactionBehavior (トランザクションコミット)
  ↓
AuditLogBehavior (監査ログ保存)
  ↓
... (逆順で終了処理)
  ↓
レスポンス
```

### Behavior追加のガイドライン

1. **最外層（最初）**
   - メトリクス収集
   - ロギング

2. **ビジネスロジック前**
   - バリデーション
   - 認可
   - キャッシング

3. **ビジネスロジック後**
   - 監査ログ
   - トランザクション（最後）

---

## ベストプラクティス

### 1. IAppContextを優先使用

```csharp
// Good: 統合コンテキストを使用
public class MyHandler
{
    private readonly IAppContext _appContext;

    public async Task Handle(...)
    {
        var userId = _appContext.UserId;
        var correlationId = _appContext.CorrelationId;
        // ...
    }
}

// Avoid: 個別サービスの使用（互換性のためのみ残存）
public class MyHandler
{
    private readonly ICurrentUserService _currentUser;
    private readonly ICorrelationIdAccessor _correlationId;
    // ...
}
```

### 2. 監査ログは重要なCommandのみ

```csharp
// Good: 重要な操作のみ監査
public record DeleteProductCommand(...) : ICommand<Result>, IAuditableCommand
{
    // ...
}

// Avoid: 読み取り操作は監査不要
public record GetProductQuery(...) : IQuery<Result<ProductDto>>, IAuditableCommand
{
    // 不要 - Queryは監査対象外
}
```

### 3. メトリクスは意味のある単位で

```csharp
// Good: 明確な命名と単位
_metrics.RequestDuration.Record(
    duration,
    new KeyValuePair<string, object?>("request_type", requestType),
    new KeyValuePair<string, object?>("status", "success"));

// Avoid: 曖昧な命名
_metrics.Time.Record(duration); // 何の時間？単位は？
```

### 4. Pipeline Behaviorsの順序を守る

```csharp
// Good: 推奨順序
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MetricsBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
// ...

// Bad: トランザクションが最初
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
// バリデーション前にトランザクションが開始される（無駄なDB接続）
```

### 5. Correlation IDを活用

```csharp
// Good: すべてのログにCorrelation IDを含める
_logger.LogInformation(
    "処理開始: {Action} [CorrelationId: {CorrelationId}]",
    action,
    _appContext.CorrelationId);

// 分散トレーシングで全体の流れを追跡可能
```

---

## まとめ

VSAにおける横断的関心事は、以下の原則に基づいて実装されています：

1. **Pipeline Behaviorによる自動適用**: 宣言的で簡潔
2. **AppContextによる情報一元管理**: 統合されたアクセスポイント
3. **監査ログによる透明性**: コンプライアンス対応
4. **メトリクスによる可観測性**: パフォーマンス監視

これらの機能により、エンタープライズグレードのアプリケーション要件を満たしつつ、VSAの利点である機能の独立性とテスト容易性を維持できます。
