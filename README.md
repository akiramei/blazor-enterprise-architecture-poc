# Product Catalog - Blazor Enterprise Architecture 実証実験

このプロジェクトは、**Blazor Enterprise Architecture Guide**に基づいた中規模業務アプリケーションの実証実験です。

## 📋 プロジェクト概要

**Monolithic Vertical Slice Architecture (モノリシックVSA)** を採用し、機能単位で完結する構造により、CQRS、DDD、Storeパターンなどを組み合わせた実践的なアプリケーション設計を示しています。

**アーキテクチャスタイル:**
- ✅ **モノリシック**: 単一Applicationプロジェクトに全機能を集約（YAGNI原則）
- ✅ **VSA**: 機能単位で垂直に分割（Features/{FeatureName}）
- ✅ **DDD**: ドメインモデルは Bounded Context 別に分離（Domain/{BC}）

## ⚡ クイックスタート（5分で動かす）

### 1分で理解する
このプロジェクトは、Blazor + モノリシックVSA + CQRS + DDDを組み合わせたエンタープライズアーキテクチャの実証実験です。

### 動かし方
```bash
# 1. データベース起動
podman run -d --name postgres-productcatalog \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=productcatalog \
  -p 5432:5432 postgres:17

# 2. アプリ実行
cd src/Application
dotnet run

# 3. ブラウザで開く: https://localhost:5001
# 4. ログイン: admin@example.com / Admin@123
```

### 次のステップ
- **アーキテクチャを理解する** → [docs/README.md](docs/README.md)
- **実装を始める** → [19_AIへの実装ガイド](docs/blazor-guide-package/docs/19_AIへの実装ガイド.md)
- **3層アーキテクチャ経験者** → [18_3層アーキテクチャからの移行ガイド](docs/blazor-guide-package/docs/18_3層アーキテクチャからの移行ガイド.md)

---

## 🏗️ アーキテクチャ構成

### モノリシックVSA (Monolithic Vertical Slice Architecture) 構造

