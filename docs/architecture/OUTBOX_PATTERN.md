# トランザクショナルOutboxパターン実装ガイド

## 概要

このプロジェクトでは、マイクロサービス間の結果整合性を保証するため、**トランザクショナルOutboxパターン**を実装しています。

## アーキテクチャ原則

### ✅ 正しい実装（現在の実装）

```
┌─────────────────────────────────────────────────────────────┐
│ ProductCatalog BC (Bounded Context)                         │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────────────────────────────────┐               │
│  │ TransactionBehavior (書き込み側)        │               │
│  ├──────────────────────────────────────────┤               │
│  │ 1. ドメインイベントを収集                │               │
│  │ 2. ProductCatalogDbContext.OutboxMessages│               │
│  │    に直接書き込み                         │               │
│  │ 3. 単一トランザクションでコミット        │               │
│  └──────────────────────────────────────────┘               │
│                     ↓                                        │
│  ┌──────────────────────────────────────────┐               │
│  │ ProductCatalogDbContext                  │               │
│  ├──────────────────────────────────────────┤               │
│  │ - Products (ドメインテーブル)            │               │
│  │ - OutboxMessages (Outboxテーブル)        │               │
│  │                                           │               │
│  │ ★ 同一データベース → トランザクション保証│               │
│  └──────────────────────────────────────────┘               │
│                     ↑                                        │
│  ┌──────────────────────────────────────────┐               │
│  │ ProductCatalogOutboxReader (読み取り側) │               │
│  ├──────────────────────────────────────────┤               │
│  │ 1. ProductCatalogDbContext.OutboxMessages│               │
│  │    から未処理メッセージを読み取り        │               │
│  │ 2. Platform DTOに変換                    │               │
│  │ 3. 配信完了後、ステータス更新            │               │
│  └──────────────────────────────────────────┘               │
│                                                              │
└─────────────────────────────────────────────────────────────┘
                     ↑
┌────────────────────┴─────────────────────────────┐
│ Platform Layer (グローバルインフラ)              │
├──────────────────────────────────────────────────┤
│                                                   │
│  ┌────────────────────────────────────┐          │
│  │ OutboxBackgroundService            │          │
│  ├────────────────────────────────────┤          │
│  │ - IEnumerable<IOutboxReader> を巡回│          │
│  │ - 各BCのOutboxをポーリング          │          │
│  │ - メッセージブローカーに配信        │          │
│  └────────────────────────────────────┘          │
│                                                   │
└───────────────────────────────────────────────────┘
```

### ❌ 間違った実装（IOutboxStoreの使用）

```
❌ TransactionBehavior
    ↓
  IOutboxStore.AddAsync()
    ↓
  PlatformDbContext.OutboxMessages

✅ TransactionBehavior
    ↓
  ProductCatalogDbContext.Products
  ProductCatalogDbContext.OutboxMessages (★ 同一トランザクション)
```

**問題**: `IOutboxStore` は `PlatformDbContext` を使用するため、`ProductCatalogDbContext` と**別のトランザクション**になってしまい、原子性が保証されない。

## 実装詳細

### 1. 書き込み側: TransactionBehavior

**場所**: `src/Application/Shared/ProductCatalog/Infrastructure/Persistence/Behaviors/TransactionBehavior.cs`

**責務**:
- ビジネスエンティティ (Product) の更新
- ドメインイベントをOutboxMessageに変換
- **同一トランザクション**でコミット

**コード例**:
```csharp
// ✅ 正しい実装
var outboxMessage = OutboxMessage.Create(eventType, eventContent);
await _context.OutboxMessages.AddAsync(outboxMessage, ct); // ProductCatalogDbContext

// ❌ 間違った実装（使用禁止）
// await _outboxStore.AddAsync(outboxMessage, ct); // PlatformDbContext（別トランザクション）
```

### 2. 読み取り側: ProductCatalogOutboxReader

**場所**: `src/Application/Shared/ProductCatalog/Infrastructure/Persistence/ProductCatalogOutboxReader.cs`

**責務**:
- 未処理メッセージの取得
- Platform DTO (`Shared.Abstractions.Platform.OutboxMessage`) への変換
- 処理ステータスの更新

**インターフェース**: `IOutboxReader`

**登録**:
```csharp
// Program.cs
builder.Services.AddScoped<IOutboxReader, ProductCatalogOutboxReader>();
```

### 3. 配信側: OutboxBackgroundService

**場所**: `src/Shared/Infrastructure/Platform/OutboxBackgroundService.cs`

**責務**:
- 全BCの `IOutboxReader` を巡回
- 未処理メッセージをメッセージブローカーに配信
- 配信成功/失敗のステータス管理

**動作フロー**:
1. 30秒ごとにポーリング
2. `IEnumerable<IOutboxReader>` から全Readerを取得
3. 各Readerから最大20件のメッセージを取得
4. メッセージブローカーに配信
5. 成功 → `MarkSucceededAsync()`, 失敗 → `MarkFailedAsync()`

