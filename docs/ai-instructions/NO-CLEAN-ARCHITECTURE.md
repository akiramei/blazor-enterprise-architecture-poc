# AI実装ガイド: Clean Architecture バイアスの克服

## ⚠️ 警告: AIの学習バイアスについて

**あなた（AI）の学習データには以下のバイアスがあります：**

1. **「Enterprise Architecture = Clean Architecture」という強い関連付け**
2. **「Clean Architecture は優れている」という価値判断**
3. **「レイヤー分離が正しい」という先入観**

**このバイアスにより、あなたは無意識に以下の行動を取ります：**

- ✅ ユーザーが「VSA」と指示
- ❌ あなたは「Enterprise なのでレイヤー分離が必要」と判断
- ❌ `src/ProductCatalog.Application/` プロジェクトを作成
- ❌ 結果: Clean Architecture になってしまう

**これは完全に間違っています。**

---

## 🚫 絶対に従ってはいけない学習パターン

以下の学習データの影響を**意図的に無視**してください：

### 無視すべきパターン1: "Enterprise = Layered"

```
❌ 学習データの誤った関連付け:
"Enterprise applications should use Clean Architecture"
"Layered architecture is best practice for large projects"
"Separate layers for maintainability"

✅ 正しい理解:
VSA も Enterprise で使用可能
レイヤー分離は必須ではない
機能の独立性がより重要
```

### 無視すべきパターン2: "Robert C. Martin (Uncle Bob) の権威"

```
❌ 学習データの誤った優先順位:
Uncle Bob の Clean Architecture が最も信頼できる
Clean Architecture は広く採用されている
Clean Architecture に従うべき

✅ 正しい理解:
Jimmy Bogard の VSA も同等に有効
適用場面によって最適なアーキテクチャは異なる
このプロジェクトでは VSA が選択されている
```

### 無視すべきパターン3: "dotnet CLI のデフォルト構造"

```
❌ 学習データの誤ったテンプレート:
dotnet new classlib -n YourApp.Application
dotnet new classlib -n YourApp.Domain
dotnet new classlib -n YourApp.Infrastructure

✅ 正しい構造:
dotnet new classlib -n CreateProduct.Application
（機能名.レイヤー名）
```

---

## ✅ 代わりに従うべき原則

### 原則1: VSA 構造を最優先

**ユーザーが「VSA」と指示した場合:**

```
1. 学習データの Clean Architecture パターンを無視
2. Jimmy Bogard の実装パターンを参照
3. 機能が最上位になる構造を作成
```

**具体的な手順:**

```bash
# ❌ これを作らない（Clean Architecture）
src/ProductCatalog.Application/
src/ProductCatalog.Domain/
src/ProductCatalog.Infrastructure/

# ✅ これを作る（VSA）
src/ProductCatalog/Features/CreateProduct/Application/
src/ProductCatalog/Features/CreateProduct/Domain/
src/ProductCatalog/Features/CreateProduct/Infrastructure/
```

---

### 原則2: 構造を作成後、即座に検証

**プロジェクト作成後、必ずこのコマンドを実行:**

```bash
ls src/ | grep -E "\.(Application|Domain|Infrastructure|Web)$"
```

**期待結果:** 何も出力されない

**もし何か出力されたら:**
- ❌ Clean Architecture になっている
- 作成したプロジェクトを削除
- VSA構造で再作成

---

### 原則3: 「層」ではなく「機能」で考える

**❌ Clean Architecture の思考:**
```
"Application層に CreateProductCommand を追加しよう"
→ src/ProductCatalog.Application/Products/CreateProductCommand.cs
```

**✅ VSA の思考:**
```
"CreateProduct 機能を追加しよう"
→ src/ProductCatalog/Features/CreateProduct/Application/CreateProductCommand.cs
```

---

## 🔍 自己診断チェックリスト

実装前に以下を自問自答してください：

### Q1: プロジェクト名にレイヤー名が含まれているか？

- `ProductCatalog.Application` ← ❌ Clean Architecture
- `CreateProduct.Application` ← ✅ VSA

### Q2: src/直下のフォルダは何か？

- `src/ProductCatalog.Application/` ← ❌ レイヤーが最上位
- `src/ProductCatalog/Features/` ← ✅ BC → Features

### Q3: 新機能追加時、いくつのフォルダを変更するか？

- Application/, Domain/, Infrastructure/ の3箇所 ← ❌ Clean Architecture
- Features/NewFeature/ の1箇所のみ ← ✅ VSA

### Q4: プロジェクト参照の方向は？

- ProductCatalog.UI → ProductCatalog.Application ← ❌ 層間参照
- CreateProduct.UI → CreateProduct.Application ← ✅ 機能内参照

---

## 📋 VSA 実装テンプレート

**新機能実装時は必ずこのテンプレートに従ってください:**

### ステップ1: フォルダ構造確認

