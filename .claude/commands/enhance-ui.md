---
description: UI を MudBlazor ベースにリッチ化する
allowed-tools:
  - Read
  - Glob
  - Grep
  - Edit
  - Write
---

# UI 強化タスク

指定された .razor ファイルを MudBlazor コンポーネントベースに変換してください。

## 対象ファイル

$ARGUMENTS

## 実行手順

### 1. 入力ファイルの読み込み

以下のファイルを読み込んでください：

1. **対象 .razor ファイル**
2. **関連する Command / Query**（.razor から参照されているもの）
3. **Domain Entity**（CanXxx() メソッドがある場合）

### 2. スキルドキュメントの参照

以下のドキュメントを参照してください：

- `catalog/skills/vsa-ui-enhancer/SKILL.md` - 概要と原則
- `catalog/skills/vsa-ui-enhancer/component-mapping.md` - HTML → MudBlazor 変換表
- `catalog/skills/vsa-ui-enhancer/boundary-integration.md` - CanXxx() 連携パターン
- `catalog/skills/vsa-ui-enhancer/ui-patterns/` - UI パターン（form, list, detail, dialog）

### 3. 変換ルール

#### やること

- HTML 要素を MudBlazor コンポーネントに変換
- CanXxx() の結果でボタンの Disabled/Tooltip を制御
- ローディング状態の表示を追加
- バリデーションエラーの表示を強化

#### やらないこと

- Command / Query / Handler の構造を変更しない
- Store / PageActions の構造を変更しない
- ドメインロジックを変更しない

### 4. 出力

変換後の .razor ファイルを出力してください。

## チェックリスト

変換後、以下を確認してください：

```
□ HTML → MudBlazor コンポーネントに変換されているか？
□ CanXxx() があれば Disabled/Tooltip に反映されているか？
□ Command/Handler/Store の構造は維持されているか？
□ ローディング状態が表示されるか？
□ バリデーションエラーが適切に表示されるか？
```
