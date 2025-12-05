# Consumer Example（カタログ利用例）

このフォルダには、**このカタログを利用する側のプロジェクト**向けの例示ファイル・テンプレートが含まれています。

---

## 含まれるファイル

### パターンマニフェスト（レガシー）

| ファイル | 目的 |
|----------|------|
| `patterns.manifest.json` | 採用パターンを宣言するマニフェストの例 |
| `patterns.manifest.schema.json` | マニフェストのJSONスキーマ |
| `patterns.manifest.README.md` | マニフェストの使い方 |

### AIワークフローテンプレート（推奨）

| ファイル | 目的 |
|----------|------|
| `templates/AI_WORKFLOW.md.template` | まっさらからの開発手順 |
| `templates/AI_PROMPTS.md.template` | 4フェーズ用プロンプト集 |
| `templates/CATALOG_VERSION.md.template` | カタログバージョン固定用 |

---

## 使い方（推奨：ローカルベンダリング）

### Step 1: カタログをベンダリング

```bash
# プロジェクト作成
mkdir ProjectX
cd ProjectX
git init

# カタログをコピー（ベンダリング）
git clone https://github.com/akiramei/blazor-enterprise-architecture-poc temp-catalog
cp -r temp-catalog/catalog ./catalog
rm -rf temp-catalog
```

### Step 2: テンプレートをコピー

```bash
# ディレクトリ作成
mkdir -p docs specs manifests

# テンプレートをコピー
cp catalog/consumer-example/templates/AI_WORKFLOW.md.template docs/AI_WORKFLOW.md
cp catalog/consumer-example/templates/AI_PROMPTS.md.template docs/AI_PROMPTS.md
cp catalog/consumer-example/templates/CATALOG_VERSION.md.template docs/CATALOG_VERSION.md
```

### Step 3: CATALOG_VERSION.md を編集

```bash
# コミットIDを取得（コピー前に実行）
cd temp-catalog && git rev-parse HEAD && cd ..

# docs/CATALOG_VERSION.md を編集
# - {COMMIT_HASH} を取得したコミットIDに置換
# - {VERSION} をカタログバージョンに置換
# - {YYYY-MM-DD} を今日の日付に置換
```

### Step 4: プレースホルダを置換

```bash
# テンプレート内の {DATE} を今日の日付に置換
sed -i 's/{DATE}/2025-12-05/g' docs/AI_WORKFLOW.md
sed -i 's/{DATE}/2025-12-05/g' docs/AI_PROMPTS.md
sed -i 's/{DATE}/2025-12-05/g' docs/CATALOG_VERSION.md
```

### Step 5: AI起動

```
このプロジェクトは ./catalog/ のカタログに準拠します。
最初に catalog/LLM_BOOTSTRAP.md を読んでください。
```

---

## ディレクトリ構造（ベンダリング後）

```
ProjectX/
├── catalog/                    # ★ ベンダリングしたカタログ（読み取り専用）
│   ├── LLM_BOOTSTRAP.md
│   ├── CLAUDE.md
│   ├── patterns/
│   └── ...
│
├── docs/
│   ├── AI_WORKFLOW.md          # テンプレートからコピー
│   ├── AI_PROMPTS.md           # テンプレートからコピー
│   └── CATALOG_VERSION.md      # テンプレートからコピー
│
├── specs/                      # SPEC（業務仕様）
│   └── {feature}/
│       └── {slice}.spec.yaml
│
├── manifests/                  # Manifest（パターン選択記録）
│   └── {feature}/
│       └── {slice}.manifest.yaml
│
└── src/                        # 生成されたコード
```

---

## ローカルベンダリングのメリット

| 観点 | リモート参照 | ローカルベンダリング |
|------|-------------|-------------------|
| ネットワーク依存 | あり | **なし** |
| バージョン固定 | タグで指定 | **コミットで完全固定** |
| 再現性 | タグ削除リスク | **100%** |
| 閉域網対応 | 不可 | **可能** |
| 監査対応 | 外部依存 | **完全トレーサブル** |

---

## 詳細ドキュメント

| ドキュメント | 目的 |
|-------------|------|
| [INTEGRATION_WITH_SPEC.md](../INTEGRATION_WITH_SPEC.md) | SPEC/Manifest連携設計 |
| [LLM_BOOTSTRAP.md](../LLM_BOOTSTRAP.md) | AI向け入口（正本） |
| [patterns.manifest.README.md](patterns.manifest.README.md) | パターンマニフェストの使い方 |
| [catalog/README.md](../README.md) | カタログ全体の説明 |

---

**最終更新: 2025-12-05**
