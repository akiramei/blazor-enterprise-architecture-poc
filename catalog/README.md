# Pattern Catalog

**v2025.11.23 - 20パターン実装済み**

このディレクトリは、業務アプリケーション開発で再利用可能なパターンのカタログです。

AIエージェント（Claude、ChatGPT等）が参照して、一貫性のあるコードを生成できるように設計されています。

---

## 🚨 実装前に必ず読むこと

> **⚠️ [COMMON_MISTAKES.md](COMMON_MISTAKES.md) を最初に読んでください**
>
> 以下のようなミスを防ぐための重要な注意事項がまとまっています：
> - Handler内でSaveChangesAsync()を呼ばない（TransactionBehaviorが自動実行）
> - EF Core + Value Objectの比較は `.Value` ではなくインスタンス同士で行う
> - 操作可否判定はBoundary経由で行う（UIにビジネスロジックを書かない）

---

## 📊 カタログ概要

### パターン統計（v2025.11.23）

| カテゴリ | パターン数 | 説明 |
|---------|----------|------|
| **Pipeline Behaviors** | 7 | 横断的関心事（自動実行） |
| **Feature Slices** | 8 | 垂直スライス（完全な機能） |
| **Domain Patterns** | 3 | ドメインパターン |
| **Query Patterns** | 2 | データ取得パターン |
| **Command Patterns** | 1 | データ変更パターン |
| **Layer Elements** | 2 | レイヤー要素 |
| **UI Patterns** | 1 | UI実装パターン |
| **合計** | **22** | |

### Phase別実装状況

- ✅ **Phase 1**: CRUD完成（Update, Delete）
- ✅ **Phase 2**: データ連携（CSV Import, File Upload）
- ✅ **Phase 3**: 監査・通知（Audit Log, SignalR Notification）
- ✅ **Phase 4**: 承認ワークフロー（Approval Workflow, State Machine, Approval History）

---

## 📖 パターン一覧

### 🔄 Pipeline Behaviors（横断的関心事）

実行順序（`order_hint`）に従って自動実行されるパイプライン処理。

| ID | パターン名 | 順序 | 目的 | 安定性 |
|---|----------|-----|------|-------|
| `metrics-behavior` | MetricsBehavior | 50 | ビジネスメトリクス・パフォーマンスメトリクス自動収集 | stable |
| `validation-behavior` | ValidationBehavior | 100 | FluentValidation による入力検証を自動実行 | stable |
| `authorization-behavior` | AuthorizationBehavior | 200 | ロールベース認可チェックを自動実行 | stable |
| `idempotency-behavior` | IdempotencyBehavior | 300 | Command の冪等性を保証し、重複実行を防止 | beta |
| `transaction-behavior` | TransactionBehavior | 400 | Command を単一トランザクションで実行、エラー時自動ロールバック | stable |
| `audit-log-behavior` | AuditLogBehavior | 550 | Command実行の監査ログを自動記録（操作履歴・変更履歴） | stable |
| `logging-behavior` | LoggingBehavior | 600 | すべての Command/Query のログ出力 | stable |

### 🎯 Feature Slices（垂直スライス）

完全な機能実装（Application + UI + API）。

| ID | パターン名 | 目的 | 安定性 |
|---|----------|------|-------|
| `feature-create-entity` | Create Entity Feature Slice | エンティティ作成の完全な垂直スライス | stable |
| `feature-search-entity` | Search Entity Feature Slice | エンティティ検索・フィルタリング・ページングの完全な垂直スライス | stable |
| `feature-export-csv` | CSV Export Feature Slice | 検索条件に基づいたデータをCSV形式でエクスポート | stable |
| `feature-update-entity` | Update Entity Feature Slice | エンティティ更新（楽観的排他制御・冪等性保証） | stable |
| `feature-delete-entity` | Delete Entity Feature Slice | エンティティ削除（論理削除 or 物理削除） | stable |
| `feature-import-csv` | CSV Import Feature Slice | CSVファイルからデータを一括インポート | stable |
| `feature-file-upload` | File Upload Feature Slice | ファイルアップロード（添付ファイル・画像アップロード） | stable |
| `feature-approval-workflow` | Approval Workflow Feature Slice | 稟議・申請の承認ワークフロー（マルチステップ承認） | stable |

### 🏛️ Domain Patterns（ドメインパターン）

ドメイン層の実装パターン。

| ID | パターン名 | 目的 | 安定性 |
|---|----------|------|-------|
| `domain-state-machine` | State Machine Pattern | ステートマシンによる状態遷移管理（不正な遷移を防止） | stable |
| `domain-approval-history` | Approval History Pattern | 承認履歴を記録・追跡（誰が・いつ・何を承認/却下したか） | stable |
| `boundary-pattern` | Boundary Pattern | 操作可否判定をドメイン層に配置し、UIから業務ルールを分離 | stable |

### 🔍 Query Patterns（データ取得）

| ID | パターン名 | 目的 | 安定性 |
|---|----------|------|-------|
| `query-get-list` | GetListQuery Pattern | 全件取得クエリ（キャッシュ対応） | stable |
| `query-service-pattern` | Query Service Pattern | Query側の実装統一（AsNoTracking最適化） | stable |

### ✏️ Command Patterns（データ変更）

| ID | パターン名 | 目的 | 安定性 |
|---|----------|------|-------|
| `command-create` | CreateCommand Pattern | 新規エンティティ作成コマンド | stable |

