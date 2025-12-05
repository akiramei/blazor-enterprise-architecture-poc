# Blazor Enterprise Architecture PoC

**AI駆動開発のための、実装パターンカタログ付きエンタープライズアーキテクチャ実証実験**

---

## 👋 あなたはどちらですか？

| 目的 | 読むべきセクション |
|------|-------------------|
| **このカタログを自分のプロジェクトで使いたい** | → [📦 カタログ利用ガイド](#-カタログ利用ガイド) |
| **このVSASampleプロジェクトを動かしたい・開発したい** | → [🛠️ VSASample開発者向け](#️-vsasample開発者向け) |

---

## 📦 カタログ利用ガイド

このカタログは、AIエージェントと人間が協調して開発するための**実装パターンカタログ**です。

### カタログの特徴

- **20個のパターン**: Pipeline Behaviors、Feature Slices、Domain Patterns等
- **SPEC/Manifest方式**: 業務仕様（What）と実装パターン（How）を分離
- **実装証跡付き**: すべてのパターンが実際のコードとリンク

### AIと一緒に始める（推奨）

1. **新規プロジェクトを作成**
   ```bash
   mkdir MyProject && cd MyProject && git init
   ```

2. **AIエージェント（Claude Code等）を起動**

3. **AIに以下のURLを伝える**：
   ```
   https://raw.githubusercontent.com/akiramei/blazor-enterprise-architecture-poc/main/FIRST_CONTACT.md
   ```

4. **AIがセットアップを完了したら、要求仕様を伝える**

### 手動でセットアップする場合

詳細は [catalog/consumer-example/README.md](catalog/consumer-example/README.md) を参照してください。

### 開発フロー概要

```
要求仕様 → SPEC作成 → Manifest作成 → 計画 → 実装
```

詳細は [catalog/INTEGRATION_WITH_SPEC.md](catalog/INTEGRATION_WITH_SPEC.md) を参照。

---

## 🛠️ VSASample開発者向け

> このセクションは、VSASampleプロジェクト自体をビルド・実行・開発する人向けです。

### 🤖 For AI Agents（VSASample開発用）

> **AI向け入口**: まず [LLM_BOOTSTRAP.md](LLM_BOOTSTRAP.md) を読み、指示に従ってください。

| 順序 | ファイル | 目的 |
|:---:|----------|------|
| 1 | [CLAUDE.md](CLAUDE.md) | 実装ルール・禁止事項・パターン選択 |
| 2 | [catalog/AI_USAGE_GUIDE.md](catalog/AI_USAGE_GUIDE.md) | 詳細な実装ガイド |
| 3 | [catalog/index.json](catalog/index.json) | パターン索引・意思決定マトリクス |

**絶対禁止（これを破るとバグになる）**:
```
❌ Handler内でSaveChangesAsync()を呼ばない → TransactionBehaviorが自動実行
❌ Singletonでサービス登録しない → すべてScoped
❌ 例外をthrowしない → Result<T>で返す
```

### クイックスタート（VSASampleを動かす）

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

### ドキュメント

| 目的 | ファイル |
|------|----------|
| パターン一覧 | [catalog/README.md](catalog/README.md) |
| アーキテクチャ全体像 | [docs/blazor-guide-package/docs/](docs/blazor-guide-package/docs/00_README.md) |
| 3層アーキテクチャからの移行 | [18_3層アーキテクチャからの移行ガイド.md](docs/blazor-guide-package/docs/18_3層アーキテクチャからの移行ガイド.md) |
| AI実装ガイド | [19_AIへの実装ガイド.md](docs/blazor-guide-package/docs/19_AIへの実装ガイド.md) |

### 技術情報

| 項目 | 値 |
|------|-----|
| .NET | 9.0 |
| カタログバージョン | v2025.12.05 |
| パターン数 | 23 |
| データベース | PostgreSQL 17 |

**採用アーキテクチャ**: Vertical Slice Architecture (VSA) + CQRS + DDD

---

## ライセンス

このプロジェクトは実証実験用のサンプルコードです。