```
src/
├── Application/                           # 単一Blazorプロジェクト（モノリシック）
│   ├── Application.csproj                 # すべての機能を含む
│   ├── Program.cs                         # DI登録、パイプライン設定
│   │
│   ├── Features/                          # 機能スライス（19機能）
│   │   ├── CreateProduct/                 # [ProductCatalog] 商品作成
│   │   │   ├── CreateProductCommand.cs
│   │   │   ├── CreateProductCommandHandler.cs
│   │   │   └── UI/
│   │   │       └── Api/Dtos/
│   │   │
│   │   ├── GetProducts/                   # [ProductCatalog] 商品一覧取得
│   │   │   ├── GetProductsQuery.cs
│   │   │   ├── GetProductsQueryHandler.cs
│   │   │   └── UI/
│   │   │       ├── Api/
│   │   │       └── Components/
│   │   │
│   │   ├── UpdateProduct/                 # [ProductCatalog] 商品更新
│   │   ├── DeleteProduct/                 # [ProductCatalog] 商品削除
│   │   ├── GetProductById/                # [ProductCatalog] 商品詳細取得
│   │   ├── SearchProducts/                # [ProductCatalog] 商品検索
│   │   ├── BulkDeleteProducts/            # [ProductCatalog] 一括削除
│   │   ├── BulkUpdateProductPrices/       # [ProductCatalog] 一括価格更新
│   │   ├── ExportProductsToCsv/           # [ProductCatalog] CSV出力
│   │   ├── ImportProductsFromCsv/         # [ProductCatalog] CSV取り込み
│   │   │
│   │   ├── SubmitPurchaseRequest/         # [PurchaseManagement] 購買申請
│   │   ├── ApprovePurchaseRequest/        # [PurchaseManagement] 申請承認
│   │   ├── RejectPurchaseRequest/         # [PurchaseManagement] 申請却下
│   │   ├── CancelPurchaseRequest/         # [PurchaseManagement] 申請キャンセル
│   │   ├── GetPurchaseRequests/           # [PurchaseManagement] 申請一覧取得
│   │   ├── GetPurchaseRequestById/        # [PurchaseManagement] 申請詳細取得
│   │   ├── GetPendingApprovals/           # [PurchaseManagement] 承認待ち一覧
│   │   ├── GetDashboardStatistics/        # [PurchaseManagement] ダッシュボード統計
│   │   └── UploadAttachment/              # [PurchaseManagement] 添付ファイルアップロード
│   │
│   ├── Core/                              # アプリケーションコア
│   │   ├── Commands/                      # 基底Commandインターフェース
│   │   ├── Queries/                       # 基底Queryインターフェース
│   │   └── Behaviors/                     # Pipeline Behaviors（横断的関心事）
│   │       ├── LoggingBehavior.cs
│   │       ├── ValidationBehavior.cs
│   │       ├── AuthorizationBehavior.cs
│   │       ├── TransactionBehavior.cs
│   │       ├── IdempotencyBehavior.cs
│   │       ├── CachingBehavior.cs
│   │       ├── AuditLogBehavior.cs
│   │       └── MetricsBehavior.cs
│   │
│   ├── Components/                        # Blazor Components（Layout/Pages）
│   ├── Hubs/                              # SignalR Hubs
│   ├── Infrastructure/                    # アプリケーション固有インフラ
│   ├── Middleware/                        # ASP.NET Core Middleware
│   ├── Services/                          # アプリケーションサービス
│   ├── Shared/                            # アプリケーション内共通コード
│   └── wwwroot/                           # 静的ファイル
│
├── Domain/                                # ドメインモデル（BC別に分離）
│   ├── ProductCatalog/                    # 商品カタログドメイン
│   │   ├── Domain.ProductCatalog.csproj
│   │   └── Products/                      # Product集約
│   │       ├── Product.cs
│   │       ├── ProductId.cs
│   │       ├── Money.cs
│   │       └── Events/
│   │           └── ProductDeletedEvent.cs
│   │
│   └── PurchaseManagement/                # 購買管理ドメイン
│       ├── Domain.PurchaseManagement.csproj
│       └── PurchaseRequests/              # PurchaseRequest集約
│           ├── PurchaseRequest.cs
│           ├── PurchaseRequestId.cs
│           └── Events/
│
└── Shared/                                # グローバル共通（全BC共有）
    ├── Kernel/                            # ドメイン基底クラス
    │   ├── Shared.Kernel.csproj
    │   ├── Entity.cs
    │   ├── AggregateRoot.cs
    │   ├── ValueObject.cs
    │   ├── DomainEvent.cs
    │   └── DomainException.cs
    │
    ├── Domain/                            # 共通ドメインモデル
    │   ├── Shared.Domain.csproj
    │   ├── Identity/                      # ApplicationUser, Roles
    │   ├── AuditLogs/                     # AuditLog
    │   ├── Idempotency/                   # IdempotencyRecord
    │   └── Outbox/                        # OutboxMessage
    │
    ├── Application/                       # 共通アプリケーション抽象化
    │   ├── Shared.Application.csproj
    │   ├── Interfaces/                    # ICommand, IQuery
    │   ├── Attributes/                    # AuthorizeAttribute等
    │   └── Common/                        # Result, PagedResult
    │
    ├── Abstractions/                      # プラットフォーム抽象化
    │   ├── Shared.Abstractions.csproj
    │   └── Platform/                      # IOutboxReader, IIdempotencyStore
    │
    └── Infrastructure/                    # 共通インフラ実装
        ├── Shared.Infrastructure.csproj
        ├── Authentication/                # JWT生成/検証
        ├── Behaviors/                     # MediatR Pipeline Behaviors実装
        ├── Metrics/                       # ApplicationMetrics
        ├── Services/                      # CurrentUserService等
        └── Platform/                      # プラットフォーム実装
            ├── Shared.Infrastructure.Platform.csproj
            ├── Api/                       # 認証API（AuthController）
            ├── Persistence/               # PlatformDbContext
            ├── Repositories/              # AuditLogRepository等
            └── Stores/                    # AuditLogStore, IdempotencyStore等
```

**モノリシックVSAの特徴:**
- 🎯 **機能ファースト**: 機能（Feature）が最上位の構造単位（Application/Features/{FeatureName}）
- 📦 **モノリシック**: 単一Applicationプロジェクトに全機能を集約（デプロイ・管理が容易）
- 🔀 **垂直統合**: 各機能はCommand/Handler/UIを持ち、共通のDomain/Coreを利用
- 🏛️ **ドメイン分離**: Product集約とPurchaseRequest集約はDomain層で明確に分離
- 🔗 **疎結合**: 機能間の直接依存を禁止（Shared/Domainを経由してのみ共有）
- 📝 **変更容易性**: 機能追加・変更時の影響範囲が明確（1つのFeatureフォルダ内で完結）
- 🧪 **テスタビリティ**: 機能単位でのテストが容易（機能間の依存が最小）

