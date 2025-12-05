# manifests/ - パターン選択記録（Manifest）

このディレクトリには、SPECに対応するManifest（パターン選択記録）を格納します。

## ファイル命名規則

```
manifests/{feature}/{slice}.manifest.yaml
```

例:
- `manifests/loan/LendBook.manifest.yaml`
- `manifests/reservation/ReserveBook.manifest.yaml`

---

## Manifest の役割

Manifestは **SPEC（What）とカタログ（How）の橋渡し（Bridge）** です。

```
SPEC (What)           →  Manifest (Bridge)        →  カタログ (How)
specs/{feature}/         manifests/{feature}/        catalog/patterns/
.spec.yaml               .manifest.yaml              *.yaml
```

---

## Manifest の構造

### 必須セクション

| セクション | 目的 |
|-----------|------|
| `meta` | 基本情報（slice名、SPEC参照、生成情報） |
| `catalog_binding` | カタログとのマッチング結果 |
| `creative_boundary` | 創造的/非創造的領域の分離 |

### オプションセクション

| セクション | 目的 |
|-----------|------|
| `spec_derived` | カタログパターンだけでは埋まらない補足 |
| `generation_hints` | コード生成時の注意事項 |

---

## meta セクション

```yaml
meta:
  slice: LendBook
  spec_path: "../../specs/loan/LendBook.spec.yaml"
  generated_at: "2025-12-05T10:00:00Z"
  generator: "ai"
  author: "ai+human-review"    # ai | human | ai+human-review
  review_status: "approved"    # draft | reviewed | approved
```

### author の値

| 値 | 意味 |
|----|------|
| `ai` | AIが自動生成、レビューなし |
| `human` | 人間が手動作成 |
| `ai+human-review` | AIが生成し、人間がレビュー済み |

### review_status の値

| 値 | 意味 |
|----|------|
| `draft` | 下書き（未レビュー） |
| `reviewed` | レビュー済み（承認待ち） |
| `approved` | 承認済み（本番利用可） |

---

## creative_boundary について

Manifestの核心部分です。どこがパターンで決まり、どこが創造的判断かを明確にします。

```yaml
creative_boundary:
  non_creative:
    # カタログパターンで決まる部分
    - category: "command_structure"
      provider: "feature-create-entity"
    - category: "validation_pipeline"
      provider: "validation-behavior"

  creative:
    # AI/人間が判断する部分
    - area: domain_model
      owner: ai
    - area: validation_logic
      owner: ai
    - area: ui_layout
      owner: ai
```

---

## 生成フロー

### Step 1: AIにManifestを生成させる

```
SPECファイル specs/loan/LendBook.spec.yaml を読み、
catalog/CHARACTERISTICS_CATALOG.md のマッピング規則に従って
Manifestを生成してください。
```

### Step 2: 人間がレビュー

確認ポイント：
- `from_catalog` のパターン選択は妥当か
- `creative` に漏れはないか
- `supplemental_guidance` で補足すべき点はないか

### Step 3: ステータス更新

レビュー後、`meta` を更新：
```yaml
meta:
  author: "ai+human-review"
  review_status: "approved"
```

---

## テンプレート

新規Manifestを作成する際は、以下のテンプレートを使用してください：

- **テンプレート**: `catalog/scaffolds/manifest-template.yaml`
- **実例**: `manifests/loan/LendBook.manifest.yaml`

---

## チェックリスト

- [ ] `meta.spec_path` がSPECファイルを正しく参照している
- [ ] `catalog_binding.from_catalog` のパターン選択をレビューした
- [ ] `creative_boundary` の分類が妥当か確認した
- [ ] 必要に応じて `supplemental_guidance` を追記した
- [ ] `meta.author` と `meta.review_status` を更新した
