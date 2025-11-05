# Pattern Catalog

このディレクトリは、業務アプリケーション開発で再利用可能なパターンのカタログです。

AIエージェント（Claude、ChatGPT等）が参照して、一貫性のあるコードを生成できるように設計されています。

---

## 📁 ディレクトリ構造

```
catalog/
├── README.md                         # このファイル
├── AI_USAGE_GUIDE.md                 # AI向けの利用ガイド
├── index.json                        # パターンカタログの索引
└── patterns/                         # 個別パターン定義（YAML）
    ├── validation-behavior.yaml
    ├── transaction-behavior.yaml
    ├── authorization-behavior.yaml
    ├── logging-behavior.yaml
    ├── metrics-behavior.yaml
    ├── idempotency-behavior.yaml
    ├── query-get-list.yaml
    └── command-create.yaml
```

---

## 🚀 クイックスタート

### 1. カタログを参照する

プロジェクトルートに `patterns.manifest.json` を作成:

```json
{
  "$schema": "./patterns.manifest.schema.json",
  "catalog_index": "github:akiramei/blazor-enterprise-architecture-poc/catalog/index.json@v2025.11",
  "selected_patterns": [
    {
      "id": "validation-behavior",
      "version": "1.3.0",
      "mode": "package"
    }
  ]
}
```

### 2. マニフェストを検証

```powershell
./scripts/pattern-scaffolder.ps1 -Command validate
```

### 3. 選択されたパターンを確認

```powershell
./scripts/pattern-scaffolder.ps1 -Command list
```

---

## 📖 パターンの種類

### Pipeline Behaviors（横断的関心事）

実行順序（`order_hint`）に従って自動実行されるパイプライン処理。

| パターン | 順序 | 目的 |
|---------|-----|------|
| **MetricsBehavior** | 50 | メトリクス収集 |
| **ValidationBehavior** | 100 | 入力検証 |
| **AuthorizationBehavior** | 200 | 認可チェック |
| **IdempotencyBehavior** | 300 | 冪等性保証 |
| **TransactionBehavior** | 400 | トランザクション管理 |
| **LoggingBehavior** | 600 | ログ出力 |

### Query Patterns（データ取得）

| パターン | 目的 |
|---------|------|
| **query-get-list** | 全件取得（キャッシュ対応） |
| **query-get-by-id** | ID指定取得 |
| **query-search** | 検索・フィルタリング・ページング |

### Command Patterns（データ変更）

| パターン | 目的 |
|---------|------|
| **command-create** | 新規作成 |
| **command-update** | 更新 |
| **command-delete** | 削除 |
| **command-bulk-operation** | 一括処理 |

---

## 🤖 AIによる利用

### 推奨プロンプト

```
このプロジェクトには catalog/ ディレクトリにパターンカタログがあります。
新機能を実装する際は、必ず以下の手順で進めてください:

1. catalog/index.json を読み込み、適切なパターンを検索
2. 該当パターンの YAML ファイルを読み込み
3. テンプレート変数を置換してコードを生成
4. ai_guidance の common_mistakes を確認
5. evidence のファイルパスを提示

必ず catalog/ を参照し、既存のパターンに従ってコードを生成してください。
```

詳細は [AI_USAGE_GUIDE.md](AI_USAGE_GUIDE.md) を参照。

---

## 📝 パターン定義の構造

各パターンは YAML 形式で定義され、以下の情報を含みます:

```yaml
id: validation-behavior
version: 1.3.0
name: ValidationBehavior
category: pipeline-behavior
intent: "FluentValidation による入力検証"
order_hint: 100

# DI登録とNuGet依存関係
wiring:
  service_registrations:
    - "services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>))"
  dependencies:
    nuget:
      - FluentValidation: "^11.0.0"

# 前提条件
preconditions:
  - "FluentValidation がインストールされている"

# 実装テンプレート
implementation:
  file_path: "src/{BoundedContext}/..."
  template: |
    public sealed class ValidationBehavior<TRequest, TResponse> { }

# 使用例
example_usage: |
  public sealed record CreateProductCommand(...);

# テストケース
tests:
  - name: "未入力で検証エラー"
    given: "Name が空文字列"
    expect: "検証エラー"

# AI向けガイダンス
ai_guidance:
  when_to_use:
    - "Command の入力検証が必要な場合"
  common_mistakes:
    - mistake: "Validator を DI 登録し忘れる"
      solution: "services.AddValidatorsFromAssembly()"

# 変更履歴
changelog:
  - version: 1.3.0
    date: 2025-11-05
    changes: ["Result 型への対応"]

# エビデンス（実装例）
evidence:
  implementation_file: "src/ProductCatalog/..."
```

---

## 🔄 バージョン管理

### セマンティックバージョニング

- **Major**: 破壊的変更
- **Minor**: 後方互換性のある機能追加
- **Patch**: バグ修正

### タグ固定

カタログを参照する際は、必ずタグを固定してください:

```json
{
  "catalog_index": "github:akiramei/blazor-enterprise-architecture-poc/catalog/index.json@v2025.11"
}
```

これにより、同じバージョンのパターンを常に取得でき、再現性が保証されます。

---

## 🧪 検証

### ローカルでの検証

```powershell
# カタログ全体の検証
./scripts/validate-catalog.ps1

# マニフェストの検証
./scripts/pattern-scaffolder.ps1 -Command validate
```

### CI/CD

GitHub Actions で自動検証:

- `.github/workflows/validate-patterns.yml`

---

## 📊 メトリクス

### カタログの統計

```powershell
$catalogIndex = Get-Content ./catalog/index.json | ConvertFrom-Json

Write-Host "パターン総数: $($catalogIndex.patterns.Count)"
Write-Host "  - Pipeline Behaviors: $(($catalogIndex.patterns | Where-Object { $_.category -eq 'pipeline-behavior' }).Count)"
Write-Host "  - Query Patterns: $(($catalogIndex.patterns | Where-Object { $_.category -eq 'query-pattern' }).Count)"
Write-Host "  - Command Patterns: $(($catalogIndex.patterns | Where-Object { $_.category -eq 'command-pattern' }).Count)"
```

---

## 🤝 コントリビューション

新しいパターンを追加する場合:

1. `catalog/patterns/` に YAML ファイルを作成
2. `catalog/index.json` にパターンを登録
3. `./scripts/validate-catalog.ps1` で検証
4. プルリクエストを作成

### パターン作成のガイドライン

- **id**: kebab-case（例: `validation-behavior`）
- **version**: セマンティックバージョニング
- **category**: 適切なカテゴリを選択
- **ai_guidance**: AI向けの詳細なガイダンスを含める
- **evidence**: 実装例へのファイルパスを明示
- **tests**: 期待される動作をGiven-When-Then形式で記述

---

## 📞 サポート

- **GitHub Issues**: https://github.com/akiramei/blazor-enterprise-architecture-poc/issues
- **ドキュメント**: docs/blazor-guide-package/

---

## 📄 ライセンス

MIT License

---

**最終更新: 2025-11-05**
**カタログバージョン: v2025.11.0**
