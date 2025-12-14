# E2E Testing Guide: 量子化フレームワークとの統合

> **目的**: 量子化（離散化）フレームワークを活用した「少数・高価値・意図ベース」のE2Eテスト戦略を定義する
>
> **バージョン**: v1.0.0
> **作成日**: 2025-12-15

---

## Overview

E2Eテストは「画面を全部なぞる」ものではなく、**量子化成果物（decisions/policy）と接続して、最小・高価値なテストに絞る**。

### 量子化がE2Eに効く理由

| 効果 | 説明 |
|------|------|
| **入力空間が有限** | enum/閾値/ポリシーで入力が限定される |
| **期待結果が仕様として固定** | 推論で埋めない、decisions.yaml に明示 |
| **原因が追跡できる** | E2E失敗時に decisions/policy 差分に落ちる |

---

## E2Eで検証すべき3種類（A/B/C）

E2Eは以下の3種類だけに絞ると強い。

### A. 状態遷移の骨（State Transition Flows）

長い業務フローの「骨」だけをテストする。

**狙い**: 状態遷移が崩れると全体が死ぬため

**例（図書館ドメイン）**:
- 貸出 → 返却（Available ↔ OnLoan）
- 予約（Waiting）→ 返却で Ready → 取り置き → 予約者への貸出

**導出元**: `command-spec.yaml` の `state_transitions`

```yaml
# command-spec.yaml から導出
state_transitions:
  - id: T1
    from_states: [Available]
    to_state: OnLoan
    # → E2E: 貸出フロー
  - id: T2
    from_states: [OnLoan]
    to_state: Available
    # → E2E: 返却フロー
```

### B. ポリシー境界（Policy Boundaries）

`policy.yaml` の値で結果が変わる「境界」をテストする。

**狙い**: 同じシナリオをポリシー差し替えで回し、差分を仕様通りに固定する

**例**:
```yaml
# policy.yaml
library_policy:
  loan:
    overdue_enforcement: HARD  # HARD / SOFT / NONE
```

| ポリシー値 | 期待動作 |
|-----------|---------|
| HARD | 延滞者は貸出拒否 |
| SOFT | 貸出成功 + 警告（UIやログ） |
| NONE | 通常通り貸出 |

**導出元**: `policy.yaml` の enum フィールド

### C. 不変条件（Invariants）

E2E後に必ず検査する「安全柵」。UIが変わっても価値が落ちにくい。

**狙い**: 少数の検査で多くのバグを安く拾う

**例（図書館ドメイン）**:
- 同一 Copy に同時に2つの未返却 Loan が存在しない
- Ready があるなら Copy は Reserved
- Position が一意で先頭が処理される

**導出元**: `command-spec.yaml` の `domain_rules`（invariant タイプ）

```yaml
# command-spec.yaml から導出
domain_rules:
  - id: R1
    type: invariant
    description: "同一 Copy に同時に2つの未返却 Loan が存在しない"
    # → E2E後に検査
```

---

## 作り過ぎないルール（4つ）

E2Eを増やすほど壊れやすくなるので、増殖を止める基準を明文化する。

### ルール1: E2Eは"流れ"を検証し、細部は下に落とす

| 検証対象 | E2E | 下位テスト |
|---------|:---:|:---------:|
| 状態遷移の骨 | ✅ | - |
| UIの見た目 | ❌ | Visual Regression |
| 細かいバリデーション | ❌ | Unit（Validator） |
| 文言差分 | ❌ | Snapshot |
| API契約 | ❌ | Integration |

### ルール2: 1コマンド=1E2Eにしない

コマンド単位の網羅は、E2Eではなく API契約テスト/統合テスト向き。
E2Eは「結合で壊れるところ」だけ。

```
❌ CreateProduct → E2E
❌ UpdateProduct → E2E
❌ DeleteProduct → E2E

✅ Product CRUD フロー（作成→更新→削除）→ 1つのE2E
```

### ルール3: UIセレクタ依存を最小化

テストは「意図（command）」と「状態（domain）」を見る。
UIは導線の確認程度（data-testid など）。

```csharp
// ❌ UIセレクタ依存
await Page.ClickAsync("button.btn-primary.submit-form");

// ✅ 意図ベース
await Page.ClickAsync("[data-testid='submit-loan']");
```

### ルール4: 直交表で"最小組み合わせ"にする

`policy.yaml` の enum をそのまま全組み合わせにしない。
**直交表（pairwise）**か、影響が大きいものだけを切り替える。

```yaml
# policy.yaml に3つの3値enumがある場合
# 全組み合わせ: 3^3 = 27パターン
# pairwise: 約9パターン（直交表）
# 推奨: 影響が大きい1-2個のみ切り替え → 3-6パターン
```

---

## 統合フロー

