# INTEGRATION_WITH_SPEC.md - SPEC/Manifest連携設計

> **目的**: このカタログをSPEC/Manifestと組み合わせて使う方法を説明する公式ドキュメント

---

## TL;DR（10行サマリ）

```
- SPEC = What（業務仕様）を記述する。技術手段は書かない。
- Manifest = SPECとカタログを橋渡しする「Bridgeレイヤー」。
- Catalog = How（実装パターン）を定義する。ProjectXにはvendoringして固定バージョンを使う。

- 開発フロー:
  1. SPECでSliceの業務仕様を定義（characteristics, domain_rules など）
  2. Catalogのapplicabilityに基づき、Manifestでパターンを選択（from_catalog）
  3. Manifestでcreative/non-creativeの境界を宣言（creative_boundary）
  4. CLAUDE.mdのStandard Generation Flowに従ってコード生成

- ケースA（カタログ充実）: Manifestは薄く、ほぼfrom_catalogのみ。
- ケースB（カタログ部分）: Manifestにsupplemental_guidanceで補足を追加。
- ケースC（カタログなし）: Manifestにadditional_patternsとgeneration_hintsを厚く書く。
```

**詳細は以降のセクションを参照してください。**

---

## 1. 三層モデル概要

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

## バージョン

- **v1.0.0** (2025-12-05): 初版リリース
  - 三層モデルの説明
  - Case A/B/C の分類
  - ローカルカタログの使用方法
  - Standard Generation Flow
