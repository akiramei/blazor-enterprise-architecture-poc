# CLAUDE.md - AI Implementation Instructions

このファイルは Claude Code および Claude AI エージェントが自動的に読み込む設定ファイルです。

---

## 🚨 CRITICAL: 実装前に必ず読むこと

**独自実装による手戻りを防ぐため、以下の順序でファイルを読んでください。**

### 読み込み優先順位（必須）

| 順序 | ファイル | 目的 | 必須度 |
|:---:|----------|------|:------:|
| 1 | `catalog/AI_USAGE_GUIDE.md` | 実装ルール・制約・アーキテクチャ全体像 | **必須** |
| 2 | `catalog/index.json` | パターン索引・意思決定マトリクス | **必須** |
| 3 | `patterns.manifest.json` | 有効なパターン一覧 | **必須** |
| 4 | `catalog/DECISION_FLOWCHART.md` | パターン選択アルゴリズム | 推奨 |

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

| ユーザーの要求 | 選択パターン | カテゴリ |
|---------------|-------------|---------|
| 「〇〇を作成する機能」 | `feature-create-entity` | feature-slice |
| 「〇〇を検索する画面」 | `feature-search-entity` | feature-slice |
| 「〇〇を編集できるように」 | `feature-update-entity` | feature-slice |
| 「〇〇を削除する」 | `feature-delete-entity` | feature-slice |
| 「CSVインポート」 | `feature-import-csv` | feature-slice |
| 「CSVエクスポート」 | `feature-export-csv` | feature-slice |
| 「承認ワークフロー」 | `feature-approval-workflow` | feature-slice |
| 「すべてのCommandに〇〇」 | pipeline-behavior | pipeline-behavior |
| 「状態管理」 | `layer-store` | layer-element |

**判断に迷った場合**: `feature-slice` をデフォルトで選択

---

## 📂 プロジェクト構造

```
src/
├── Application/                  # 単一Blazor Serverプロジェクト
│   ├── Features/                 # 垂直スライス（19機能）
│   │   └── {Feature}/
│   │       ├── {Feature}Command.cs
│   │       ├── {Feature}CommandHandler.cs
│   │       └── UI/
│   ├── Shared/{BC}/              # BC別共通コード
│   └── Core/                     # Commands, Queries, Behaviors
├── Domain/{BC}/                  # ドメインプロジェクト（分離）
└── Shared/                       # グローバル共通
```

---

## 🔄 実装フロー

```
1. catalog/index.json を読む
2. ai_decision_matrix でカテゴリを特定
3. 該当パターンの YAML を読む
4. ai_guidance.common_mistakes を確認
5. テンプレート変数を置換してコード生成
6. evidence のファイルパスで実装例を確認
```

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

---

## 🔗 クイックリファレンス

- **パターン一覧**: `catalog/README.md`
- **AI利用ガイド**: `catalog/AI_USAGE_GUIDE.md`
- **意思決定フロー**: `catalog/DECISION_FLOWCHART.md`
- **アーキテクチャ詳細**: `docs/blazor-guide-package/docs/`
- **実装例**: `src/Application/Features/` 配下

---

**カタログバージョン**: v2025.11.19
**最終更新**: 2025-11-23
