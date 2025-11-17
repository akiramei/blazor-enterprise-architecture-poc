# Product Catalog - Blazor Enterprise Architecture 実証実験

このプロジェクトは、**Blazor Enterprise Architecture Guide**に基づいた中規模業務アプリケーションの実証実験です。

## 📋 プロジェクト概要

**Vertical Slice Architecture (VSA)** を採用し、機能単位で完結する構造により、CQRS、DDD、Storeパターンなどを組み合わせた実践的なアプリケーション設計を示しています。

## 🏗️ アーキテクチャ構成

### モノリシックVSA (Vertical Slice Architecture) 構造

```
src/
├── Application/                       # 単一Blazor Server プロジェクト
│   ├── Application.csproj
│   ├── Program.cs                     # DI登録、パイプライン設定
│   │
│   ├── Features/                      # 機能スライス（19機能）
│   │   ├── CreateProduct/             # 機能1: 商品作成
│   │   │   ├── CreateProductCommand.cs
│   │   │   ├── CreateProductCommandHandler.cs
│   │   │   └── UI/
│   │   │       └── Api/
│   │   │           └── Dtos/
│   │   ├── GetProducts/               # 機能2: 商品一覧取得
│   │   │   ├── GetProductsQuery.cs
│   │   │   ├── GetProductsQueryHandler.cs
│   │   │   └── UI/
│   │   │       ├── Api/
│   │   │       ├── Components/
│   │   │       └── ProductList.razor
│   │   ├── DeleteProduct/             # 機能3: 商品削除
│   │   ├── UpdateProduct/             # 機能4: 商品更新
│   │   ├── BulkDeleteProducts/        # 機能5: 一括削除
│   │   ├── BulkUpdateProductPrices/   # 機能6: 一括価格更新
│   │   ├── ExportProductsToCsv/       # 機能7: CSV出力
│   │   ├── ImportProductsFromCsv/     # 機能8: CSV取り込み
│   │   ├── GetProductById/            # 機能9: 商品詳細取得
│   │   ├── SearchProducts/            # 機能10: 商品検索
│   │   ├── SubmitPurchaseRequest/     # 機能11: 購買申請
│   │   ├── ApprovePurchaseRequest/    # 機能12: 購買承認
│   │   └── ... (全19機能)
│   │
│   ├── Shared/                        # BC別の共通コード
│   │   ├── ProductCatalog/            # 商品カタログBC共通
│   │   │   ├── Application/
│   │   │   │   └── DTOs/              # ProductDto等
│   │   │   ├── Infrastructure/
│   │   │   │   └── Persistence/
│   │   │   │       ├── ProductCatalogDbContext.cs
│   │   │   │       ├── Configurations/
│   │   │   │       └── Repositories/
│   │   │   └── UI/
│   │   │       ├── Actions/           # ProductListActions等
│   │   │       └── Store/             # ProductsStore等
│   │   └── PurchaseManagement/        # 購買管理BC共通
│   │       ├── Application/
│   │       ├── Domain/
│   │       └── Infrastructure/
│   │
│   ├── Core/                          # アプリケーション基盤
│   │   ├── Commands/                  # CommandPipeline
│   │   ├── Queries/                   # QueryPipeline
│   │   └── Behaviors/                 # GenericTransactionBehavior
│   │
│   ├── Components/                    # Blazor共通コンポーネント
│   │   ├── Layout/
│   │   ├── Pages/
│   │   └── ErrorBoundary/
│   │
│   ├── Infrastructure/                # インフラ層
│   │   ├── Models/                    # アプリケーション状態
│   │   ├── Services/                  # LocalStorage等
│   │   └── Stores/                    # Fluxパターン状態管理
│   │
│   ├── Hubs/                          # SignalR Hubs
│   ├── Jobs/                          # Hangfire バックグラウンドジョブ
│   ├── Middleware/                    # ASP.NET Core ミドルウェア
│   └── Services/                      # アプリケーションサービス
│
├── Domain/                            # ドメインプロジェクト（分離）
│   ├── ProductCatalog/
│   │   ├── Domain.ProductCatalog.csproj
│   │   ├── Products/
│   │   │   ├── Product.cs             # Product集約
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
└── Shared/                            # グローバル共通コード（全BC共有）
    ├── Kernel/                        # ドメイン基底クラス
    │   ├── Shared.Kernel.csproj
    │   ├── Entity.cs
    │   ├── AggregateRoot.cs
    │   ├── ValueObject.cs
    │   └── DomainEvent.cs
    │
    ├── Domain/                        # ドメイン共通
    │   ├── Shared.Domain.csproj
    │   ├── Identity/                  # ApplicationUser, Roles
    │   ├── AuditLogs/
    │   ├── Idempotency/
    │   └── Outbox/
    │
    ├── Application/                   # アプリケーション共通
    │   ├── Shared.Application.csproj
    │   ├── Interfaces/                # ICommand, IQuery
    │   ├── Common/                    # Result, PagedResult
    │   └── Attributes/
    │
    ├── Infrastructure/                # インフラ共通
    │   ├── Shared.Infrastructure.csproj
    │   ├── Authentication/            # JWT生成/検証
    │   ├── Behaviors/                 # MediatR Pipeline Behaviors
    │   ├── Caching/
    │   └── Services/                  # CurrentUserService等
    │
    ├── Infrastructure.Platform/       # プラットフォーム共通
    │   ├── Shared.Infrastructure.Platform.csproj
    │   ├── Outbox/
    │   ├── Persistence/
    │   └── Stores/
    │
    └── Abstractions/                  # 抽象化共通
        ├── Shared.Abstractions.csproj
        └── Interfaces/
```

