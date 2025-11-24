# AGENTS.md - AI Agent Instructions

このファイルは GitHub Copilot、OpenAI Codex、その他の AI エージェントが自動的に読み込む設定ファイルです。

---

## 🚨 CRITICAL: 実装前に必ず読むこと

**このプロジェクトはカタログ駆動開発を採用しています。独自実装は禁止です。**

### 必須ドキュメントの読み込み順序

| 順序 | ファイル | 目的 |
|:---:|----------|------|
| 1 | `catalog/AI_USAGE_GUIDE.md` | 実装ルール・制約・アーキテクチャ全体像・UI配置ルール |
| 2 | `catalog/index.json` | パターン索引・意思決定マトリクス |
| 3 | `catalog/COMMON_MISTAKES.md` | 頻出ミスと回避方法 |
| 4 | `catalog/DECISION_FLOWCHART.md` | パターン選択アルゴリズム |

**これらを読まずに実装を開始してはいけません。**

---

## ⛔ 絶対禁止事項（NEVER DO）

```
❌ Handler内でSaveChangesAsync()を呼ばない
   → TransactionBehaviorが自動実行する

❌ SingletonでDbContextやScopedサービスを注入しない
   → すべてのサービスはScopedで登録

❌ MediatRのHandleメソッド名をHandleAsyncにしない
   → 正しくは Handle（AsyncはMediatRの規約外）

❌ 独自のCQRS基盤を作らない
   → MediatR + ICommand<T> / IQuery<T> を使用

❌ 例外をthrowしてエラーを伝播しない
   → Result<T> パターンを使用

❌ カタログに存在するパターンを独自実装しない
   → 必ず catalog/patterns/*.yaml を参照
```

---

## 🏗️ アーキテクチャ: Vertical Slice Architecture (VSA)

### 基本原則

- **機能単位で垂直統合**: 1つの機能に必要なすべての層を1箇所に集約
- **CQRS**: Command/Query責務分離（MediatR使用）
- **Pipeline Behaviors**: 横断的関心事の自動実行

### プロジェクト構造

```
src/Application/                     # 単一Blazor Serverプロジェクト
├── Components/                      # Blazorテンプレート由来（活用する）
│   ├── Layout/                      # MainLayout, NavMenu（フレームワーク必須）
│   ├── Pages/                       # 複数機能で使う/アプリ基盤ページ
│   ├── Shared/                      # BC横断の共有コンポーネント
│   ├── App.razor                    # エントリーポイント
│   └── Routes.razor                 # ルーティング
│
├── Features/                        # VSA機能スライス
│   └── {Feature}/
│       ├── {Feature}Command.cs      # コマンド定義
│       ├── {Feature}CommandHandler.cs
│       ├── {Feature}Validator.cs    # FluentValidation
│       └── UI/                      # ★ 単一機能専用のページ/コンポーネント
│           └── {Feature}.razor
│
├── Shared/{BC}/                     # BC（Bounded Context）内共有
│   ├── Application/                 # DTOs, Interfaces, ReadRepository
│   ├── Infrastructure/              # DbContext, Repository, Behaviors
│   └── UI/                          # ★ BC内で複数機能が共有するUI
│       ├── Store/                   # 状態管理（State + Store）
│       ├── Actions/                 # PageActions（UI手順）
│       └── Components/              # 共有コンポーネント
│
└── Core/                            # Commands, Queries, Behaviors基盤

src/Domain/{BC}/                     # ドメインプロジェクト（分離）
├── Entities/                        # Product, PurchaseRequest
├── ValueObjects/                    # Money, ProductId
├── Enums/                           # ProductStatus, ApprovalStatus
├── DomainEvents/                    # ProductCreatedEvent
└── Boundaries/                      # ★ バウンダリー（ドメインモデルの一部）
```

---

## 📁 UI配置ルール

### 判断フローチャート

```
Q1: そのページ/コンポーネントは特定の1機能でしか使わないか？
    │
    ├─ Yes → Features/{Feature}/UI/ に配置
    │        例: Login.razor, ProductEdit.razor
    │
    └─ No → Q2へ
         │
         Q2: ページか？コンポーネントか？
         │
         ├─ ページ（@page属性あり）→ Components/Pages/
         │   例: Home.razor, Dashboard.razor
         │
         └─ コンポーネント → Q3へ
              │
              Q3: 特定のBounded Contextに属するか？
              │
              ├─ Yes → Shared/{BC}/UI/Components/
              │        例: StatusBadge.razor
              │
              └─ No → Components/Shared/
                      例: ErrorPage.razor
```

### 配置ルール早見表

| 条件 | 配置場所 | 例 |
|-----|---------|-----|
| **単一機能専用ページ** | `Features/{Feature}/UI/` | Login.razor, ProductDetail.razor |
| **複数機能で使う/基盤ページ** | `Components/Pages/` | Home.razor, Error.razor |
| **BC横断の共有部品** | `Components/Shared/` | ErrorPage.razor |
| **BC内で共有するStore/Actions** | `Shared/{BC}/UI/Store/`, `Actions/` | ProductsStore.cs |
| **BC内で共有するコンポーネント** | `Shared/{BC}/UI/Components/` | StatusBadge.razor |
| **フレームワーク必須** | `Components/Layout/` | MainLayout.razor |

---

## 🔲 バウンダリー（Boundary）の理解

### バウンダリーとは

バウンダリーは **UIではなく、ドメインモデルの一部** です。

