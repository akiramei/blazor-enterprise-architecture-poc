# Product Catalog - Blazor Enterprise Architecture 実証実験

このプロジェクトは、**Blazor Enterprise Architecture Guide**に基づいた中規模業務アプリケーションの実証実験です。

## 📋 プロジェクト概要

**Vertical Slice Architecture (VSA)** を採用し、機能単位で完結する構造により、CQRS、DDD、Storeパターンなどを組み合わせた実践的なアプリケーション設計を示しています。

## 🏗️ アーキテクチャ構成

### VSA (Vertical Slice Architecture) 構造

```
src/
├── ProductCatalog/                    # Bounded Context
│   ├── Features/                      # 機能スライス（Feature Slices）
│   │   ├── CreateProduct/             # 機能1: 商品作成
│   │   │   ├── Application/           # Command/Handler/Validator
│   │   │   ├── Domain/                # ドメインロジック（この機能固有）
│   │   │   ├── Infrastructure/        # Repository実装（この機能固有）
│   │   │   └── UI/                    # Component/Page（この機能固有）
│   │   ├── GetProducts/               # 機能2: 商品一覧取得
│   │   │   ├── Application/
│   │   │   └── Infrastructure/
│   │   ├── DeleteProduct/             # 機能3: 商品削除
│   │   │   └── Application/
│   │   └── UpdateProduct/             # 機能4: 商品更新
│   │       └── Application/
│   │
│   └── Shared/                        # 機能横断の共通コード
│       ├── Application/               # 共通インターフェース（ICommand, IQuery, Result）
│       ├── Domain/                    # 共通ドメイン（Product集約、ValueObject）
│       ├── Infrastructure/            # 共通インフラ（DbContext, Behaviors）
│       └── UI/                        # 共通UIコンポーネント
│           ├── Store/                 # Store Pattern（状態管理）
│           ├── Actions/               # PageActions Pattern（UI手順）
│           ├── Pages/                 # Smart Components
│           └── Components/            # Dumb Components
│
└── ProductCatalog.Host/               # Blazor Server ホストプロジェクト
    ├── Program.cs                     # DI登録、パイプライン設定
    └── Infrastructure/                # 認証・認可、グローバルサービス
```

**VSAの特徴:**
- **機能ファースト**: 機能（Feature）が最上位の構造単位
- **垂直統合**: 各機能が UI → Application → Domain → Infrastructure を含む完結した垂直スライス
- **疎結合**: 機能間の依存を最小化（Shared経由でのみ共有）
- **変更容易性**: 機能追加・変更時の影響範囲が明確

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
- パスワード: `Admin@123456`
- ロール: Admin

**一般ユーザーアカウント:**
- メールアドレス: `user@example.com`
- パスワード: `User@123456`
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

### VSAにおける依存関係

**機能スライス内の依存方向:**
```
UI → Application → Domain ← Infrastructure
```

- **UI層**: Application層のCommand/Queryを呼び出し
- **Application層**: Domain層に依存（ビジネスルール適用）
- **Domain層**: 他の層に依存しない（ピュアなビジネスロジック）
- **Infrastructure層**: Domain層とApplication層に依存（依存性逆転の原則）

**機能スライス間の依存:**
```
各Feature → Shared（共通コード）のみ依存可能
Feature間の直接依存は禁止
```

### 主要な設計判断

1. **VSA採用**: 機能単位の垂直スライスによる高凝集・疎結合
2. **都度スコープ作成**: `IServiceScopeFactory`を使用してDbContextリークを防止
3. **不変State**: `record`による不変状態オブジェクト
4. **ビジネスルール保護**: Product集約によるルール集約
5. **I/O分離**: PageActionsはI/Oを持たず、Storeに完全委譲

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
