# Consumer Example（カタログ利用例）

このフォルダには、**このカタログを利用する側のプロジェクト**向けの例示ファイルが含まれています。

## 含まれるファイル

| ファイル | 目的 |
|----------|------|
| `patterns.manifest.json` | 採用パターンを宣言するマニフェストの例 |
| `patterns.manifest.schema.json` | マニフェストのJSONスキーマ |
| `patterns.manifest.README.md` | マニフェストの使い方 |

## 使い方

新規プロジェクトでこのカタログを利用する場合：

1. このフォルダの内容をプロジェクトルートにコピー
2. `patterns.manifest.json` を編集して使用するパターンを選択
3. `catalog_index` をGitHubタグ参照に変更

```json
{
  "catalog_index": "github:akiramei/blazor-enterprise-architecture-poc/catalog/index.json@v2025.11"
}
```

## 詳細

- [patterns.manifest.README.md](patterns.manifest.README.md) - 詳細な使い方
- [catalog/README.md](../README.md) - カタログ全体の説明

---

**最終更新: 2025-11-26**
