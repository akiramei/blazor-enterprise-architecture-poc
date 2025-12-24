# Workpack サンプル

このディレクトリには、workpack の動作確認用サンプルが含まれています。

## 使い方

### パターンA: 生成済み workpack を使う（推奨）

```bash
# サンプルを active/ にコピー
cp -r workpacks/_samples/T001-create-todo workpacks/active/

# Dry Run で確認
/workpack.run -TaskId "T001-create-todo" -DryRun
```

### パターンB: spec から workpack を生成

```bash
# spec から workpack を生成
/workpack.generate -TaskId "T002-create-todo" -SpecPath "specs/_samples/CreateTodo"

# 生成された workpack を確認
ls workpacks/active/T002-create-todo/

# Dry Run で確認
/workpack.run -TaskId "T002-create-todo" -DryRun
```

## ファイル構成

```
_samples/
├── README.md                    # このファイル
└── T001-create-todo/            # 生成済み workpack サンプル
    ├── task.md                  # タスク定義
    ├── spec.extract.md          # 仕様抽出
    ├── policy.yaml              # 実装ポリシー
    ├── guardrails.yaml          # 禁止事項
    └── repo.snapshot.md         # 既存コード参照
```

対応する spec:
```
specs/_samples/
├── CreateTodo.spec.yaml         # 仕様定義
├── CreateTodo.guardrails.yaml   # ガードレール
└── CreateTodo.manifest.yaml     # パターン指定
```

## サンプルの内容

**CreateTodo**: シンプルな TODO アイテム作成機能

| 項目 | 内容 |
|------|------|
| パターン | `feature-create-entity` |
| 入力 | Title (必須), DueDate (任意) |
| 出力 | 作成された TODO の ID (Guid) |
| ドメインルール | タイトル1-100文字、期限日は今日以降 |

## 注意事項

- サンプルはチュートリアル用です。実際のプロジェクトでは独自の spec を作成してください
- `workpacks/active/` にコピーしてから実行してください（`_samples/` では動作しません）
