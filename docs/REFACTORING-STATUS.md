# リファクタリング状況報告

## 🎯 目的

**ドメインモデルを物理的にアーキテクチャから独立させる**

このプロジェクトは他のプロジェクトが参照するテンプレートであり、誤った構造が量産されることを防ぐため、正しいアーキテクチャを確立する必要がある。

---

## ✅ 完了した作業（フェーズ1）

### 1. Domain/ルートディレクトリの作成

```
src/Domain/
├── PurchaseManagement/
└── ProductCatalog/
```

**目的:** ドメインモデルを技術構造（VSA、Clean Architecture等）から物理的に独立させる

---

### 2. ドメインモデルの移動

#### Before（誤った構造）
```
src/PurchaseManagement/Shared/Domain/  ← BC構造内（技術構造に従属）
src/ProductCatalog/Shared/Domain/      ← BC構造内（技術構造に従属）
```

#### After（正しい構造）
```
src/Domain/PurchaseManagement/         ← 完全独立
  ├── PurchaseRequests/
  │   ├── PurchaseRequest.cs
  │   ├── ApprovalStep.cs
  │   ├── Events/
  │   ├── StateMachine/
  │   └── Boundaries/
  │       ├── ApprovalBoundaryService.cs
  │       ├── UIMetadata.cs
  │       └── ...
  └── PurchaseRequestAttachment.cs

src/Domain/ProductCatalog/              ← 完全独立
  └── Products/
      └── Product.cs
```

**成果:**
- ✅ ドメインモデルが物理的に独立
- ✅ アーキテクチャ選択（VSA、Clean Architecture等）から分離

---

### 3. プロジェクトファイルの作成

#### Domain.PurchaseManagement.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RootNamespace>Domain.PurchaseManagement</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <!-- ✅ Shared.Kernel と Shared.Domain のみに依存 -->
    <ProjectReference Include="..\..\Shared\Kernel\Shared.Kernel.csproj" />
    <ProjectReference Include="..\..\Shared\Domain\Shared.Domain.csproj" />
  </ItemGroup>
</Project>
```

**重要なポイント:**
- ✅ Application/ や Infrastructure/ への依存は一切ない
- ✅ ドメインモデルの独立性が保証される

---

### 4. 名前空間の更新

#### Before
```csharp
namespace PurchaseManagement.Shared.Domain.PurchaseRequests;
using PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;
```

#### After
```csharp
namespace Domain.PurchaseManagement.PurchaseRequests;
using Domain.PurchaseManagement.PurchaseRequests.Boundaries;
```

**影響範囲:**
- ✅ `src/Domain/PurchaseManagement/` 内のすべての.csファイル
- ✅ `src/Domain/ProductCatalog/` 内のすべての.csファイル

---

## ⏳ 残りの作業（フェーズ2以降）

### フェーズ2: Application層の整理

#### 目標構造
```
src/Application/
├── PurchaseManagement/
│   ├── Features/                    ← VSA（垂直スライス）
│   │   ├── ApprovePurchaseRequest/
│   │   │   └── Application/
│   │   ├── GetPurchaseRequestById/
│   │   │   ├── Application/
│   │   │   └── UI/
│   │   └── ...
│   └── Shared/
│       └── Application/
└── ProductCatalog/
    └── Features/
```

#### 作業内容
1. `src/Application/` ディレクトリ作成
2. `src/PurchaseManagement/Features/` → `src/Application/PurchaseManagement/Features/` に移動
3. `src/PurchaseManagement/Shared/Application/` → `src/Application/PurchaseManagement/Shared/` に移動
4. プロジェクトファイル作成（`Application.PurchaseManagement.csproj`）
5. 名前空間更新
6. プロジェクト参照更新

**依存方向:**
```
Application/PurchaseManagement/
  ↓ 依存
Domain/PurchaseManagement/
```

---

### フェーズ3: Infrastructure層の整理

#### 目標構造
```
src/Infrastructure/
├── PurchaseManagement/
│   ├── Persistence/
│   │   ├── PurchaseManagementDbContext.cs
│   │   ├── Repositories/
│   │   └── Configurations/
│   └── Services/
└── ProductCatalog/
    └── Persistence/
