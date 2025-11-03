# VSA移行: 現在の状況と次の作業

**最終更新**: 2025-11-03 19:45
**現在のフェーズ**: Phase 3（進行中 10%）

---

## ✅ 完了した作業

### Phase 1: Shared プロジェクト作成（完了）
- **コミット**: `b4217eb`
- **状態**: ✅ 完了
- **内容**:
  - `src/Shared/` フォルダ作成
  - 共通基盤49ファイルをコピー
    - Kernel: 5個（AggregateRoot, Entity, ValueObject等）
    - Application: 19個（ICommand, IQuery, Behaviors等）
    - Domain: 4個（AuditLog, Identity, Outbox）
    - Infrastructure: 21個（Authentication, Metrics, Migrations等）

### Phase 2: ProductCatalog BC フォルダ作成（完了）
- **コミット**: `53bb9d4`
- **状態**: ✅ 完了
- **内容**:
  - `src/ProductCatalog/Shared/` フォルダ作成
  - Product集約13ファイルをコピー
    - Domain/Products: 10個（Product.cs, ProductId.cs, Money.cs等）
    - Infrastructure/Persistence: 3個（Repository, Configuration）

### Phase 3: Features フォルダ作成（進行中 10%）
- **コミット**: `6e3a080`
- **状態**: 🔄 進行中
- **完了した内容**:
  - 10機能のフォルダ構造作成
  - CreateProduct 機能の Application層コピー（3ファイル）

---

## 🔄 現在の作業: Phase 3続き

### 次にやるべきこと

#### ステップ1: 残り9機能のApplication層をコピー

```bash
# UpdateProduct
cp -r src/ProductCatalog.Application/Features/Products/UpdateProduct/* \
      src/ProductCatalog/Features/UpdateProduct/Application/

# DeleteProduct
cp -r src/ProductCatalog.Application/Features/Products/DeleteProduct/* \
      src/ProductCatalog/Features/DeleteProduct/Application/

# GetProducts
cp -r src/ProductCatalog.Application/Features/Products/GetProducts/* \
      src/ProductCatalog/Features/GetProducts/Application/

# GetProductById
cp -r src/ProductCatalog.Application/Features/Products/GetProductById/* \
      src/ProductCatalog/Features/GetProductById/Application/

# SearchProducts
cp -r src/ProductCatalog.Application/Features/Products/SearchProducts/* \
      src/ProductCatalog/Features/SearchProducts/Application/

# BulkDeleteProducts
cp -r src/ProductCatalog.Application/Features/Products/BulkDeleteProducts/* \
      src/ProductCatalog/Features/BulkDeleteProducts/Application/

# BulkUpdateProductPrices
cp -r src/ProductCatalog.Application/Features/Products/BulkUpdateProductPrices/* \
      src/ProductCatalog/Features/BulkUpdateProductPrices/Application/

# ExportProductsToCsv
cp -r src/ProductCatalog.Application/Features/Products/ExportProductsToCsv/* \
      src/ProductCatalog/Features/ExportProductsToCsv/Application/

# ImportProductsFromCsv
cp -r src/ProductCatalog.Application/Features/Products/ImportProductsFromCsv/* \
      src/ProductCatalog/Features/ImportProductsFromCsv/Application/
```

#### ステップ2: 各機能のUI層をコピー

**Web Pages（Blazor）:**
```bash
# ProductList, ProductDetail, ProductEdit, ProductSearch
cp src/ProductCatalog.Web/Features/Products/Pages/ProductList.razor \
   src/ProductCatalog/Features/GetProducts/UI/

cp src/ProductCatalog.Web/Features/Products/Pages/ProductDetail.razor \
   src/ProductCatalog/Features/GetProductById/UI/

cp src/ProductCatalog.Web/Features/Products/Pages/ProductEdit.razor \
   src/ProductCatalog/Features/UpdateProduct/UI/

cp src/ProductCatalog.Web/Features/Products/Pages/ProductSearch.razor \
   src/ProductCatalog/Features/SearchProducts/UI/
```

**Components:**
```bash
cp -r src/ProductCatalog.Web/Features/Products/Components/* \
      src/ProductCatalog/Features/GetProducts/UI/Components/
```

**Actions:**
```bash
cp src/ProductCatalog.Web/Features/Products/Actions/ProductListActions.cs \
   src/ProductCatalog/Features/GetProducts/UI/

cp src/ProductCatalog.Web/Features/Products/Actions/ProductDetailActions.cs \
   src/ProductCatalog/Features/GetProductById/UI/

cp src/ProductCatalog.Web/Features/Products/Actions/ProductEditActions.cs \
   src/ProductCatalog/Features/UpdateProduct/UI/

cp src/ProductCatalog.Web/Features/Products/Actions/ProductSearchActions.cs \
   src/ProductCatalog/Features/SearchProducts/UI/
```

**Store:**
```bash
cp src/ProductCatalog.Web/Features/Products/Store/* \
   src/ProductCatalog/Features/GetProducts/UI/Store/
```

