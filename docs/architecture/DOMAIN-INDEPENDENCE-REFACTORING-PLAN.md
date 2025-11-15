# ドメイン独立性リファクタリング計画

## 🎯 目的

**ドメインモデルを物理的にもアーキテクチャから独立させる**

---

## 📐 現在の状態

### 論理的独立性

✅ **保たれている**
- ドメインモデルにUI、DB、VSAの概念が含まれていない
- 純粋なビジネスルールのみを表現

```csharp
// 現在のドメインモデル
public sealed class PurchaseRequest : AggregateRoot<Guid>
{
    public Result Approve(Guid approverId)
    {
        // ✅ 純粋なビジネスルール
        // ✅ UI、DB、VSAの概念なし
    }
}
```

### 物理的独立性

❌ **不完全**
- ドメインモデルがBC構造内に配置されている
- アーキテクチャ選択（VSA）と混在

```
現在の構造:
src/PurchaseManagement/          ← BC + 技術構造の混在
  ├── Shared/Domain/             ← ドメインモデル（従属）
  ├── Features/                  ← VSA
  └── Infrastructure/
```

---

## 🏗️ 目標構造

### 完全に独立したドメインモデル

```
src/
├── Domain/                      ← ★ ドメインモデル（完全独立）
│   ├── PurchaseManagement/      ← BC1（ビジネス境界）
│   │   └── PurchaseRequests/
│   │       ├── PurchaseRequest.cs
│   │       ├── ApprovalStep.cs
│   │       ├── Events/
│   │       └── Boundaries/
│   │           ├── IApprovalBoundary.cs
│   │           ├── ApprovalBoundaryService.cs
│   │           └── UIMetadata.cs
│   │
│   └── ProductCatalog/          ← BC2（ビジネス境界）
│       └── Products/
│           └── Product.cs
│
├── Application/                 ← ★ アプリケーション層（VSA）
│   ├── PurchaseManagement/
│   │   └── Features/            ← 機能ごとの垂直パイプライン
│   │       ├── ApprovePurchaseRequest/
│   │       │   ├── UI/
│   │       │   └── Application/
│   │       │       ├── ApprovePurchaseRequestCommand.cs
│   │       │       └── ApprovePurchaseRequestHandler.cs
│   │       │
│   │       └── GetPurchaseRequestById/
│   │           ├── UI/
│   │           └── Application/
│   │
│   └── ProductCatalog/
│       └── Features/
│
├── Infrastructure/              ← ★ インフラ層
│   ├── PurchaseManagement/
│   │   ├── Persistence/
│   │   │   ├── PurchaseManagementDbContext.cs
│   │   │   ├── Repositories/
│   │   │   └── Configurations/
│   │   └── Services/
│   │
│   └── ProductCatalog/
│       └── Persistence/
│
└── Shared/                      ← BC横断の共通基盤
    ├── Kernel/
    │   ├── Entity.cs
    │   ├── ValueObject.cs
    │   └── Money.cs
    ├── Domain/
    │   ├── Identity/
    │   └── Outbox/
    └── Infrastructure/
```

---

## 📋 リファクタリング手順

### フェーズ1: ドメインモデルの物理的分離

#### ステップ1.1: Domain ルートの作成

```bash
# 新しいディレクトリ構造を作成
mkdir -p src/Domain/PurchaseManagement
mkdir -p src/Domain/ProductCatalog
```

#### ステップ1.2: ドメインモデルの移動

```bash
# PurchaseManagement のドメインモデルを移動
mv src/PurchaseManagement/Shared/Domain/PurchaseRequests \
   src/Domain/PurchaseManagement/

mv src/PurchaseManagement/Shared/Domain/PurchaseRequestAttachment.cs \
   src/Domain/PurchaseManagement/

# ProductCatalog のドメインモデルを移動
mv src/ProductCatalog/Shared/Domain/Products \
   src/Domain/ProductCatalog/
```

**結果:**
```
src/Domain/
├── PurchaseManagement/
│   ├── PurchaseRequests/
│   │   ├── PurchaseRequest.cs
│   │   ├── ApprovalStep.cs
│   │   ├── Events/
│   │   └── Boundaries/
│   └── PurchaseRequestAttachment.cs
│
└── ProductCatalog/
    └── Products/
        └── Product.cs
```

#### ステップ1.3: プロジェクトファイルの作成

```xml
<!-- src/Domain/PurchaseManagement/Domain.PurchaseManagement.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>Domain.PurchaseManagement</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <!-- ✅ Shared.Kernel のみに依存 -->
    <ProjectReference Include="..\..\Shared\Kernel\Shared.Kernel.csproj" />
  </ItemGroup>
</Project>
```

**重要なポイント:**
- ✅ `Shared.Kernel` のみに依存
- ❌ `Application/` や `Infrastructure/` への依存は一切ない

