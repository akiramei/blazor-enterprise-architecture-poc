# CLAUDE.md - AI Implementation Instructions

このファイルは Claude Code および Claude AI エージェントが自動的に読み込む設定ファイルです。

> **このファイルについて**:
> このCLAUDE.mdは「参照されるガイド」です。
> 別プロジェクトのCLAUDE.mdから参照して使用します。
>
> **推奨**: 初めて読む場合は、まず `LLM_BOOTSTRAP.md` を読んでください。

> **詳細版**: より詳しい情報は `AGENTS.md` を参照してください。

---

## 🚨 CRITICAL: 実装前に必ず読むこと

**このプロジェクトはカタログ駆動開発を採用しています。独自実装は禁止です。**

### ⚠️ 計画フェーズが必要か判断する（最初に確認）

**以下の条件に1つでも該当する場合、計画フェーズを先に実行すること：**

| 条件 | 該当例 |
|-----|-------|
| ✅ 新規機能の追加 | 「〇〇機能を作って」「〇〇画面を追加」 |
| ✅ 3ファイル以上の作成が予想される | Command + Handler + Validator + UI など |
| ✅ 複数パターンの組み合わせが必要 | CRUD + 状態遷移 + 重複チェック |
| ✅ UIがある | 画面、フォーム、一覧表示など |
| ✅ ドメイン固有の業務ロジック | 貸出ルール、予約ルール、承認フローなど |
| ✅ 非機能要件の考慮が必要 | 監査ログ、同時実行制御、キャッシュなど |

```
該当する場合 → catalog/planning/PLANNING_GUIDE.md を先に読む
該当しない場合 → 下記の実装ガイドに進む
```

### 計画をスキップできる条件

以下の場合のみ、計画フェーズをスキップして直接実装可能：

```
✅ 単一ファイルの修正（バグ修正、リファクタリング）
✅ 既存機能への小さな変更
✅ テンプレート変数の置換のみで完了する実装
✅ ドキュメントの更新
```

---

### 読み込み優先順位（必須）

| 順序 | ファイル | 目的 | 必須度 |
|:---:|----------|------|:------:|
| 0 | `LLM_BOOTSTRAP.md` | AIの入口（超要約版） | **推奨** |
| 1 | `catalog/LLM_PATTERN_INDEX.md` | パターン選択早見表（Markdown版） | **推奨** |
| 2 | `catalog/AI_USAGE_GUIDE.md` | 実装ルール・制約・UI配置ルール | **必須** |
| 3 | `catalog/index.json` | パターン索引・意思決定マトリクス | **必須** |
| 4 | `catalog/COMMON_MISTAKES.md` | 頻出ミスと回避方法 | **必須** |
| 5 | `catalog/DECISION_FLOWCHART.md` | パターン選択アルゴリズム | 推奨 |

### 基盤パターン（Kernel）

| ファイル | 目的 | 必須度 |
|----------|------|:------:|
| `catalog/kernel/result-pattern.yaml` | エラーハンドリング基盤（Result<T>） | **必須** |
| `catalog/kernel/value-object.yaml` | 値オブジェクト基底クラス | 参照 |
| `catalog/kernel/entity-base.yaml` | エンティティ基底クラス | 参照 |

**これらを読まずに実装を開始してはいけません。**

---

## 🚨 計画フェーズ: Boundaryモデリング必須化（CRITICAL）

**UIがある機能を計画する際、Boundaryモデリングを忘れる問題が頻発しています。**

### AIの学習バイアス問題

```
古典的DDDはUIを対象外とするため、AIの学習データには
「Boundaryをモデリングする」という発想がほとんど含まれていません。

これは「Boundaryが重要」という説明では解決できません。
成果物（計画書・仕様書）で強制する必要があります。
```

### 計画フェーズの必須アクション

| 条件 | 必須アクション |
|------|--------------|
| UIがある | `boundary-pattern.yaml` を読む → Boundaryセクションを計画に含める |
| ユーザー対話がある | Intent（ユーザーの意図）を列挙 |
| 操作可否判定がある | Entity.CanXxx() を設計 |

### 計画が不完全とみなされる条件

```
❌ UIがあるのに Boundary セクションがない → 不完全
❌ Intent が定義されていない → 不完全
❌ Entity.CanXxx() の設計がない → 不完全
❌ 「後から Boundary を追加する」という計画 → 不完全
❌ boundary-pattern.yaml を読まずに計画を立てた → 不完全
```

### 仕様書テンプレート

> **参照**: `catalog/scaffolds/spec-template.yaml`

仕様書を作成する際は、このテンプレートを使用してください。
`boundary` セクションが必須フィールドとして定義されています。

---

## ⚖️ 実装優先順位（CRITICAL）