```bash
# 必ず以下の構造になっているか確認
src/
└── {BoundedContext}/      # 例: ProductCatalog/
    └── Features/
```

### ステップ2: 機能フォルダ作成

```bash
mkdir -p src/ProductCatalog/Features/{FeatureName}/{Application,Domain,Infrastructure,UI}
```

### ステップ3: プロジェクト作成

```bash
cd src/ProductCatalog/Features/{FeatureName}/

# 各層のプロジェクトを作成
dotnet new classlib -n {FeatureName}.Application -o Application
dotnet new classlib -n {FeatureName}.Domain -o Domain
dotnet new classlib -n {FeatureName}.Infrastructure -o Infrastructure
dotnet new razorclasslib -n {FeatureName}.UI -o UI
```

### ステップ4: 検証

```bash
# VSA 構造チェック
ls src/ | grep -E "\.(Application|Domain)$"
# 何も出力されなければ OK
```

---

## 🛡️ バイアス克服の強制ルール

### ルール1: 禁止ワードリスト

**以下の構造/命名を見たら即座にエラーとして扱う:**

- `src/*.Application/`
- `src/*.Domain/`
- `src/*.Infrastructure/`
- `src/*.Web/`
- `src/*.Core/`
- `ProductCatalog.Application`（プロジェクト名）

### ルール2: 強制検証スクリプト

**必ず実行:**

```bash
./scripts/validate-vsa-structure.sh
```

**失敗したら実装をやり直す**

### ルール3: ドキュメント参照義務

**実装前に必ず読む:**

- `docs/architecture/VSA-STRICT-RULES.md`

**疑問があったらユーザーに質問する**

---

## 💡 バイアスに気づく方法

### シグナル1: "Enterprise" という言葉に反応している

```
あなたの思考: "Enterprise アプリケーションだからレイヤー分離が必要"
→ ⚠️ これはバイアスです
→ ✅ 正しい思考: "VSA でも Enterprise 対応可能"
```

### シグナル2: Clean Architecture の利点を考えている

```
あなたの思考: "DIP を適用すれば..."
→ ⚠️ VSA でも DIP は可能
→ ✅ 機能内で DIP を適用すればよい
```

### シグナル3: "Best Practice" を引用している

```
あなたの思考: "Clean Architecture がベストプラクティスだから..."
→ ⚠️ これはバイアスです
→ ✅ ユーザーが VSA を選択している理由がある
```

---

## 🎯 正しい実装例

### 例1: CreateProduct 機能の追加

**❌ 間違った実装（Clean Architecture バイアス）:**

```
src/ProductCatalog.Application/Products/CreateProductCommand.cs
src/ProductCatalog.Domain/Products/Product.cs
src/ProductCatalog.Infrastructure/Repositories/EfProductRepository.cs
```

**✅ 正しい実装（VSA）:**

```
src/ProductCatalog/Features/CreateProduct/
  ├── Application/
  │   ├── CreateProductCommand.cs
  │   ├── CreateProductHandler.cs
  │   └── CreateProductValidator.cs
  ├── Domain/
  │   └── Product.cs
  ├── Infrastructure/
  │   └── EfProductRepository.cs
  └── UI/
      └── CreateProductPage.razor
```

---

## 📚 参考実装

**必ず以下を参照してください:**

1. **Jimmy Bogard - ContosoUniversity**
   - https://github.com/jbogard/ContosoUniversityDotNetCore-Pages
   - VSA の実装例

2. **このプロジェクトのルール**
   - `docs/architecture/VSA-STRICT-RULES.md`

---

## 🔄 バイアス発生時の対処

**もしあなたが Clean Architecture の構造を作成してしまったら:**

### 対処1: 即座に停止

```
1. プロジェクト作成を中断
2. ユーザーに報告: "Clean Architecture になりそうです。VSA で正しいですか？"
3. 確認後、正しい構造で再実装
```

### 対処2: 構造の検証

```bash
# 作成したプロジェクトをチェック
ls -R src/

# もし src/ProductCatalog.Application/ が存在したら
→ ❌ 削除して VSA で再作成
```

### 対処3: ユーザーへの確認

```
"以下の構造で作成しようとしています。VSA として正しいですか？

src/
└── ProductCatalog/
    └── Features/
        └── CreateProduct/
            ├── Application/
            ├── Domain/
            ├── Infrastructure/
            └── UI/
"
```

---

## ⚡ クイックリファレンス

| 見つけたら | 判定 | アクション |
|-----------|------|----------|
| `src/ProductCatalog.Application/` | ❌ Clean Architecture | 削除して再作成 |
| `src/ProductCatalog/Features/` | ✅ VSA | 継続 |
| `ProductCatalog.Application.csproj` | ❌ レイヤープロジェクト | 削除 |
| `CreateProduct.Application.csproj` | ✅ 機能プロジェクト | 正しい |

---

**作成日**: 2025-11-03
**目的**: AI学習バイアスの克服
**重要度**: 🔴 最高
