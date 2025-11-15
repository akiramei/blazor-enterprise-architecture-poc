# ドメインモデルのBC間分離

## 🎯 重要な原則

**BC固有のドメインモデルは、BC間で共有されない。**

---

## 📐 現在の構造

```
src/
├── Shared/
│   ├── Kernel/
│   │   └── Money.cs                     ← 全BC共通の値オブジェクト
│   └── Domain/                          ← ★ BC横断の共通ドメイン
│       ├── Identity/
│       │   └── ApplicationUser.cs       ← 全BCで使用
│       ├── Outbox/
│       │   └── OutboxMessage.cs
│       └── AuditLogs/
│           └── AuditLog.cs
│
├── PurchaseManagement/                  ← BC1（購買管理）
│   ├── Features/
│   │   ├── ApprovePurchaseRequest/
│   │   └── GetPurchaseRequestById/
│   │
│   └── Shared/
│       └── Domain/                      ← ★ PurchaseManagement BC固有
│           └── PurchaseRequests/
│               ├── PurchaseRequest.cs   ← このBC内のみ
│               ├── ApprovalStep.cs      ← このBC内のみ
│               └── Boundaries/          ← このBC内のみ
│
└── ProductCatalog/                      ← BC2（商品カタログ）
    ├── Features/
    │   └── CreateProduct/
    │
    └── Shared/
        └── Domain/                      ← ★ ProductCatalog BC固有
            └── Products/
                └── Product.cs           ← このBC内のみ
```

---

## 🔍 3つの「Shared/Domain」の違い

| パス | スコープ | 内容 | 参照元 |
|------|---------|------|--------|
| **`src/Shared/Domain/`** | **BC横断** | 全BC共通のドメイン知識 | すべてのBC |
| **`src/PurchaseManagement/Shared/Domain/`** | **BC内** | PurchaseManagement BC固有のドメインモデル | PurchaseManagement BC内のみ |
| **`src/ProductCatalog/Shared/Domain/`** | **BC内** | ProductCatalog BC固有のドメインモデル | ProductCatalog BC内のみ |

---

## ✅ 正しい参照関係

### 1. BC横断の共有ドメイン（`src/Shared/Domain/`）

```csharp
// ✅ PurchaseManagement BC から参照
// src/PurchaseManagement/Features/.../Handler.cs
using Shared.Domain.Identity;  // ← ApplicationUser（全BC共通）

var user = await _userManager.FindByIdAsync(currentUserId);
```

```csharp
// ✅ ProductCatalog BC から参照
// src/ProductCatalog/Features/.../Handler.cs
using Shared.Domain.Identity;  // ← ApplicationUser（全BC共通）

var user = await _userManager.FindByIdAsync(currentUserId);
```

**両方のBCから参照できる** ← 全BC共通だから ✅

---

### 2. BC固有のドメイン（`src/PurchaseManagement/Shared/Domain/`）

```csharp
// ✅ PurchaseManagement BC内から参照（OK）
// src/PurchaseManagement/Features/ApprovePurchaseRequest/Application/Handler.cs
using PurchaseManagement.Shared.Domain.PurchaseRequests;

var request = await _repository.GetByIdAsync(requestId);
var result = request.Approve(approverId);
```

```csharp
// ❌ ProductCatalog BC から参照（NG）
// src/ProductCatalog/Features/CreateProduct/Application/Handler.cs
using PurchaseManagement.Shared.Domain.PurchaseRequests;  // ← ❌ BC境界を越える参照

var request = ...  // ← ❌ 絶対にやってはいけない
```

**BC境界を越えた参照は禁止** ← BC固有のドメインだから ❌

---

### 3. BC固有のドメイン（`src/ProductCatalog/Shared/Domain/`）

```csharp
// ✅ ProductCatalog BC内から参照（OK）
// src/ProductCatalog/Features/CreateProduct/Application/Handler.cs
using ProductCatalog.Shared.Domain.Products;

var product = Product.Create(name, price);
```

```csharp
// ❌ PurchaseManagement BC から参照（NG）
// src/PurchaseManagement/Features/.../Handler.cs
using ProductCatalog.Shared.Domain.Products;  // ← ❌ BC境界を越える参照

var product = ...  // ← ❌ 絶対にやってはいけない
```

**BC境界を越えた参照は禁止** ← BC固有のドメインだから ❌

---

## 🚫 BC間でドメインモデルを共有してはいけない理由

### 理由1: BC境界の独立性が失われる

```
もし PurchaseManagement BC が ProductCatalog BC のドメインモデルを参照すると:

PurchaseManagement BC
  ↓ 依存
ProductCatalog BC

→ ProductCatalog BC の変更が PurchaseManagement BC に影響
→ BC境界の意味がなくなる
```

### 理由2: ドメイン知識の混在