```
decisions.yaml（Layer 1/2/3）
  │
  ├─ Layer 1 → policy.yaml
  │              │
  │              └─→ B. ポリシー境界テスト
  │
  ├─ Layer 2 → command-spec.yaml
  │              │
  │              ├─→ A. 状態遷移の骨（state_transitions）
  │              │
  │              └─→ C. 不変条件（domain_rules.type=invariant）
  │
  └─ Layer 3 → 記録のみ（背景・リスク・例外）
                 │
                 └─→ decisions/policy/command-spec を束ねて e2e-spec.yaml を作成
                         ↓
                    E2Eテスト実装（Playwright等）
```

---

## decisions/policy からの E2E ケース導出

### Step 1: policy.yaml から境界テストを抽出

```yaml
# policy.yaml
library_policy:
  loan:
    overdue_enforcement: HARD  # enum: HARD/SOFT/NONE → B
    max_loan_days: 14          # 閾値 → 境界値テスト候補
  reservation:
    max_active_per_member: 3   # 閾値 → 境界値テスト候補
```

**導出**:
- `overdue_enforcement` の HARD/SOFT/NONE → ポリシー境界テスト（B）
- `max_loan_days=14` → 13日/14日/15日の境界（オプション）

### Step 2: command-spec.yaml から状態遷移を抽出

```yaml
# command-spec.yaml
state_transitions:
  - id: T1
    description: "貸出"
    from_states: [Available]
    to_state: OnLoan
  - id: T2
    description: "返却"
    from_states: [OnLoan]
    to_state: Available
```

**導出**:
- T1 + T2 を連結 → 「貸出→返却」フロー（A）

### Step 3: domain_rules から不変条件を抽出

```yaml
# command-spec.yaml
domain_rules:
  - id: R1
    type: invariant
    description: "同一 Copy に2つの未返却 Loan が存在しない"
```

**導出**:
- R1 → E2E後の検査項目（C）

---

## 推奨最小セット（例: 図書館ドメイン）

| # | 種類 | テスト内容 | 優先度 |
|:-:|:----:|-----------|:------:|
| 1 | A | Happy path: 貸出→返却（Available↔OnLoan） | 必須 |
| 2 | A | 予約フロー: Waiting→Ready→貸出 | 必須 |
| 3 | B | 延滞ブロック: HARD vs NONE の差分 | 必須 |
| 4 | C | Invariants: 3-5個の安全柵チェック | 必須 |
| 5 | B | （次段階）SOFT の警告表示確認 | 推奨 |

**これで「工程の再現性」と「本番で死ぬバグ」の両方をカバーできる。**

---

## テストピラミッドとの整合性

```
        /\
       /  \  E2E: 状態遷移の骨 + ポリシー境界 + 不変条件（少数）
      /----\
     /      \  Integration: API契約テスト、コマンド単位
    /--------\
   /          \  Unit: Validator, CanXxx, ドメインロジック
  /------------\
```

| レベル | テスト対象 | 導出元 |
|--------|-----------|--------|
| E2E | A/B/C の3種類 | e2e-spec.yaml |
| Integration | API契約、コマンド | command-spec.yaml |
| Unit | Validator, CanXxx | command-spec.yaml |

---

## 量子化があるからできる運用上の強み

1. **E2E失敗の原因追跡**: decisions/policy 差分に落ちやすい
2. **仕様変更のレビュー**: 「仕様変更」= decisions 変更としてPRレビュー
3. **AIモデル更新への耐性**: プロンプト変更があっても、E2Eの期待値が固定される

---

## e2e-spec.yaml の作成

E2Eテスト仕様は `specs/{feature}/{slice}.e2e-spec.yaml` に配置する。

テンプレート: `catalog/scaffolds/e2e-spec-template.yaml`

```yaml
# specs/library/loan.e2e-spec.yaml
metadata:
  feature: "Loan"
  slice: "LoanManagement"

state_transition_flows:  # A
  - id: E2E-A1
    name: "Happy path: 貸出→返却"
    steps:
      - action: CreateLoan
        from_state: Available
        to_state: OnLoan
      - action: ReturnLoan
        from_state: OnLoan
        to_state: Available

policy_boundaries:  # B
  - id: E2E-B1
    name: "延滞ブロック境界"
    policy_key: library_policy.loan.overdue_enforcement
    scenarios:
      - value: HARD
        expected: "貸出拒否"
      - value: NONE
        expected: "通常通り貸出"

invariants:  # C
  - id: E2E-C1
    description: "同一 Copy に2つの未返却 Loan が存在しない"
    check_after: [E2E-A1]
```

---

## Related Documents

- `decision-guide.md` - 量子化フレームワーク全体ガイド
- `../scaffolds/e2e-spec-template.yaml` - E2E仕様テンプレート
- `../scaffolds/command-spec-template.yaml` - Layer 2 仕様テンプレート
- `../scaffolds/policy-template.yaml` - Layer 1 policy テンプレート
- `lint-rules.md` - 破綻検知ルール

---

## 変更履歴

| バージョン | 日付 | 変更内容 |
|-----------|------|---------|
| v1.0.0 | 2025-12-15 | 初版リリース |
