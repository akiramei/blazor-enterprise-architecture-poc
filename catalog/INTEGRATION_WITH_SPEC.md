# INTEGRATION_WITH_SPEC.md - spec-kit / カタログ統合ガイド

> **目的**: GitHub spec-kit とこのカタログを組み合わせて使う方法を説明する公式ドキュメント

---

## TL;DR（推奨ワークフロー）

```
1. spec-kit を初期化: specify init . --ai claude
2. カタログをベンダリング: catalog/ と speckit-extensions/ をコピー
3. 拡張コマンドを適用: speckit.plan.md を上書き
4. Constitution にカタログルールを追記
5. /speckit.specify → /speckit.plan → /speckit.tasks → /speckit.implement
```

---

## 0. spec-kit + カタログ統合セットアップ（推奨）

### 0.1 前提条件

- [uv](https://github.com/astral-sh/uv) がインストールされていること
- spec-kit CLI: `uv tool install specify-cli --from git+https://github.com/github/spec-kit.git`

### 0.2 消費者プロジェクトのセットアップ

```bash
# 1. プロジェクト作成
mkdir MyProject && cd MyProject

# 2. spec-kit 初期化
specify init . --ai claude

# 3. カタログをベンダリング
git clone https://github.com/akiramei/blazor-enterprise-architecture-poc temp-catalog
cp -r temp-catalog/catalog ./catalog
rm -rf temp-catalog

# 4. 拡張コマンドを適用（spec-kit の plan.md を上書き）
cp catalog/speckit-extensions/commands/speckit.plan.md .claude/commands/

# 5. Constitution にカタログルールを追記
cat catalog/speckit-extensions/constitution-additions.md >> memory/constitution.md

# 6. バージョン情報を記録（任意）
echo "Catalog version: $(date +%Y-%m-%d)" > docs/CATALOG_VERSION.md
```

### 0.3 統合後のディレクトリ構造

```
MyProject/
├── .claude/commands/
│   ├── speckit.specify.md      ← spec-kit 標準
│   ├── speckit.plan.md         ← カタログ拡張版で上書き ★
│   ├── speckit.tasks.md        ← spec-kit 標準
│   └── speckit.implement.md    ← spec-kit 標準
├── memory/
│   └── constitution.md         ← カタログルール追記済み ★
├── templates/                  ← spec-kit 標準
├── specs/                      ← 機能仕様（spec-kit 形式）
├── catalog/                    ← カタログ（読み取り専用）★
│   ├── index.json
│   ├── patterns/*.yaml
│   └── speckit-extensions/     ← 拡張ファイル（コピー元）
└── src/                        ← 生成コード
```

### 0.4 ワークフロー

```
/speckit.specify  →  /speckit.plan  →  /speckit.tasks  →  /speckit.implement
業務仕様(What)        技術計画(How)      タスク分解         実装
                      ↓
                  Catalog Binding
                  (パターン選択)
                      ↓
               catalog/patterns/*.yaml
```

---

## 1. Alternative: SPEC/Manifest方式（従来方式）

> **注意**: 以下は spec-kit を使わない従来方式です。spec-kit 統合（セクション0）を推奨します。

このカタログは **三層モデル** で設計されています：

```
┌─────────────────────────────────────────────────────────────────────┐
│                         開発フロー                                    │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│   SPEC (What)        →     Manifest (Bridge)    →    カタログ (How)   │
│   業務仕様                  パターン選択記録           実装パターン      │
│                                                                     │
│   specs/{feature}/        manifests/{feature}/       catalog/        │
│   .spec.yaml              .manifest.yaml             patterns/*.yaml │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 各層の責務

| 層 | 役割 | 責任者 | 変更頻度 |
|----|------|--------|---------|
| **SPEC** | 業務仕様（What） | 人間（ドメインエキスパート/開発者） | 機能ごと |
| **Manifest** | パターン選択記録（Bridge） | AI + 人間レビュー | 機能ごと |
| **カタログ** | 実装パターン（How） | カタログメンテナ | まれ |

---

## 2. ファイル間の関係図

```
specs/loan/LendBook.spec.yaml
    │
    │ characteristics を抽出
    │ catalog_interface を参照
    ▼
manifests/loan/LendBook.manifest.yaml
    │
    │ catalog_binding.from_catalog でパターンを特定
    │ creative_boundary で創造的領域を分離
    ▼
catalog/patterns/*.yaml
    │
    │ テンプレートを適用
    │ ai_guidance に従って実装
    ▼
src/Application/Features/LendBook/
    ├── LendBookCommand.cs
    ├── LendBookCommandHandler.cs
    ├── LendBookCommandValidator.cs
    └── LendBook.razor
```

### 参照関係

```yaml
# SPEC → Manifest
manifest.meta.spec_path: "../../specs/loan/LendBook.spec.yaml"

# Manifest → カタログ
manifest.catalog_binding.from_catalog:
  - id: feature-create-entity
    matched_by: "op:mutates-state + struct:single-aggregate"

# カタログ → コード
pattern.template: |
  public sealed record {Feature}Command(...) : ICommand<Result<{ReturnType}>>;
```

---

## 3. カタログ成熟度とCase分類

### Case A: カタログが充実（Full Match）

```yaml
# Manifest
catalog_binding:
  match_result:
    status: "full"
    from_catalog:
      - id: feature-create-entity
      - id: validation-behavior
      - id: transaction-behavior
    unmatched: []
```

**特徴**:
- すべての要件がカタログパターンでカバー可能
- AIはテンプレート適用のみで実装完了
- 創造的判断が最小限

### Case B: カタログが部分的（Partial Match）

```yaml
# Manifest
catalog_binding:
  match_result:
    status: "partial"
    from_catalog:
      - id: feature-create-entity
    unmatched:
      - requirement: "特殊な重複チェックロジック"
        reason: "カタログに汎用パターンなし"

spec_derived:
  supplemental_guidance:
    - pattern_id: domain-validation-service
      guidance: |
        SPECのDR1を参考に、カスタム実装が必要。
        Entity.CanXxx() で判定ロジックを実装する。
```

**特徴**:
- 基本構造はカタログパターン使用
- 一部に創造的判断が必要
- `supplemental_guidance` で補足指示

### Case C: カタログ未整備（No Match）

```yaml
# Manifest
catalog_binding:
  match_result:
    status: "none"
    from_catalog: []
    unmatched:
      - requirement: "全体"
        reason: "該当パターンなし"

spec_derived:
  additional_patterns:
    - id: custom-pattern
      description: "このプロジェクト固有のパターン"
```

**特徴**:
- カタログパターンが使えない
- AIは SPEC から直接コード生成
- 将来的にパターン化を検討

---

## 4. ローカルカタログの使用方法

### 4.1 なぜローカルカタログか

| 観点 | リモート参照 | ローカルベンダリング |
|------|-------------|-------------------|
| ネットワーク依存 | あり | なし |
| バージョン固定 | タグで指定 | コミットで完全固定 |
| 再現性 | タグ削除リスク | 100% |
| 閉域網対応 | 不可 | 可能 |

### 4.2 ベンダリング手順

```bash
# 1. カタログをコピー
git clone https://github.com/akiramei/blazor-enterprise-architecture-poc temp-catalog
cp -r temp-catalog/catalog ./catalog
rm -rf temp-catalog

# 2. バージョン情報を記録
# docs/CATALOG_VERSION.md を作成（テンプレートは catalog/consumer-example/templates/ を参照）
```

### 4.3 Manifest でのローカル参照

```yaml
# manifests/{feature}/{slice}.manifest.yaml
catalog_binding:
  catalog:
    id: "blazor-vsa-catalog"
    local_path: "../catalog"                    # ★ ローカルパス
    upstream_repo: "https://github.com/akiramei/blazor-enterprise-architecture-poc"
    upstream_commit: "abc123def..."             # ★ コピー時のコミット
```

### 4.4 AIへの指示

```
このプロジェクトでは `./catalog/` のカタログを唯一の参照元とします。
外部ネットワークは参照しません。

カタログの変更が必要だと思った場合は、
コードを変更するのではなく「提案」としてテキストで説明してください。
```

---

## 5. characteristics からパターンへのマッピング

`CHARACTERISTICS_CATALOG.md` で定義された語彙を使用します。

### 5.1 基本マッピング

| characteristics | 対応パターン |
|----------------|-------------|
| `op:mutates-state` | feature-create-entity, feature-update-entity, feature-delete-entity |
| `op:read-only` | query-get-list, query-get-by-id, feature-search-entity |
| `xcut:auth` | authorization-behavior |
| `xcut:audit` | audit-log-behavior |
| `xcut:validation` | validation-behavior |
| `struct:state-machine` | domain-state-machine |
| `struct:multi-aggregate` | domain-validation-service |

### 5.2 暗黙適用パターン

`op:mutates-state` の場合、以下が自動適用：
- `validation-behavior`
- `transaction-behavior`
- `logging-behavior`

---

## 6. Standard Generation Flow

SPEC + Manifest がある場合の標準生成フロー。

### Phase 0: 事前読み込み

```
1. specs/{feature}/{slice}.spec.yaml を読む
2. manifests/{feature}/{slice}.manifest.yaml を読む
3. catalog/CHARACTERISTICS_CATALOG.md で語彙を確認
4. catalog_binding.from_catalog からパターンIDリストを取得
5. 各パターンYAMLを読み込む
```

### Phase 1: 骨組み生成（Non-Creative）

Manifest の `non_creative` 領域に基づき、パターンテンプレートを適用：

| カテゴリ | パターン | 生成物 |
|---------|---------|-------|
| command_structure | feature-create-entity | Command.cs, Handler.cs |
| validation_pipeline | validation-behavior | Validator.cs（自動適用） |
| transaction_management | transaction-behavior | （自動適用、SaveChangesAsync不要） |

### Phase 2: Cross-cutting適用

SPEC の `characteristics` に基づき、非機能要件パターンを適用：

| characteristic | 適用パターン | 実行順序 |
|---------------|-------------|:--------:|
| xcut:validation | validation-behavior | 100 |
| xcut:auth | authorization-behavior | 200 |
| xcut:transaction | transaction-behavior | 400 |
| xcut:audit | audit-log-behavior | 550 |

### Phase 3: Business Logic実装（Creative）

Manifest の `creative` 領域に基づき実装：

```
□ Domain Model設計
  └── SPECの domain_rules を参照
  └── Entity.CanXxx() メソッドを実装

□ Validation Logic
  └── SPECの scenarios.exceptions を参照
  └── FluentValidation ルールを実装
```

### Phase 4: UI実装（Creative）

SPEC の `boundary` 定義に基づき実装：

```
□ Store + State定義
□ PageActions定義
□ Razorコンポーネント
  └── Features/{Feature}/{Feature}.razor に配置
```

---

## 7. 関連ドキュメント

| ドキュメント | 目的 |
|-------------|------|
| `LLM_BOOTSTRAP.md` | AI向け入口（正本） |
| `CLAUDE.md` | 詳細ルール |
| `CHARACTERISTICS_CATALOG.md` | characteristics語彙定義 |
| `docs/SPEC_MANIFEST_GUIDE.md` | SPEC/Manifest開発フロー |
| `consumer-example/README.md` | 利用側向けガイド |

---

## 8. Process Gates（品質ゲート）

プロセスの各段階で品質を保証するためのゲートを定義。
Library8 ドッグフーディングで発見された問題を防止するために追加。

### ゲート一覧

| Phase | Gate | 検証内容 | 失敗時アクション |
|-------|------|---------|-----------------|
| plan → tasks | UI-IR Gate | UI がある場合、UI-IR が生成されているか | ERROR: /speckit.plan 再実行 |
| tasks → implement | Infra Gate | インフラ設定の整合性 | ERROR: 設定修正 |
| implement → run | Build Gate | dotnet build が成功するか | ERROR: ビルドエラー修正 |
| run | Runtime Gate | 実行時エラーがないか | troubleshooting 参照 |

### 8.1 UI-IR Gate（plan → tasks）

**目的**: UI 実装がある場合に UI-IR が生成されていることを保証

**チェック項目**:
```
□ plan.md に Boundary セクションがある
□ Boundary セクションに Intent が定義されている
□ specs/{feature}/{slice}.ui-ir.yaml が存在する
□ plan.md に UI-IR Summary セクションがある
```

**詳細**: `speckit-extensions/commands/speckit.tasks.md` の「UI-IR-to-Task Mapping」を参照

### 8.2 Infrastructure Gate（tasks → implement）

**目的**: 実装前にインフラ設定の整合性を検証

**チェック項目**:
```
□ DB プロバイダーと接続文字列が一致
□ wwwroot フォルダが存在（Web プロジェクトの場合）
□ UseStaticFiles() が設定されている
□ MudBlazor 使用時は CSS/JS リンクが存在
```

**詳細**: `catalog/checklists/infrastructure-setup.yaml` を参照

**自動検証スクリプト**:
```powershell
# PowerShell
.\scripts\Verify-Infrastructure.ps1 -ProjectPath ./src/UI

# Bash
./scripts/verify-infrastructure.sh ./src/UI
```

### 8.3 Build Gate（implement → run）

**目的**: 実装後にビルドが成功することを検証

**チェック項目**:
```
□ dotnet build が成功
□ 警告が許容範囲内
□ nullable 警告が解消されている
```

**推奨コマンド**:
```bash
dotnet build --warnaserror
```

### 8.4 Runtime Gate（run）

**目的**: 実行時エラーを早期検出

**チェック項目**:
```
□ アプリケーションが起動する
□ 静的ファイルが正しくロードされる
□ DB 接続が成功する
□ 主要機能が動作する
```

**エラー発生時**: `catalog/troubleshooting/blazor-runtime-errors.yaml` を参照

---

## 9. Troubleshooting

実行時エラーが発生した場合は、以下のカタログを参照：

| カテゴリ | ファイル | 内容 |
|---------|---------|------|
| Blazor 実行時エラー | `troubleshooting/blazor-runtime-errors.yaml` | DB, 静的ファイル, MediatR 関連 |
| インフラ設定 | `checklists/infrastructure-setup.yaml` | 設定不整合の検出・修正 |

---

## バージョン

- **v2.1.0** (2025-12-14): プロセスゲート追加
  - Process Gates セクションを追加（UI-IR, Infrastructure, Build, Runtime）
  - Troubleshooting 参照セクションを追加
  - Library8 ドッグフーディングのフィードバックを反映

- **v2.0.0** (2025-12-06): spec-kit 統合版
  - GitHub spec-kit との統合セットアップ手順を追加
  - speckit-extensions/ に拡張コマンドを配置
  - 従来の SPEC/Manifest 方式は Alternative として残存

- **v1.0.0** (2025-12-05): 初版リリース
  - 三層モデルの説明
  - Case A/B/C の分類
  - ローカルカタログの使用方法
  - Standard Generation Flow
