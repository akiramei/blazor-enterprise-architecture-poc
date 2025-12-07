# AI Guardrails & Self-correction Flow

このドキュメントは、AI エージェントが実装時に「絶対に守るべきルール（Guardrails）」を抽出し、
自己修正するためのフローを定義します。

> **背景**: 図書館ドッグフーディングで FR-017, FR-021 などの重要ルールが
> 実装まで伝播せず、仕様違反が発生しました。Guardrails で強制伝播します。

---

## Phase 0.25: Guardrails 抽出（CRITICAL）

**目的**: ビジネス上の重要ルールを SPEC から抽出し、実装フェーズまで確実に伝播させる。

### Guardrail の条件

以下に該当するルールは Guardrail として抽出する：

- 「〜のみ可能」「〜の場合のみ」という前提条件
- 「〜が優先」「〜が先」という優先権ルール
- 違反するとビジネス上の問題が発生するルール

### 抽出手順

1. **SPEC を読み、Guardrail 候補を特定**

   SPEC を読む際に以下をチェック：
   - [ ] 「〜のみ」「〜だけ」「〜の場合のみ」という文言があるか？
   - [ ] 「優先」「先着」「順番」という文言があるか？
   - [ ] 複数の条件を満たす必要がある操作があるか？
   - [ ] 状態に依存する操作可否判定があるか？

2. **Guardrails セクションを作成**

   ```markdown
   ## Guardrails（絶対遵守）

   以下のルールは実装で必ず満たすこと。違反は仕様違反とみなす。

   | ID | FR | ルール | 対象スコープ | 違反時の問題 |
   |----|----|----|------|-------------|
   | GR-001 | FR-021 | Ready 状態の予約者が最優先で貸出権を持つ | LoanBoundaryService | Ready 予約者以外に貸出してしまう |
   | GR-002 | FR-017 | Available なコピーがある間は予約不可 | ReservationValidationService | 不要な予約を受け付けてしまう |
   | GR-003 | FR-018 | 予約は先着順（Position で管理） | Reservation Entity | 順番が管理できない |
   ```

3. **plan.md に Guardrails セクションを追加**

### 警告

- Guardrails が 0 件の場合、SPEC に前提条件が明示されていない可能性がある
- その場合は research フェーズで確認すること

---

## Phase 1.5: Self-correction Loop（CRITICAL - AUTO）

**目的**: AI 自身が設計・実装を検証し、COMMON_MISTAKES.md 違反を自動修正する。

> このフェーズは Phase 1（Design & Contracts）の直後に **自動的に** 実行される。
> ユーザーの許可を待たずに実行すること。

### なぜ自動実行か？

- 人間のレビューはエラーを見逃しやすい
- AI が生成した計画は AI が検証すべき
- 「機械にできることは機械にやらせる」（spec-kit 哲学）
- ここで違反を検出すれば、全タスクへの波及を防げる

### Step 1: COMMON_MISTAKES.md を読む

```
Read: catalog/COMMON_MISTAKES.md
```

### Step 2: 計画を各ミスパターンと照合

| Check Item | 確認ポイント |
|------------|-------------|
| SaveChangesAsync in Handler | Handler設計がSaveChangesAsyncを呼ぶ前提になっていないか |
| Singleton/Scoped混在 | サービス登録がSingletonになっていないか |
| BoundaryServiceに業務ロジック | BoundaryService設計にif文（業務判定）が含まれていないか |
| Entity.CanXxx()の欠如 | Boundary設計でEntity.CanXxx()が定義されているか |
| Result<T>不使用 | 例外をthrowする設計になっていないか |
| Feature sliceの逸脱 | 共通サービスに機能固有ロジックが含まれていないか |

### Step 3: 自己修正ループ（IMPORTANT）

```
WHILE violations exist:
    1. Identify the violation
    2. Determine the correction based on catalog patterns
    3. Apply the correction to the plan
    4. Re-check the corrected plan

UNTIL all checks pass
```

**このループは自動実行。修正のためにユーザーの許可を求めない。**

### Step 4: 修正済み計画を出力

違反があり修正した場合、**修正版** を出力：