## 🎯 採用パターン

### UI層
- **Smart/Dumb Component分離**: 状態管理と表示の責務分離
- **Store Pattern**: Flux/Redux風の単一状態管理（不変State）
- **PageActions Pattern**: UI手順のオーケストレーション（I/O分離）

### Application層
- **CQRS**: Command/Query責務分離
- **MediatR**: Mediatorパターンによる疎結合
- **Pipeline Behaviors**: 横断的関心事の一元管理

### Domain層
- **Aggregate Pattern**: ビジネスルールの保護
- **Value Object**: 不変な値オブジェクト
- **Domain Event**: ドメイン内イベントの発行

### Infrastructure層
- **Repository Pattern**: 永続化の抽象化
- **EF Core + Dapper**: 書き込みはEF Core、読み取りはDapperで最適化
- **PostgreSQL**: 本番用データベース（自動マイグレーション対応）
- **Outbox Pattern**: 統合イベント配信の信頼性保証
- **ASP.NET Core Identity**: 本番用認証・認可

## 🚀 実行方法

### 前提条件
- .NET 9.0 SDK
- Podman（またはDocker）

### データベースのセットアップ

```bash
# PostgreSQLコンテナを起動（Podman使用）
podman run -d \
  --name postgres-productcatalog \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=productcatalog \
  -p 5432:5432 \
  postgres:17

# データベース接続確認
podman exec -it postgres-productcatalog psql -U postgres -d productcatalog
```

### ビルドと実行

```bash
# ソリューション全体をビルド
dotnet build ProductCatalog.sln

# Webアプリを起動（初回起動時に自動でマイグレーション実行）
cd src/ProductCatalog.Host
dotnet run
```

### 初期ユーザーアカウント

起動時に自動的に以下のテストアカウントが作成されます：

**管理者アカウント:**
- メールアドレス: `admin@example.com`
- パスワード: `Admin@123`
- ロール: Admin

**一般ユーザーアカウント:**
- メールアドレス: `user@example.com`
- パスワード: `User@123`
- ロール: User

ブラウザで `https://localhost:5001` を開き、上記アカウントでログイン後、「商品管理」を選択します（管理者のみアクセス可）。

## 📊 実装機能

### 認証・認可機能
- **ASP.NET Core Identity**: 本番用認証基盤
- **ロールベース認可**: Admin/Userロールによるアクセス制御
- **ログイン/ログアウト**: Cookie認証
- **自動ユーザーシード**: 初回起動時にテストアカウント自動作成

### 商品管理機能
- **商品一覧表示**: サンプルデータ3件を表示（Admin専用）
- **商品削除**: ビジネスルール適用（在庫がある商品は削除不可）
- **リアルタイム状態管理**: Store Patternによる予測可能な状態変更
- **SignalR**: リアルタイム更新通知の基盤（準備済み）

### 横断的機能
- **構造化ログ (Serilog)**: HTTPリクエスト、エラー、実行時間のログ記録
- **CorrelationID**: 分散トレーシングのための一意ID付与
- **グローバルエラーハンドリング**: 未処理例外の一元管理

### サンプルデータ
起動時に以下のサンプルデータが自動的に投入されます：
- ノートパソコン（¥150,000、在庫10）
- ワイヤレスマウス（¥3,500、在庫50）
- USBキーボード（¥12,000、在庫20）

## 🧪 アーキテクチャの特徴

### モノリシックVSAにおける依存関係

**プロジェクト間の依存方向:**
```
Application
  ↓ 参照
Domain (ProductCatalog, PurchaseManagement)
  ↓ 参照
Shared (Kernel, Domain, Application, Infrastructure, Abstractions)
```

