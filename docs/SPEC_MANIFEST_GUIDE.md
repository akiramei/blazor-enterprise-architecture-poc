# SPEC + Manifest 開発フロー ガイド

> **このドキュメントの目的**: カタログを使ってアプリケーションを開発する際の「使い方」を説明します。

---

## 概要

このプロジェクトでは、**三層モデル**を採用しています：

```
SPEC (What)           →  Manifest (Bridge)        →  カタログ (How)
specs/{feature}/         manifests/{feature}/        catalog/patterns/
.spec.yaml               .manifest.yaml              *.yaml
```

| 層 | 役割 | 誰が書くか |
|----|------|-----------|
| SPEC | 業務仕様（What） | 人間（ドメインエキスパート/開発者） |
| Manifest | パターン選択記録（Bridge） | AI + 人間レビュー |
| カタログ | 実装パターン（How） | 既存（変更不要） |

---

## 開発フロー

### Step 1: SPEC を作成する

```bash
# 新規機能のSPECを作成
specs/{feature}/{slice}.spec.yaml
```

**使用テンプレート**: `catalog/scaffolds/spec-template.yaml`

**必須セクション**:
- `meta`: 基本情報
- `actor`: 誰が使うか
- `domain_rules`: 業務ルール
- `characteristics`: パターン選択のヒント

**オプションセクション**:
- `catalog_interface`: カタログ連携を強化したい場合
- `boundary`: UIがある場合

### Step 2: Manifest を生成する

```bash
# AIにManifestを生成させる
manifests/{feature}/{slice}.manifest.yaml
```

**使用テンプレート**: `catalog/scaffolds/manifest-template.yaml`

**AIへの指示例**:
```
SPECファイル specs/loan/LendBook.spec.yaml を読み、
catalog/CHARACTERISTICS_CATALOG.md のマッピング規則に従って
Manifestを生成してください。
```

**人間レビューのポイント**:
- `catalog_binding.from_catalog` のパターン選択は妥当か
- `creative_boundary.creative` に漏れはないか
- `supplemental_guidance` で補足すべき点はないか

### Step 3: コードを生成する

**AIへの指示**:
```
以下の順序でファイルを読み、Standard Generation Flowに従って
コードを生成してください：

1. specs/loan/LendBook.spec.yaml
2. manifests/loan/LendBook.manifest.yaml
3. catalog/CHARACTERISTICS_CATALOG.md
4. Manifestで参照されているパターンYAML
```

**生成フロー（CLAUDE.md参照）**:
1. Phase 0: 事前読み込み
2. Phase 1: 骨組み生成（Non-Creative）
3. Phase 2: Cross-cutting適用
4. Phase 3: Business Logic実装（Creative）
5. Phase 4: UI実装（Creative）

---

## ディレクトリ構造

```
プロジェクトルート/
├── specs/                          # SPEC（業務仕様）
│   ├── README.md                   # SPECの書き方ガイド
│   └── {feature}/
│       └── {slice}.spec.yaml
│
├── manifests/                      # Manifest（パターン選択記録）
│   ├── README.md                   # Manifestの書き方ガイド
│   └── {feature}/
│       └── {slice}.manifest.yaml
│
├── catalog/                        # カタログ（参照のみ）
│   ├── CHARACTERISTICS_CATALOG.md
│   ├── scaffolds/
│   │   ├── spec-template.yaml
│   │   └── manifest-template.yaml
│   └── patterns/
│
└── src/                            # 生成されたコード
    ├── Application/Features/
    └── Domain/
```

---

## characteristics タグの使い方

SPECの `characteristics` セクションには、以下のprefixでタグを記述します：

| Prefix | 用途 | 例 |
|--------|------|-----|
| `op:` | 操作タイプ | `op:mutates-state`, `op:read-only` |
| `xcut:` | 横断的関心事 | `xcut:auth`, `xcut:audit`, `xcut:validation` |
| `struct:` | 構造的特性 | `struct:single-aggregate`, `struct:multi-aggregate` |
| `domain:` | ドメイン分類 | `domain:library-core`, `domain:reservation` |
| `layer:` | 層の指定 | `layer:full-slice`, `layer:ui` |

**詳細**: `catalog/CHARACTERISTICS_CATALOG.md`

---

## よくあるパターン

### パターン1: 基本的なCRUD機能

```yaml
# SPEC
characteristics:
  - op:mutates-state
  - struct:single-aggregate
  - xcut:validation
  - layer:full-slice

# → 自動選択されるパターン
# - feature-create-entity
# - validation-behavior
# - transaction-behavior
```

### パターン2: 検索機能

```yaml
# SPEC
characteristics:
  - op:read-only
  - layer:full-slice

# → 自動選択されるパターン
# - query-get-list または feature-search-entity
```

### パターン3: 承認ワークフロー

```yaml
# SPEC
characteristics:
  - op:mutates-state
  - domain:workflow
  - xcut:audit
  - struct:multi-aggregate

# → 自動選択されるパターン
# - feature-approval-workflow
# - domain-state-machine
# - audit-log-behavior
```

---

## チェックリスト

### SPEC作成時
- [ ] `meta` に基本情報を記入した
- [ ] `actor` を明確にした
- [ ] `domain_rules` に業務ルールを列挙した
- [ ] `characteristics` を適切なprefixで記述した
- [ ] UIがある場合、`boundary` セクションを含めた

### Manifest生成後
- [ ] `from_catalog` のパターン選択をレビューした
- [ ] `creative_boundary` の分類が妥当か確認した
- [ ] 必要に応じて `supplemental_guidance` を追記した
- [ ] `review_status` を更新した

### コード生成後
- [ ] Handler内で `SaveChangesAsync()` を呼んでいない
- [ ] `Result<T>` パターンを使用している
- [ ] UIが `Features/{Feature}/` に配置されている
- [ ] ビルドが通る

---

## 参照ドキュメント

| ドキュメント | 目的 |
|-------------|------|
| `LLM_BOOTSTRAP.md` | AI向け入口（正本） |
| `CLAUDE.md` | 詳細ルール・Standard Generation Flow |
| `catalog/CHARACTERISTICS_CATALOG.md` | characteristics語彙定義 |
| `catalog/AI_USAGE_GUIDE.md` | カタログ利用ガイド |
| `catalog/COMMON_MISTAKES.md` | 頻出ミス回避 |

---

## 例: LendBook（貸出登録）

完全な実装例として、以下のファイルを参照してください：

- **SPEC**: `specs/loan/LendBook.spec.yaml`
- **Manifest**: `manifests/loan/LendBook.manifest.yaml`
- **生成コード**: `src/Application/Features/LendBook/`
- **ドメイン**: `src/Domain/LibraryManagement/`
