# workpack.generate

specs/ + tasks.yaml から workpack を生成します。

## 引数

$ARGUMENTS

## 実行

以下のコマンドを実行してください：

```powershell
./scripts/generate-workpack.ps1 $ARGUMENTS
```

## 使用例

```
/workpack.generate -TaskId "T001-create-book-entity" -SpecPath "specs/loan/LendBook"
/workpack.generate -TaskId "T001-create-book-entity" -SpecPath "specs/loan/LendBook" -TasksYamlPath "specs/loan/tasks.yaml"
```

## 必須パラメータ

| パラメータ | 説明 |
|-----------|------|
| `-TaskId` | タスクID（例: T001-create-book-entity） |
| `-SpecPath` | spec.yaml のパス（例: specs/loan/LendBook） |

## 推奨パラメータ

| パラメータ | 説明 |
|-----------|------|
| `-TasksYamlPath` | tasks.yaml のパス。指定すると objective/AC/対象ファイルを自動抽出 |

## オプション

| パラメータ | 説明 |
|-----------|------|
| `-OutputDir` | 出力先（デフォルト: workpacks/active/{TaskId}/） |
| `-Force` | 既存workpackを上書き |

## 生成物

```
workpacks/active/{TaskId}/
├── task.md
├── spec.extract.md
├── policy.yaml
├── guardrails.yaml
└── repo.snapshot.md
```