---

### フェーズ2: アプリケーション層の整理

#### ステップ2.1: Application ルートの作成

```bash
mkdir -p src/Application/PurchaseManagement/Features
mkdir -p src/Application/ProductCatalog/Features
```

#### ステップ2.2: Features の移動

```bash
# PurchaseManagement の Features を移動
mv src/PurchaseManagement/Features/* \
   src/Application/PurchaseManagement/Features/
```

**結果:**
```
src/Application/
└── PurchaseManagement/
    └── Features/
        ├── ApprovePurchaseRequest/
        │   └── Application/
        │       ├── ApprovePurchaseRequestCommand.cs
        │       └── ApprovePurchaseRequestHandler.cs
        ├── GetPurchaseRequestById/
        └── SubmitPurchaseRequest/
```

#### ステップ2.3: プロジェクト参照の更新

```xml
<!-- src/Application/PurchaseManagement/Application.PurchaseManagement.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>Application.PurchaseManagement</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <!-- ✅ Domain を参照 -->
    <ProjectReference Include="..\..\Domain\PurchaseManagement\Domain.PurchaseManagement.csproj" />
    <ProjectReference Include="..\..\Shared\Kernel\Shared.Kernel.csproj" />
  </ItemGroup>
</Project>
```

---

### フェーズ3: インフラ層の整理

#### ステップ3.1: Infrastructure の移動

```bash
mv src/PurchaseManagement/Infrastructure \
   src/Infrastructure/PurchaseManagement
```

**結果:**
```
src/Infrastructure/
└── PurchaseManagement/
    ├── Persistence/
    │   ├── PurchaseManagementDbContext.cs
    │   ├── Repositories/
    │   │   └── EfPurchaseRequestRepository.cs
    │   └── Configurations/
    └── Services/
```

#### ステップ3.2: プロジェクト参照の更新

```xml
<!-- src/Infrastructure/PurchaseManagement/Infrastructure.PurchaseManagement.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <!-- ✅ Domain を参照（リポジトリ実装のため） -->
    <ProjectReference Include="..\..\Domain\PurchaseManagement\Domain.PurchaseManagement.csproj" />

    <!-- ✅ Application を参照（必要な場合のみ） -->
    <ProjectReference Include="..\..\Application\PurchaseManagement\Application.PurchaseManagement.csproj" />
  </ItemGroup>
</Project>
```

---

### フェーズ4: 依存関係の検証

#### 依存方向の確認

```
Infrastructure/PurchaseManagement/
  ↓ 依存
Application/PurchaseManagement/
  ↓ 依存
Domain/PurchaseManagement/
  ↓ 依存（のみ）
Shared/Kernel/

✅ 一方向の依存関係
❌ Domain は他のどの層にも依存しない
```

#### プロジェクト参照の検証スクリプト

```bash
# Domain プロジェクトが Application や Infrastructure を参照していないことを確認
grep -r "Application" src/Domain/*/Domain.*.csproj && echo "❌ NG: Domain が Application を参照" || echo "✅ OK"
grep -r "Infrastructure" src/Domain/*/Domain.*.csproj && echo "❌ NG: Domain が Infrastructure を参照" || echo "✅ OK"
```

---

### フェーズ5: 名前空間の更新

#### Before（現在）

```csharp
namespace PurchaseManagement.Shared.Domain.PurchaseRequests;

public sealed class PurchaseRequest : AggregateRoot<Guid>
{
    // ...
}
```

#### After（リファクタリング後）

```csharp
namespace Domain.PurchaseManagement.PurchaseRequests;

public sealed class PurchaseRequest : AggregateRoot<Guid>
{
    // ...
}
```

#### 自動変更スクリプト（例）

```bash
# PurchaseManagement のドメインモデルの名前空間を変更
find src/Domain/PurchaseManagement -name "*.cs" -exec sed -i \
  's/namespace PurchaseManagement\.Shared\.Domain/namespace Domain.PurchaseManagement/g' {} \;
```

---

### フェーズ6: アプリケーション層の using 更新

#### Before

```csharp
using PurchaseManagement.Shared.Domain.PurchaseRequests;
using PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;
```

#### After

```csharp
using Domain.PurchaseManagement.PurchaseRequests;
using Domain.PurchaseManagement.PurchaseRequests.Boundaries;
```

---

## 🧪 移行テスト

### テスト1: ビルドの成功

```bash
dotnet build src/Domain/PurchaseManagement/Domain.PurchaseManagement.csproj
dotnet build src/Application/PurchaseManagement/Application.PurchaseManagement.csproj
dotnet build src/Infrastructure/PurchaseManagement/Infrastructure.PurchaseManagement.csproj
```

### テスト2: ユニットテストの実行

