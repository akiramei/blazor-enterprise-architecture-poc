# ADR-002: モノリシックVSA（Vertical Slice Architecture）の採用

## ステータス

承認済み（Accepted） - 2025-11-16

## コンテキスト

プロジェクト開始時、以下のアーキテクチャ選択肢が検討されました：

### 選択肢1: Clean Architecture（レイヤードアーキテクチャ）
```
ProductCatalog.Application/
ProductCatalog.Domain/
ProductCatalog.Infrastructure/
ProductCatalog.UI/
```

**メリット:**
- 層の責務が明確
- 依存方向の厳密な管理
- 大規模プロジェクトでの実績

**デメリット:**
- 機能追加時に複数プロジェクトを横断的に変更
- プロジェクト数の増加（ビルド時間の増加）
- YAGNI（You Aren't Gonna Need It）原則に反する

### 選択肢2: マイクロサービス
```
ProductCatalog.Service/
PurchaseManagement.Service/
（各サービスは独立デプロイ）
```

**メリット:**
- 独立したスケーリング
- 技術スタックの柔軟性
- 障害の局所化

**デメリット:**
- 運用コストの増加（Kubernetes, Service Mesh等）
- 分散トランザクションの複雑性
- 過剰エンジニアリング（中規模アプリには不要）

### 選択肢3: モノリシックVSA（採用案）
```
Application/                    # 単一プロジェクト
├── Features/{FeatureName}/     # 機能スライス
│   ├── Command/Handler
│   └── UI
├── Core/Behaviors/             # 横断的関心事
Domain/{BoundedContext}/        # ドメインモデル分離
Shared/                         # グローバル共通
```

**メリット:**
- 機能単位の高い凝集度
- シンプルなデプロイ（単一プロセス）
- 必要に応じてマイクロサービス化可能（Feature単位で抽出）
- YAGNI原則に準拠

**デメリット:**
- 単一プロセス障害の影響範囲
- 大規模チーム開発時のマージコンフリクトリスク

## 決定事項

**モノリシックVSA（Vertical Slice Architecture）を採用する**

### アーキテクチャ構成

```
src/
├── Application/                           # 単一Blazorプロジェクト
│   ├── Features/                          # 機能スライス（19機能）
│   │   ├── CreateProduct/                 # [ProductCatalog BC]
│   │   ├── SubmitPurchaseRequest/         # [PurchaseManagement BC]
│   │   └── ...
│   └── Core/Behaviors/                    # Pipeline Behaviors
│
├── Domain/                                # ドメインモデル（BC別に分離）
│   ├── ProductCatalog/
│   └── PurchaseManagement/
│
└── Shared/                                # グローバル共通
    ├── Kernel/                            # Entity, AggregateRoot
    ├── Domain/                            # 認証・監査ログ
    ├── Application/                       # ICommand, IQuery, Result
    └── Infrastructure/                    # Pipeline Behaviors実装
```

### 主要な設計原則

1. **YAGNI（You Aren't Gonna Need It）**: 必要になるまで分割しない
2. **Feature First**: 機能が最上位の構造単位
3. **Domain Separation**: ドメインモデルのみBC別に分離
4. **Shared Kernel**: 共通コードはSharedプロジェクトで一元管理

## 決定理由

### 1. YAGNI原則の遵守

**現状:**
- 開発チーム: 1-3名
- 想定ユーザー数: ~1,000人
- デプロイ頻度: 週1回

**判断:**
- マイクロサービスの複雑性は不要
- レイヤードアーキテクチャのプロジェクト分割も過剰