```markdown
## Design-Level Check (COMMON_MISTAKES)

### Check Results

| Check Item | Initial | After Correction |
|------------|:-------:|:----------------:|
| SaveChangesAsync | ❌ | ✅ |
| Singleton/Scoped | ✅ | ✅ |
| BoundaryService | ❌ | ✅ |
| Entity.CanXxx() | ✅ | ✅ |
| Result<T> | ✅ | ✅ |
| Feature slice | ✅ | ✅ |

### Violations Found & Corrected

| Violation | Location | Correction Applied |
|-----------|----------|-------------------|
| Handler calls SaveChangesAsync | LendBookCommandHandler設計 | TransactionBehaviorに任せる設計に修正 |
| BoundaryService has business logic | BookBoundaryService設計 | Entity.CanBorrow()に委譲する設計に修正 |

### Corrected Plan Sections

(修正されたセクションのみ出力)

#### Before:
```
LendBookCommandHandler:
  - Call repository.AddAsync()
  - Call dbContext.SaveChangesAsync()  ← violation
```

#### After:
```
LendBookCommandHandler:
  - Call repository.AddAsync()
  - Return Result.Success()  ← TransactionBehavior handles SaveChanges
```
```

### Step 5: 最終ゲートチェック

```
IF all checks pass (after self-correction):
  → Output: "✅ Design-Level Check PASSED. Ready for /speckit.tasks"
  → Proceed to tasks

IF unable to auto-correct (requires spec clarification):
  → Output: "❌ Design-Level Check FAILED. Manual intervention required."
  → List unresolvable issues
  → STOP - Do NOT proceed to tasks
```

---

## 属性の強制反映チェック（FR-018 対策）

> **背景**: 図書館ドッグフーディングで `Position` 属性が SPEC に明記されていたにもかかわらず、
> data-model から欠落した。

**ルール**: SPEC に明記された属性は data-model から落としてはならない。

### チェック手順

1. SPEC から全エンティティの属性を抽出
2. data-model.md と照合
3. 欠落があればエラー

```markdown
## Attribute Enforcement Check

| Entity | Attribute | Spec | data-model | Status |
|--------|-----------|:----:|:----------:|:------:|
| Reservation | Position | ✅ | ✅ | OK |
| Reservation | Status | ✅ | ✅ | OK |
| Loan | DueDate | ✅ | ❌ | **ERROR** |

**欠落属性**: Loan.DueDate が data-model にありません。追加してください。
```

### 特に注意すべき属性

- Position, Order, Sequence（順序管理）
- Status, State（状態管理）
- Priority, Rank（優先度）
- Limit, Max, Min（上限/下限）

---

## Query Semantics の明示（Query バグ対策）

> **背景**: 図書館ドッグフーディングで `GetLoansQuery` が一般一覧と Overdue の
> 両方の意味を持ち、実装で混同された（コピペバグ）。

**ルール**: Query の意味（semantic）を plan で明示し、使用すべき Repository メソッドを指定する。

```markdown
## Query Semantics

| Query | Semantic | Repository Method | 禁止 |
|-------|----------|-------------------|------|
| GetLoansQuery | 全 Loan 一覧 / Active 一覧 | GetAllLoansAsync, GetActiveLoansByMemberIdAsync | ❌ GetOverdueLoansAsync |
| GetOverdueLoansQuery | Overdue 専用 | GetOverdueLoansAsync | ❌ GetActiveLoansByMemberIdAsync |
| GetReservationsQuery | 予約一覧 | GetReservationsByMemberIdAsync | - |

**注意**: Query と Repository メソッドの対応を間違えないこと。
```

### チェック項目

- [ ] 同じ Repository メソッドを異なる Query で使いまわしていないか？
- [ ] Query の semantic が明確に区別されているか？
- [ ] 「〜専用」の Query が他の用途に使われていないか？

---

## まとめ

| フェーズ | 目的 | 実行タイミング |
|---------|------|--------------|
| Phase 0.25 | Guardrails 抽出 | Phase 0（Research）の後 |
| Phase 1.5 | Self-correction | Phase 1（Design）の後、自動実行 |

**重要**: これらのフェーズをスキップしてはならない。
スキップすると、仕様違反が実装まで伝播するリスクがある。
