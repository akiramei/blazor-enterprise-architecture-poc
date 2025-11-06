# Vertical Slice Architecture: 厳格なルール

## ⚠️ 重要な前提

**このプロジェクトは Vertical Slice Architecture (VSA) です**

- ❌ Clean Architecture ではありません
- ❌ Layered Architecture ではありません
- ❌ Onion Architecture ではありません

**VSA と Clean Architecture は根本的に異なるアーキテクチャです**

---

## 🏗️ 正しい構造

### VSA の原則

**機能が最上位、層はその中に配置**

VSAには2つの実装パターンがあります：

#### パターン1: 完全な垂直スライス（理想形）
各機能が独立したDomain/Infrastructureを持つ

```
src/
└── {BoundedContext}/          # ステップ1: コンテキスト境界
    └── Features/              # ステップ2: 機能群
        └── {FeatureName}/     # ステップ3: 個別機能（スライス）
            ├── Application/   # ステップ4: 層（機能内）
            ├── Domain/
            ├── Infrastructure/
            └── UI/
```

#### パターン2: 共通ドメインパターン（実用的）
複数機能が同じドメインモデルを共有する場合

```
src/
└── {BoundedContext}/              # ステップ1: コンテキスト境界
    ├── Features/                  # ステップ2: 機能群
    │   └── {FeatureName}/         # ステップ3: 個別機能（スライス）
    │       ├── Application/       # Command/Handler/Validator
    │       └── UI/                # API/Components
    │
    └── Shared/                    # 共通コード（BC内共通）
        ├── Application/           # 共通DTO等
        ├── Domain/                # 共通ドメインモデル（集約）
        ├── Infrastructure/        # 共通Repository実装
        └── UI/                    # 共通UIコンポーネント
```

**このプロジェクトは「パターン2: 共通ドメインパターン」を採用しています**

### 具体例（このプロジェクトの実装）

```
src/
└── ProductCatalog/                    # BC: 商品カタログコンテキスト
    ├── Features/
    │   ├── CreateProduct/             # スライス1
    │   │   ├── Application/
    │   │   │   ├── CreateProductCommand.cs
    │   │   │   ├── CreateProductHandler.cs
    │   │   │   └── CreateProductValidator.cs
    │   │   └── UI/
    │   │       └── Api/
    │   │           ├── CreateProductEndpoint.cs
    │   │           └── Dtos/
    │   │
    │   ├── GetProducts/               # スライス2
    │   │   ├── Application/
    │   │   │   ├── GetProductsQuery.cs
    │   │   │   └── GetProductsHandler.cs
    │   │   └── UI/
    │   │       ├── Api/
    │   │       └── Components/
    │   │           └── ProductList.razor
    │   │
    │   ├── UpdateProduct/             # スライス3
    │   ├── DeleteProduct/             # スライス4
    │   └── ... (他の機能)
    │
    └── Shared/                        # ProductCatalog共通（全機能で共有）
        ├── Application/
        │   └── DTOs/
        │       └── ProductDto.cs
        ├── Domain/
        │   └── Products/              # Product集約
        │       ├── Product.cs
        │       ├── Money.cs
        │       └── Events/
        ├── Infrastructure/
        │   └── Persistence/
        │       ├── Configurations/
        │       │   └── ProductConfiguration.cs
        │       └── Repositories/
        │           └── ProductRepository.cs
        └── UI/
            ├── Actions/
            │   └── ProductsActions.cs
            └── Store/
                └── ProductsStore.cs
```

**重要な違い:**
- **各機能（CreateProduct, GetProducts等）はApplication + UIのみ**
- **Domain/Infrastructureは`ProductCatalog/Shared`に集約**（全機能が同じProduct集約を使うため）

---

## ❌ 絶対禁止事項

### 禁止1: src/直下にレイヤー名プロジェクトを作成

**❌ これは Clean Architecture / Layered Architecture です:**

```
src/
├── ProductCatalog.Application/    # ❌ レイヤーが最上位
├── ProductCatalog.Domain/
├── ProductCatalog.Infrastructure/
└── ProductCatalog.Web/
```

**検証方法:**
```bash
ls src/ | grep -E "\.(Application|Domain|Infrastructure|Web|UI)$"
```

**期待結果:** 何も出力されない（exit code 1）

**もし何か出力されたら:** ❌ Clean Architecture になっている

---

### 禁止2: Features フォルダがBCの外にある

**❌ 間違い:**
```
src/
└── Features/                  # ❌ BCがない
    └── CreateProduct/
```

**✅ 正しい:**
```
src/
└── ProductCatalog/            # ✅ BC
    └── Features/
        └── CreateProduct/
```

