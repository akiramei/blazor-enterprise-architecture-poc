# VSA移行計画

## 現状分析

### 現在の構造（Clean Architecture / Layered Architecture）

```
src/
├── ProductCatalog.Application/     # レイヤープロジェクト
│   └── Features/Products/
│       ├── CreateProduct/
│       ├── UpdateProduct/
│       ├── DeleteProduct/
│       ├── GetProducts/
│       ├── GetProductById/
│       ├── SearchProducts/
│       ├── BulkDeleteProducts/
│       ├── BulkUpdateProductPrices/
│       ├── ExportProductsToCsv/
│       └── ImportProductsFromCsv/
│
├── ProductCatalog.Domain/          # レイヤープロジェクト
│   ├── Common/                     # 共通基盤
│   ├── Products/                   # Products機能のDomain
│   ├── AuditLogs/                  # 共通機能
│   ├── Identity/                   # 共通機能
│   └── Outbox/                     # 共通機能
│
├── ProductCatalog.Infrastructure/  # レイヤープロジェクト
│   ├── Authentication/             # 共通機能
│   ├── Behaviors/                  # 共通機能
│   ├── Idempotency/                # 共通機能
│   ├── Metrics/                    # 共通機能
│   ├── Outbox/                     # 共通機能
│   ├── Persistence/
│   │   ├── AppDbContext.cs         # 共通
│   │   ├── Configurations/
│   │   │   ├── ProductConfiguration.cs      # Products機能
│   │   │   ├── AuditLogConfiguration.cs     # 共通
│   │   │   ├── OutboxMessageConfiguration.cs # 共通
│   │   │   └── RefreshTokenConfiguration.cs  # 共通
│   │   └── Repositories/
│   │       ├── EfProductRepository.cs         # Products機能
│   │       ├── DapperProductReadRepository.cs # Products機能
│   │       └── AuditLogRepository.cs          # 共通
│   └── Migrations/                 # 共通
│
└── ProductCatalog.Web/             # レイヤープロジェクト
    ├── Features/Api/
    │   └── V1/
    │       ├── Auth/               # Auth機能のAPI
    │       └── Products/           # Products機能のAPI
    └── Features/Products/
        ├── Pages/
        ├── Components/
        ├── Actions/
        └── Store/
```

**問題点:**
- ❌ レイヤーが最上位（Clean Architecture）
- ❌ 機能追加時に複数のレイヤープロジェクトを変更
- ❌ プロジェクト名が `ProductCatalog.{Layer}`

---

## 目標構造（VSA）

### BC (Bounded Context) ベースの構造

```
src/
├── ProductCatalog/                         # BC: 商品カタログコンテキスト
│   └── Features/
│       ├── CreateProduct/                 # スライス1
│       │   ├── Application/
│       │   │   ├── CreateProductCommand.cs
│       │   │   ├── CreateProductHandler.cs
│       │   │   └── CreateProductValidator.cs
│       │   ├── Domain/
│       │   │   └── Product.cs (*)
│       │   ├── Infrastructure/
│       │   │   ├── EfProductRepository.cs (*)
│       │   │   └── ProductConfiguration.cs (*)
│       │   └── UI/
│       │       ├── CreateProductPage.razor
│       │       └── CreateProductRequest.cs (API DTO)
│       │
│       ├── UpdateProduct/                 # スライス2
│       │   ├── Application/
│       │   ├── Domain/ (Product.csへの参照)
│       │   ├── Infrastructure/ (共有Repository参照)
│       │   └── UI/
│       │
│       ├── DeleteProduct/
│       ├── GetProducts/
│       ├── GetProductById/
│       ├── SearchProducts/
│       ├── BulkDeleteProducts/
│       ├── BulkUpdateProductPrices/
│       ├── ExportProductsToCsv/
│       └── ImportProductsFromCsv/
│
└── Shared/                                # 共通基盤
    ├── Kernel/
    │   ├── AggregateRoot.cs
    │   ├── Entity.cs
    │   ├── ValueObject.cs
    │   ├── DomainEvent.cs
    │   └── Result.cs
    ├── Application/
    │   ├── ICommand.cs
    │   ├── IQuery.cs
    │   └── Behaviors/                     # Pipeline Behaviors
    │       ├── LoggingBehavior.cs
    │       ├── ValidationBehavior.cs
    │       ├── AuthorizationBehavior.cs
    │       ├── IdempotencyBehavior.cs
    │       ├── CachingBehavior.cs
    │       ├── AuditLogBehavior.cs
    │       ├── MetricsBehavior.cs
    │       └── TransactionBehavior.cs
    ├── Infrastructure/
    │   ├── Authentication/                # JWT
    │   ├── Idempotency/
    │   ├── Metrics/
    │   ├── Outbox/
    │   ├── Persistence/
    │   │   ├── BaseDbContext.cs
    │   │   └── Configurations/
    │   │       ├── AuditLogConfiguration.cs
    │   │       ├── OutboxMessageConfiguration.cs
    │   │       └── RefreshTokenConfiguration.cs
    │   └── Migrations/                    # EF Migrations
    └── Domain/
        ├── AuditLogs/
        ├── Identity/
        └── Outbox/
```

