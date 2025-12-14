# Lint Rules: 仕様量子化の破綻検知

> **目的**: decisions.yaml と policy.yaml の整合性・完全性を検証し、AIが誤解しやすい箇所を機械的に検出する
>
> **バージョン**: v1.1.0
> **作成日**: 2025-12-14

---

## 概要

このドキュメントは、仕様量子化プロセスで発生しやすい破綻を検出するためのルールを定義する。

### なぜ lint が必要か

| 問題 | 発生原因 | lint による検出 |
|------|---------|----------------|
| AIが勝手に穴埋め | 未決事項が policy.yaml に混入 | TBD/null 検出 |
| 矛盾する設定 | 複数の決定が衝突 | 条件矛盾検出 |
| 抜け漏れ | 状態遷移の穴 | 未カバー検出 |
| 用語不一致 | 同じものを別名で呼ぶ | 用語検証 |
| 過剰な複雑性 | 7±2 ルール違反 | 粒度検証 |

---

## Lint ルール一覧

### カテゴリ

| カテゴリ | 説明 | 重大度 | CI |
|---------|------|:------:|:--:|
| **L6xx** | 未離散化検証（UNQUANTIZED） | Critical | **fail** |
| **L1xx** | 構造検証（7±2ルール等） | Medium | warn |
| **L2xx** | 整合性検証（矛盾検出） | High | fail |
| **L3xx** | 完全性検証（抜け漏れ） | High | fail |
| **L4xx** | 用語検証（不一致） | Medium | warn |
| **L5xx** | Layer 検証（層の分離） | Critical | fail |

> **注意**: L6xx は最初に実行し、fail した場合は後続の検証を行わない。

---

## L1xx: 構造検証

### L101: 7±2 ルール違反（options）

**対象**: decisions.yaml の `decisions[].options`

**ルール**: 選択肢は最大7個まで

```yaml
# NG: 8個以上の選択肢
options:
  - "1日"
  - "2日"
  - "3日"
  - "5日"
  - "7日"
  - "14日"
  - "21日"
  - "30日"  # 8個目 → L101 違反

# OK: 7個以下
options:
  - "1日"
  - "7日"
  - "14日"
  - "30日"
```

**修正方法**: 選択肢を統合・グループ化する

---

### L102: 7±2 ルール違反（policy_levels）

**対象**: decisions.yaml の `policy_levels[].levels`

**ルール**: 段階は最大7個まで

```yaml
# NG: 8段階以上
levels: [NONE, DEBUG, INFO, NOTICE, WARNING, ERROR, CRITICAL, EMERGENCY]  # 8個

# OK: 7段階以下
levels: [NONE, SOFT, HARD]  # 3個
```

---

### L103: 7±2 ルール違反（exception_types）

**対象**: decisions.yaml の `exception_types[]`

**ルール**: 例外の型は最大7種類まで

---

### L104: 7±2 ルール違反（policy categories）

**対象**: policy.yaml の `{domain}_policy` 配下のカテゴリ数

**ルール**: カテゴリは最大7個まで

---

### L105: ネスト深度超過

**対象**: policy.yaml

**ルール**: ネストは最大2階層まで（`domain_policy.category.setting`）

```yaml
# NG: 3階層以上
library_policy:
  loan:
    overdue:
      enforcement:  # 3階層目 → L105 違反
        level: HARD

# OK: 2階層
library_policy:
  loan:
    overdue_enforcement: HARD
```

---

## L2xx: 整合性検証

### L201: 条件矛盾（同条件で異なる結果）

**対象**: decisions.yaml の複数の決定

**ルール**: 同じ条件に対して異なる結果を返す決定がないこと

```yaml
# NG: D1 と D2 で矛盾
decisions:
  - id: D1
    question: "延滞時の処理は？"
    selected: "HARD"  # 貸出不可

  - id: D2
    question: "延滞会員への対応は？"
    selected: "貸出可能（警告のみ）"  # 矛盾 → L201 違反
```

**検出方法**: 類似の質問/条件を持つ決定をグループ化し、selected の整合性を検証

---

### L202: policy と decisions の不整合

**対象**: policy.yaml と decisions.yaml

