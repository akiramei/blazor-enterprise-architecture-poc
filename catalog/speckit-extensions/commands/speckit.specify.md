---
description: Generate feature specification with unsupported intents check.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

---

## Step 0: Unsupported Intents Pre-scan (MANDATORY)

要求文を受け取ったら、**仕様生成前に** 以下をスキャンすること。

1. Read: `catalog/index.json` → `ai_decision_matrix.unsupported_intents`
2. 要求文に該当キーワードがあれば **STOP**
3. インフラ前提をユーザーに確認してから仕様生成を続行

**STOP時の出力**:
⚠️ カタログ外機能を検出しました。
- 検出キーワード: {keyword}
- 必要な確認: {action from unsupported_intents}
- 続行前に上記を明確にしてください。

---

## 以降は標準の specify フロー

1. 要求仕様を読み取り、ユースケースと境界を特定する。
2. UIがある場合は Boundary モデリングを最初に行う（Intent → Entity.CanXxx()）。
3. SPEC テンプレート `catalog/scaffolds/spec-template.yaml` に従って `specs/{feature}/{slice}.spec.yaml` を作成する。
4. `characteristics` を明示し、後続の plan/tasks/implement が機械的にパターン選択できるようにする。

