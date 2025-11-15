# リファクタリング - 次のステップ

## 🎯 目標

**ドメインモデルの物理的独立性を完全に達成する**

---

## ✅ 完了済み（フェーズ1）

1. ✅ `src/Domain/` ディレクトリ作成
2. ✅ ドメインモデルを `src/Domain/` に移動
3. ✅ Domain プロジェクトファイル作成
4. ✅ Domain の名前空間更新
5. ✅ 進捗文書化

---

## 📋 残りの作業

### フェーズ2: Application層の整理

#### 手順

1. **ディレクトリ作成**
   ```bash
   mkdir -p src/Application/PurchaseManagement
   mkdir -p src/Application/ProductCatalog
   ```

2. **Features の移動**
   ```bash
   # Visual Studioを閉じてから実行
   git mv src/PurchaseManagement/Features src/Application/PurchaseManagement/
   git mv src/ProductCatalog/Features src/Application/ProductCatalog/
   ```

3. **Shared/Application の移動**
   ```bash
   git mv src/PurchaseManagement/Shared/Application src/Application/PurchaseManagement/Shared
   git mv src/ProductCatalog/Shared/Application src/Application/ProductCatalog/Shared
   ```

4. **プロジェクトファイル作成**
   - `src/Application/PurchaseManagement/Application.PurchaseManagement.csproj`
   - `src/Application/ProductCatalog/Application.ProductCatalog.csproj`

5. **名前空間更新**
   ```bash
   # すべての .cs ファイルで置換
   namespace PurchaseManagement.Features → namespace Application.PurchaseManagement.Features
   namespace PurchaseManagement.Shared.Application → namespace Application.PurchaseManagement.Shared
   ```

---

### フェーズ3: Infrastructure層の整理

#### 手順

1. **ディレクトリ作成**
   ```bash
   mkdir -p src/Infrastructure/PurchaseManagement
   mkdir -p src/Infrastructure/ProductCatalog
   ```

2. **Infrastructure の移動**
   ```bash
   git mv src/PurchaseManagement/Infrastructure src/Infrastructure/PurchaseManagement/
   git mv src/ProductCatalog/Infrastructure src/Infrastructure/ProductCatalog/
   ```

3. **Shared/Infrastructure の移動**
   ```bash
   git mv src/PurchaseManagement/Shared/Infrastructure src/Infrastructure/PurchaseManagement/Shared
   git mv src/ProductCatalog/Shared/Infrastructure src/Infrastructure/ProductCatalog/Shared
   ```

4. **プロジェクトファイル作成/更新**
   - `src/Infrastructure/PurchaseManagement/Infrastructure.PurchaseManagement.csproj`
   - `src/Infrastructure/ProductCatalog/Infrastructure.ProductCatalog.csproj`

5. **名前空間更新**
   ```bash
   namespace PurchaseManagement.Infrastructure → namespace Infrastructure.PurchaseManagement
   ```

---

### フェーズ4: UI層の整理

#### 手順

1. **Shared/UI の移動**
   ```bash
   git mv src/PurchaseManagement/Shared/UI src/Application/PurchaseManagement/Shared/UI
   ```

2. **名前空間更新**
   ```bash
   namespace PurchaseManagement.Shared.UI → namespace Application.PurchaseManagement.Shared.UI
   ```

---

### フェーズ5: 古いディレクトリの削除

#### 手順

1. **空になったディレクトリを削除**
   ```bash
   rm -rf src/PurchaseManagement/Shared/Domain
   rm -rf src/ProductCatalog/Shared/Domain

   # すべてが移動済みなら
   rm -rf src/PurchaseManagement
   rm -rf src/ProductCatalog
   ```

---

### フェーズ6: プロジェクト参照の更新

#### 更新が必要なファイル

1. **Application プロジェクト**
   ```xml
   <!-- Before -->
   <ProjectReference Include="..\PurchaseManagement\Shared\Domain\PurchaseManagement.Shared.Domain.csproj" />

   <!-- After -->
   <ProjectReference Include="..\..\Domain\PurchaseManagement\Domain.PurchaseManagement.csproj" />
   ```

