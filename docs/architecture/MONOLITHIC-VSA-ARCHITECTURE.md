# モノリシックVSAアーキテクチャ設計書

## 概要

このプロジェクトは **モノリシックVSA (Vertical Slice Architecture)** を採用しています。
**YAGNI (You Aren't Gonna Need It)** 原則に基づき、シンプルな単一プロジェクトから始め、必要に応じて複雑化する方針です。

---

## 設計哲学

### YAGNI原則の適用

- **現状**: 単一のモノリシックアプリケーション（Applicationプロジェクト）
- **将来**: 規模が巨大化した場合のみマイクロサービスへ分割
- **理由**: 過度な抽象化・複雑化を避け、実際の必要性に応じて進化させる

### VSAのメリット

- **変更の局所化**: 機能追加時の影響範囲最小化
- **垂直スライス**: 機能ごとにUI + Application + Infrastructureを含む
- **独立性**: 機能単位で理解・テスト・デプロイが可能

---

## プロジェクト構造

```
src/
├── Application/              # 単一Blazor Server プロジェクト
│   ├── Application.csproj    # すべての機能を含む
│   ├── Program.cs            # エントリーポイント
│   │
│   ├── Core/                 # アプリケーション基盤
│   │   ├── Commands/
│   │   │   └── CommandPipeline.cs
│   │   ├── Queries/
│   │   │   └── QueryPipeline.cs
│   │   └── Behaviors/
│   │       └── GenericTransactionBehavior.cs
│   │
│   ├── Features/             # 19機能の垂直スライス
│   │   ├── CreateProduct/
│   │   │   ├── CreateProductCommand.cs
│   │   │   ├── CreateProductCommandHandler.cs
│   │   │   ├── CreateProductCommandValidator.cs
│   │   │   └── UI/
│   │   │       └── CreateProductForm.razor
│   │   │
│   │   ├── GetProducts/
│   │   │   ├── GetProductsQuery.cs
│   │   │   ├── GetProductsQueryHandler.cs
│   │   │   └── UI/
│   │   │       ├── ProductList.razor
│   │   │       ├── ProductCard.razor
│   │   │       └── Api/
│   │   │           └── ProductsController.cs
│   │   │
│   │   └── ... (全19機能)
│   │
│   ├── Infrastructure/       # インフラ層
│   │   ├── Models/           # アプリケーション状態
│   │   ├── Services/         # LocalStorageなど
│   │   ├── Stores/           # Fluxパターン状態管理
│   │   └── PurchaseManagement/
│   │       └── Services/
│   │
│   ├── Shared/               # BC別の共有コード
│   │   ├── ProductCatalog/
│   │   │   ├── Application/
│   │   │   │   ├── DTOs/
│   │   │   │   └── Interfaces/
│   │   │   ├── Infrastructure/
│   │   │   │   └── Persistence/
│   │   │   │       ├── ProductCatalogDbContext.cs
│   │   │   │       ├── Configurations/
│   │   │   │       └── Repositories/
│   │   │   └── UI/
│   │   │       ├── Stores/
│   │   │       └── Actions/
│   │   │
│   │   └── PurchaseManagement/
│   │       ├── Application/
│   │       ├── Domain/
│   │       └── Infrastructure/
│   │
│   ├── Components/           # Blazor共通コンポーネント
│   │   ├── Layout/
│   │   ├── ErrorBoundary/
│   │   └── Routes.razor
│   │
│   ├── Hubs/                 # SignalR Hubs
│   ├── Jobs/                 # Hangfire バックグラウンドジョブ
│   ├── Middleware/           # ASP.NET Core ミドルウェア
│   ├── Services/             # アプリケーションサービス
│   └── wwwroot/              # 静的ファイル
│
├── Domain/                   # ドメインプロジェクト（分離）
│   ├── ProductCatalog/
│   │   ├── Domain.ProductCatalog.csproj
│   │   ├── Products/
│   │   │   ├── Product.cs
│   │   │   ├── IProductRepository.cs
│   │   │   └── ProductSpecifications.cs
│   │   ├── Services/
│   │   └── Events/
│   │
│   └── PurchaseManagement/
│       ├── Domain.PurchaseManagement.csproj
│       ├── PurchaseRequests/
│       ├── Services/
│       └── Events/
│
└── Shared/                   # 共通ライブラリプロジェクト
    ├── Kernel/               # ドメイン共通基盤
    │   ├── Shared.Kernel.csproj
    │   ├── AggregateRoot.cs
    │   ├── Entity.cs
    │   ├── ValueObject.cs
    │   └── Money.cs
    │
    ├── Domain/               # ドメイン共通
    │   ├── Shared.Domain.csproj
    │   ├── Interfaces/
    │   └── Events/
    │
    ├── Application/          # アプリケーション共通
    │   ├── Shared.Application.csproj
    │   ├── Interfaces/
    │   └── Behaviors/
    │
    ├── Infrastructure/       # インフラ共通
    │   ├── Shared.Infrastructure.csproj
    │   ├── Authentication/
    │   ├── Behaviors/
    │   ├── Caching/
    │   └── Services/
    │
    ├── Infrastructure.Platform/  # プラットフォーム共通
    │   ├── Shared.Infrastructure.Platform.csproj
    │   ├── Outbox/
    │   ├── Persistence/
    │   ├── Repositories/
    │   └── Stores/
    │
    └── Abstractions/         # 抽象化共通
        ├── Shared.Abstractions.csproj
        └── Interfaces/
```

---

## 層の責務

### Application/ (単一プロジェクト)

**役割**: すべての機能を含む単一のBlazor Serverプロジェクト

**含まれるもの**:
- UI層 (Blazorコンポーネント、Razorページ)
- Application層 (Command/QueryHandler)
- Infrastructure層 (DbContext、Repository実装)
- 機能スライス (Features/)

**依存関係**:
- Domain/ プロジェクト (ProductCatalog, PurchaseManagement)
- Shared/ プロジェクト (Kernel, Domain, Application, Infrastructure, Platform, Abstractions)

**プロジェクト設定**:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>Application</RootNamespace>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  </PropertyGroup>
</Project>
```

---

### Domain/ (分離プロジェクト)

**役割**: ドメインモデルの定義（技術要素ゼロ）

**含まれるもの**:
- エンティティ (Aggregate Root)
- 値オブジェクト (Value Object)
- ドメインサービス
- ドメインイベント
- リポジトリインターフェース

**依存関係**:
- Shared.Kernel のみ（他の技術要素に依存しない）

**BC (Bounded Context) 分離**:
- `ProductCatalog/`: 商品カタログドメイン
- `PurchaseManagement/`: 購買管理ドメイン

---

### Shared/ (共通ライブラリ)

**役割**: プロジェクト横断の再利用可能なコード

**6つのプロジェクト**:
1. **Kernel**: ドメイン共通基盤 (AggregateRoot, Entity, ValueObject)
2. **Domain**: ドメイン共通 (インターフェース、イベント)
3. **Application**: アプリケーション共通 (インターフェース、Behaviors)
4. **Infrastructure**: インフラ共通 (認証、キャッシング、サービス)
5. **Infrastructure.Platform**: プラットフォーム共通 (Outbox、永続化)
6. **Abstractions**: 抽象化共通 (インターフェース)

---

## 機能スライス (Vertical Slice)

### スライスの構成

各機能は `Application/Features/{FeatureName}/` に配置され、以下を含みます:

```
Features/CreateProduct/
├── CreateProductCommand.cs          # Command DTO
├── CreateProductCommandHandler.cs   # ビジネスロジック
├── CreateProductCommandValidator.cs # FluentValidation
└── UI/
    └── CreateProductForm.razor      # Blazorコンポーネント
```

### スライスの独立性

- ✅ **独立してテスト可能**: 機能単位でユニット/統合テストを記述
- ✅ **独立して理解可能**: 1つのフォルダで機能全体を把握
- ✅ **独立してデプロイ可能**: 将来的にマイクロサービス化する際の単位

---

## データフロー

### Command フロー (書き込み)

```
1. UI (Blazor Component)
   ↓
2. MediatR (IRequest<Result<T>>)
   ↓
3. CommandHandler (Features/)
   ↓
4. Domain (Aggregate)
   ↓
5. Repository (Infrastructure)
   ↓
6. DbContext (Infrastructure)
```

### Query フロー (読み込み)

```
1. UI (Blazor Component)
   ↓
2. MediatR (IRequest<T>)
   ↓
3. QueryHandler (Features/)
   ↓
4. Dapper / EF Core (直接クエリ)
   ↓
5. DTO返却
```

---

## 横断的関心事

### MediatR Pipeline Behaviors

Applicationプロジェクト内で以下のBehaviorを実装:

1. **LoggingBehavior**: すべてのリクエスト/レスポンスをログ記録
2. **ValidationBehavior**: FluentValidationによる検証
3. **TransactionBehavior**: DbContextのトランザクション管理
4. **AuditLogBehavior**: 監査ログの記録
5. **IdempotencyBehavior**: 冪等性保証
6. **MetricsBehavior**: Prometheusメトリクス収集

### 状態管理 (Flux パターン)

**2層モデル**:
1. **システムレベル**: `Infrastructure/Stores/` (Layout, Notification, Preferences)
2. **ドメインレベル**: `Shared/{BC}/UI/Stores/` (ProductsStore, ProductDetailStore)

詳細: [STATE-MANAGEMENT-LAYERS.md](STATE-MANAGEMENT-LAYERS.md)

---

## 設計原則

### 1. YAGNI (You Aren't Gonna Need It)

- ✅ **現時点で必要なもの**: 単一Applicationプロジェクト
- ❌ **将来必要かもしれないもの**: マイクロサービス分割、CQRS完全分離

**移行パス**:
- **小規模 → 中規模**: 単一プロジェクトのまま（現状）
- **中規模 → 大規模**: BC単位でプロジェクト分離
- **大規模 → 超大規模**: マイクロサービス化

### 2. Vertical Slice Architecture

- ✅ **機能単位の垂直スライス**: Features/{FeatureName}/
- ✅ **変更の局所化**: 1つのフォルダで完結
- ✅ **チーム独立性**: BC単位で開発可能

### 3. Domain-Driven Design

- ✅ **Bounded Context分離**: ProductCatalog / PurchaseManagement
- ✅ **ドメインモデルの純粋性**: Domain/は技術要素ゼロ
- ✅ **ユビキタス言語**: ドメイン用語をコードに反映

---

## 将来の拡張パス

### ケース1: 中規模化（100機能程度）

**対応**: Applicationプロジェクトを BC 単位で分割

```
Before:
src/Application/ (すべて)

After:
src/
├── Application.ProductCatalog/
├── Application.PurchaseManagement/
└── Application.Host/
```

**工数**: 1週間程度

---

### ケース2: 大規模化（マイクロサービス化）

**対応**: BC 単位でサービス分離

```
Before:
src/Application/ (すべて)

After:
services/
├── ProductCatalog.Service/
│   ├── API/
│   ├── Application/
│   ├── Domain/
│   └── Infrastructure/
│
└── PurchaseManagement.Service/
    ├── API/
    ├── Application/
    ├── Domain/
    └── Infrastructure/
```

**工数**: 4週間程度

---

## まとめ

### 採用アーキテクチャ

**モノリシックVSA (Vertical Slice Architecture)**

### メリット

1. ✅ **シンプル**: 単一プロジェクトで管理が容易
2. ✅ **高速開発**: プロジェクト間の依存関係が少ない
3. ✅ **柔軟性**: 将来の拡張パスが明確
4. ✅ **保守性**: 機能単位で理解・修正が容易

### 設計方針

- **YAGNI**: 必要になってから複雑化する
- **VSA**: 機能単位の垂直スライス
- **DDD**: ドメインモデルの純粋性維持

---

**作成日**: 2025-11-16
**バージョン**: 1.0.0
**対象プロジェクト**: VSASample
