# FIRST_CONTACT.md - カタログセットアップ手順（AI向け）

このファイルはAIエージェント向けのセットアップ手順書です。
開発者からこのURLを受け取った場合、以下のコマンドを順番に実行してください。

---

## 前提条件

- カレントディレクトリが新規プロジェクトディレクトリであること
- `git init` 済みであること

確認コマンド：
```bash
pwd          # カレントディレクトリを確認
ls -la       # 空または最小限のファイルのみであること
git status   # git リポジトリであること
```

---

## Step 1: カタログをベンダリング

```bash
git clone https://github.com/akiramei/blazor-enterprise-architecture-poc temp-catalog
cp -r temp-catalog/catalog ./catalog
rm -rf temp-catalog
```

---

## Step 2: テンプレートをセットアップ

```bash
mkdir -p docs specs manifests

cp catalog/consumer-example/templates/AI_WORKFLOW.md.template docs/AI_WORKFLOW.md
cp catalog/consumer-example/templates/AI_PROMPTS.md.template docs/AI_PROMPTS.md
cp catalog/consumer-example/templates/CATALOG_VERSION.md.template docs/CATALOG_VERSION.md
cp catalog/consumer-example/templates/SPEC_SLICE.yaml.template specs/_template.yaml
```

---

## Step 3: 完了報告

上記が完了したら、ユーザーに以下を報告してください：

```
カタログのセットアップが完了しました。

セットアップ内容：
- catalog/ : パターンカタログ（読み取り専用）
- docs/AI_WORKFLOW.md : 開発ワークフロー
- docs/AI_PROMPTS.md : フェーズ別プロンプト集
- docs/CATALOG_VERSION.md : カタログバージョン情報
- specs/_template.yaml : SPEC テンプレート
- manifests/ : Manifest 格納用ディレクトリ

要求仕様をお伝えください。
```

---

## Step 4: 開発フェーズへ移行

`catalog/LLM_BOOTSTRAP.md` を読み、開発ルールを理解してください。

主な開発フロー：
1. 要求仕様を受け取る
2. SPEC ファイルを作成（`specs/{feature}/{slice}.spec.yaml`）
3. Manifest ファイルを作成（`manifests/{feature}/{slice}.manifest.yaml`）
4. 計画を立てる
5. コードを生成

---

## 人間の読者へ

このファイルはAIエージェント向けの手順書です。
人間向けの説明は [README.md](README.md) を参照してください。

---

**最終更新: 2025-12-05**