```
PurchaseRequest (購買申請) と Product (商品) は異なるドメイン知識

PurchaseRequest の「承認」ロジック
  ≠ Product の「在庫」ロジック

BC固有のドメイン知識は BC内に閉じるべき
```

### 理由3: 変更の影響範囲が拡大

```
Product エンティティの変更:
  ✅ 影響範囲: ProductCatalog BC のみ（独立している）
  ❌ 影響範囲: PurchaseManagement BC にも波及（依存している場合）
```

---

## ✅ BC間でデータを共有する正しい方法

### ❌ 間違った方法：ドメインモデルの直接参照

```csharp
// ❌ 間違い
// src/PurchaseManagement/Features/.../Handler.cs
using ProductCatalog.Shared.Domain.Products;  // ← ❌ BC境界を越える

public class SubmitPurchaseRequestHandler
{
    private readonly IProductRepository _productRepository;  // ← ❌ 他のBCのリポジトリ

    public async Task<Result> Handle(SubmitPurchaseRequestCommand command)
    {
        // ❌ 他のBCのドメインモデルを直接使用
        var product = await _productRepository.GetByIdAsync(command.ProductId);

        if (product.Stock < command.Quantity)  // ← ❌ BC境界を越えた知識
            return Result.Failure("在庫不足");
    }
}
```

**問題点:**
- ❌ BC境界を越えた依存
- ❌ ProductCatalog BC の変更が PurchaseManagement BC に影響

---

### ✅ 正しい方法1：統合イベント（非同期通信）

```csharp
// ✅ ProductCatalog BC がイベントを発行
// src/ProductCatalog/Shared/Domain/Products/Events/ProductStockChangedEvent.cs
namespace ProductCatalog.Shared.Domain.Products.Events;

public sealed record ProductStockChangedEvent : DomainEvent
{
    public Guid ProductId { get; init; }
    public int NewStock { get; init; }
}
```

```csharp
// ✅ PurchaseManagement BC がイベントを受信
// src/PurchaseManagement/Application/EventHandlers/ProductStockChangedEventHandler.cs
namespace PurchaseManagement.Application.EventHandlers;

using ProductCatalog.Shared.Domain.Products.Events;  // ← イベントの参照はOK

public class ProductStockChangedEventHandler : INotificationHandler<ProductStockChangedEvent>
{
    public async Task Handle(ProductStockChangedEvent @event)
    {
        // PurchaseManagement BC内の読み取りモデルを更新
        await _readModelRepository.UpdateProductStock(@event.ProductId, @event.NewStock);
    }
}
```

**メリット:**
- ✅ BC間の疎結合
- ✅ イベント駆動アーキテクチャ
- ✅ 各BCのドメインモデルは独立

---

### ✅ 正しい方法2：API経由（同期通信）

```csharp
// ✅ PurchaseManagement BC が ProductCatalog BC の API を呼び出す
// src/PurchaseManagement/Infrastructure/ProductCatalog/ProductCatalogClient.cs
namespace PurchaseManagement.Infrastructure.ProductCatalog;

public class ProductCatalogClient : IProductCatalogClient  // ← インターフェース
{
    private readonly HttpClient _httpClient;

    public async Task<ProductStockInfo> GetProductStock(Guid productId)
    {
        // ProductCatalog BC の REST API を呼び出す
        var response = await _httpClient.GetAsync($"/api/products/{productId}/stock");
        return await response.Content.ReadFromJsonAsync<ProductStockInfo>();
    }
}

// DTO（BC間の契約）
public record ProductStockInfo
{
    public Guid ProductId { get; init; }
    public int Stock { get; init; }
}
```

```csharp
// ✅ PurchaseManagement BC のハンドラー
public class SubmitPurchaseRequestHandler
{
    private readonly IProductCatalogClient _productCatalogClient;  // ← インターフェース経由

    public async Task<Result> Handle(SubmitPurchaseRequestCommand command)
    {
        // API経由で在庫情報を取得（ドメインモデルは参照しない）
        var stockInfo = await _productCatalogClient.GetProductStock(command.ProductId);

        if (stockInfo.Stock < command.Quantity)
            return Result.Failure("在庫不足");
    }
}
```

**メリット:**
- ✅ BC間の疎結合
- ✅ 明確な API 契約（DTO）
- ✅ 各BCのドメインモデルは独立

---

## 📊 BC間通信の比較

| 方法 | ドメインモデルの独立性 | 適用場面 | 一貫性 |
|------|----------------------|---------|--------|
| **直接参照（❌）** | ❌ 失われる | 使用禁止 | 強い一貫性 |
| **統合イベント（✅）** | ✅ 保たれる | 非同期で問題ない場合 | 結果整合性 |
| **API呼び出し（✅）** | ✅ 保たれる | 同期的な確認が必要な場合 | 強い一貫性 |

---

## 🎯 設計ガイドライン

### ガイドライン1: 「BC固有のドメインモデルは BC内の Shared/Domain に配置」

