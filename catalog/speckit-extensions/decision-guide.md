# Decision Guide: Resolving Ambiguity

> **目的**: 曖昧な仕様に遭遇した際の対処フローと意思決定記録（Layer 1/2/3）の方法を定義する
>
> **バージョン**: v2.2.0
> **作成日**: 2025-12-09
> **更新日**: 2025-12-15

---

## Overview

曖昧な仕様は必ず発生する。重要なのは「曖昧さを無視して進める」のではなく、**離散化して意思決定として記録し、Spec（SSOT）へ反映する**こと。

このガイドは以下を扱う:

- **3層アーキテクチャ**（Layer 1/2/3）で壊れにくく積み上げる
- **7±2 ルール**で粒度と選択肢を制限する
- **出力先（output_target）**を明確にし、policy/validator/domain に落とす
- **L6xx（未離散化検証）**で工程抜けを fail として検出する
- **decisions diff** をレビューの主戦場にする（PRテンプレート連携）
- **E2Eテスト**と接続し、少数・高価値なテストを導出する（→ `e2e-testing-guide.md`）

---

## 3層アーキテクチャ（AI追従型・仕様量子化）

AIに一気に高精度を要求しない代わりに、壊れにくい層から積み上げる。

### Layer 1: コア離散（AIが最も得意）

| 対象 | 説明 | 出力先 |
|------|------|--------|
| 状態機械（State） | Available, OnLoan, Overdue 等 | `{slice}.policy.yaml` |
| 役割（Role） | Member, Librarian, Admin 等 | `{slice}.policy.yaml` |
| 許可/禁止（Allow/Deny） | 操作の可否 | `{slice}.policy.yaml` |
| 例外カテゴリ | AUTH, VALIDATION, CONFLICT 等 | `{slice}.policy.yaml` |
| 主要制約（Must/MustNot） | 不変条件 | `{slice}.policy.yaml` |

**ポイント**: 3〜7値に抑える（7±2ルール）。多すぎると境界が崩れる。

### Layer 2: ルール化（条件→結果）

| 対象 | 説明 | 出力先 |
|------|------|--------|
| preconditions | 入力検証・前提条件 | Validator |
| state_transitions | 状態×操作の可否 | Entity.CanXxx() |
| failure_reasons | 失敗理由の型 | Result/BoundaryDecision |
| domain_rules | ビジネスルール（if/then） | domain_logic |

**ポイント**: 条件は「単純な述語のAND」に限定（OR/入れ子は壊れやすい）。

### Layer 3: ニュアンス保管（監査ログ）

| 対象 | 説明 | 出力先 |
|------|------|--------|
| rationale | なぜそう決めたか | 記録のみ |
| alternatives | 代替案と却下理由 | 記録のみ |
| risk / impact / fallback | 判断の監査ログ（今すぐ価値が出る） | 記録のみ |
| waiver | L6xx を意図的にスキップする記録 | 記録のみ |

**ポイント**: Layer 3 は AI 自動実装の入力にしない（将来の高精度化用のトラック）。

---

## 7±2 ルール

選択肢・段階・例外は最大7個（3〜7）に抑える。

| 対象 | 最大数 |
|------|:------:|
| decisions.options | 7 |
| policy_levels.levels | 7 |
| exception_types | 7 |
| policy categories | 7 |
| failure_reasons | 7 |

---

## 統合フロー（推奨）

```
spec.yaml
  ↓  (L6xx: 未離散化検出)
discretization-questions.yaml で質問生成
  ↓  (ユーザー回答)
{slice}.decisions.yaml（layer + output_target + rationale）
  ├─ Layer 1 → {slice}.policy.yaml（設定値中心DSL）
  ├─ Layer 2 → {slice}.command-spec.yaml（Validator/CanXxx直結）
  └─ Layer 3 → 記録のみ（監査ログ）
  ↓
lint-rules.md（L6xx → L1xx..L5xx）
  ↓
Spec.Assumptions に反映（SSOT維持）
  ↓
{slice}.e2e-spec.yaml（E2Eテスト仕様）
  ├─ A. state_transition_flows（状態遷移の骨）
  ├─ B. policy_boundaries（ポリシー境界）
  └─ C. invariants（不変条件）
```

詳細は `e2e-testing-guide.md` を参照。

---

## ドキュメント / 成果物（SSOT と出力）

### decisions.yaml（意思決定の記録）

- 形式: `catalog/scaffolds/decisions-template.yaml`
- 配置: `specs/{feature}/{slice}.decisions.yaml`
- 必須: `layer`, `options (max 7)`, `selected`, `rationale`
- 推奨: `output_target`, `alternatives`, `risk/impact/fallback`, `waiver`

### policy.yaml（Layer 1 の出力）

- 形式: `catalog/scaffolds/policy-template.yaml`
- 配置: `specs/{feature}/{slice}.policy.yaml`
- ルール: 確定値のみ（`TBD`/`TODO`/`null`/空文字は禁止）

### command-spec.yaml（Layer 2 直結）

- 形式: `catalog/scaffolds/command-spec-template.yaml`
- 配置: `specs/{feature}/{slice}.command-spec.yaml`
- 目的: Layer 2 を **Validator / Entity.CanXxx() / Result.Failure** に直結させる

### e2e-spec.yaml（E2Eテスト仕様）

- 形式: `catalog/scaffolds/e2e-spec-template.yaml`
- 配置: `specs/{feature}/{slice}.e2e-spec.yaml`
- 目的: 量子化成果物から **少数・高価値なE2Eテスト** を導出する
- 詳細: `e2e-testing-guide.md`