**(*) Domain/Infrastructure の共有:**
- Product.cs は複数の機能で共有される集約ルート
- EfProductRepository.cs も複数の機能で共有
- これらは `src/ProductCatalog/Shared/` 配下に配置する選択肢もある

---

## 移行課題

### 課題1: Domain集約の共有

**問題:**
- `Product.cs` は11個すべての機能で使用される
- 各機能フォルダにコピーするのは非現実的

**解決策（3つのオプション）:**

#### オプションA: Shared/Domain に配置

```
src/
├── ProductCatalog/
│   ├── Shared/
│   │   ├── Domain/
│   │   │   └── Products/
│   │   │       ├── Product.cs
│   │   │       ├── ProductId.cs
│   │   │       ├── Money.cs
│   │   │       ├── ProductImage.cs
│   │   │       ├── ProductStatus.cs
│   │   │       ├── IProductRepository.cs
│   │   │       └── Events/
│   │   └── Infrastructure/
│   │       └── Persistence/
│   │           ├── EfProductRepository.cs
│   │           ├── DapperProductReadRepository.cs
│   │           └── ProductConfiguration.cs
│   └── Features/
│       ├── CreateProduct/
│       │   ├── Application/
│       │   └── UI/
│       └── ...
```

**メリット:**
- Product集約を1箇所で管理
- 機能はApplicationとUIのみ実装

**デメリット:**
- 完全なVSAではない（機能内で完結しない）

---

#### オプションB: 最初の機能に配置、他は参照

```
src/
└── ProductCatalog/
    └── Features/
        ├── CreateProduct/
        │   ├── Domain/
        │   │   └── Products/
        │   │       └── Product.cs     # オリジナル
        │   └── Infrastructure/
        │       └── EfProductRepository.cs
        │
        ├── UpdateProduct/
        │   ├── Application/
        │   └── UI/
        │   # Domain/ は CreateProduct/Domain を参照
        │
        └── DeleteProduct/
            # 同上
```

**メリット:**
- 機能フォルダ内に集約が存在

**デメリット:**
- CreateProductが特別な位置づけになる
- プロジェクト参照が複雑

---

#### オプションC: 集約ごとにBCを分ける

```
src/
├── Products/                          # BC: Products集約
│   ├── Domain/
│   │   └── Product.cs
│   └── Infrastructure/
│       └── EfProductRepository.cs
│
└── ProductCatalog/                    # BC: 商品カタログ機能群
    └── Features/
        ├── CreateProduct/
        │   ├── Application/
        │   └── UI/
        │   # Domain は Products/ を参照
        └── ...
```

**メリット:**
- DDDのBounded Contextに忠実
- 集約単位で独立

**デメリット:**
- 複数のBCが登場し、構造が複雑

---

### 課題2: DbContext と Migrations

**問題:**
- 現在は1つの`AppDbContext`ですべてのテーブルを管理
- EF Migrationsも全体で1つ

**解決策:**

#### 維持する方針（推奨）

```
src/
└── Shared/
    └── Infrastructure/
        ├── Persistence/
        │   ├── AppDbContext.cs         # 全テーブルを管理
        │   └── Configurations/
        │       ├── ProductConfiguration.cs
        │       ├── AuditLogConfiguration.cs
        │       └── ...
        └── Migrations/                  # 全体のマイグレーション
```

