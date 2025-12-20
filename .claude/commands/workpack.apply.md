# workpack.apply

workpack の output.diff をリポジトリに適用します。

## 引数

$ARGUMENTS

## 実行

以下のコマンドを実行してください：

```powershell
./scripts/apply-diff.ps1 $ARGUMENTS
```

## 使用例

```
/workpack.apply -TaskId "T001-create-book-entity" -DryRun
/workpack.apply -TaskId "T001-create-book-entity"
/workpack.apply -TaskId "T001-create-book-entity" -MoveToCompleted
```

## 必須パラメータ

| パラメータ | 説明 |
|-----------|------|
| `-TaskId` | タスクID（例: T001-create-book-entity） |

## オプション

| パラメータ | 説明 | デフォルト |
|-----------|------|-----------|
| `-DryRun` | 適用せず変更内容のみ表示 | false |
| `-SkipBaseCommitCheck` | base_commit チェックをスキップ | false |
| `-SkipVerification` | build/test 検証をスキップ | false |
| `-MoveToCompleted` | 成功後に completed/ へ移動 | false |

## 処理フロー

1. violations.md の有無を確認（警告）
2. base_commit と HEAD の一致を確認
3. `git apply --check` で適用可能性を確認
4. `git apply` で実際に適用
5. `dotnet build` / `dotnet test` で検証
6. verification.md に結果を保存

## 生成物

```
workpacks/active/{TaskId}/
└── verification.md   # build/test 結果
```

## 推奨フロー

```
1. /workpack.apply -TaskId "T001-xxx" -DryRun    # 確認
2. /workpack.apply -TaskId "T001-xxx"            # 適用
3. git diff                                       # 変更確認
4. git add . && git commit                        # コミット
```

## 注意: ローカル専用推奨

このコマンドは**ローカル環境での手動実行**を想定しています。

CI では以下のみ実行し、自動 apply は避けてください：
- `/workpack.run` → exit code で成否判定
- `git apply --check` → 適用可能性の確認
- `dotnet build` / `dotnet test` → ビルド・テスト検証

自動 apply はガードレールと検証が十分に回ってから、かつブランチ限定で導入してください。