**詳細な依存関係:**
```
Application/Features/{FeatureName}/
  ├─ UI (Components, API)
  │   ↓ 依存
  ├─ Command/Query Handler
  │   ↓ 依存
  ├─ Application/Core (Commands, Queries, Behaviors)
  │   ↓ 依存
  ├─ Domain/{BoundedContext} (Product, PurchaseRequest集約)
  │   ↓ 依存
  └─ Shared/Kernel (Entity, AggregateRoot, ValueObject)

Infrastructure層 → Domain層 (依存性逆転の原則)
```

**機能スライス間の依存:**
```
✅ 許可される依存:
Application/Features/CreateProduct → Domain/ProductCatalog → Shared/Kernel
Application/Features/GetProducts   → Domain/ProductCatalog → Shared/Kernel
Application/Features/UpdateProduct → Domain/ProductCatalog → Shared/Kernel

❌ 禁止される依存:
Application/Features/CreateProduct → Application/Features/GetProducts
（機能間の直接依存は禁止。必要ならShared/Domainを経由）
```

**層の責務:**
- **Application/Features/{FeatureName}**: 機能固有のCommand/Handler/UI
- **Application/Core**: 全機能共通のBehaviors（横断的関心事）
- **Domain/{BoundedContext}**: ビジネスルールとドメインモデル（純粋なビジネスロジック）
- **Shared/Kernel**: Entity, AggregateRoot等の基底クラス
- **Shared/Application**: ICommand, IQuery, Result等の抽象化
- **Shared/Infrastructure**: MediatR Behaviors, DbContext等の実装

### 主要な設計判断

1. **モノリシックVSA採用**: 単一Applicationプロジェクト + 機能単位の垂直スライス（YAGNI原則）
2. **ドメインモデル分離**: BC別にDomainプロジェクトを分離（Domain/ProductCatalog, Domain/PurchaseManagement）
3. **グローバル共通の一元化**:
   - `Shared/Kernel`: 基底クラス（Entity, AggregateRoot, ValueObject）
   - `Shared/Domain`: 認証・監査ログ等の共通ドメインモデル
   - `Shared/Application`: ICommand, IQuery, Result等の抽象化
   - `Shared/Infrastructure`: Pipeline Behaviors, DbContext等の実装
4. **Pipeline Behaviors**: 横断的関心事の一元管理（ログ、認可、トランザクション等）
5. **都度スコープ作成**: `IServiceScopeFactory`を使用してDbContextリークを防止
6. **不変State**: `record`による不変状態オブジェクト（Store Pattern）
7. **ビジネスルール保護**: 集約ルートによるルール集約（Product, PurchaseRequest）
8. **I/O分離**: PageActionsはI/Oを持たず、Storeに完全委譲

## 📖 アーキテクチャドキュメント

このプロジェクトには、詳細なアーキテクチャ設計ドキュメントが用意されています。

### 🎯 読者別の推奨スタート地点

#### **3層アーキテクチャ経験者（WPF/WinForms + RESTful Web API）**
最短3時間で学習できる最適パスです：
1. **[3層アーキテクチャからの移行ガイド](docs/blazor-guide-package/docs/18_3層アーキテクチャからの移行ガイド.md)** ← まずはここから！
2. [イントロダクション](docs/blazor-guide-package/docs/01_イントロダクション.md) - 段階的な学習パス参照
3. [具体例: 商品管理機能](docs/blazor-guide-package/docs/08_具体例_商品管理機能.md) - 実装パターン確認

#### **Blazor初心者**
基礎から学びたい方向け（約4.5時間）：
1. [アーキテクチャ概要](docs/blazor-guide-package/docs/03_アーキテクチャ概要.md) - 設計原則
2. [全体アーキテクチャ図](docs/blazor-guide-package/docs/06_全体アーキテクチャ図.md) - データフロー
3. 各層の詳細設計（09-12章）を順番に読む

#### **すぐに実装を始めたい方**
1. [具体例: 商品管理機能](docs/blazor-guide-package/docs/08_具体例_商品管理機能.md) - コードテンプレート
2. [ベストプラクティス](docs/blazor-guide-package/docs/16_ベストプラクティス.md) - よくある落とし穴
3. [テスト戦略](docs/blazor-guide-package/docs/15_テスト戦略.md) - テストの書き方

### 📚 ドキュメント一覧

**目次（全20章）:**
- **[00_README.md](docs/blazor-guide-package/docs/00_README.md)** - 目次と推奨される読み方

