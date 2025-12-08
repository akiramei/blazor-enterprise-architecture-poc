# Decision Guide: Resolving Ambiguity

> **目的**: 曖昧な仕様に遭遇した際の対処フローと意思決定記録の方法を定義する
>
> **バージョン**: v1.0.0
> **作成日**: 2025-12-09

---

## When Spec is Ambiguous

曖昧な仕様は必ず発生する。重要なのは「曖昧さを無視して進める」のではなく「意思決定として記録し、Spec に反映する」こと。

---

## Step 1: Identify Ambiguity Type

| タイプ | 特徴 | 例 | 対処 |
|--------|------|----|----|
| **未定義の制約** | 数値・期間が明記されていない | 予約上限が未記載 | デフォルト値を決定し Spec に追記 |
| **曖昧な表現** | 解釈の余地がある文言 | 「適切な期間」「十分な量」 | /speckit.clarify で確認 |
| **暗黙の前提** | 業界では当然だが明記されていない | 「通常の業務時間」 | Assumptions に明示化 |
| **選択肢の未決定** | 複数の実現方法がある | 「通知方法」 | 決定し Unknowns Resolved に記録 |
| **Optional の扱い** | 実装するか否かが不明 | 「延滞ブロック（optional）」 | MVP 判断を Decision Record に記録 |

---

## Step 2: Resolution Workflow

```
┌─────────────────────────────────────────────────────────────┐
│  1. Spec を読む                                              │
│       ↓                                                     │
│  2. 曖昧さを特定                                             │
│       ↓                                                     │
│  3. タイプを分類（上記表を参照）                              │
│       ↓                                                     │
│  4. 解決方法を選択                                           │
│       │                                                     │
│       ├─→ 未定義の制約 → デフォルト値を決定                  │
│       ├─→ 曖昧な表現 → /speckit.clarify で質問              │
│       ├─→ 暗黙の前提 → Assumptions に明示化                 │
│       ├─→ 選択肢の未決定 → 決定し記録                        │
│       └─→ Optional の扱い → MVP 判断を記録                   │
│       ↓                                                     │
│  5. Plan の "Unknowns Resolved" に記録                       │
│       ↓                                                     │
│  6. Spec の Assumptions に反映（SSOT 維持）                   │
└─────────────────────────────────────────────────────────────┘
```

---

## Step 3: Documentation Formats

### 3.1 Unknowns Resolved セクション

Plan に以下のセクションを追加:

```markdown
## Unknowns Resolved

| 項目 | Spec の状態 | 決定 | 理由 | Spec 反映 |
|------|------------|------|------|:---------:|
| 予約上限 | 明記なし | 3件 | 図書館業界の標準的な慣行 | Y |
| Ready 有効期限 | 明記なし | 24時間 | 一般的な取り置き期間 | Y |
| ページサイズ | 明記なし | 20件 | 一般的なUI慣行 | Y |
```

### 3.2 Spec Assumptions への反映

```markdown
# spec.md

## Assumptions

- Maximum active reservations per member is 3 (default limit)
- Ready reservation expires after 24 hours if not picked up
- Default page size for list queries is 20 items
- Business hours are 9:00-18:00 JST on weekdays
```

---

## Optional Rules Handling

### キーワード分類

| キーワード | RFC 準拠 | 意味 | Plan での扱い |
|-----------|----------|------|--------------|
| **MUST** | RFC 2119 | 必須 | Guardrails に抽出 |
| **MUST NOT** | RFC 2119 | 禁止 | Guardrails に抽出 |
| **SHOULD** | RFC 2119 | 推奨 | 実装する（優先度: 中） |
| **SHOULD NOT** | RFC 2119 | 非推奨 | 実装しない |
| **MAY** | RFC 2119 | 任意 | 実装判断を記録 |
| **optional** | 一般 | 任意 | MVP では見送り可 |
| **recommended** | 一般 | 推奨 | 実装する（優先度: 中） |

### Decision Record Format

Optional な機能の実装判断は記録する:

```markdown
## Optional Rule Decisions

| Rule | Spec Location | キーワード | 決定 | 理由 |
|------|---------------|-----------|------|------|
| 延滞会員への貸出禁止 | spec.md L132 | optional | MVP 見送り | Spec L234 で明示的に無効化 |
| メール通知 | spec.md L98 | MAY | MVP 見送り | 外部依存を減らすため |
| 予約キャンセル通知 | spec.md L156 | SHOULD | 実装する | UX 向上のため |
```