### 📦 Layer Elements（レイヤー要素）

| ID | パターン名 | 目的 | レイヤー | 安定性 |
|---|----------|------|---------|-------|
| `layer-store` | Store Pattern | UI層の状態管理とI/Oを担当 | UI | stable |
| `layer-pageactions` | PageActions Pattern | UI手順のオーケストレーションを担当 | UI | stable |

### 🎨 UI Patterns（UI実装パターン）

| ID | パターン名 | 目的 | 安定性 |
|---|----------|------|-------|
| `realtime-notification-pattern` | Real-time Notification Pattern (SignalR) | SignalRを使用したリアルタイム通知でUI自動更新 | stable |

---

## 📁 ディレクトリ構造

```
catalog/
├── README.md                         # このファイル
├── COMMON_MISTAKES.md                # ⚠️ 実装前に必ず読む（頻出ミス集）
├── AI_USAGE_GUIDE.md                 # AI向けの利用ガイド
├── PATTERN_SELECTION_GUIDE.md        # パターン選択ガイド
├── DECISION_FLOWCHART.md             # 意思決定フローチャート
├── index.json                        # パターンカタログの索引（マスターファイル）
├── patterns/                         # Pipeline Behaviors, Domain Patterns等
│   ├── validation-behavior.yaml
│   ├── transaction-behavior.yaml
│   ├── audit-log-behavior.yaml
│   ├── domain-state-machine.yaml
│   └── ...
├── features/                         # Feature Slices（垂直スライス）
│   ├── feature-create-entity.yaml
│   ├── feature-approval-workflow.yaml
│   └── ...
├── layers/                           # Layer Elements（レイヤー要素）
│   ├── layer-store.yaml
│   └── layer-pageactions.yaml
├── behaviors/                        # Pipeline Behaviors（シンボリックリンク）
└── domain-patterns/                  # Domain Patterns（シンボリックリンク）
```

---

## 🚀 クイックスタート

### 1. カタログを参照する

プロジェクトルートに `patterns.manifest.json` を作成:

```json
{
  "$schema": "./patterns.manifest.schema.json",
  "catalog_version": "v2025.11.19",
  "catalog_index": "./catalog/index.json",
  "selected_patterns": [
    {
      "id": "validation-behavior",
      "version": "1.3.0",
      "enabled": true,
      "order": 100
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
stability: stable

# AI選択ヒント
ai_selection_hints:
  trigger_phrases: ["入力検証", "バリデーション"]
  confidence_keywords:
    high: ["validation", "検証"]
  typical_requests:
    - "入力値をチェックしてください"

# 実装テンプレート
implementation:
  file_path: "src/Shared/Infrastructure/Behaviors/ValidationBehavior.cs"
  template: |
    public sealed class ValidationBehavior<TRequest, TResponse> { }

# AI向けガイダンス
ai_guidance:
  when_to_use:
    - "Command の入力検証が必要な場合"
  common_mistakes:
    - mistake: "Validator を DI 登録し忘れる"
      solution: "services.AddValidatorsFromAssembly()"

# エビデンス（実装例）
evidence:
  implementation_file: "src/Shared/Infrastructure/Behaviors/ValidationBehavior.cs"

# 変更履歴
changelog:
  - version: 1.3.0
    date: 2025-11-05
    changes: ["Result 型への対応"]
```

---

## 🔄 バージョン管理

### セマンティックバージョニング

- **Major**: 破壊的変更
- **Minor**: 後方互換性のある機能追加
- **Patch**: バグ修正

### 安定性レベル

| レベル | 説明 |
|-------|------|
| **stable** | 本番環境で使用可能。破壊的変更はメジャーバージョンアップ時のみ |
| **beta** | 機能は動作するが、APIが変更される可能性あり |
| **alpha** | 実験的機能。本番環境では使用非推奨 |
| **deprecated** | 非推奨。将来のバージョンで削除予定 |

---

## 🧪 検証

### ローカルでの検証

```powershell
# カタログ全体の検証
./scripts/validate-catalog-sync.ps1

# マニフェストの検証
./scripts/pattern-scaffolder.ps1 -Command validate
```

### CI/CD

GitHub Actions で自動検証:

- `.github/workflows/validate-patterns.yml`

---

## 🤝 コントリビューション

新しいパターンを追加する場合:

1. `catalog/patterns/` または `catalog/features/` に YAML ファイルを作成
2. `catalog/index.json` にパターンを登録
3. `./scripts/validate-catalog-sync.ps1` で検証
4. プルリクエストを作成

### パターン作成のガイドライン

- **id**: kebab-case（例: `validation-behavior`）
- **version**: セマンティックバージョニング
- **category**: 適切なカテゴリを選択
- **ai_selection_hints**: AIが適切にパターンを選択できるようにトリガーフレーズを含める
- **ai_guidance**: AI向けの詳細なガイダンスを含める
- **evidence**: 実装例へのファイルパスを明示
- **stability**: 安定性レベルを明記

---

## 📞 サポート

- **GitHub Issues**: https://github.com/akiramei/blazor-enterprise-architecture-poc/issues
- **ドキュメント**: docs/blazor-guide-package/

---

## 📄 ライセンス

MIT License

---

**最終更新: 2025-11-23**
**カタログバージョン: v2025.11.23**
**パターン総数: 22**