---

### 禁止3: 機能追加時に複数の機能フォルダを変更

**VSA原則:** 1つの機能追加 = 1つのスライスフォルダ内で完結

**検証方法（共通ドメインパターンの場合）:**
```bash
# 新機能追加後
git diff --name-only | grep "^src/ProductCatalog/Features" | cut -d/ -f1-4 | sort -u
```

**期待結果:** `src/ProductCatalog/Features/NewFeature/` のみ

**もし複数の機能フォルダが表示されたら:** ❌ VSA違反

**注意:**
- `src/ProductCatalog/Shared/`の変更は許容される（共通ドメインモデルの拡張）
- ただし、既存機能のフォルダに変更が入る場合は設計を見直すべき

---

## ✅ 必須事項

### 必須1: フォルダ階層

**パターン1（完全な垂直スライス）:**
```
Level 1: src/
Level 2: {BoundedContext}/           例: ProductCatalog/
Level 3: Features/
Level 4: {FeatureName}/              例: CreateProduct/
Level 5: {Layer}/                    例: Application/, Domain/, Infrastructure/, UI/
```

**パターン2（共通ドメインパターン - このプロジェクト）:**
```
Level 1: src/
Level 2: {BoundedContext}/           例: ProductCatalog/
Level 3a: Features/
Level 4:  {FeatureName}/             例: CreateProduct/
Level 5:  {Layer}/                   例: Application/, UI/

Level 3b: Shared/                    BC内共通コード
Level 4:  {Layer}/                   例: Domain/, Infrastructure/, UI/
```

### 必須2: プロジェクトファイル配置

**パターン1（完全な垂直スライス）:**
```
src/ProductCatalog/Features/CreateProduct/
├── Application/
│   └── CreateProduct.Application.csproj
├── Domain/
│   └── CreateProduct.Domain.csproj
├── Infrastructure/
│   └── CreateProduct.Infrastructure.csproj
└── UI/
    └── CreateProduct.UI.csproj
```

**パターン2（共通ドメインパターン - このプロジェクト）:**
```
src/ProductCatalog/Features/CreateProduct/
├── Application/
│   └── CreateProduct.Application.csproj
└── UI/
    └── CreateProduct.UI.csproj

src/ProductCatalog/Shared/
├── Application/
│   └── ProductCatalog.Shared.Application.csproj
├── Domain/
│   └── Products/
│       └── ProductCatalog.Shared.Domain.Products.csproj
├── Infrastructure/
│   └── Persistence/
│       └── ProductCatalog.Shared.Infrastructure.Persistence.csproj
└── UI/
    └── ProductCatalog.Shared.UI.csproj
```

### 必須3: 依存関係

**パターン1（完全な垂直スライス）:**
```
CreateProduct.UI
  → CreateProduct.Application
      → CreateProduct.Domain
          ← CreateProduct.Infrastructure
```

**パターン2（共通ドメインパターン - このプロジェクト）:**
```
CreateProduct.UI
  → CreateProduct.Application
      → ProductCatalog.Shared.Domain.Products
          ← ProductCatalog.Shared.Infrastructure.Persistence
```

**機能間の依存は禁止:**
```
CreateProduct → UpdateProduct  # ❌ 禁止
```

**共通機能は Shared プロジェクトで:**
```
src/
├── ProductCatalog/
│   ├── Features/
│   │   ├── CreateProduct/ → ProductCatalog.Shared (BC内共通)
│   │   └── UpdateProduct/ → ProductCatalog.Shared (BC内共通)
│   └── Shared/            # BC内共通（Domain/Infrastructure）
│       ├── Domain/
│       └── Infrastructure/
│
└── Shared/                # グローバル共通（全BC共有）
    ├── Kernel/            # Entity, AggregateRoot等
    ├── Application/       # ICommand, IQuery, Result
    └── Infrastructure/    # MediatR Behaviors等
```

---

## 🔍 実装前チェックリスト

新しい機能を実装する前に必ず確認：

**共通項目:**
- [ ] `src/` 直下に `*.Application`, `*.Domain`, `*.Infrastructure`, `*.Web` が**存在しない**
- [ ] BCフォルダ（例: ProductCatalog/）が存在する
- [ ] Features/ フォルダがBCの直下にある
- [ ] 新機能は `Features/{FeatureName}/` 配下に作成する

**パターン1（完全な垂直スライス）:**
- [ ] 新機能フォルダ内に Application/, Domain/, Infrastructure/, UI/ を作成する