**理由:**
- DbContextを分割するとトランザクション管理が複雑
- Migrationsの履歴が分散すると管理困難
- VSAは「機能の独立性」であり、DBスキーマの分離は必須ではない

---

### 課題3: Web層のAPI

**問題:**
- `Features/Api/V1/Products/ProductsController.cs` の配置

**解決策:**

各機能のUIフォルダに配置:

```
src/ProductCatalog/Features/CreateProduct/
└── UI/
    ├── CreateProductPage.razor    # Blazor Page
    ├── CreateProductController.cs # REST API
    └── Dtos/
        └── CreateProductRequest.cs
```

**理由:**
- BlazorとREST APIは同じ「UI層」
- 機能ごとに独立したエンドポイント

---

## 移行方針（推奨）

### 採用する構造

**オプションA（Shared/Domain）を採用:**

```
src/
├── ProductCatalog/
│   ├── Shared/
│   │   ├── Domain/
│   │   │   └── Products/            # Product集約（全機能で共有）
│   │   └── Infrastructure/
│   │       └── Persistence/         # Repository, Configuration（全機能で共有）
│   └── Features/
│       ├── CreateProduct/
│       │   ├── Application/         # CreateProductCommand, Handler, Validator
│       │   └── UI/                  # Blazor Page, API Controller, DTOs
│       ├── UpdateProduct/
│       └── ...
│
└── Shared/                          # プロジェクト横断の共通基盤
    ├── Kernel/
    ├── Application/
    ├── Infrastructure/
    └── Domain/
```

**理由:**
1. Product集約の重複を避ける
2. Repositoryの重複を避ける
3. 実用的で管理しやすい
4. VSAの「機能単位の独立性」の本質は損なわない

---

## 移行手順

### Phase 1: Shared プロジェクト作成

1. `src/Shared/` フォルダ作成
2. 共通基盤を移動:
   - `Shared/Kernel/` - AggregateRoot, Entity, ValueObject等
   - `Shared/Application/` - ICommand, IQuery, Behaviors
   - `Shared/Infrastructure/` - Authentication, Metrics, Idempotency, Outbox, Migrations
   - `Shared/Domain/` - AuditLogs, Identity, Outbox

### Phase 2: ProductCatalog BC フォルダ作成

1. `src/ProductCatalog/` フォルダ作成
2. `src/ProductCatalog/Shared/` フォルダ作成
3. Product集約を移動:
   - `ProductCatalog/Shared/Domain/Products/` - Product.cs等
   - `ProductCatalog/Shared/Infrastructure/Persistence/` - Repository, Configuration

### Phase 3: Features フォルダ作成

1. `src/ProductCatalog/Features/` フォルダ作成
2. 各機能を移動（11個）:
   - `Application/` - Command, Handler, Validator
   - `UI/` - Blazor Page, API Controller, DTOs

### Phase 4: 旧プロジェクト削除

1. `ProductCatalog.Application/` 削除
2. `ProductCatalog.Domain/` 削除
3. `ProductCatalog.Infrastructure/` 削除
4. `ProductCatalog.Web/` 削除

### Phase 5: プロジェクトファイル作成

各機能にプロジェクトファイルを作成:
- `CreateProduct.Application.csproj`
- `CreateProduct.UI.csproj`
- etc...

### Phase 6: ソリューションファイル更新

`ProductCatalog.sln` を新しいプロジェクト構成に更新

### Phase 7: 検証

```bash
./scripts/validate-vsa-structure.ps1
```

---

## リスクと対策

### リスク1: ビルドエラー

**対策:** Phase 5でプロジェクト参照を慎重に設定

### リスク2: 既存テストの破損

**対策:** テストプロジェクトも同時に移行

### リスク3: Migrations の破損

**対策:** Migrations は Shared/Infrastructure/ にそのまま移動、DbContext参照を更新

---

## 作業時間見積もり

- Phase 1: 2時間
- Phase 2: 1時間
- Phase 3: 4時間（11機能）
- Phase 4: 30分
- Phase 5: 3時間
- Phase 6: 1時間
- Phase 7: 30分

**合計: 約12時間**

---

## 次のステップ

1. ユーザーに移行方針を確認
2. 承認後、Phase 1から順次実施
3. 各Phaseごとにコミット
4. 最後に検証スクリプトで確認

---

**作成日**: 2025-11-03
**ステータス**: 承認待ち