```

#### 作業内容
1. `src/Infrastructure/` ディレクトリ作成
2. `src/PurchaseManagement/Infrastructure/` → `src/Infrastructure/PurchaseManagement/` に移動
3. プロジェクトファイル作成
4. 名前空間更新
5. プロジェクト参照更新

**依存方向:**
```
Infrastructure/PurchaseManagement/
  ↓ 依存
Domain/PurchaseManagement/
```

---

### フェーズ4: 古いディレクトリの削除

```bash
# 移動が完了したら古いディレクトリを削除
rm -rf src/PurchaseManagement/Shared/Domain
rm -rf src/PurchaseManagement/Features
rm -rf src/PurchaseManagement/Infrastructure
rm -rf src/ProductCatalog/Shared/Domain
```

---

### フェーズ5: プロジェクト参照の更新

#### 更新対象
1. すべての `.csproj` ファイル
2. `VSASample.sln` ソリューションファイル
3. テストプロジェクト

#### 更新内容
```xml
<!-- Before -->
<ProjectReference Include="..\PurchaseManagement\Shared\Domain\PurchaseManagement.Shared.Domain.csproj" />

<!-- After -->
<ProjectReference Include="..\Domain\PurchaseManagement\Domain.PurchaseManagement.csproj" />
```

---

### フェーズ6: テストの更新

#### テストプロジェクトの名前空間更新
```csharp
// Before
using PurchaseManagement.Shared.Domain.PurchaseRequests;

// After
using Domain.PurchaseManagement.PurchaseRequests;
```

---

### フェーズ7: ビルドとテストの実行

```bash
# Domain のビルド
dotnet build src/Domain/PurchaseManagement/Domain.PurchaseManagement.csproj
dotnet build src/Domain/ProductCatalog/Domain.ProductCatalog.csproj

# 全体のビルド
dotnet build

# テストの実行
dotnet test
```

---

### フェーズ8: ドキュメントの更新

#### 更新対象ドキュメント

1. **README.md**
   - 新しいディレクトリ構造を明記
   - Domain の独立性を強調

2. **AGENTS.md** または **AI-INSTRUCTIONS.md**
   - AIが参照する構造ガイドを更新
   - 「Domainはアーキテクチャから独立」を明記

3. **アーキテクチャドキュメント**
   - `VSA-BC-SLICE-BOUNDARY-RELATIONSHIP.md` を更新
   - `SHARED-VS-KERNEL-DISTINCTION.md` を更新
   - `VSA-DOMAIN-INDEPENDENCE.md` を更新
   - `DOMAIN-MODEL-SEPARATION-BY-BC.md` を更新

---

## 📊 現在の構造（部分完了）

```
src/
├── Domain/                          ← ✅ 完了（ドメインモデル独立）
│   ├── PurchaseManagement/
│   │   ├── PurchaseRequests/
│   │   └── PurchaseRequestAttachment.cs
│   └── ProductCatalog/
│       └── Products/
│
├── Application/                     ← ⏳ 作業中（一部コピー済み）
│   └── PurchaseManagement/
│       ├── Features/                ← コピー済み（未統合）
│       └── Shared/
│
├── PurchaseManagement/              ← ⚠️ 古い構造（削除予定）
│   ├── Features/                    ← 移動予定
│   ├── Infrastructure/              ← 移動予定
│   └── Shared/
│       ├── Application/             ← 移動予定
│       ├── Domain/                  ← ✅ 空（移動済み）
│       ├── Infrastructure/
│       └── UI/
│
├── ProductCatalog/                  ← ⚠️ 古い構造（削除予定）
│   ├── Features/
│   └── Shared/
│       └── Domain/                  ← ✅ 空（移動済み）
│
└── Shared/                          ← ✅ BC横断の共通基盤（変更なし）
    ├── Kernel/
    ├── Domain/
    ├── Application/
    └── Infrastructure/