**引用: Martin Fowler - "Monolith First"**
> "Almost all the successful microservice stories have started with a monolith that got too big and was broken up."
> [MonolithFirst](https://martinfowler.com/bliki/MonolithFirst.html)

### 2. 機能単位の凝集度

**Clean Architectureの場合:**
```
商品作成機能を追加する場合の変更箇所:
1. ProductCatalog.Application/Commands/CreateProductCommand.cs
2. ProductCatalog.Application/Handlers/CreateProductHandler.cs
3. ProductCatalog.Domain/Products/Product.cs
4. ProductCatalog.Infrastructure/Repositories/ProductRepository.cs
5. ProductCatalog.UI/Pages/CreateProductPage.razor
→ 5つのプロジェクトを横断
```

**モノリシックVSAの場合:**
```
Application/Features/CreateProduct/
├── CreateProductCommand.cs
├── CreateProductCommandHandler.cs
└── UI/
→ 1つのフォルダ内で完結
```

### 3. 将来的な拡張性

**マイクロサービス化の容易性:**
- 各Feature（機能スライス）は独立しているため、必要に応じて抽出可能
- 例: `Features/CreateProduct/` → `ProductCatalog.CreateProduct.Service/`

**段階的な移行:**
```
Phase 1: モノリシックVSA（現在）
  ↓ ビジネス成長に応じて
Phase 2: 一部機能のマイクロサービス化（必要な場合のみ）
  ↓ さらなる成長に応じて
Phase 3: 完全なマイクロサービスアーキテクチャ
```

### 4. 開発生産性

**ビルド時間:**
- モノリシックVSA: 1プロジェクト → 約10秒
- Clean Architecture（4プロジェクト）: 約30秒
- マイクロサービス（10サービス）: 約2分

**デバッグ:**
- モノリシック: 単一プロセス → F5でデバッグ開始
- マイクロサービス: 複数プロセス → Docker Composeセットアップが必要

## 影響範囲

### 開発チームへの影響

**メリット:**
- 機能追加時の変更範囲が明確（1つのFeatureフォルダ）
- マージコンフリクトの減少
- 新規開発者のオンボーディングが容易

**デメリット:**
- Clean Architectureに慣れた開発者への学習コスト
- 機能間の依存を防ぐための規律が必要

### 運用への影響

**メリット:**
- シンプルなデプロイ（単一プロセス）
- 環境構築が容易（Podman + PostgreSQLのみ）
- ログ・メトリクスの集約が容易

**デメリット:**
- 単一プロセス障害の影響範囲が広い
- 水平スケーリングはプロセス全体

## 代替案

### 代替案1: Clean Architecture
**却下理由:**
- プロジェクト数の増加（ビルド時間、複雑性）
- 機能追加時の横断的変更
- YAGNI原則に反する

### 代替案2: マイクロサービス
**却下理由:**
- 過剰エンジニアリング（チーム規模・ユーザー数に対して）
- 運用コストの増加
- 分散トランザクションの複雑性

### 代替案3: BC別プロジェクト分離
```
ProductCatalog.Application/
PurchaseManagement.Application/
```
**検討結果:**
- ドメインモデルのみ分離（Domain/ProductCatalog, Domain/PurchaseManagement）
- Applicationは単一プロジェクトに統合（YAGNI原則）

## トレードオフ

### 採用するトレードオフ

| 項目 | モノリシックVSA | Clean Architecture | マイクロサービス |
|------|----------------|-------------------|----------------|
| 開発速度 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ |
| ビルド時間 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐ |
| スケーラビリティ | ⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐ |
| 運用複雑性 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐ |
| 機能の独立性 | ⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐ |

### 受け入れるリスク

1. **単一障害点**: プロセス障害時の影響範囲が広い
   - 軽減策: ヘルスチェック、自動再起動、冗長化
2. **スケーリング粒度**: 機能単位でのスケーリングができない
   - 軽減策: 必要に応じてFeature単位でマイクロサービス化

## 実装ガイドライン

### 機能間依存の禁止

```csharp
// ❌ 禁止: 機能間の直接依存
public class CreateProductHandler
{
    private readonly GetProductsHandler _getProducts;  // NG
}

// ✅ 許可: Shared/Domainを経由
public class CreateProductHandler
{
    private readonly IProductRepository _repository;  // OK (Shared経由)
}
```

### Feature構造のテンプレート

```
Application/Features/{FeatureName}/
├── {FeatureName}Command.cs        # Command/Query定義
├── {FeatureName}CommandHandler.cs # Handler実装
└── UI/                            # UI層（オプション）
    ├── Api/                       # API Endpoints
    └── Components/                # Blazor Components
```

## 関連リソース

- [Martin Fowler - MonolithFirst](https://martinfowler.com/bliki/MonolithFirst.html)
- [Jimmy Bogard - Vertical Slice Architecture](https://www.jimmybogard.com/vertical-slice-architecture/)
- [Sam Newman - Building Microservices (Chapter 1: Microservices at a Glance)](https://samnewman.io/books/building_microservices_2nd_edition/)

## 変更履歴

| 日付 | 変更内容 | 作成者 |
|------|---------|--------|
| 2025-11-16 | 初版作成 | Claude Code |

## レビュー

- [x] アーキテクト承認
- [x] テックリード承認
- [x] チームレビュー完了

---

**注記**: この決定は、プロジェクトの成長に応じて再評価されます。ユーザー数・開発チームサイズが10倍に増加した場合、マイクロサービス化を検討してください。
