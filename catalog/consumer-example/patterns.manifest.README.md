# patterns.manifest.json の使い方

> **このフォルダについて**: ここにあるファイルは、このカタログを**利用する側のプロジェクト**向けの例示です。
> カタログ本体の開発には直接関係しません。

このファイルは、プロジェクトで使用するパターンの採用結果を記録します。
新規プロジェクトでこのカタログを利用する場合、このフォルダの内容をプロジェクトルートにコピーしてください。

---

## 📝 catalog_index の設定

### 開発環境（ローカル）

カタログをローカルにクローンしている場合：

```json
{
  "catalog_index": "./catalog/index.json"
}
```

**メリット:**
- インターネット接続不要
- カタログの変更を即座に反映
- デバッグが容易

---

### 本番環境（GitHub参照）

本番環境やCI/CDでは**GitHubタグ固定**を使用してください：

```json
{
  "catalog_index": "github:akiramei/blazor-enterprise-architecture-poc/catalog/index.json@v2025.11.19"
}
```

**メリット:**
- 再現性の保証（同じバージョンを常に取得）
- バージョン管理
- 複数プロジェクトでの共有

---

## 🏷️ タグの作成方法

本番環境で使用する前に、GitHubでタグを作成してください：

```bash
# タグを作成
git tag -a v2025.11 -m "Pattern Catalog v2025.11"

# タグをプッシュ
git push origin v2025.11
```

---

## 🔄 切り替えのタイミング

### ローカルパスを使う場合
- カタログを開発中
- 新しいパターンを追加中
- パターンの修正をテスト中

### GitHubタグを使う場合
- カタログが安定版に到達
- 他のプロジェクトで使用する場合
- CI/CDで自動検証する場合

---

## 📊 検証方法

### ローカルでの検証

```powershell
# patterns.manifest.json を検証
./scripts/pattern-scaffolder.ps1 -Command validate

# 選択されたパターンを表示
./scripts/pattern-scaffolder.ps1 -Command list
```

### CI/CDでの検証

GitHub Actions が自動的に検証します：
- `.github/workflows/validate-patterns.yml`

---

## 🎯 推奨設定

### 新規プロジェクトでの利用

```json
{
  "catalog_index": "github:akiramei/blazor-enterprise-architecture-poc/catalog/index.json@v2025.11"
}
```

GitHubタグを使用して、安定版を参照。

---

## 🔧 トラブルシューティング

### エラー: "404: Not Found"

**原因:** GitHubタグが存在しない

**解決策:**
1. ローカルパスに変更: `"./catalog/index.json"`
2. または、タグを作成してプッシュ

### エラー: "カタログファイルが見つかりません"

**原因:** ローカルパスが間違っている

**解決策:**
- パスを確認: `./catalog/index.json` （ルートディレクトリから相対パス）
- ファイルの存在を確認: `Test-Path ./catalog/index.json`

---

**最終更新: 2025-11-26**
