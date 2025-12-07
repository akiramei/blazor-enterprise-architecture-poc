# Constitution Additions for Catalog-Driven Development

> **使い方**: このファイルの内容を `memory/constitution.md` に追記してください。

---

## Catalog-Driven Development (NON-NEGOTIABLE)

すべての実装はカタログのパターンを参照して行う。

- カタログに存在するパターンを独自実装してはならない
- `catalog/index.json` でパターンを検索し、該当パターンの YAML を読む
- パターンが存在しない場合のみ、新規実装を検討する
- カタログの変更が必要な場合は「提案」として説明し、直接変更しない

## Catalog Integration Rules

### Pattern Selection

1. `catalog/DECISION_FLOWCHART.md` でパターンカテゴリを特定
2. `catalog/index.json` の `ai_decision_matrix` でパターン ID を取得
3. 該当パターンの YAML を読み込む
4. `ai_guidance.common_mistakes` を確認して回避

### Plan Phase Requirements

Plan フェーズ（/speckit.plan）では以下を必須とする：

- **Guardrails セクション**：絶対に破ってはいけないビジネスルールを列挙
- Catalog Binding セクション：使用するパターン ID を列挙
- Creative Areas セクション：パターンでカバーできない創造的領域を明示
- Boundary セクション：UI がある場合は Intent と Entity.CanXxx() を設計
- **Attribute Enforcement Check**：spec の属性が data-model から落ちていないか確認
- **Query Semantics**：Query と Repository メソッドの対応を明示

## Guardrails（絶対遵守ルール）

### Guardrails とは

- 「絶対に破ってはいけないビジネスルール」
- 違反すると仕様違反となるもの
- FR 番号で追跡可能

### Guardrails の識別条件

spec を読む際に以下をチェック：

- 「〜のみ」「〜だけ」「〜の場合のみ」という文言
- 「優先」「先着」「順番」という文言
- 複数の条件を満たす必要がある操作
- 状態に依存する操作可否判定

### Guardrails の伝播ルール

1. **spec → plan**: Guardrails セクションとして抽出
2. **plan → tasks**: 各 Guardrail を最低1つのタスクに紐付け
3. **tasks → implement**: Acceptance Criteria に Guardrail チェックを含める

### 違反時の扱い

- Guardrail が紐付いていないタスクがある場合 → **エラー**
- Guardrail を満たさない実装 → **仕様違反**

## Validation Contract（検証契約）

ValidationService 実装時は以下を必須とする：

```yaml
validation_contract:
  service: "{ServiceName}"
  method: "{MethodName}"
  requires:
    - "{IRepository}.{Method}({params}) - {説明}"
  ensures:
    - "{条件} ({FR番号})"
```

- **requires**: 呼び出すべきリポジトリメソッド（省略禁止）
- **ensures**: 保証すべき条件（FR 番号付き）

## Query Semantics（クエリの意味）

Query 実装時は以下を明示：

- Query の意味（semantic）
- 使用すべき Repository メソッド
- 使用禁止の Repository メソッド

これにより、Query と Repository の混同を防ぐ。

## Technology Stack (Catalog Patterns)

このカタログで使用する技術スタック：

- .NET 8 / C# 12
- Blazor Server (InteractiveServer)
- Entity Framework Core
- MediatR (CQRS)
- FluentValidation
- Result<T> Pattern for error handling
- Pipeline Behaviors for cross-cutting concerns
