# Workpacks - ステートレスAI駆動開発

## 概要

workpacksは、AI駆動開発における**再現性**を確保するための自己完結型入力パッケージです。

**問題**: 会話ベースのLLM利用はコンテキスト圧縮が非決定的で、再現性が低い

**解決策**: LLMを「ステートレス関数」として使用し、各タスクを自己完結型パッケージで実行

## アーキテクチャ

```
Phase A: 設計・高コンテキスト（考えるAI）
├── spec.yaml → decisions.yaml → guardrails.yaml → plan.md
└── 対話的、カタログ全体参照、パターン選択
        │
        ▼
    workpack（境界成果物）
        │
        ▼
Phase B: 実装・低コンテキスト（作るAI）
├── workpack → unified diff
└── 非対話的、純関数的、claude -p で実行
```

## ディレクトリ構造

```
workpacks/
├── README.md              # このファイル
├── _templates/            # workpack テンプレート
│   ├── task.template.md
│   ├── spec.extract.template.md
│   ├── policy.template.yaml
│   ├── guardrails.template.yaml
│   ├── repo.snapshot.template.md
│   └── implementation-prompt.md
│
├── active/                # 進行中タスク
│   └── {task-id}/
│       ├── task.md              # コア: タスク定義
│       ├── spec.extract.md      # コア: 仕様抽出
│       ├── policy.yaml          # コア: ポリシー
│       ├── guardrails.yaml      # コア: ガードレール
│       ├── repo.snapshot.md     # コア: リポジトリスナップショット
│       ├── assembled-prompt.md  # 派生: 組み立て済みプロンプト
│       ├── reproducibility.yaml # 派生: 再現性メタ
│       ├── output.diff          # 派生: 出力差分
│       ├── violations.md        # 派生: ガードレール違反レポート（あれば）
│       └── verification.md      # 派生: 適用後検証結果
│
├── completed/             # 完了済み（履歴）
│   └── {task-id}/
│
└── failed/                # 失敗・保留
    └── {task-id}/
        └── failure.md
```

## workpack のファイル構成

### コア5ファイル（入力）

| ファイル | 役割 | 生成元 |
|---------|------|-------|
| task.md | タスク定義（What to do） | plan.md + tasks.yaml |
| spec.extract.md | 関連仕様抽出（Domain context） | spec.yaml |
| policy.yaml | 適用ポリシー（How to do） | decisions.yaml + manifest.yaml |
| guardrails.yaml | ガードレール（What NOT to do） | guardrails.yaml |
| repo.snapshot.md | 関連コード（Where to do） | 既存コードベース |

### 派生成果物（出力・検証）

| ファイル | 役割 | 生成タイミング |
|---------|------|--------------|
| assembled-prompt.md | claude -p に渡す完全プロンプト | run-implementation.ps1 実行時 |
| reproducibility.yaml | 再現性メタ（model, commit hash等） | run-implementation.ps1 実行時 |
| output.diff | unified diff 形式の実装出力 | run-implementation.ps1 実行後 |
| violations.md | ガードレール違反レポート | 違反検出時のみ生成 |
| verification.md | 適用後の build/test 結果 | apply-diff.ps1 実行後 |

## 使用方法

### 1. workpack 生成（Phase A → 境界）

```powershell
# specs/ から workpack を生成
./scripts/generate-workpack.ps1 -TaskId "T001-create-book-entity" -SpecPath "specs/loan/LendBook"
```

### 2. ステートレス実装（Phase B）

```powershell
# claude -p でステートレス実行
./scripts/run-implementation.ps1 -TaskId "T001-create-book-entity"
```

### 3. diff 適用

```powershell
# 生成された diff をリポジトリに適用
./scripts/apply-diff.ps1 -TaskId "T001-create-book-entity"
```

## 設計原則

### 純関数的実行

```
f(workpack) → diff
```

- 入力: workpack（コア5ファイル）のみ
- 出力: unified diff のみ
- 副作用: なし（diff適用は別ステップ）
- 派生: 実行ごとに reproducibility.yaml / assembled-prompt.md を生成

### 自己完結性

workpack 内のファイルだけで実装可能。外部参照不要。

### 再現性

同一 workpack → 同一 diff（を目指す）

- モデル更新の影響を局所化
- CIで差分検証可能

## Phase A/B の責務分離

| 属性 | Phase A（設計） | Phase B（実装） |
|------|----------------|-----------------|
| 役割 | 考えるAI（Architect） | 作るAI（Coder） |
| 入力 | 自然言語、カタログ全体 | workpack のみ |
| 出力 | spec, decisions, guardrails, plan | unified diff |
| コンテキスト | 高（対話的） | 低（純関数的） |
| 再現性 | 中程度（LLMの非決定性あり） | 高 |

## 関連ドキュメント

- `catalog/speckit-extensions/commands/speckit.workpack.md` - workpack生成コマンド
- `catalog/SPEC_KIT_GUARDRAILS_DESIGN.md` - ガードレール設計
- `catalog/AI_USAGE_GUIDE.md` - AI利用ガイド
