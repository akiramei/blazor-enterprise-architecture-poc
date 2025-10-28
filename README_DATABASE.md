# データベースセットアップ手順

このプロジェクトではPostgreSQLを使用します。

## Podmanでのセットアップ（推奨）

### 1. PostgreSQLコンテナの起動

```bash
# コンテナを起動
podman compose up -d

# コンテナの状態を確認
podman compose ps

# ログを確認（オプション）
podman compose logs postgres
```

### 2. データベースマイグレーションの適用

```bash
# プロジェクトルートディレクトリで実行
dotnet ef database update --project src/ProductCatalog.Infrastructure --startup-project src/ProductCatalog.Web
```

### 3. アプリケーションの起動

```bash
dotnet run --project src/ProductCatalog.Web
```

ブラウザで `https://localhost:5001` にアクセスしてください。

### 4. コンテナの停止

```bash
# コンテナを停止（データは保持）
podman compose stop

# コンテナを停止して削除（データは保持）
podman compose down

# コンテナとボリュームを削除（データも削除）
podman compose down -v
```

## 接続情報

- **ホスト**: localhost
- **ポート**: 5432
- **データベース名**: ProductCatalogDb
- **ユーザー名**: postgres
- **パスワード**: postgres

接続文字列: `Host=localhost;Port=5432;Database=ProductCatalogDb;Username=postgres;Password=postgres`

## トラブルシューティング

### ポート5432が既に使用されている

他のPostgreSQLが起動している可能性があります：

```bash
# Windowsでポート使用状況を確認
netstat -ano | findstr :5432

# compose.ymlのポートを変更（例: 5433:5432）
# その後、appsettings.jsonの接続文字列も Port=5433 に変更
```

### コンテナが起動しない

```bash
# ログを確認
podman compose logs postgres

# 既存コンテナを削除して再起動
podman compose down -v
podman compose up -d
```

### マイグレーションがエラーになる

```bash
# dotnet-ef ツールがインストールされているか確認
dotnet ef --version

# インストールされていない場合
dotnet tool install --global dotnet-ef

# パスを更新
# Windowsの場合、コマンドプロンプトを再起動
```

## データベース管理ツール

### psqlでの接続（Podman経由）

```bash
podman exec -it productcatalog-postgres psql -U postgres -d ProductCatalogDb
```

### pgAdmin（GUI）

1. pgAdmin 4をインストール
2. サーバー接続情報を上記の接続情報で設定

### VS Code拡張機能

- **PostgreSQL** (by Chris Kolkman)

## 開発Tips

### マイグレーションの作成

```bash
dotnet ef migrations add <MigrationName> --project src/ProductCatalog.Infrastructure --startup-project src/ProductCatalog.Web
```

### マイグレーションの取り消し

```bash
dotnet ef migrations remove --project src/ProductCatalog.Infrastructure --startup-project src/ProductCatalog.Web
```

### データベースのリセット

```bash
# 1. データベースを削除
dotnet ef database drop --project src/ProductCatalog.Infrastructure --startup-project src/ProductCatalog.Web --force

# 2. マイグレーションを再適用
dotnet ef database update --project src/ProductCatalog.Infrastructure --startup-project src/ProductCatalog.Web
```
