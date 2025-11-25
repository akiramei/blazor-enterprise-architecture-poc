# CLAUDE.md - AI Implementation Instructions

このファイルは Claude Code および Claude AI エージェントが自動的に読み込む設定ファイルです。

> **詳細版**: より詳しい情報は `AGENTS.md` を参照してください。

---

## 🚨 CRITICAL: 実装前に必ず読むこと

**このプロジェクトはカタログ駆動開発を採用しています。独自実装は禁止です。**

### 読み込み優先順位（必須）

| 順序 | ファイル | 目的 | 必須度 |
|:---:|----------|------|:------:|
| 1 | `catalog/AI_USAGE_GUIDE.md` | 実装ルール・制約・UI配置ルール | **必須** |
| 2 | `catalog/index.json` | パターン索引・意思決定マトリクス | **必須** |
| 3 | `catalog/COMMON_MISTAKES.md` | 頻出ミスと回避方法 | **必須** |
| 4 | `catalog/DECISION_FLOWCHART.md` | パターン選択アルゴリズム | 推奨 |

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

## 📁 UI配置ルール（要約）

> **詳細は `catalog/scaffolds/project-structure.yaml` を参照**

| 条件 | 配置場所 |
|-----|---------|
| 機能固有UI | `Features/{Feature}/` に .cs と同列配置 |
| 複数機能で使う基盤ページ | `Components/Pages/` |
| BC横断の共有コンポーネント | `Components/Shared/` |

---

## 🔲 バウンダリー（要約）

> **詳細は `AGENTS.md` を参照**

- バウンダリーは **UIではなく、ドメインモデルの一部**
- ユーザーがシステムに「意図」を伝える境界
- Command/Query がバウンダリーの実現形態

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

**カタログバージョン**: v2025.11.25b
**最終更新**: 2025-11-25