**モノリシックVSAの特徴:**
- **単一プロジェクト**: Application/配下に全機能を集約（YAGNI原則）
- **機能ファースト**: 機能（Feature）が最上位の構造単位
- **垂直統合**: 各機能は Command/Query/Handler + UI を持つ完結した垂直スライス
- **BC別共通化**: Application/Shared/{BC}/に各BCの共通コード（Store, Actions, DbContext等）
- **ドメイン分離**: Domain/はプロジェクトとして分離（純粋なビジネスロジック）
- **疎結合**: 機能間の依存を最小化（Shared経由でのみ共有）
- **変更容易性**: 機能追加・変更時の影響範囲が明確（1つのFeatureフォルダ内で完結）

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
dotnet build

# Webアプリを起動（初回起動時に自動でマイグレーション実行）
cd src/Application
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
- **ログイン/ログアウト**: Cookie認証（Blazor Server）、JWT Bearer認証（REST API）
- **二要素認証（2FA）**: TOTP（Time-based One-Time Password）対応
  - QRコードによる認証アプリ登録（Google Authenticator/Microsoft Authenticator等）
  - リカバリーコード（10個、BCryptハッシュ化）
  - 2FA有効化/無効化機能
  - ログイン時の2FA検証（TOTP/リカバリーコード対応）
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

**機能スライス内の依存方向:**
```
Application/Features/{Feature}/UI
  ↓
Application/Features/{Feature}/CommandHandler
  ↓
Domain/{BC}/ (例: Domain/ProductCatalog/Products/)
  ↑
Application/Shared/{BC}/Infrastructure/Repositories/
```

- **Feature/UI層**: MediatR経由でCommand/Queryを送信
- **Feature/CommandHandler**: Domain層のAggregateを操作、Repositoryで永続化
- **Domain層**: 他の層に依存しない（純粋なビジネスロジック）
- **Application/Shared/Infrastructure**: Domain層に依存（依存性逆転の原則）

**機能スライス間の依存:**
```
Application/Features/CreateProduct → Application/Shared/ProductCatalog/
Application/Features/GetProducts   → Application/Shared/ProductCatalog/
Application/Features/UpdateProduct → Application/Shared/ProductCatalog/

Feature間の直接依存は禁止（例: CreateProduct → GetProducts ❌）
```

**プロジェクト間の依存:**
```
Application/
  ↓ 依存
Domain/{BC}/ (ProductCatalog, PurchaseManagement)
  ↓ 依存
Shared/ (Kernel, Domain, Application, Infrastructure, Platform, Abstractions)
```

### 主要な設計判断

1. **モノリシックVSA採用**: 単一Applicationプロジェクト + 機能単位の垂直スライス（YAGNI原則）
2. **ドメイン分離**: `Domain/`プロジェクトのみ分離し、純粋なビジネスロジックを保護
3. **BC別共通化**: `Application/Shared/{BC}/`で各BCの共通コード（Store, DbContext等）を管理
4. **グローバル共通**: `Shared/`プロジェクトで全BC共有のKernel/Application/Infrastructure
5. **都度スコープ作成**: `IServiceScopeFactory`を使用してDbContextリークを防止
6. **不変State**: `record`による不変状態オブジェクト
7. **ビジネスルール保護**: Aggregate RootによるDomain層でのルール集約
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

### セキュリティ機能
- **二要素認証（2FA）**: TOTP + リカバリーコード
  - TOTP秘密鍵生成とQRコード表示
  - リカバリーコード生成（BCryptハッシュ化でDB保存）
  - ログイン時の2FA検証フロー
  - REST API対応（JWT Bearer認証）
- **JWT Bearer認証**: REST APIクライアント向け
  - Access Token（15分）+ Refresh Token（7日）
  - Refresh Token Rotation（セキュリティ強化）
- **アカウントロックアウト**: 5回失敗で5分間ロック

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
- [x] 二要素認証（2FA）- TOTP + リカバリーコード
- [x] JWT Bearer認証（REST API向け）
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