## なぜIOutboxStoreを使わないのか？

### 理由1: トランザクション保証の破綻

```csharp
// ❌ IOutboxStoreを使った場合
await using var transaction = await _productCatalogDbContext.Database.BeginTransactionAsync();

// Product更新（ProductCatalogDbContext）
await _productCatalogDbContext.Products.AddAsync(product);

// Outbox保存（PlatformDbContext）← 別のDbContext！
await _outboxStore.AddAsync(outboxMessage); // 内部でPlatformDbContextを使用

await transaction.CommitAsync(); // ← Productだけコミット、Outboxは別トランザクション
```

**結果**: Productは保存されたが、Outboxメッセージは保存されなかった（またはその逆）→ **データ不整合**

### 理由2: VSAの境界を尊重

- **ProductCatalog BC** は自分のデータ（Product + Outbox）を完全に管理すべき
- **Platform Layer** は配信のみを担当（読み取りと配信）
- 書き込みまでPlatform Layerが担当すると、BCの自律性が損なわれる

### 理由3: 実装のシンプルさ

- `IOutboxStore` は不要な抽象化レイヤー
- BCごとに専用のDbContextがあれば、直接書き込みで十分
- テストも簡単（単一DbContextのみモック）

## 新しいBCを追加する場合の手順

### 1. Outboxテーブルを追加

```csharp
// OrderDbContext.cs
public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
```

### 2. TransactionBehaviorを実装

```csharp
// OrderTransactionBehavior.cs
public class OrderTransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly OrderDbContext _context; // BC専用DbContext

    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        // ... ドメインイベント収集 ...

        var outboxMessage = OutboxMessage.Create(eventType, eventContent);
        await _context.OutboxMessages.AddAsync(outboxMessage, ct); // ✅ 直接書き込み
    }
}
```

### 3. IOutboxReaderを実装

```csharp
// OrderOutboxReader.cs
public class OrderOutboxReader : IOutboxReader
{
    private readonly OrderDbContext _context; // BC専用DbContext

    public async Task<IReadOnlyList<OutboxMessage>> DequeueAsync(int take, CancellationToken ct)
    {
        var messages = await _context.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(take)
            .ToListAsync(ct);

        // Platform DTOに変換
        return messages.Select(m => new Shared.Abstractions.Platform.OutboxMessage
        {
            Id = m.Id,
            Type = m.Type,
            Content = m.Content,
            OccurredOnUtc = m.OccurredOnUtc
        }).ToList();
    }

    // MarkSucceededAsync, MarkFailedAsync も実装
}
```

### 4. DIコンテナに登録

```csharp
// Program.cs
builder.Services.AddScoped<IOutboxReader, OrderOutboxReader>();
```

**これだけ！** `OutboxBackgroundService` が自動的に検出して配信を開始します。

## IOutboxStoreについて（削除済み）

**注意**: 以前のバージョンでは `IOutboxStore` が存在しましたが、以下の理由で削除されました:

### 削除理由

1. **実際に使用されていなかった** - どのBehaviorからも呼ばれていない
2. **誤解を招く** - 新規開発者が「これを使うべき」と誤解する可能性
3. **トランザクション保証を破る危険性** - 使うとProductとOutboxが別トランザクションになる

### 削除したファイル

- ~~`src/Shared/Abstractions/Platform/IOutboxStore.cs`~~ (削除済み)
- ~~`src/Shared/Infrastructure/Platform/Stores/OutboxStore.cs`~~ (削除済み)
- ~~`src/Application/Program.cs` の DI登録~~ (削除済み)

### 代わりに使用するパターン

各BCの `TransactionBehavior` が直接 `DbContext.OutboxMessages` に書き込み、`IOutboxReader` が読み取る構成になっています。

## テストでの検証

統合テストでトランザクション保証を確認できます:

```csharp
[Fact]
public async Task TransactionalOutbox_ProductCreation_EnsuresAtomicity()
{
    // Act: Product作成
    var response = await _client.PostAsJsonAsync("/api/v1/products", createRequest);

    // Assert: ProductとOutboxが両方存在
    var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Name == "Test");
    var outboxMessage = await dbContext.OutboxMessages
        .FirstOrDefaultAsync(m => m.Content.Contains(product.Id.ToString()));

    product.Should().NotBeNull();
    outboxMessage.Should().NotBeNull(); // ← 同一トランザクションで保存されている
}
```

## まとめ

### ✅ 現在の実装（正しい）

- **書き込み**: `TransactionBehavior` → `ProductCatalogDbContext.OutboxMessages`
- **読み取り**: `ProductCatalogOutboxReader` → `ProductCatalogDbContext.OutboxMessages`
- **配信**: `OutboxBackgroundService` → `IOutboxReader`

### ❌ 使用禁止

- `IOutboxStore` を使った書き込み（トランザクション保証が破綻）

### 次のアクション

1. ✅ 統合テストの追加（完了）
2. ⏳ `IOutboxStore` の削除（推奨）
3. ⏳ 他BCへの展開（Order, Inventory等）