---

## Decisions Diff: 追加/変更の判断基準（PRレビュー用）

decisions diff は「仕様量子化の差分」であり、レビューの主戦場。

### 変更種別と互換性（目安）

| 変更種別 | 例 | 影響 | 互換性 |
|---------|----|------|:------:|
| Layer 3 のみ | rationale/risk/impact/fallback の追加 | 実装挙動なし | Compatible |
| Layer 1 の selected 変更 | 閾値 7→14 / enforcement SOFT→HARD | 挙動が変わる | Breaking |
| Layer 1 options の変更 | 選択肢の統合/分割 | 解釈が変わる可能性 | Depends |
| Layer 2 preconditions 追加 | 必須項目追加 | 入力が通らなくなる | Breaking |
| Layer 2 state_transitions 追加/変更 | 状態遷移の許可条件変更 | 操作可否が変わる | Breaking |
| failure_reasons の追加 | エラー理由の明確化 | 例外/Result の表現が増える | Compatible |

### 互換性判定フロー（簡易）

```
決定を変更した？
  ├─ No → 互換性に影響なし
  └─ Yes
      ├─ Layer 3 のみ？ → Compatible
      ├─ Layer 1 (policy) が変わる？ → Breaking（policy.yaml 更新 + 影響説明）
      └─ Layer 2 (rules) が変わる？ → Depends（入力/操作可否/失敗理由の変化を明示）
```

### 移行手順の記録形式（PR本文に必須）

互換性のない変更（Breaking）がある場合、PR に以下を記載する:

```markdown
## Migration Notes

- Decision: D1 (Layer 1)
- Change: overdue_threshold_days 7 → 14
- Impact: 既存ユーザーの貸出可否が変わる可能性
- Required Updates:
  - specs/.../{slice}.policy.yaml を再生成
  - 関連する Validator / CanXxx / テスト を更新
- Rollback: 元の policy に戻す（D1.selected を 7 に戻す）
```

### レビュー観点（最低限）

- decisions.yaml の diff が意図した変更になっているか（穴埋めがないか）
- L6xx が Pass か（未離散化が残っていないか / waiver の妥当性）
- Layer 1 変更なら policy.yaml が追随しているか
- Layer 2 変更なら Validator/CanXxx/FailureReasons が追随しているか
- Spec.Assumptions に反映されているか（SSOT）

---

## command-spec.yaml（Layer 2 直結）

### 概念とマッピング

| command-spec.yaml | 出力 | 目的 |
|-------------------|------|------|
| policy_bindings | 定数/設定参照 | Layer 1 の値を Layer 2 で参照 |
| preconditions | Validator | 入力・前提条件の検証 |
| state_transitions | Entity.CanXxx() | 状態×操作の許可/禁止 |
| failure_reasons | Result/BoundaryDecision | 失敗理由を 7±2 で型化 |
| domain_rules | domain_logic | ビジネスルール（if/then） |
| generation_targets | 出力パス | 生成ファイルの場所を明示 |

### preconditions → Validator（例）

```csharp
public sealed class CreateXxxCommandValidator : AbstractValidator<CreateXxxCommand>
{
    public CreateXxxCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.Count)
            .InclusiveBetween(1, 7)
            .WithMessage("Count must be between 1 and 7");
    }
}
```

### state_transitions → Entity.CanXxx()（例）

```csharp
public sealed class Xxx
{
    public BoundaryDecision CanDoXxx(XxxState state)
    {
        return state switch
        {
            XxxState.Draft => BoundaryDecision.Allow(),
            _ => BoundaryDecision.Deny("STATE_TRANSITION_DENIED", "現在の状態では実行できません")
        };
    }
}
```

---

## Integration with spec-kit Commands

### /speckit.clarify との連携

曖昧さが多い場合:

```
1. undiscretized-detection.md で曖昧さを検出（L6xx）
2. /speckit.clarify で質問を生成
3. ユーザーの回答を decisions.yaml に記録（layer 指定）
4. Layer 1 → policy.yaml を生成
5. Layer 2 → command-spec.yaml を作成
6. lint-rules.md で検証
7. Spec.Assumptions に反映
```

---

## Related Documents

- `../scaffolds/decisions-template.yaml` - 意思決定スキーマ（Layer 3 拡張含む）
- `../scaffolds/policy-template.yaml` - Layer 1 → policy DSL
- `../scaffolds/command-spec-template.yaml` - Layer 2 → Validator/CanXxx 直結
- `../scaffolds/e2e-spec-template.yaml` - E2Eテスト仕様テンプレート
- `e2e-testing-guide.md` - E2Eテストと量子化フレームワークの統合ガイド
- `lint-rules.md` - 破綻検知（L6xx〜L5xx）
- `undiscretized-detection.md` - 未離散化検出ガイド
- `spec-plan-consistency.md` - SSOT ルール定義
- `.claude/commands/speckit.clarify.md` - 仕様の明確化コマンド

---

## 変更履歴

| バージョン | 日付 | 変更内容 |
|-----------|------|---------|
| v2.2.0 | 2025-12-15 | E2Eテストガイド（e2e-testing-guide.md）への参照追加、統合フローにe2e-spec.yaml追加 |
| v2.1.0 | 2025-12-14 | command-spec 追加、decisions diff の判断基準（互換性/移行/レビュー観点）追加 |
| v2.0.0 | 2025-12-14 | 3層アーキテクチャ + 7±2 ルール + policy/lint 統合フローを追加 |
| v1.0.0 | 2025-12-09 | 初版リリース |