```

---

## 🚧 現在の課題

### 1. ファイルロックの問題

`git mv` コマンドでファイルが移動できない（"Permission denied"）

**原因:**
- Visual Studio や他のプロセスがファイルをロック中
- ビルド成果物（bin/obj）の影響

**対策:**
- コピー（`cp -r`）で対応済み
- 後で元のファイルを手動削除

---

### 2. プロジェクト参照の更新

大量のプロジェクト参照を更新する必要がある

**対策:**
- スクリプトで自動化（次のステップ）
- または手動で段階的に更新

---

### 3. 名前空間の一括更新

Application層、Infrastructure層の名前空間も更新が必要

**対策:**
- sed コマンドで一括置換（スクリプト化）

---

## 📋 次のステップ

### 優先順位1: 移行スクリプトの作成

ファイルロックの問題を回避し、段階的に移行できるスクリプトを作成

### 優先順位2: Application層の移行完了

`src/Application/` への移行を完了させる

### 優先順位3: Infrastructure層の移行完了

`src/Infrastructure/` への移行を完了させる

### 優先順位4: 古いディレクトリの削除

移行が完了したら、古い構造を削除

### 優先順位5: ドキュメントの更新

新しい構造をドキュメントに反映

---

## ⚠️ 重要な注意事項

### このプロジェクトの位置づけ

**このプロジェクトは他のプロジェクトが参照するテンプレートである**

```
VSASample (このプロジェクト)
  ↓ AIが参照
量産される複数のプロジェクト
  ↓ 誤った構造が複製される
深刻な被害
```

**したがって:**
- ✅ 完璧な構造にすることが最優先
- ✅ 互換性や影響を心配する必要はない
- ✅ 中途半端な状態で公開しない

---

## 🎯 最終目標構造

```
src/
├── Domain/                          ← ドメインモデル（完全独立）
│   ├── PurchaseManagement/
│   └── ProductCatalog/
│
├── Application/                     ← アプリケーション層（VSA）
│   ├── PurchaseManagement/
│   │   └── Features/
│   └── ProductCatalog/
│       └── Features/
│
├── Infrastructure/                  ← インフラ層
│   ├── PurchaseManagement/
│   │   └── Persistence/
│   └── ProductCatalog/
│       └── Persistence/
│
└── Shared/                          ← BC横断の共通基盤
    ├── Kernel/
    ├── Domain/
    ├── Application/
    └── Infrastructure/
```

**依存方向:**
```
Infrastructure/
  ↓
Application/
  ↓
Domain/          ← どの層にも依存しない（完全独立）
  ↓
Shared/Kernel/
```

---

## 📚 関連ドキュメント

- **リファクタリング計画:** `docs/architecture/DOMAIN-INDEPENDENCE-REFACTORING-PLAN.md`
- **ドメインの独立性:** `docs/architecture/VSA-DOMAIN-INDEPENDENCE.md`
- **BC間のドメイン分離:** `docs/architecture/DOMAIN-MODEL-SEPARATION-BY-BC.md`

---

## 📝 作業ログ

### 2025-11-15

- ✅ `src/Domain/` ディレクトリ作成
- ✅ ドメインモデル移動（PurchaseManagement, ProductCatalog）
- ✅ Domain プロジェクトファイル作成
- ✅ Domain の名前空間更新
- ⏳ Application層の移行開始（ファイルロック問題で中断）
- ✅ 現状文書化（このドキュメント）

---

## ✅ 完了条件

リファクタリング完了とみなす条件：

1. ✅ Domain が物理的に独立している
2. ✅ Application が Domain を参照している
3. ✅ Infrastructure が Domain を参照している
4. ✅ Domain が Application/Infrastructure を参照していない
5. ✅ すべてのビルドが通る
6. ✅ すべてのテストが通る
7. ✅ ドキュメントが更新されている
8. ✅ 古いディレクトリ構造が削除されている

**現在の達成度: 1/8 (12.5%)**
