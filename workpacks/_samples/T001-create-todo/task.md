# Task: T001-create-todo

## Meta

| Key | Value |
|-----|-------|
| ID | T001-create-todo |
| Feature | todo |
| Slice | CreateTodo |
| Parent Plan | specs/_samples/CreateTodo.spec.yaml |
| Pattern | feature-create-entity |
| Status | pending |
| Created | 2025-01-01T00:00:00Z |

## Objective

TODO アイテムを作成する機能を実装する。
ユーザーがタイトル（必須）と期限日（任意）を入力し、新しい TODO を登録できる。

## Scope

### In Scope

- Todo エンティティの作成
- CreateTodoCommand / Handler の実装
- FluentValidation によるバリデーション
- Result<T> パターンでの戻り値

### Out of Scope

- UI（Razor コンポーネント）
- 一覧表示・編集・削除機能
- 認証・認可

## Acceptance Criteria

- [ ] AC-001: タイトル1-100文字でTODOを作成できる
- [ ] AC-002: 期限日を省略してTODOを作成できる
- [ ] AC-003: タイトルが空の場合、バリデーションエラーになる
- [ ] AC-004: Handler内でSaveChangesAsync()を呼んでいない

## Dependencies

| Type | Task ID | Description |
|------|---------|-------------|
| Depends on | - | なし（最初のタスク） |
| Blocks | T002-list-todos | 一覧表示機能 |

## Expected Output

### Files to Create

| Path | Purpose |
|------|---------|
| src/Domain/Todo/Todo.cs | Todo エンティティ |
| src/Domain/Todo/TodoId.cs | 型付きID |
| src/Application/Features/CreateTodo/CreateTodoCommand.cs | コマンド定義 |
| src/Application/Features/CreateTodo/CreateTodoCommandHandler.cs | ハンドラー |
| src/Application/Features/CreateTodo/CreateTodoCommandValidator.cs | バリデーター |

### Files to Modify

| Path | Change Type |
|------|-------------|
| src/Application/DependencyInjection.cs | DI登録追加 |

## Guardrail References

このタスクで遵守すべきガードレール（guardrails.yaml から抽出）:

| ID | Rule | Severity |
|----|------|----------|
| FA-001 | Handler内でSaveChangesAsync()を呼ばない | critical |
| FA-002 | throw new で例外を投げない | high |

## Notes

- このタスクは workpack チュートリアル用のサンプルです
- 実際のプロジェクトでは、既存の Bounded Context に合わせて調整してください