**カタログのルールとフレームワークの慣習が矛盾する場合は、必ずカタログを優先する。**

| 順位 | 優先度 | 説明 |
|:---:|--------|------|
| 1 | **最優先** | カタログのアーキテクチャルール |
| 2 | 次点 | フレームワーク（Blazor等）のベストプラクティス |
| 3 | 最後 | 一般的な慣習 |

### 具体例

```
❌ Blazorの慣習: Pages/ にルーティング可能ページを配置
✅ カタログの規則: 機能固有ページは Features/{Feature}/ に配置

→ カタログの規則を優先する
```

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

❌ 機能固有の.razorをComponents/Pages/に配置しない
   → Features/{Feature}/ に .cs と同列配置
```

---

## ✅ 必須パターン

### アーキテクチャ

- **Vertical Slice Architecture (VSA)**: 機能単位で垂直統合
- **CQRS**: Command/Query責務分離（MediatR使用）
- **Pipeline Behaviors**: 横断的関心事の自動実行

### 実装規約

```csharp
// Command の戻り値は必ず Result<T>
public sealed record CreateProductCommand(...) : ICommand<Result<Guid>>;

// Handler は SaveChangesAsync を呼ばない（TransactionBehavior が自動実行）
public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
{
    var entity = new Product(...);
    await _repository.AddAsync(entity, ct);
    return Result.Success(entity.Id);  // SaveChangesAsync は呼ばない！
}

// サービスのライフタイムは Scoped
services.AddScoped<IProductRepository, ProductRepository>();
```

---

## 🎯 パターン選択の早見表

### 機能スライス（Feature Slices）

| ユーザーの要求 | 選択パターン |
|---------------|-------------|
| 「〇〇を作成する機能」 | `feature-create-entity` |
| 「〇〇を検索する画面」 | `feature-search-entity` |
| 「〇〇を編集できるように」 | `feature-update-entity` |
| 「〇〇を削除する」 | `feature-delete-entity` |
| 「CSVインポート」 | `feature-import-csv` |
| 「CSVエクスポート」 | `feature-export-csv` |
| 「承認ワークフロー」 | `feature-approval-workflow` |
| 「ログイン・認証」 | `feature-authentication` |

### クエリパターン（Query Patterns）

| ユーザーの要求 | 選択パターン |
|---------------|-------------|
| 「一覧を取得」 | `query-get-list` |
| 「IDで詳細を取得」 | `query-get-by-id` |
| 「今日/今週/期間で取得」 | `query-get-by-period` |
| 「空き検索・NOT EXISTS」 | `complex-query-service` |

### ドメインパターン（Domain Patterns）

| ユーザーの要求 | 選択パターン |
|---------------|-------------|
| 「時間枠・予約時間の管理」 | `domain-timeslot` |
| 「型安全ID（BookingId等）」 | `domain-typed-id` |
| 「重複チェック・在庫確認」 | `domain-validation-service` |
| 「状態遷移の管理」 | `domain-state-machine` |
| 「操作可否の判定」 | `boundary-pattern` |

### 横断的関心事（Pipeline Behaviors）

| ユーザーの要求 | 選択パターン |
|---------------|-------------|
| 「すべてのCommandに〇〇」 | 各 `*-behavior` |
| 「キャッシュで高速化」 | `caching-behavior` |
| 「状態管理」 | `layer-store` |

**判断に迷った場合**: `feature-slice` をデフォルトで選択

---

## 📂 プロジェクト構造

> **詳細は `catalog/scaffolds/project-structure.yaml` を参照**

```
src/
├── Kernel/                           # DDD基盤（Entity, ValueObject, AggregateRoot）
├── Domain/{BC}/                      # BC固有ドメイン（Aggregate単位でフォルダ分け）
│   ├── {Aggregate}/
│   └── Boundaries/
├── Shared/
│   ├── Application/                  # ICommand, IQuery, Result<T>（BC非依存）
│   └── Infrastructure/               # Behaviors（BC非依存）
└── Application/
    ├── Features/{Feature}/           # VSA機能スライス
    │   ├── {Feature}Command.cs
    │   ├── {Feature}CommandHandler.cs
    │   └── {Feature}.razor           # ★ 機能固有UI（同列配置）
    ├── Infrastructure/{BC}/          # ★ BC固有インフラ（DbContext, Repository実装）
    └── Components/                   # Blazorテンプレート由来
        ├── Pages/                    # 複数機能で使う基盤ページ
        └── Shared/                   # BC横断の共有コンポーネント
