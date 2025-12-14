## Summary

<!-- 変更の概要を1-3行で記述 -->

## Decisions Diff (Required)

<!--
decisions.yaml の差分リンクを必ず記載してください。
人間がレビューすべき差分は spec や plan ではなく decisions です。
-->

### Changed Decisions

<!-- 変更された決定ポイントを列挙 -->

| Decision ID | 変更内容 | Layer | 互換性 |
|-------------|---------|:-----:|:------:|
| D1 | 閾値を 7日 → 14日 に変更 | 1 | Breaking |
| D2 | 新規追加 | 2 | - |

### Decisions File Link

<!-- decisions.yaml への差分リンク -->
- [ ] `specs/{feature}/{slice}.decisions.yaml` の差分を確認した

### Migration Required?

<!-- 互換性のない変更がある場合、移行手順を記載 -->

- [ ] 移行不要（互換性あり）
- [ ] 移行手順を別途記載

---

## Lint Results

<!-- lint-rules.md の検証結果 -->

- [ ] L6xx（未離散化検証）: Pass
- [ ] L1xx-L5xx: Pass
- [ ] 新規 waiver がある場合、理由を記載

---

## Layer Classification

<!-- この PR で変更される Layer を選択 -->

- [ ] **Layer 1**: policy.yaml（設定値の変更）
- [ ] **Layer 2**: Validator / Entity.CanXxx()（ルールの変更）
- [ ] **Layer 3**: rationale / risk / impact（監査ログのみ）

---

## Checklist

### decisions.yaml

- [ ] 新規決定に `layer` が指定されているか
- [ ] 新規決定に `rationale` が記録されているか
- [ ] Layer 3 拡張（risk / impact / fallback）を記録したか
- [ ] 7±2 ルールに違反していないか（options 最大7個）

### policy.yaml

- [ ] Layer 1 の変更が policy.yaml に反映されているか
- [ ] TBD / null / 空文字がないか

### Spec

- [ ] decisions.yaml の決定が Spec.Assumptions に反映されているか

---

## Test Plan

<!-- テスト計画を記述 -->

- [ ] 単体テスト
- [ ] 統合テスト
- [ ] 手動確認

---

## Related Issues / PRs

<!-- 関連する Issue や PR を記載 -->

- Closes #
- Related to #
