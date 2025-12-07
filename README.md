# Blazor Enterprise Architecture Catalog

**アプリの仕様を自然言語で書いて、AIと一緒にソフトウェアを作るためのカタログ**

---

## 🚀 5分で始める

### 必要なもの

以下を先にインストールしてください：

1. **Claude Code**: https://claude.ai/download （AI コーディングツール）
2. **uv**: 以下のコマンドでインストール
   ```bash
   # macOS / Linux
   curl -LsSf https://astral.sh/uv/install.sh | sh

   # Windows (PowerShell)
   powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
   ```
3. **.NET 9 SDK**: https://dotnet.microsoft.com/download

### Step 1: spec-kit をインストール

```bash
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git
```

### Step 2: spec-kit を初期化

```bash
mkdir MyApp
cd MyApp
specify init . --ai claude
```

> **Windows ユーザーへ**: `specify init` が止まる場合は、以下を試してください：
> ```powershell
> $env:PYTHONUTF8=1
> specify init . --ai claude
> ```

### Step 3: カタログを追加

**Linux / macOS (bash):**
```bash
git clone https://github.com/akiramei/blazor-enterprise-architecture-poc temp-catalog
cp -r temp-catalog/catalog ./catalog
mkdir -p .claude/commands
mkdir -p memory
cp catalog/speckit-extensions/commands/*.md .claude/commands/
cat catalog/speckit-extensions/constitution-additions.md >> memory/constitution.md
rm -rf temp-catalog
```

**Windows (PowerShell):**
```powershell
git clone https://github.com/akiramei/blazor-enterprise-architecture-poc temp-catalog
Copy-Item temp-catalog/catalog ./catalog -Recurse
New-Item -ItemType Directory -Force -Path .claude/commands
New-Item -ItemType Directory -Force -Path memory
Copy-Item catalog/speckit-extensions/commands/*.md .claude/commands/
Get-Content catalog/speckit-extensions/constitution-additions.md | Add-Content memory/constitution.md
Remove-Item temp-catalog -Recurse -Force
```

### Step 4: Claude Code を起動

```bash
claude
```

### Step 5: アプリを作る

Claude Code で以下のように話しかけてください：

```
/speckit.specify 図書館の貸出管理アプリを作りたい。
会員がバーコードで本を借りて、返却期限を管理できる。
1人5冊まで借りられる。
```

> **より詳細な要求仕様の例**: [図書館貸出管理システム要求仕様](docs/samples/library-loan-system-requirements.md)
>
> 上記ファイルには、ドメインモデル、機能要件、ビジネスルールが詳細に記載されています。
> 本格的なシステムを作る場合は、このような詳細仕様を `/speckit.specify` に渡すことで、
> AI がより正確に意図を理解できます。

AI が仕様書を作成したら、続けて：

```
/speckit.plan
```

技術計画ができたら：

```
/speckit.tasks
```

タスクが分解されたら：

```
/speckit.implement
```

これで AI がコードを生成します。

### Step 6: アプリを起動

```bash
dotnet run --project Application
```

ブラウザで表示された URL（例: `https://localhost:5001`）を開きます。

---

## 🤖 ヘッドレスモード（自動化 / CI向け）

対話なしでコマンドラインから直接実行することもできます。

### 基本的な使い方

```bash
# 仕様書を作成（詳細仕様ファイルを渡す）
claude -p "/speckit.specify $(cat docs/samples/library-loan-system-requirements.md)"

# 計画を立てる
claude -p --continue "/speckit.plan"

# タスクを分解
claude -p --continue "/speckit.tasks"

# 実装する
claude -p --continue "/speckit.implement"
```

### オプション

| オプション | 説明 |
|-----------|------|
| `-p, --print` | 非対話モード（結果を出力して終了） |
| `--continue` | 直前のセッションを継続 |
| `--output-format json` | JSON形式で出力 |
| `--dangerously-skip-permissions` | 権限確認をスキップ（サンドボックス環境向け） |

### パイプラインの例