**Web API Controllers:**
```bash
# Auth API
mkdir -p src/Shared/Infrastructure/Api/Auth
cp -r src/ProductCatalog.Web/Features/Api/V1/Auth/* \
      src/Shared/Infrastructure/Api/Auth/

# Products API
cp src/ProductCatalog.Web/Features/Api/V1/Products/ProductsController.cs \
   src/ProductCatalog/Features/GetProducts/UI/Api/

cp -r src/ProductCatalog.Web/Features/Api/V1/Products/Dtos/* \
      src/ProductCatalog/Features/CreateProduct/UI/Api/Dtos/
```

#### ステップ3: Phase 3完了後コミット

```bash
git add src/ProductCatalog/Features/
git commit -m "refactor: Phase 3完了 - 全10機能のファイル移動完了"
git push
```

---

## ⏳ 今後の作業（Phase 4-7）

### Phase 4: 旧レイヤープロジェクト削除（推定: 30分）

```bash
# 旧プロジェクトを削除
rm -rf src/ProductCatalog.Application/
rm -rf src/ProductCatalog.Domain/
rm -rf src/ProductCatalog.Infrastructure/
rm -rf src/ProductCatalog.Web/

git add -A
git commit -m "refactor: Phase 4 - 旧レイヤープロジェクト削除"
git push
```

### Phase 5: プロジェクトファイル作成（推定: 3時間）

各機能に`.csproj`ファイルを作成する必要があります。

**テンプレート: Application層**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MediatR" Version="12.1.1" />
    <PackageReference Include="FluentValidation" Version="11.8.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Shared\Domain\Products\Shared.Domain.Products.csproj" />
    <ProjectReference Include="..\..\..\Shared\Application\Shared.Application.csproj" />
    <ProjectReference Include="..\..\..\Shared\Kernel\Shared.Kernel.csproj" />
  </ItemGroup>
</Project>
```

**作成が必要なプロジェクトファイル:**
- 各機能 × 2層（Application, UI） = 20個のプロジェクトファイル
- Sharedプロジェクト = 4個
- ProductCatalog/Sharedプロジェクト = 2個

**合計: 26個のプロジェクトファイル**

### Phase 6: ソリューションファイル更新（推定: 1時間）

```bash
# 既存のソリューションから全プロジェクトを削除
dotnet sln ProductCatalog.sln remove $(dotnet sln list | grep -E "\.csproj$")

# 新しいプロジェクトを追加
find src -name "*.csproj" | xargs -I {} dotnet sln add {}
```

### Phase 7: 検証とビルド（推定: 30分）

```bash
# VSA構造検証
./scripts/validate-vsa-structure.ps1

# ビルド確認
dotnet build

# テスト実行
dotnet test
```

---

## 📊 進捗状況

| Phase | タスク | 状態 | 推定時間 | 完了日 |
|-------|--------|------|----------|--------|
| Phase 1 | Shared作成 | ✅ 完了 | 2時間 | 2025-11-03 |
| Phase 2 | ProductCatalog BC作成 | ✅ 完了 | 1時間 | 2025-11-03 |
| Phase 3 | Features移動 | 🔄 10% | 4時間 | - |
| Phase 4 | 旧プロジェクト削除 | ⏳ 未着手 | 30分 | - |
| Phase 5 | プロジェクトファイル作成 | ⏳ 未着手 | 3時間 | - |
| Phase 6 | ソリューション更新 | ⏳ 未着手 | 1時間 | - |
| Phase 7 | 検証 | ⏳ 未着手 | 30分 | - |

**全体進捗**: 約25% 完了
**残り推定時間**: 約9時間

---

## 🚀 次のセッションでの開始方法

### 1. この文書を確認
```bash
cat docs/architecture/VSA-MIGRATION-STATUS.md
```

### 2. 現在の構造を確認
```bash
ls -la src/
ls -la src/ProductCatalog/Features/
```

### 3. 検証スクリプトで現状確認
```bash
./scripts/validate-vsa-structure.ps1
```

### 4. Phase 3の続きから開始
上記の「ステップ1: 残り9機能のApplication層をコピー」から実施

---

## 📝 重要な参考ドキュメント

- `docs/architecture/VSA-STRICT-RULES.md` - VSA厳格ルール
- `docs/architecture/VSA-MIGRATION-PLAN.md` - 詳細な移行計画
- `docs/ai-instructions/NO-CLEAN-ARCHITECTURE.md` - AIバイアス克服
- `docs/ai-instructions/README.md` - AI実装指示

---

## ⚠️ 注意事項

### Clean Architectureバイアスの回避

新しいセッションでAIに作業を依頼する際は、必ず以下を明示してください：

```
このプロジェクトは Vertical Slice Architecture (VSA) です。
Clean Architecture ではありません。

必ず以下を確認してください：
1. docs/ai-instructions/README.md を読む
2. docs/architecture/VSA-MIGRATION-STATUS.md で現状確認
3. src/ 直下にレイヤープロジェクトを作らない
```

### 検証の重要性

各Phase完了後、必ず検証スクリプトを実行してください：
```bash
./scripts/validate-vsa-structure.ps1
```

---

**作成日**: 2025-11-03
**最終更新**: 2025-11-03 19:45
**次回更新**: Phase 3完了時