```

---

## 🔄 実装フロー

```
1. catalog/index.json を読む
2. ai_decision_matrix でカテゴリを特定
3. 該当パターンの YAML を読む
4. ai_guidance.common_mistakes を確認
5. テンプレート変数を置換してコード生成
6. UI配置ルールに従ってファイルを配置
7. evidence のファイルパスで実装例を確認
```

---

## 📋 推奨実装順序（機能スライス単位）

**1つの機能を完了してから次の機能へ進む。** これにより、UIを作成する時点で「この.razorはどの機能の一部か」が明確になる。

```
┌─────────────────────────────────────────────────────────┐
│ Feature: Products                                       │
├─────────────────────────────────────────────────────────┤
│ 1. Domain Entity/ValueObject                            │
│    └── Product.cs, ProductId.cs                         │
│                                                         │
│ 2. Repository Interface                                 │
│    └── IProductRepository.cs                            │
│                                                         │
│ 3. Command/Query + Handler                              │
│    └── CreateProductCommand.cs                          │
│    └── CreateProductCommandHandler.cs                   │
│                                                         │
│ 4. Validator                                            │
│    └── CreateProductCommandValidator.cs                 │
│                                                         │
│ 5. UI (.razor) ★ 同じFeatureフォルダに配置             │
│    └── Features/CreateProduct/CreateProduct.razor       │
│                                                         │
│ 6. Infrastructure (Repository実装)                      │
│    └── ProductRepository.cs                             │
└─────────────────────────────────────────────────────────┘
          ↓ 完了後に次の機能へ
┌─────────────────────────────────────────────────────────┐
│ Feature: Orders                                         │
│ ...                                                     │
└─────────────────────────────────────────────────────────┘
```

### なぜこの順序か？

| 問題のある順序 | 推奨順序 |
|--------------|---------|
| バックエンドを全部作る → UIを後から追加 | 機能ごとにUI含めて完了 |
| UIが既存構造（Pages/）に引きずられる | UI作成時に機能の文脈が明確 |

---

## 📁 UI配置ルール（要約）

> **詳細は `catalog/scaffolds/project-structure.yaml` を参照**

| 条件 | 配置場所 |
|-----|---------|
| 機能固有UI | `Features/{Feature}/` に .cs と同列配置 |
| 複数機能で使う基盤ページ | `Components/Pages/` |
| BC横断の共有コンポーネント | `Components/Shared/` |

---

## 🔲 バウンダリー（要約）

> **詳細は `catalog/patterns/boundary-pattern.yaml` を参照**

- バウンダリーは **UIではなく、ドメインモデルの一部**
- ユーザーがシステムに「意図」を伝える境界
- Command/Query がバウンダリーの実現形態

### Entity.CanXxx() パターン（重要）

**業務ロジックは Entity が持つ。BoundaryService は委譲のみ。**

```csharp
// Entity 側（業務ロジックを持つ）
public class Order : AggregateRoot<OrderId>
{
    public BoundaryDecision CanPay()
    {
        return Status switch
        {
            OrderStatus.Pending => BoundaryDecision.Allow(),
            OrderStatus.Paid => BoundaryDecision.Deny("既に支払い済みです"),
            _ => BoundaryDecision.Deny("この状態では支払いできません")
        };
    }
}

// BoundaryService 側（委譲のみ）
public async Task<BoundaryDecision> ValidatePayAsync(OrderId id, CancellationToken ct)
{
    var order = await _repository.GetByIdAsync(id, ct);
    if (order == null)
        return BoundaryDecision.Deny("見つかりません");  // 存在チェックのみ

    return order.CanPay();  // ★ Entity に委譲
}
```

### チェックリスト

- [ ] Entity に CanXxx() メソッドがあるか？
- [ ] BoundaryService に業務ロジック（if文）がないか？

---

## 📊 Pipeline Behavior 実行順序

| 順序 | Behavior | 目的 |
|:---:|----------|------|
| 50 | MetricsBehavior | メトリクス収集 |
| 100 | ValidationBehavior | 入力検証（FluentValidation） |
| 200 | AuthorizationBehavior | 認可チェック |
| 300 | IdempotencyBehavior | 冪等性保証（beta） |
| 350 | CachingBehavior | Query結果キャッシュ |
| 400 | TransactionBehavior | トランザクション + SaveChangesAsync |
| 550 | AuditLogBehavior | 監査ログ |
| 600 | LoggingBehavior | リクエストログ |

---

## 🔗 クイックリファレンス

- **パターン一覧**: `catalog/README.md`
- **AI利用ガイド**: `catalog/AI_USAGE_GUIDE.md`
- **意思決定フロー**: `catalog/DECISION_FLOWCHART.md`
- **アーキテクチャ詳細**: `docs/blazor-guide-package/docs/`
- **実装例**: `src/Application/Features/` 配下

---

**カタログバージョン**: v2025.11.27.1
**最終更新**: 2025-11-27