**パターン2（共通ドメインパターン - このプロジェクト）:**
- [ ] 新機能フォルダ内に Application/, UI/ を作成する
- [ ] 既存の `ProductCatalog/Shared/Domain` を利用する
- [ ] 既存の `ProductCatalog/Shared/Infrastructure` を利用する

---

## 🚫 Clean Architecture との違い

| 観点 | Clean Architecture | VSA |
|------|-------------------|-----|
| **最上位** | レイヤー (Application, Domain...) | BC → Features → 機能名 |
| **プロジェクト名** | ProductCatalog.Application | CreateProduct.Application |
| **機能追加** | 複数の層プロジェクトを変更 | 1つのスライスフォルダのみ |
| **依存方向** | 層間の依存方向を厳密に管理 | 機能内で完結、機能間は疎結合 |
| **テストの粒度** | 層単位 | 機能単位 |
| **デプロイ** | 全層を一緒にデプロイ | 機能単位でデプロイ可能 |

---

## 📝 新機能追加の手順

### パターン2（共通ドメインパターン - このプロジェクト）

#### ステップ1: フォルダ作成

```bash
mkdir -p src/ProductCatalog/Features/NewFeature/{Application,UI}
```

#### ステップ2: プロジェクトファイル作成

```bash
cd src/ProductCatalog/Features/NewFeature/

# Application層
dotnet new classlib -n NewFeature.Application -o Application

# UI層
dotnet new razorclasslib -n NewFeature.UI -o UI
```

#### ステップ3: プロジェクト参照設定

```bash
# Application → 共通Domain
cd Application
dotnet add reference ../../Shared/Domain/Products/ProductCatalog.Shared.Domain.Products.csproj
dotnet add reference ../../Shared/Infrastructure/Persistence/ProductCatalog.Shared.Infrastructure.Persistence.csproj

# UI → Application
cd ../UI
dotnet add reference ../Application/NewFeature.Application.csproj
```

#### ステップ4: ソリューションに追加

```bash
cd ../../../../..  # ルートディレクトリへ
dotnet sln add src/ProductCatalog/Features/NewFeature/Application/NewFeature.Application.csproj
dotnet sln add src/ProductCatalog/Features/NewFeature/UI/NewFeature.UI.csproj
```

---

### パターン1（完全な垂直スライス）

#### ステップ1: フォルダ作成

```bash
mkdir -p src/ProductCatalog/Features/NewFeature/{Application,Domain,Infrastructure,UI}
```

#### ステップ2: プロジェクトファイル作成

```bash
cd src/ProductCatalog/Features/NewFeature/

# Application層
dotnet new classlib -n NewFeature.Application -o Application

# Domain層
dotnet new classlib -n NewFeature.Domain -o Domain

# Infrastructure層
dotnet new classlib -n NewFeature.Infrastructure -o Infrastructure

# UI層
dotnet new razorclasslib -n NewFeature.UI -o UI
```

#### ステップ3: プロジェクト参照設定

```bash
cd Application
dotnet add reference ../Domain/NewFeature.Domain.csproj

cd ../Infrastructure
dotnet add reference ../Domain/NewFeature.Domain.csproj

cd ../UI
dotnet add reference ../Application/NewFeature.Application.csproj
```

#### ステップ4: ソリューションに追加

```bash
cd ../../../../..  # ルートディレクトリへ
dotnet sln add src/ProductCatalog/Features/NewFeature/Application/NewFeature.Application.csproj
dotnet sln add src/ProductCatalog/Features/NewFeature/Domain/NewFeature.Domain.csproj
dotnet sln add src/ProductCatalog/Features/NewFeature/Infrastructure/NewFeature.Infrastructure.csproj
dotnet sln add src/ProductCatalog/Features/NewFeature/UI/NewFeature.UI.csproj
```

---

## 🔧 既存プロジェクトの修正

現在のプロジェクトがClean Architecture（レイヤードアーキテクチャ）になっている場合：

### 修正方針

1. 既存の `src/ProductCatalog.Application/Features/Products/` 配下の各機能を
2. `src/ProductCatalog/Features/{FeatureName}/` に移動
3. レイヤープロジェクト（*.Application, *.Domain等）を削除

### 移行スクリプト

```bash
# TODO: 移行スクリプトを作成
./scripts/migrate-to-vsa.sh
```

---

## 📚 参考資料

- **Jimmy Bogard - Vertical Slice Architecture**
  - https://www.jimmybogard.com/vertical-slice-architecture/
- **ContosoUniversity - VSA実装例**
  - https://github.com/jbogard/ContosoUniversityDotNetCore-Pages

---

**作成日**: 2025-11-03
**最終更新**: 2025-11-03
**ステータス**: ✅ 厳格ルール確定