```
PurchaseRequest → src/PurchaseManagement/Shared/Domain/PurchaseRequests/
Product         → src/ProductCatalog/Shared/Domain/Products/
```

**理由:**
- BC固有のドメイン知識を明確に分離
- BC境界の独立性を保つ

---

### ガイドライン2: 「BC間でドメインモデルを直接参照しない」

```
❌ NG: using ProductCatalog.Shared.Domain.Products;
✅ OK: using ProductCatalog.Contracts.DTOs;  // DTO経由
✅ OK: using ProductCatalog.Events;          // イベント経由
```

**理由:**
- BC間の疎結合を保つ
- ドメインモデルの独立性を保つ

---

### ガイドライン3: 「BC横断の共通ドメイン知識のみ src/Shared/Domain に配置」

```
✅ src/Shared/Domain/Identity/ApplicationUser.cs      ← 全BC共通
✅ src/Shared/Domain/Outbox/OutboxMessage.cs          ← 全BC共通
❌ src/Shared/Domain/PurchaseRequests/...             ← BC固有（誤配置）
```

**理由:**
- 全BCで共有すべきものだけを `src/Shared/Domain/` に配置
- BC固有のドメイン知識は各BCの `Shared/Domain/` に配置

---

## 🔍 実際の配置確認

### 現在の正しい配置

```
src/
├── Shared/Domain/
│   ├── Identity/ApplicationUser.cs       ← ✅ 全BC共通（認証）
│   ├── Outbox/OutboxMessage.cs           ← ✅ 全BC共通（イベント配信）
│   └── AuditLogs/AuditLog.cs             ← ✅ 全BC共通（監査）
│
├── PurchaseManagement/Shared/Domain/
│   └── PurchaseRequests/
│       ├── PurchaseRequest.cs            ← ✅ PurchaseManagement BC固有
│       ├── ApprovalStep.cs               ← ✅ PurchaseManagement BC固有
│       └── Boundaries/                   ← ✅ PurchaseManagement BC固有
│
└── ProductCatalog/Shared/Domain/
    └── Products/
        └── Product.cs                    ← ✅ ProductCatalog BC固有
```

**すべて正しく配置されています** ✅

---

## 🚫 アンチパターン

### アンチパターン1: BC固有のドメインモデルを src/Shared/Domain に配置

```
❌ 間違った配置

src/Shared/Domain/
├── PurchaseRequests/
│   └── PurchaseRequest.cs  ← ❌ BC固有なのにグローバルShared
└── Products/
    └── Product.cs          ← ❌ BC固有なのにグローバルShared
```

**問題点:**
- PurchaseRequest は PurchaseManagement BC 固有
- Product は ProductCatalog BC 固有
- グローバル Shared に配置すると BC境界が曖昧になる

---

### アンチパターン2: BC間でドメインモデルを直接参照

```csharp
// ❌ 間違った参照
// src/PurchaseManagement/Features/.../Handler.cs
using ProductCatalog.Shared.Domain.Products;  // ← ❌ BC境界を越える

var product = await _productRepository.GetByIdAsync(productId);  // ← ❌
```

**問題点:**
- BC境界の独立性が失われる
- ProductCatalog BC の変更が PurchaseManagement BC に影響

---

## 🎓 まとめ

### BC固有のドメインモデルの配置

| BC | ドメインモデル配置場所 | スコープ |
|----|-----------------------|---------|
| **PurchaseManagement** | `src/PurchaseManagement/Shared/Domain/` | PurchaseManagement BC内のみ |
| **ProductCatalog** | `src/ProductCatalog/Shared/Domain/` | ProductCatalog BC内のみ |

### BC横断の共通ドメイン知識の配置

| ドメイン知識 | 配置場所 | スコープ |
|-------------|---------|---------|
| **ApplicationUser** | `src/Shared/Domain/Identity/` | 全BC共通 |
| **OutboxMessage** | `src/Shared/Domain/Outbox/` | 全BC共通 |
| **AuditLog** | `src/Shared/Domain/AuditLogs/` | 全BC共通 |

### BC間通信

| 方法 | 推奨度 | 理由 |
|------|-------|------|
| **ドメインモデル直接参照** | ❌ 禁止 | BC境界の独立性が失われる |
| **統合イベント** | ✅ 推奨 | 非同期通信、疎結合 |
| **API呼び出し** | ✅ 推奨 | 同期通信、DTO経由 |

---

## 📚 関連ドキュメント

- **BC-VSA-Slice関係:** `docs/architecture/VSA-BC-SLICE-BOUNDARY-RELATIONSHIP.md`
- **Shared vs Kernel:** `docs/architecture/SHARED-VS-KERNEL-DISTINCTION.md`
- **VSAとドメインの独立性:** `docs/architecture/VSA-DOMAIN-INDEPENDENCE.md`
- **BC整合性ガイドライン:** `docs/architecture/BC-CONSISTENCY-GUIDELINES.md`