**ルール**: policy.yaml の設定値は decisions.yaml の selected と一致すること

```yaml
# decisions.yaml
- id: D1
  selected: "7日以上"

# policy.yaml
# NG: 異なる値
overdue_threshold_days: 14  # L202 違反（D1 では 7日）
```

---

### L203: 未参照の決定

**対象**: decisions.yaml

**ルール**: Layer 1 の決定は policy.yaml で参照されていること

```yaml
# decisions.yaml
- id: D1
  layer: 1
  selected: "HARD"

# policy.yaml
# NG: D1 が参照されていない → L203 違反
```

---

### L204: Reserved 状態の矛盾

**対象**: policy.yaml（図書館ドメイン固有）

**ルール**: `use_reserved_state: false` の場合、Reserved 状態を使う設定がないこと

```yaml
# NG: 矛盾
copy_state:
  use_reserved_state: false

# ... 別の場所で
return:
  set_reserved_on_hold: true  # L204 違反
```

---

## L3xx: 完全性検証

### L301: 未カバーの状態遷移

**対象**: 状態機械に関連する決定

**ルール**: すべての状態遷移パスが定義されていること

```
# 状態: Available → OnLoan → Available

# NG: Overdue からの遷移が未定義
Available → OnLoan: ✓
OnLoan → Available: ✓
OnLoan → Overdue: ✓
Overdue → ???: L301 違反
```

---

### L302: 未定義の例外ハンドリング

**対象**: exception_types

**ルール**: すべての例外型に handling が定義されていること

```yaml
# NG: handling 未定義
exception_types:
  - type: CONFLICT
    description: "同時更新の競合"
    # handling: ???  → L302 違反
```

---

### L303: 未決事項の混入

**対象**: policy.yaml

**ルール**: TBD, TODO, null, 空文字は禁止

```yaml
# NG: 未決事項
overdue_threshold_days: TBD  # L303 違反
notification_email: null     # L303 違反
description: ""              # L303 違反
```

---

### L304: 必須フィールドの欠落

**対象**: decisions.yaml

**ルール**: 必須フィールド（id, layer, category, selected, rationale）が存在すること

---

## L4xx: 用語検証

### L401: 用語の不一致

**対象**: decisions.yaml, policy.yaml, spec.yaml

**ルール**: 同じ概念に同じ用語を使うこと

```yaml
# NG: 同じ概念に異なる用語
# decisions.yaml
question: "延滞の閾値は？"

# policy.yaml
overdue_days: 7  # "延滞" vs "overdue"

# spec.yaml
"返却期限超過の場合..."  # "延滞" vs "返却期限超過" → L401 違反
```

**修正方法**: Glossary（用語集）を作成し、統一する

---

### L402: 略語の不統一

**対象**: 全ファイル

**ルール**: 略語は一貫して使用すること

```
# NG: 不統一
- "Res" vs "Reservation"
- "Lib" vs "Library"
- "Auth" vs "Authorization" vs "Authentication"
```

---

## L5xx: Layer 検証

### L501: Layer 1 に複雑なルールが混入

**対象**: decisions.yaml

**ルール**: Layer 1 は enum / 固定値のみ。if/then は Layer 2 へ

```yaml
# NG: Layer 1 に条件ルール
- id: D1
  layer: 1
  selected: "延滞 >= 7日 AND 研究者でない場合は HARD"  # L501 違反

# OK: Layer 2 に分離
- id: D1
  layer: 1
  selected: "HARD"

- id: D2
  layer: 2
  selected: "研究者の場合は SOFT に緩和"
```

---

### L502: Layer 3 が policy.yaml に出力されている

**対象**: policy.yaml

**ルール**: Layer 3（rationale, alternatives）は policy.yaml に含めない

```yaml
# NG: rationale が混入
library_policy:
  loan:
    overdue_enforcement: HARD
    # L502 違反
    overdue_enforcement_rationale: "未返却本の増加を防ぐため"
```

---

### L503: Layer 2 が policy.yaml に enum として出力

**対象**: decisions.yaml, policy.yaml

**ルール**: Layer 2（ルール）は Validator / domain_logic に出力。policy.yaml は Layer 1 のみ

