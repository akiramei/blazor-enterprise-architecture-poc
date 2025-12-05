# LLM_BOOTSTRAP.md - AI向け入口ファイル

> **このファイルの目的**: AIエージェントがこのカタログを理解するための「入口」です。
> 最初にこのファイルを読み、指示に従って必要なファイルを読んでください。

> **正本について**:
> このファイルは「禁止事項」「パターン早見表」「基本ルール」の**正本**です。
> `CLAUDE.md` にも同内容を再掲していますが、更新時はこのファイルを先に修正し、
> `CLAUDE.md` に同期してください。

---

## 1. このカタログについて

このリポジトリは **Blazor Enterprise Architecture のパターンカタログ** です。

- **URL**: https://github.com/akiramei/blazor-enterprise-architecture-poc
- **用途**: 別プロジェクトから参照して、アーキテクチャルールとパターンに従って開発する
- **特徴**: カタログ駆動開発（独自実装禁止、パターンに従う）

---

## 2. アーキテクチャ（固定）

以下のアーキテクチャは **変更不可** です。他のアーキテクチャを提案してはいけません。

| 項目 | 採用技術 |
|------|---------|
| アーキテクチャ | **Vertical Slice Architecture (VSA)** |
| CQRS | **MediatR** + ICommand<T> / IQuery<T> |
| DDD | Entity, ValueObject, AggregateRoot |
| UI | **Blazor** |
| 横断的関心事 | **Pipeline Behaviors** |

### 禁止されるアーキテクチャ提案

```
❌ Clean Architecture（Onion Architecture）
❌ レイヤードアーキテクチャ（3層/N層）
❌ Application / Domain / Infrastructure のレイヤー分離
```

---

## 3. 絶対禁止事項（NEVER DO）

```
❌ Handler内でSaveChangesAsync()を呼ばない
   → TransactionBehaviorが自動実行する

❌ SingletonでDbContextやScopedサービスを注入しない
   → すべてのサービスはScopedで登録

❌ MediatRのHandleメソッド名をHandleAsyncにしない
   → 正しくは Handle

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

## 4. 必読ファイルと読み順

### 常に読むべきファイル（TIER 1）

| 順序 | ファイル | 目的 |
|:---:|----------|------|
| 1 | **CLAUDE.md** | 実装ルール・禁止事項・クイックリファレンス |
| 2 | **catalog/LLM_PATTERN_INDEX.md** | パターン選択早見表（Markdown版） |
| 3 | **catalog/AI_USAGE_GUIDE.md** | 詳細実装ガイド |
| 4 | **catalog/index.json** | パターン索引・意思決定マトリクス |
| 5 | **catalog/COMMON_MISTAKES.md** | 頻出ミス回避 |

### フェーズ別に読むファイル（TIER 2）

#### 計画フェーズ（新規機能・3ファイル以上・UI有り）
| ファイル | 目的 |
|----------|------|
| catalog/planning/PLANNING_GUIDE.md | 計画の6ステップ |
| catalog/DECISION_FLOWCHART.md | パターン選択アルゴリズム |
| catalog/patterns/boundary-pattern.yaml | UI→Domain責務分離（**UIがある場合必須**） |
| catalog/planning/pattern-combinations.yaml | パターン組み合わせ例 |

#### 実装フェーズ（計画承認後）
| ファイル | 目的 |
|----------|------|
| catalog/implementation/IMPLEMENTATION_GUIDE.md | 実装の6ステップ |
| 計画で選択されたパターンYAML | テンプレート適用 |

### SPEC/Manifestがある場合の読み順

`specs/` と `manifests/` フォルダが存在する場合、以下の順序で読むこと：

| 順序 | ファイル | 目的 |
|:---:|----------|------|
| 1 | `specs/{feature}/{slice}.spec.yaml` | 業務仕様（What） |
| 2 | `manifests/{feature}/{slice}.manifest.yaml` | パターン選択記録（Bridge） |
| 3 | `catalog/CHARACTERISTICS_CATALOG.md` | characteristics語彙理解 |
| 4 | `catalog_binding.from_catalog` のパターンYAML | テンプレート取得 |

**characteristicsからパターン選択**:

SPECの `characteristics` を確認し、対応するパターンを特定：

```
op:mutates-state → feature-create/update/delete-entity
op:read-only → query-get-list, feature-search-entity
xcut:auth → authorization-behavior
xcut:audit → audit-log-behavior
struct:multi-aggregate → domain-validation-service
```

**生成フロー**: CLAUDE.md の「Standard Generation Flow」セクションを参照

---

## 5. フェーズ判断フロー

```
ユーザーからの要求
    ↓
【計画フェーズが必要か？】

以下に1つでも該当 → YES:
  ✅ 新規機能の追加
  ✅ 3ファイル以上の作成
  ✅ 複数パターンの組み合わせ
  ✅ UIがある
  ✅ ドメイン固有の業務ロジック
  ✅ 非機能要件の考慮が必要

YES → catalog/planning/PLANNING_GUIDE.md を読む
       ↓
     計画書作成 → ユーザー承認
       ↓
     実装フェーズへ

NO → 直接実装フェーズへ
     catalog/implementation/IMPLEMENTATION_GUIDE.md を読む
```

---

## 6. 計画フェーズでの必須確認（UIがある場合）

**UIがある機能を計画する際、Boundaryモデリングを忘れないこと。**

### 必須アクション
1. `catalog/patterns/boundary-pattern.yaml` を読む
2. Intent（ユーザーの意図）を列挙
3. `Entity.CanXxx()` メソッドを設計
4. 計画書に Boundary セクションを含める

### 不完全な計画の例
```
❌ UIがあるのに Boundary セクションがない
❌ Intent が定義されていない
❌ Entity.CanXxx() の設計がない
❌ 「後から Boundary を追加する」という計画
```

---

## 7. パターン選択の早見表

| ユーザーの要求 | 選択パターン |
|---------------|-------------|
| 「〇〇を作成」 | feature-create-entity |
| 「〇〇を検索」 | feature-search-entity |
| 「〇〇を編集」 | feature-update-entity |
| 「〇〇を削除」 | feature-delete-entity |
| 「CSVインポート」 | feature-import-csv |
| 「CSVエクスポート」 | feature-export-csv |
| 「承認ワークフロー」 | feature-approval-workflow |
| 「状態遷移」 | domain-state-machine |
| 「操作可否判定」 | boundary-pattern |

**迷った場合**: `feature-slice` をデフォルトで選択

---

## 8. 実装の基本ルール

```csharp
// Command の戻り値は必ず Result<T>
public sealed record CreateXxxCommand(...) : ICommand<Result<Guid>>;

// Handler は SaveChangesAsync を呼ばない
public async Task<Result<Guid>> Handle(CreateXxxCommand request, CancellationToken ct)
{
    var entity = new Xxx(...);
    await _repository.AddAsync(entity, ct);
    return Result.Success(entity.Id);  // SaveChangesAsync は呼ばない！
}

// サービスのライフタイムは Scoped
services.AddScoped<IXxxRepository, XxxRepository>();
```

---

## 9. カタログバージョン

- **バージョン**: v2025.11.27.3
- **最終更新**: 2025-11-27

---

## 次のステップ

1. **CLAUDE.md** を読む（詳細なルールと禁止事項）
2. **catalog/LLM_PATTERN_INDEX.md** を読む（パターン一覧）
3. 計画フェーズが必要か判断する
4. 必要なパターンYAMLを読んで実装
