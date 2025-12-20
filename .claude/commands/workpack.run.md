# workpack.run

workpack を使って claude -p でステートレス実装を実行します。

## 引数

$ARGUMENTS

## 実行

以下のコマンドを実行してください：

```powershell
./scripts/run-implementation.ps1 $ARGUMENTS
```

## 使用例

```
/workpack.run -TaskId "T001-create-book-entity"
/workpack.run -TaskId "T001-create-book-entity" -DryRun
```

## 必須パラメータ

| パラメータ | 説明 |
|-----------|------|
| `-TaskId` | タスクID（例: T001-create-book-entity） |

## オプション

| パラメータ | 説明 | デフォルト |
|-----------|------|-----------|
| `-DryRun` | 実行せずプロンプトのみ生成 | false |
| `-Model` | 使用モデル | claude-sonnet-4-20250514 |
| `-Temperature` | 温度パラメータ | 0 |

## 生成物

```
workpacks/active/{TaskId}/
├── assembled-prompt.md    # 組み立て済みプロンプト
├── reproducibility.yaml   # 再現性メタ
├── output.diff            # 実装結果（unified diff）
└── violations.md          # ガードレール違反（あれば）
```

## 注意

- 実行前に workpack が生成済みであること
- violations.md が生成された場合は修正が必要