---

## L6xx: 未離散化検証（UNQUANTIZED）

> **重要**: このカテゴリは CI で **fail** させる。warning ではない。
> 離散化漏れは「工程抜け」として確実に露出させる。

### L601: 未離散化キーワードの検出

**対象**: spec.yaml, decisions.yaml の `original_text`, `question`

**ルール**: 以下のキーワードが残っている場合は fail

**検出キーワード（日本語）**:
```
適切, 柔軟, 原則, 可能な限り, なるべく, できるだけ,
基本的に, 十分, 必要に応じて, 状況に応じて,
ある程度, 概ね, おおむね, 多少, 若干
```

**検出キーワード（英語 / RFC準拠）**:
```
MAY, SHOULD, SHOULD NOT, RECOMMENDED, NOT RECOMMENDED,
appropriate, flexible, sufficient, adequate, reasonable,
as needed, if necessary, where possible, generally
```

**重大度**: `error`（CI fail）

```yaml
# NG: 未離散化キーワードが残っている
# spec.yaml
description: "適切な期間内に処理する"  # L601 違反

# decisions.yaml
- id: D1
  original_text: "柔軟に対応できるようにしたい"
  selected: "柔軟に対応"  # L601 違反（selected も未離散化）
```

```yaml
# OK: 離散化済み
# decisions.yaml
- id: D1
  original_text: "柔軟に対応できるようにしたい"  # original_text は元の表現なのでOK
  selected: "SOFT"  # 離散化された値
```

---

### L602: WAIVED の理由欠落

**対象**: decisions.yaml

**ルール**: L601 を意図的にスキップする場合、`waiver` フィールドに理由を記録すること

```yaml
# NG: waiver の理由がない
- id: D1
  original_text: "適切な期間内に処理する"
  selected: "適切"  # L601 違反だが waiver なし → L602 違反

# OK: waiver に理由を記録
- id: D1
  original_text: "適切な期間内に処理する"
  selected: "適切"
  waiver:
    rule: L601
    reason: "外部システムの仕様に依存するため、現時点では離散化不可"
    owner: "human"
    expires: "2025-03-31"  # 再検討期限
```

---

### L603: RFC キーワードの未変換

**対象**: spec.yaml

**ルール**: RFC 2119 キーワード（MUST, MUST NOT, SHALL, SHALL NOT）以外の RFC キーワードは離散化が必要

| キーワード | 扱い |
|-----------|------|
| MUST, MUST NOT | OK（明確な制約） |
| SHALL, SHALL NOT | OK（明確な制約） |
| **MAY** | **要離散化**（実装判断を decisions.yaml に記録） |
| **SHOULD** | **要離散化**（優先度を decisions.yaml に記録） |
| **RECOMMENDED** | **要離散化**（採用/不採用を decisions.yaml に記録） |

```yaml
# NG: MAY が離散化されていない
# spec.yaml
"延滞会員への貸出禁止（MAY）"  # L603 違反

# OK: decisions.yaml に記録
# decisions.yaml
- id: D1
  original_text: "延滞会員への貸出禁止（MAY）"
  question: "延滞会員への貸出禁止を実装するか？"
  options: ["実装する", "実装しない（MVP外）"]
  selected: "実装する"
  rationale: "運用上必須と判断"
```

---

### L604: 曖昧な数量表現

**対象**: spec.yaml, decisions.yaml

**ルール**: 数量を示す曖昧な表現は具体的な値に離散化すること

**検出パターン**:
```
多い, 少ない, 大量, 少量, 頻繁, まれ,
しばしば, ときどき, たまに, 長い, 短い,
many, few, large, small, frequent, rare, often, sometimes
```

```yaml
# NG: 曖昧な数量
"多くのユーザーが..."  # L604 違反
"長い期間..."  # L604 違反

# OK: 具体的な値
"100人以上のユーザーが..."
"30日以上の期間..."
```

---

## 検証実行フロー

