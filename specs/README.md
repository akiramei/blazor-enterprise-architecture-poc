# specs/ - 業務仕様（SPEC）

このディレクトリには、機能ごとの業務仕様（SPEC）を格納します。

## ファイル命名規則

```
specs/{feature}/{slice}.spec.yaml
```

例:
- `specs/loan/LendBook.spec.yaml`
- `specs/reservation/ReserveBook.spec.yaml`

---

## SPEC の構造

### 必須セクション

| セクション | 目的 |
|-----------|------|
| `meta` | 基本情報（slice名、BC名、バージョン） |
| `actor` | 誰が使うか |
| `domain_rules` | 業務ルール（DR1, DR2...） |
| `characteristics` | パターン選択のヒント |

### オプションセクション

| セクション | 目的 | いつ使うか |
|-----------|------|-----------|
| `catalog_interface` | カタログとの連携強化 | カタログ連携を活用したいSlice |
| `boundary` | UIの入出力仕様 | UIがある場合 |
| `scenarios` | ユースケースシナリオ | 複雑な業務フローがある場合 |
| `out_of_scope` | 対象外事項 | 誤解を防ぎたい場合 |

---

## catalog_interface について（重要）

**`catalog_interface` セクションは任意（オプション）です。**

### 書くべき場合
- カタログのパターン自動選択を活用したいSlice
- 複数のパターンを組み合わせる複雑な機能
- 非機能要件が多い機能

### 省略可能な場合
- 単純な内部用Slice
- パターン選択を手動で行う場合（`fallback_characteristics` なしで手動選択）
- 既存の類似Sliceをコピーして使う場合

### 省略時の動作
`catalog_interface` がない場合、AIは `characteristics` セクションのみを使って
パターンをマッチングします。

---

## characteristics の書き方

`characteristics` セクションには、以下のprefixでタグを記述します：

```yaml
characteristics:
  - op:mutates-state      # 操作タイプ
  - xcut:validation       # 横断的関心事
  - struct:single-aggregate  # 構造的特性
  - domain:library-core   # ドメイン分類
  - layer:full-slice      # 層の指定
```

### Prefix一覧

| Prefix | 用途 | 例 |
|--------|------|-----|
| `op:` | 操作タイプ | `op:mutates-state`, `op:read-only`, `op:batch` |
| `xcut:` | 横断的関心事 | `xcut:auth`, `xcut:audit`, `xcut:validation`, `xcut:cache` |
| `struct:` | 構造的特性 | `struct:single-aggregate`, `struct:multi-aggregate` |
| `domain:` | ドメイン分類 | `domain:library-core`, `domain:reservation`, `domain:workflow` |
| `layer:` | 層の指定 | `layer:full-slice`, `layer:ui`, `layer:domain-only` |

**詳細**: `catalog/CHARACTERISTICS_CATALOG.md`

---

## テンプレート

新規SPECを作成する際は、以下のテンプレートを使用してください：

- **フルテンプレート**: `catalog/scaffolds/spec-template.yaml`
- **実例**: `specs/loan/LendBook.spec.yaml`

---

## チェックリスト

- [ ] `meta` に基本情報を記入した
- [ ] `actor` を明確にした
- [ ] `domain_rules` に業務ルールを列挙した
- [ ] `characteristics` を適切なprefixで記述した
- [ ] UIがある場合、`boundary` セクションを含めた
- [ ] 複雑な機能の場合、`catalog_interface` を検討した
