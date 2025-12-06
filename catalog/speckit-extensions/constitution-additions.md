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

- Catalog Binding セクション：使用するパターン ID を列挙
- Creative Areas セクション：パターンでカバーできない創造的領域を明示
- Boundary セクション：UI がある場合は Intent と Entity.CanXxx() を設計

## Technology Stack (Catalog Patterns)

このカタログで使用する技術スタック：

- .NET 8 / C# 12
- Blazor Server (InteractiveServer)
- Entity Framework Core
- MediatR (CQRS)
- FluentValidation
- Result<T> Pattern for error handling
- Pipeline Behaviors for cross-cutting concerns