**主要な章:**
- [02_このプロジェクトについて](docs/blazor-guide-package/docs/02_このプロジェクトについて.md) - AI駆動開発のための実装パターンカタログ
- [03_アーキテクチャ概要](docs/blazor-guide-package/docs/03_アーキテクチャ概要.md) - 設計原則と3層アーキテクチャとの対応
- [05_パターンカタログ一覧](docs/blazor-guide-package/docs/05_パターンカタログ一覧.md) - 実装済み全パターンの詳細
- [09_UI層の詳細設計](docs/blazor-guide-package/docs/09_UI層の詳細設計.md) - Store/PageActions/Component設計
- [10_Application層の詳細設計](docs/blazor-guide-package/docs/10_Application層の詳細設計.md) - CQRS/MediatR/Pipeline Behaviors
- [13_信頼性パターン](docs/blazor-guide-package/docs/13_信頼性パターン.md) - Outbox/リトライ/エラーハンドリング
- [18_3層アーキテクチャからの移行ガイド](docs/blazor-guide-package/docs/18_3層アーキテクチャからの移行ガイド.md) - WPF/WinForms経験者向け
- [19_AIへの実装ガイド](docs/blazor-guide-package/docs/19_AIへの実装ガイド.md) - AIが正しく実装を生成するための指針

**完全版（単一ファイル - 自動生成）:**
- [BLAZOR_ARCHITECTURE_GUIDE_COMPLETE.md](docs/blazor-guide-package/BLAZOR_ARCHITECTURE_GUIDE_COMPLETE.md) - 全章を結合した完全版（章別ファイルから自動生成）

## ✅ 実装済みの高度な機能

### Pipeline Behaviors（横断的関心事の一元管理）
- **LoggingBehavior**: 全リクエストの実行時間とエラーをログ出力
- **ValidationBehavior**: FluentValidationによる入力検証
- **AuthorizationBehavior**: ロール・ポリシーベースの認可
- **IdempotencyBehavior**: 冪等性保証（InMemoryストア）
- **CachingBehavior**: Query結果のメモリキャッシュ（ユーザー/テナント分離）
- **TransactionBehavior**: トランザクション境界管理とドメインイベント配信

### Store並行制御（高度なパターン）
- **Single-flight Pattern**: 同一リクエストの自動合流
- **Versioning Pattern**: 連打対策のバージョン管理
- **CancellationToken管理**: 古い処理の自動キャンセル

### その他の実装
- **ICurrentUserService**: 現在のユーザー情報管理
- **Result型**: エラーハンドリングパターン
- **不変State**: `record`による予測可能な状態管理

## ⚠️ 注意事項

このプロジェクトは実証実験用です。本番環境で使用する場合は、以下を追加実装してください：

### ✅ 実装済みの機能

#### P0: コアアーキテクチャ
- [x] Pipeline Behaviors（横断的関心事）
- [x] CQRS（Command/Query分離）
- [x] DDD（Domain-Driven Design）
- [x] Result型（エラーハンドリング）

#### P1: 重要度が高い機能
- [x] Smart/Dumb Component分離（コンポーネント再利用性の向上）
- [x] SignalR（リアルタイム更新通知の基盤）
- [x] Dapper統合（読み取りクエリの最適化）
- [x] テストコード（Unit/Integration tests: 21テスト実装済み）

#### P2: 本番運用に必要な機能
- [x] Outbox Pattern（統合イベント配信の信頼性向上）
- [x] CorrelationIdトラッキング（分散トレーシング）
- [x] 構造化ログ（Serilog）
- [x] 本番用認証・認可（ASP.NET Core Identity）
- [x] 本番用データベース（PostgreSQL）
- [x] エラーハンドリングの強化（グローバルエラーハンドラ）
- [x] 自動マイグレーション（起動時にDatabase.MigrateAsync実行）

### 推奨される追加機能

#### 機能拡張
- [ ] 商品作成・更新機能
- [ ] 在庫管理機能の拡張
- [ ] マルチテナント対応
- [ ] API公開（REST/GraphQL）

#### 運用強化
- [ ] ヘルスチェックエンドポイント
- [ ] メトリクス収集（OpenTelemetry等）
- [ ] レート制限（Rate Limiting）
- [ ] API バージョニング

## 📝 ライセンス

このプロジェクトは実証実験用のサンプルコードです。