```bash
# 一連の処理を連続実行
claude -p "/speckit.specify $(cat requirements.md)" && \
claude -p --continue "/speckit.plan" && \
claude -p --continue "/speckit.tasks" && \
claude -p --continue "/speckit.implement"
```

> **注意**: `--dangerously-skip-permissions` はインターネットアクセスのないサンドボックス環境でのみ使用してください。

---

## 📖 開発の流れ

```
あなたの頭の中のアイデア
    ↓
/speckit.specify  「〇〇を作りたい」と自然言語で伝える
    ↓
/speckit.plan     AI が技術計画を立てる（カタログのパターンを選択）
    ↓
/speckit.tasks    タスクを分解する
    ↓
/speckit.implement  コードを生成する
    ↓
動くソフトウェア
```

### 困ったときは

| 状況 | 対処 |
|------|------|
| 仕様が曖昧と言われた | `/speckit.clarify` で AI に質問してもらう |
| 計画を確認したい | `/speckit.analyze` で整合性をチェック |
| テスト項目を作りたい | `/speckit.checklist` でチェックリスト生成 |

---

## 🎯 このカタログでできること

- **20+ の実装パターン**を提供（CRUD、検索、状態管理、認証など）
- **Blazor Server + .NET 9** のエンタープライズアプリを生成
- **MediatR (CQRS)** による Command/Query 分離
- **Pipeline Behaviors** で検証・トランザクション・ログを自動化

---

## 📚 詳細ドキュメント

| 目的 | ファイル |
|------|----------|
| spec-kit + カタログ統合の詳細 | [catalog/INTEGRATION_WITH_SPEC.md](catalog/INTEGRATION_WITH_SPEC.md) |
| パターン一覧 | [catalog/README.md](catalog/README.md) |
| パターン選択フローチャート | [catalog/DECISION_FLOWCHART.md](catalog/DECISION_FLOWCHART.md) |
| 要求仕様サンプル（図書館システム） | [docs/samples/library-loan-system-requirements.md](docs/samples/library-loan-system-requirements.md) |

---

## 🤖 AI エージェント向け情報

> このセクションは AI エージェントが読むための情報です。人間は読み飛ばして構いません。

<details>
<summary>AI エージェント向け詳細（クリックで展開）</summary>

### 読み込み順序

1. [LLM_BOOTSTRAP.md](LLM_BOOTSTRAP.md) - 最初に読む入口
2. [CLAUDE.md](CLAUDE.md) - 実装ルール・禁止事項
3. [catalog/index.json](catalog/index.json) - パターン索引

### 絶対禁止事項

```
❌ Handler内でSaveChangesAsync()を呼ばない → TransactionBehaviorが自動実行
❌ Singletonでサービス登録しない → すべてScoped
❌ 例外をthrowしない → Result<T>で返す
❌ カタログに存在するパターンを独自実装しない
```

### Constitution 参照

`memory/constitution.md` にカタログ駆動開発のルールが記載されています。

</details>

---

## 🛠️ このリポジトリを開発する人向け

> このセクションは、カタログ自体を改善・開発する人向けです。カタログを使うだけなら読む必要はありません。

<details>
<summary>開発者向け情報（クリックで展開）</summary>

### VSASample を動かす

```bash
# PostgreSQL を起動
podman run -d --name postgres-productcatalog \
  -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=productcatalog -p 5432:5432 postgres:17

# ビルド＆実行
dotnet build
cd src/Application && dotnet run

# ブラウザで https://localhost:5001 を開く
# ログイン: admin@example.com / Admin@123
```

### 技術情報

| 項目 | 値 |
|------|-----|
| .NET | 9.0 |
| カタログバージョン | v2025.12.06 |
| パターン数 | 23 |

### アーキテクチャドキュメント

- [アーキテクチャ全体像](docs/blazor-guide-package/docs/00_README.md)
- [3層アーキテクチャからの移行](docs/blazor-guide-package/docs/18_3層アーキテクチャからの移行ガイド.md)

</details>

---

## ライセンス

MIT License