```
┌─────────────────────────────────────────────────────────────┐
│  1. spec.yaml を読み込む                                     │
│       ↓                                                     │
│  2. L6xx（未離散化検証）を実行 ★ 最初に実行                  │
│       ↓ fail したら即座に停止（離散化漏れは工程抜け）        │
│  3. decisions.yaml を読み込む                                │
│       ↓                                                     │
│  4. L1xx（構造検証）を実行                                   │
│       ↓                                                     │
│  5. L5xx（Layer 検証）を実行                                 │
│       ↓                                                     │
│  6. policy.yaml を読み込む                                   │
│       ↓                                                     │
│  7. L2xx（整合性検証）を実行                                 │
│       ↓                                                     │
│  8. L3xx（完全性検証）を実行                                 │
│       ↓                                                     │
│  9. L4xx（用語検証）を実行                                   │
│       ↓                                                     │
│ 10. レポート出力                                             │
└─────────────────────────────────────────────────────────────┘
```

> **注意**: L6xx は最初に実行し、fail した場合は即座に停止する。
> 未離散化のまま後続の検証を行っても意味がないため。

---

## レポートフォーマット

```markdown
## Lint Report: {feature}/{slice}

### Summary

| Category | Pass | Fail | Warn |
|----------|:----:|:----:|:----:|
| L6xx (Unquantized) | 3 | 0 | 0 |
| L1xx (Structure) | 4 | 1 | 0 |
| L2xx (Consistency) | 3 | 0 | 1 |
| L3xx (Completeness) | 2 | 1 | 0 |
| L4xx (Terminology) | 5 | 0 | 0 |
| L5xx (Layer) | 3 | 0 | 0 |
| **Total** | **20** | **2** | **1** |

### Failures

#### L101: 7±2 ルール違反（options）

**File**: decisions.yaml
**Line**: 45
**Detail**: D3.options has 9 items (max: 7)
**Suggested Fix**: Group similar options or split into multiple decisions

#### L303: 未決事項の混入

**File**: policy.yaml
**Line**: 23
**Detail**: `notification_email` is null
**Suggested Fix**: Remove or provide a concrete value

### Warnings

#### L401: 用語の不一致

**Files**: decisions.yaml:12, spec.yaml:89
**Detail**: "延滞" vs "返却期限超過"
**Suggested Fix**: Use Glossary to standardize terminology
```

---

## AI向けチェックリスト

### spec.yaml 読み込み後（最初に実行）

- [ ] L601: 未離散化キーワード（適切/柔軟/MAY/SHOULD等）が残っていないか？
- [ ] L603: RFC キーワード（MAY/SHOULD/RECOMMENDED）が離散化されているか？
- [ ] L604: 曖昧な数量表現（多い/少ない/長い等）が具体的な値になっているか？

> **重要**: L6xx が fail した場合、後続の検証を行わずに離散化を先に完了させる

### decisions.yaml 作成後

- [ ] L101: options は7個以下か？
- [ ] L102: policy_levels.levels は7段階以下か？
- [ ] L103: exception_types は7種類以下か？
- [ ] L304: 必須フィールド（id, layer, category, selected, rationale）があるか？
- [ ] L501: Layer 1 に条件ルールが混入していないか？
- [ ] L601: selected に未離散化キーワードが含まれていないか？
- [ ] L602: waiver を使う場合、理由・owner・expires が記録されているか？

### policy.yaml 生成後

- [ ] L104: カテゴリは7個以下か？
- [ ] L105: ネストは2階層以下か？
- [ ] L202: decisions.yaml の selected と一致しているか？
- [ ] L203: Layer 1 の決定が参照されているか？
- [ ] L303: TBD, null, 空文字がないか？
- [ ] L502: rationale が混入していないか？

### spec.yaml との整合性

- [ ] L401: 用語が統一されているか？
- [ ] L402: 略語が一貫しているか？

---

## 関連ドキュメント

- `decisions-template.yaml` - 意思決定スキーマ
- `policy-template.yaml` - ポリシーDSLテンプレート
- `decision-guide.md` - 曖昧さ解決フロー

---

## 変更履歴

| バージョン | 日付 | 変更内容 |
|-----------|------|---------|
| v1.1.0 | 2025-12-14 | L6xx（未離散化検証）追加。CI fail として離散化漏れを工程抜けとして検出 |
| v1.0.0 | 2025-12-14 | 初版リリース |