```
┌─────────────────────────────────────────────────────────────┐
│                        システム境界                          │
│                                                              │
│   ユーザー ──────────────────────────────────▶ システム     │
│            │                                                 │
│            │  Boundary（バウンダリー）                       │
│            │  = ユーザーがシステムに「意図」を伝える境界     │
│            │  = Command/Query の入口                         │
│            │                                                 │
│            ▼                                                 │
│   ┌─────────────────┐                                       │
│   │ CreateProduct   │  ← ユーザーの意図:「商品を作りたい」  │
│   │ Command         │                                        │
│   └─────────────────┘                                       │
│            │                                                 │
│            ▼                                                 │
│   ┌─────────────────┐                                       │
│   │ Domain Model    │  ← ビジネスルールの実行               │
│   │ (Entity, VO)    │                                        │
│   └─────────────────┘                                       │
└─────────────────────────────────────────────────────────────┘
```

### バウンダリーの特徴

| 特徴 | 説明 |
|-----|------|
| **UIではない** | 画面やコンポーネントではなく、システムへの入口 |
| **ドメインモデルの一部** | `src/Domain/{BC}/Boundaries/` に配置 |
| **意図を伝える** | ユーザーが「何をしたいか」を表現 |
| **Command/Queryで実現** | `CreateProductCommand` = 「商品を作成したい」という意図 |

### 一般的なDDDとの違い

```
一般的なDDD:
  Domain = Entity + ValueObject + DomainService + DomainEvent

このプロジェクト:
  Domain = Entity + ValueObject + DomainService + DomainEvent + Boundary
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
           バウンダリーもドメインモデルに含める
```

---

## 🔄 実装フロー

### 新機能を追加する場合

```
1. catalog/index.json を読む
2. ai_decision_matrix でカテゴリを特定
3. 該当パターンの YAML を読む（catalog/patterns/*.yaml）
4. ai_guidance.common_mistakes を確認
5. テンプレート変数を置換してコード生成
6. UI配置ルールに従ってファイルを配置
7. evidence のファイルパスで実装例を確認
```

### パターン選択の早見表

| ユーザーの要求 | 選択パターン |
|---------------|-------------|
| 「〇〇を作成する機能」 | `feature-create-entity` |
| 「〇〇を検索する画面」 | `feature-search-entity` |
| 「〇〇を編集できるように」 | `feature-update-entity` |
| 「〇〇を削除する」 | `feature-delete-entity` |
| 「CSVインポート」 | `feature-import-csv` |
| 「CSVエクスポート」 | `feature-export-csv` |
| 「承認ワークフロー」 | `feature-approval-workflow` |
| 「すべてのCommandに〇〇」 | pipeline-behavior |

---

## 📊 Pipeline Behavior 実行順序

| 順序 | Behavior | 目的 |
|:---:|----------|------|
| 50 | MetricsBehavior | メトリクス収集 |
| 100 | ValidationBehavior | 入力検証（FluentValidation） |
| 200 | AuthorizationBehavior | 認可チェック |
| 300 | IdempotencyBehavior | 冪等性保証（beta） |
| 400 | TransactionBehavior | トランザクション + SaveChangesAsync |
| 550 | AuditLogBehavior | 監査ログ |
| 600 | LoggingBehavior | リクエストログ |

**重要**: Handler内で SaveChangesAsync を呼ばないこと。TransactionBehavior が自動実行します。

---

## 💻 コーディング規約

### 命名規則

| 対象 | 命名パターン | 例 |
|-----|-------------|-----|
| Command | `{Action}{Entity}Command` | `CreateProductCommand` |
| Query | `{Action}{Entity}Query` | `GetProductsQuery` |
| Handler | `{Command/Query}Handler` | `CreateProductCommandHandler` |
| Validator | `{Command}Validator` | `CreateProductValidator` |
| Store | `{Entity}{Operation}Store` | `ProductsStore` |
| State | `{Entity}{Operation}State` | `ProductsState` |
| PageActions | `{Entity}{Operation}Actions` | `ProductListActions` |

### コードスタイル

- ファイルスコープ名前空間を使用
- クラスは `sealed` を付ける
- レコード型でイミュータブルに
- FluentValidation はルールを宣言的に記述
- 日本語XMLドキュメントコメントを維持

---

## 🧪 テスト

- xUnit + FluentAssertions + Moq
- 命名: `Method_ShouldOutcome_WhenCondition`
- `dotnet test -p:CollectCoverage=true` でカバレッジ確認
- 統合テスト: `Microsoft.AspNetCore.Mvc.Testing`
- E2Eテスト: Playwright

---

## 🔧 開発コマンド

```bash
# ビルド
dotnet build

# 実行
dotnet run --project src/Application/Application.csproj

# テスト
dotnet test

# EFマイグレーション
dotnet ef database update --project src/Application
```

---

## 📝 コミット規約

Conventional Commits を使用:
- `feat:` 新機能
- `fix:` バグ修正
- `docs:` ドキュメント
- `refactor:` リファクタリング
- `test:` テスト
- `chore:` その他

---

## 🔗 クイックリファレンス

- **カタログ**: `catalog/README.md`
- **AI利用ガイド**: `catalog/AI_USAGE_GUIDE.md`
- **意思決定フロー**: `catalog/DECISION_FLOWCHART.md`
- **よくあるミス**: `catalog/COMMON_MISTAKES.md`
- **アーキテクチャ詳細**: `docs/blazor-guide-package/docs/`

---

**カタログバージョン**: v2025.11.24
**最終更新**: 2025-11-24