---

## Edge Cases Handling

### エッジケースの記録フォーマット

```markdown
## Edge Cases

| Case | 条件 | 期待動作 | 処理方法 |
|------|------|----------|----------|
| 全コピー貸出中に予約 | Available copies = 0 | 予約を受け付ける | FR-017 準拠 |
| Ready 中に他の会員が返却 | Ready + 別コピー返却 | Ready 予約は維持 | Queue は更新しない |
| 同時刻に複数予約 | 同一秒に2件の予約 | 先にDBに書き込まれた方が優先 | Position で一意性保証 |
```

### エッジケースの Acceptance Criteria 化

```markdown
## Edge Case Acceptance Criteria

### EC-001: 全コピー貸出中の予約
**Given**: 書籍「ABC」の全コピーが貸出中
**When**: 会員が「ABC」を予約する
**Then**: 予約が成功し、Position が付与される

### EC-002: Ready 状態での別コピー返却
**Given**: 会員 A が Ready 状態の予約を持つ
**And**: 別の会員 B が同じ書籍のコピーを返却
**When**: 返却処理が完了
**Then**: 会員 A の Ready 状態は維持される
**And**: 返却されたコピーは Available になる（Reserved にならない）
```

---

## Common Ambiguity Patterns

### パターン 1: 数値の未指定

**症状**: 「上限」「期間」「件数」が明記されていない

**対処**:
1. ドメイン知識から妥当な値を決定
2. Unknowns Resolved に記録
3. Spec.Assumptions に反映

**例**:
```markdown
# 曖昧な仕様
"予約には上限がある"

# 解決後
Unknowns Resolved: 予約上限 → 3件（図書館業界の標準）
Spec.Assumptions: Maximum active reservations per member is 3
```

### パターン 2: 状態遷移の未定義

**症状**: ある状態から別の状態への遷移条件が不明

**対処**:
1. 状態遷移図を作成
2. 全ての遷移パスを列挙
3. 未定義の遷移は「不可」または「追加定義」を決定

**例**:
```markdown
# 曖昧な仕様
"Ready 状態の予約は貸出される"

# 解決後（状態遷移を明示）
Ready → Fulfilled: 貸出実行時
Ready → Cancelled: 期限切れ or 会員キャンセル
Ready → Waiting: (不可 - 一度 Ready になったら戻らない)
```

### パターン 3: 権限の未定義

**症状**: 誰がその操作を実行できるかが不明

**対処**:
1. Actor を明示
2. 操作ごとに権限を定義
3. Spec に Actor セクションを追加（なければ）

**例**:
```markdown
# 曖昧な仕様
"予約をキャンセルできる"

# 解決後
- 会員: 自分の予約をキャンセルできる
- スタッフ: 任意の予約をキャンセルできる（理由記録必須）
```

---

## Integration with spec-kit Commands

### /speckit.clarify との連携

曖昧さが多い場合:
```
1. /speckit.clarify で質問を生成
2. ユーザーの回答を Spec に反映
3. /speckit.plan で Plan 生成
4. Unknowns Resolved は最小限になる
```

### /speckit.plan での自動処理

```
1. Spec を読み込む
2. 曖昧さを検出
3. ドメイン知識からデフォルト値を提案
4. Unknowns Resolved セクションを生成
5. **WARNING**: "These decisions should be reflected in Spec.Assumptions"
```

---

## Checklist

### Plan 作成時

- [ ] 曖昧さを全て特定したか
- [ ] Unknowns Resolved セクションを作成したか
- [ ] 各決定に理由を記録したか
- [ ] Optional の実装判断を記録したか
- [ ] Edge Cases を列挙したか

### Spec 更新時

- [ ] Unknowns Resolved の決定を Assumptions に追記したか
- [ ] Optional Rule Decisions の内容を反映したか
- [ ] Edge Cases に Acceptance Criteria を追加したか

---

## Related Documents

- `spec-plan-consistency.md` - SSOT ルール定義
- `constitution-additions.md` - Constitution への統合
- `.claude/commands/speckit.clarify.md` - 仕様の明確化コマンド