```bash
# Domain のテストが通ることを確認
dotnet test tests/Domain.PurchaseManagement.UnitTests/

# 全体のテストが通ることを確認
dotnet test
```

### テスト3: 依存関係の検証

```bash
# Domain が Application や Infrastructure を参照していないことを確認
dotnet list src/Domain/PurchaseManagement/Domain.PurchaseManagement.csproj reference
# → Shared.Kernel のみ表示されるべき
```

---

## 📊 移行前後の比較

### Before（現在）

```
src/
├── PurchaseManagement/          ← BC + 技術構造の混在
│   ├── Shared/Domain/           ← ドメインモデル
│   ├── Features/                ← VSA
│   └── Infrastructure/          ← インフラ
```

**問題点:**
- ❌ ドメインモデルが BC構造内に配置
- ❌ BC と技術構造が混在
- ❌ アーキテクチャ変更時に Domain も移動が必要

---

### After（リファクタリング後）

```
src/
├── Domain/                      ← ★ 完全独立
│   ├── PurchaseManagement/
│   └── ProductCatalog/
│
├── Application/                 ← ★ 技術構造（VSA）
│   ├── PurchaseManagement/
│   └── ProductCatalog/
│
└── Infrastructure/              ← ★ 技術構造
    ├── PurchaseManagement/
    └── ProductCatalog/
```

**利点:**
- ✅ ドメインモデルが物理的に独立
- ✅ BC と技術構造が分離
- ✅ アーキテクチャ変更時も Domain は不変

---

## 🎯 段階的移行戦略

### パイロットBC: PurchaseManagement

**理由:**
- 既に実装が進んでいる
- ドメインモデルが充実している
- 移行の影響範囲を測定できる

**手順:**
1. `Domain/PurchaseManagement/` を作成
2. ドメインモデルを移動
3. 参照を更新
4. テスト実行
5. 問題があれば修正

**検証:**
- ✅ ビルドが通る
- ✅ テストが通る
- ✅ Domain が独立している

---

### 全体展開: ProductCatalog

パイロットBCで得た知見を基に：
1. `Domain/ProductCatalog/` を作成
2. 同様の手順で移行
3. スクリプトを整備（自動化）

---

## 📚 ドキュメント更新

### 更新対象

1. **README.md**
   - 新しいディレクトリ構造を明記
   - ドメインモデルの独立性を強調

2. **AGENTS.md**
   - AI エージェントへの指示を更新
   - 「Domain はアーキテクチャから独立」を明記

3. **アーキテクチャドキュメント**
   - `VSA-BC-SLICE-BOUNDARY-RELATIONSHIP.md` を更新
   - `DOMAIN-INDEPENDENCE.md` を新規作成

---

## ⚠️ 注意事項

### 移行中の注意点

1. **Git履歴の保持**
   ```bash
   # mv ではなく git mv を使用
   git mv src/PurchaseManagement/Shared/Domain/PurchaseRequests \
          src/Domain/PurchaseManagement/
   ```

2. **段階的コミット**
   - フェーズごとにコミット
   - ビルドが通る状態を維持

3. **チームへの周知**
   - 移行計画を共有
   - ペアプログラミングで実施

---

## 🔄 ロールバック計画

万が一問題が発生した場合：

```bash
# Git で戻す
git revert <commit-hash>

# または
git reset --hard <commit-before-refactoring>
```

---

## ✅ 完了条件

### 必須条件

- ✅ Domain が Application/Infrastructure を参照していない
- ✅ すべてのビルドが通る
- ✅ すべてのテストが通る
- ✅ ドキュメントが更新されている

### 望ましい条件

- ✅ CI/CDパイプラインが正常動作
- ✅ チーム全員が新構造を理解
- ✅ AIエージェントが新構造で動作

---

## 📖 参考資料

### Domain-Driven Design (Eric Evans)

> "ドメインモデルはインフラストラクチャから独立すべきである"

### Clean Architecture (Robert C. Martin)

> "ビジネスルールは技術的詳細から独立すべきである"

---

## 🎓 結論

### 現状

- ✅ 論理的独立性は保たれている
- ❌ 物理的独立性が不完全

### 目標

- ✅ 論理的独立性を維持
- ✅ 物理的独立性を達成

### アプローチ

- 段階的リファクタリング（パイロットBC → 全体展開）
- 依存関係の厳格な検証
- ドキュメントの徹底的な更新

---

## 📝 次のアクション

当面は現在の配置で運用し、以下のルールを厳格に守る：

1. **ドメインモデルに UI・DB の概念を持ち込まない**
2. **Domain → Application/Infrastructure の依存を作らない**
3. **将来的なリファクタリングを計画する**

必要に応じて、このドキュメントに基づいた段階的移行を実施する。
