# Blazor Enterprise Architecture PoC

**AI駆動開発のための、実装パターンカタログ付きエンタープライズアーキテクチャ実証実験**

Vertical Slice Architecture（VSA）と20個の実装パターンを提供し、AIエージェントが正確なコードを生成できる環境を実現します。

---

## 🤖 For AI Agents

> **人間の読者へ**: このセクションはAIエージェント専用です。[👤 For Humans](#-for-humans) へスキップしてください。

> **AI向け入口**: まず [LLM_BOOTSTRAP.md](LLM_BOOTSTRAP.md) を読み、指示に従ってください。

### 必読ファイル（実装前に必ず読むこと）

| 順序 | ファイル | 目的 |
|:---:|----------|------|
| 1 | [CLAUDE.md](CLAUDE.md) | 実装ルール・禁止事項・パターン選択 |
| 2 | [catalog/AI_USAGE_GUIDE.md](catalog/AI_USAGE_GUIDE.md) | 詳細な実装ガイド |
| 3 | [catalog/index.json](catalog/index.json) | パターン索引・意思決定マトリクス |

### 絶対禁止（これを破るとバグになる）

```
❌ Handler内でSaveChangesAsync()を呼ばない → TransactionBehaviorが自動実行
❌ Singletonでサービス登録しない → すべてScoped
❌ 例外をthrowしない → Result<T>で返す
```

**迷ったら**: `feature-slice` パターンをデフォルトで選択

---

## 👤 For Humans

> **AIエージェントへ**: このセクションは人間向けの説明です。実装には上記セクションとCLAUDE.mdを参照してください。

### このプロジェクトとは

AIエージェント（Claude、ChatGPT等）と人間が協調して開発するための**実装パターンカタログ**です。

**解決する課題:**
- AIが毎回異なるコードを生成する → **パターンテンプレートで一貫性を保証**
- アーキテクチャ知識がAIに伝わらない → **YAML定義でルールを明示化**
- 生成コードの品質が不安定 → **実装証跡（Evidence）で検証可能**

**主な特徴:**
- **20個のパターン**: Pipeline Behaviors、Feature Slices、Domain Patterns等
- **実装証跡付き**: すべてのパターンが実際のコードとリンク
- **意思決定支援**: パターン選択フローチャート付き

### クイックスタート

```bash
# 1. PostgreSQLを起動
podman run -d --name postgres-productcatalog \
  -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=productcatalog -p 5432:5432 postgres:17

# 2. ビルド＆実行
dotnet build
cd src/Application && dotnet run

# 3. ブラウザで https://localhost:5001 を開く
#    ログイン: admin@example.com / Admin@123
```

### このカタログを別プロジェクトで使う

1. [catalog/scaffolds/project-structure.yaml](catalog/scaffolds/project-structure.yaml) でプロジェクト構造を確認
2. プロジェクトに `CLAUDE.md` を作成し、このリポジトリを参照先として記載
3. 詳細: [catalog/README.md](catalog/README.md#-新規プロジェクトでの利用方法)

### ドキュメント

| 目的 | ファイル |
|------|----------|
| パターン一覧 | [catalog/README.md](catalog/README.md) |
| アーキテクチャ全体像 | [docs/blazor-guide-package/docs/](docs/blazor-guide-package/docs/00_README.md) |
| 3層アーキテクチャからの移行 | [18_3層アーキテクチャからの移行ガイド.md](docs/blazor-guide-package/docs/18_3層アーキテクチャからの移行ガイド.md) |
| AI実装ガイド | [19_AIへの実装ガイド.md](docs/blazor-guide-package/docs/19_AIへの実装ガイド.md) |

---

## 技術情報

| 項目 | 値 |
|------|-----|
| .NET | 9.0 |
| カタログバージョン | v2025.11.19 |
| パターン数 | 20 |
| データベース | PostgreSQL 17 |

**採用アーキテクチャ**: Vertical Slice Architecture (VSA) + CQRS + DDD

---

## ライセンス

このプロジェクトは実証実験用のサンプルコードです。