2. **Infrastructure プロジェクト**
   ```xml
   <!-- Domain への参照を更新 -->
   <ProjectReference Include="..\..\Domain\PurchaseManagement\Domain.PurchaseManagement.csproj" />

   <!-- Application への参照を更新 -->
   <ProjectReference Include="..\..\Application\PurchaseManagement\Application.PurchaseManagement.csproj" />
   ```

3. **Host プロジェクト**
   ```xml
   <!-- すべての参照を新しいパスに更新 -->
   ```

---

### フェーズ7: ソリューションファイルの更新

#### VSASample.sln の更新

```bash
# 古いプロジェクト参照を削除
dotnet sln remove src/PurchaseManagement/Shared/Domain/PurchaseManagement.Shared.Domain.csproj

# 新しいプロジェクト参照を追加
dotnet sln add src/Domain/PurchaseManagement/Domain.PurchaseManagement.csproj
dotnet sln add src/Application/PurchaseManagement/Application.PurchaseManagement.csproj
dotnet sln add src/Infrastructure/PurchaseManagement/Infrastructure.PurchaseManagement.csproj
```

---

### フェーズ8: テストプロジェクトの更新

#### 更新対象

1. **tests/PurchaseManagement.Domain.UnitTests/**
   ```csharp
   // Before
   using PurchaseManagement.Shared.Domain.PurchaseRequests;

   // After
   using Domain.PurchaseManagement.PurchaseRequests;
   ```

2. **プロジェクト参照**
   ```xml
   <!-- Before -->
   <ProjectReference Include="..\..\src\PurchaseManagement\Shared\Domain\PurchaseManagement.Shared.Domain.csproj" />

   <!-- After -->
   <ProjectReference Include="..\..\src\Domain\PurchaseManagement\Domain.PurchaseManagement.csproj" />
   ```

---

### フェーズ9: ビルドとテストの実行

#### 段階的なビルド

```bash
# 1. Domain のビルド
dotnet build src/Domain/PurchaseManagement/Domain.PurchaseManagement.csproj
dotnet build src/Domain/ProductCatalog/Domain.ProductCatalog.csproj

# 2. Application のビルド
dotnet build src/Application/PurchaseManagement/Application.PurchaseManagement.csproj
dotnet build src/Application/ProductCatalog/Application.ProductCatalog.csproj

# 3. Infrastructure のビルド
dotnet build src/Infrastructure/PurchaseManagement/Infrastructure.PurchaseManagement.csproj
dotnet build src/Infrastructure/ProductCatalog/Infrastructure.ProductCatalog.csproj

# 4. 全体のビルド
dotnet build

# 5. テストの実行
dotnet test
```

---

### フェーズ10: ドキュメントの更新

#### 更新が必要なドキュメント

1. **README.md**
   - ディレクトリ構造の図を更新
   - Domain の独立性を強調

2. **ARCHITECTURE.md** または同等のファイル
   - 新しい構造を反映
   - 依存方向の図を更新

3. **VSA関連ドキュメント**
   - `docs/architecture/VSA-BC-SLICE-BOUNDARY-RELATIONSHIP.md`
   - `docs/architecture/VSA-DOMAIN-INDEPENDENCE.md`
   - `docs/architecture/SHARED-VS-KERNEL-DISTINCTION.md`
   - `docs/architecture/DOMAIN-MODEL-SEPARATION-BY-BC.md`

4. **AI向けドキュメント（最重要）**
   - `AGENTS.md` または `AI-INSTRUCTIONS.md`
   - 「Domainはアーキテクチャから独立」を明記
   - 新しい構造のテンプレートを提供

---

## 🛠️ 便利なスクリプト

### 一括名前空間置換スクリプト

```bash
#!/bin/bash

# Application層の名前空間を一括置換
find src/Application/PurchaseManagement -name "*.cs" -type f -exec sed -i \
  's/namespace PurchaseManagement\.Features/namespace Application.PurchaseManagement.Features/g' {} \;

find src/Application/PurchaseManagement -name "*.cs" -type f -exec sed -i \
  's/namespace PurchaseManagement\.Shared\.Application/namespace Application.PurchaseManagement.Shared/g' {} \;

# using ステートメントも置換
find src/Application/PurchaseManagement -name "*.cs" -type f -exec sed -i \
  's/using PurchaseManagement\.Shared\.Domain/using Domain.PurchaseManagement/g' {} \;
```

### プロジェクト参照一括更新スクリプト

```bash
#!/bin/bash

# Application プロジェクトで Domain への参照を更新
find src/Application -name "*.csproj" -type f -exec sed -i \
  's|\.\.\\PurchaseManagement\\Shared\\Domain\\PurchaseManagement\.Shared\.Domain\.csproj|..\\..\\Domain\\PurchaseManagement\\Domain.PurchaseManagement.csproj|g' {} \;
```

---

## ⚠️ 注意事項

### 作業前の準備

1. **Visual Studioを閉じる**
   - ファイルロックを避けるため

2. **ビルド成果物を削除**
   ```bash
   find . -name "bin" -type d -exec rm -rf {} \;
   find . -name "obj" -type d -exec rm -rf {} \;
   ```

3. **Gitコミット**
   - 各フェーズ完了後にコミット
   - ロールバック可能な状態を維持

---

### 推奨作業順序

1. **フェーズ2（Application）を完了**
2. **ビルドとテストを確認**
3. **コミット**
4. **フェーズ3（Infrastructure）を完了**
5. **ビルドとテストを確認**
6. **コミット**
7. **フェーズ4-10を順次実施**

---

## 📊 完了チェックリスト

### Domain（完了済み）
- [x] `src/Domain/` ディレクトリ作成
- [x] ドメインモデル移動
- [x] プロジェクトファイル作成
- [x] 名前空間更新

### Application（未完了）
- [ ] `src/Application/` ディレクトリ作成
- [ ] Features 移動
- [ ] Shared/Application 移動
- [ ] プロジェクトファイル作成
- [ ] 名前空間更新
- [ ] プロジェクト参照更新

### Infrastructure（未完了）
- [ ] `src/Infrastructure/` ディレクトリ作成
- [ ] Infrastructure 移動
- [ ] プロジェクトファイル作成
- [ ] 名前空間更新
- [ ] プロジェクト参照更新

### 整理（未完了）
- [ ] 古いディレクトリ削除
- [ ] ソリューションファイル更新
- [ ] テストプロジェクト更新

### 検証（未完了）
- [ ] ビルド成功
- [ ] テスト成功
- [ ] 依存方向の確認

### ドキュメント（未完了）
- [ ] README.md 更新
- [ ] アーキテクチャドキュメント更新
- [ ] AI向けドキュメント更新

---

## 🎯 最終目標

```
src/
├── Domain/                          ← ✅ 完了
│   ├── PurchaseManagement/
│   └── ProductCatalog/
│
├── Application/                     ← ⏳ 作業中
│   ├── PurchaseManagement/
│   │   ├── Features/
│   │   └── Shared/
│   └── ProductCatalog/
│       └── Features/
│
├── Infrastructure/                  ← ⏳ 未着手
│   ├── PurchaseManagement/
│   └── ProductCatalog/
│
└── Shared/                          ← ✅ 変更なし
    ├── Kernel/
    ├── Domain/
    ├── Application/
    └── Infrastructure/
```

**依存方向:**
```
Infrastructure/ → Application/ → Domain/ → Shared/Kernel/
```

---

## 📚 参考ドキュメント

- **現状報告:** `docs/REFACTORING-STATUS.md`
- **詳細計画:** `docs/architecture/DOMAIN-INDEPENDENCE-REFACTORING-PLAN.md`
- **ドメイン独立性:** `docs/architecture/VSA-DOMAIN-INDEPENDENCE.md`
