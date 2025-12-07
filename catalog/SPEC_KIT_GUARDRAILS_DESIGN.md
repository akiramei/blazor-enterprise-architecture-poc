# spec-kit Guardrails 設計ドキュメント

> **目的**: spec-kit のルール伝播を強化し、仕様から実装への「抜け漏れ」を防止する
>
> **背景**: 図書館ドッグフーディングで発見された FR-017, FR-018, FR-021 問題

---

## 1. 問題の分析

### 発見された問題パターン

| 問題 | 仕様 | 実装 | 根本原因 |
|------|------|------|----------|
| FR-017 | 全コピー貸出中のみ予約可能 | BookCopy チェックなし | **伝播ミス**: 重要ルールが tasks に落ちなかった |
| FR-018 | 予約に Position あり | Position 未実装 | **属性欠落**: data-model で属性が落ちた |
| FR-021 | Ready 予約者が優先 | Waiting のみ検索 | **伝播ミス**: 優先権ロジックが tasks に落ちなかった |
| Query バグ | 一覧 / Overdue 分離 | 両方 Overdue | **Query 意味の混同**: コピペミス |

### 共通パターン

```
✔ spec には書いてある
✖ plan / tasks への伝播が弱い
✖ 実装者（AI）が「当然」として省略
```

---

## 2. Guardrails（ガードレール）の設計

### 2.1 概念

**Guardrail** = 「絶対に破ってはいけないビジネスルール」

- spec に明記されている重要ルール
- 実装で省略すると仕様違反になるもの
- FR 番号で追跡可能

### 2.2 spec での記述

```yaml
# spec.yaml
guardrails:
  - id: GR-001
    fr: FR-021
    rule: "Ready 状態の予約者が最優先で貸出権を持つ"
    scope: LoanBoundaryService
    violation: "Ready 予約者以外への貸出を許可してしまう"

  - id: GR-002
    fr: FR-017
    rule: "Book に Available なコピーがある間は予約不可"
    scope: ReservationValidationService
    violation: "利用可能なコピーがあるのに予約を受け付けてしまう"

  - id: GR-003
    fr: FR-018
    rule: "予約は先着順（Position で管理）"
    scope: Reservation Entity
    violation: "Position がないため順番が管理できない"
```

### 2.3 plan.md への伝播

spec → plan 生成時に、Guardrails セクションを **必ず** 含める。

```markdown
# plan.md

## Guardrails（絶対遵守）

以下のルールは実装で必ず満たすこと。違反は仕様違反とみなす。

| ID | FR | ルール | 対象 |
|----|----|----|------|
| GR-001 | FR-021 | Ready 状態の予約者が最優先で貸出権を持つ | LoanBoundaryService |
| GR-002 | FR-017 | Available なコピーがある間は予約不可 | ReservationValidationService |
| GR-003 | FR-018 | 予約は先着順（Position で管理） | Reservation Entity |
```

### 2.4 tasks.md への紐付け

各タスクに関連する Guardrail を **必ず** 紐付ける。

```markdown
# tasks.md

## Task 12: Implement ReservationValidationService

**Guardrails**: GR-002

**Validation Contract**:
```yaml
requires:
  - IBookCopyRepository.GetAvailableCopiesByBookIdAsync(bookId)
ensures:
  - Available なコピーが0件である (FR-017)
```

**実装条件**:
- [ ] IBookCopyRepository.GetAvailableCopiesByBookIdAsync を呼び出す
- [ ] Available が1件でもあればエラーを返す
- [ ] GR-002 を満たすこと

---

## Task 15: Implement LoanBoundaryService.CanBorrowAsync

**Guardrails**: GR-001

**実装条件**:
- [ ] Ready 状態の予約を検索する（Waiting だけではダメ）
- [ ] Ready 予約者以外への貸出を拒否する
- [ ] GR-001 を満たすこと
```

---

## 3. 属性の強制反映（data-model）

### 3.1 問題

spec に `Position: 1, 2, 3...` と明記されていたが、data-model 生成で落ちた。

### 3.2 解決策

**spec の属性定義を機械的に抽出し、data-model に強制反映する。**

```yaml
# spec.yaml - エンティティ定義
entities:
  Reservation:
    attributes:
      - name: Position
        type: int
        description: "予約順番（1から始まる連番）"
        required: true  # ← これがあれば data-model から落ちない
```

### 3.3 data-model 生成ルール

```
IF spec.entities[X].attributes[Y].required == true
THEN data-model.entities[X] MUST include attribute Y
```

### 3.4 検証チェックリスト

data-model 生成後に自動チェック：

```
□ spec の required 属性が全て data-model に存在するか
□ 型が一致しているか
□ 欠落があればエラーとして報告
```

---

## 4. Validation Contract（検証契約）

### 4.1 概念

ValidationService が「何を参照し、何を保証するか」を明示的に定義する。

### 4.2 スキーマ

```yaml
validation_contract:
  service: "{ServiceName}"
  method: "{MethodName}"
  requires:
    - "{IRepository}.{Method}({params}) - {説明}"
  ensures:
    - "{条件} ({FR番号})"
  fr_references: [FR-XXX, FR-YYY]
```

### 4.3 tasks.md での記述例

```yaml
# Task: Implement ReservationValidationService.ValidateCanReserveAsync

validation_contract:
  service: ReservationValidationService
  method: ValidateCanReserveAsync
  requires:
    - IBookRepository.GetByIdAsync(bookId) - 書籍存在確認
    - IBookCopyRepository.GetAvailableCopiesByBookIdAsync(bookId) - ★必須: 利用可能コピー確認
    - IMemberRepository.GetByIdAsync(memberId) - 会員存在・状態確認
    - IReservationRepository.GetActiveReservationsByMemberIdAsync(memberId) - 予約上限確認
  ensures:
    - "書籍が存在すること"
    - "★ Available なコピーが0件であること (FR-017)"
    - "会員がアクティブであること"
    - "予約上限に達していないこと"
  fr_references: [FR-017]
```

### 4.4 AI 実装者への指示

```
★★★ 重要 ★★★

validation_contract.requires に列挙されたリポジトリメソッドは
全て呼び出すこと。「不要」と判断して省略してはならない。

❌ NG: 「MemberRepository だけ見れば十分」
✅ OK: requires の全メソッドを呼び出す
```

---

## 5. Query Semantics（クエリの意味）

### 5.1 問題

`GetLoansQuery` が一般一覧と Overdue 専用の両方の意味を持ち、実装で混同された。

### 5.2 解決策

plan.md で Query の semantic を明示する。

```markdown
# plan.md - Query セクション

## Queries

| Query | Semantic | Repository Method |
|-------|----------|-------------------|
| GetLoansQuery | 全 Loan 一覧 / Active 一覧 | GetAllLoansAsync, GetActiveLoansByMemberIdAsync |
| GetOverdueLoansQuery | Overdue 専用 | GetOverdueLoansAsync |

**注意**: GetLoansQuery で GetOverdueLoansAsync を使ってはならない。
```

### 5.3 tasks.md での記述

```markdown
## Task: Implement GetLoansQueryHandler

**Query Semantic**: 全 Loan 一覧 / Active 一覧

**使用すべき Repository メソッド**:
- GetAllLoansAsync（管理者用）
- GetActiveLoansByMemberIdAsync（会員用）

**使用禁止**:
- ❌ GetOverdueLoansAsync（これは GetOverdueLoansQuery 用）

**実装条件**:
- [ ] MemberId が null → GetAllLoansAsync
- [ ] MemberId が指定 → GetActiveLoansByMemberIdAsync
- [ ] GetOverdueLoansAsync を使っていない
```

---

## 6. FR 番号の紐付け

### 6.1 目的

タスクに FR 番号を紐付けることで、「何を満たすべきか」を忘れにくくする。

### 6.2 tasks.md フォーマット

```markdown
## Task 12: Implement ReserveBookCommandHandler

**関連 FR**: FR-017, FR-018, FR-021

**Guardrails**: GR-001, GR-002, GR-003

**実装条件**:
- [ ] FR-017: Available コピーがある場合は予約不可
- [ ] FR-018: Position を採番して設定
- [ ] FR-021: Ready 状態の予約者優先（BoundaryService 側で対応）
```

### 6.3 自動検証

tasks.md 生成後にチェック：

```
□ spec の全 FR が少なくとも1つのタスクに紐付いているか
□ Guardrail が少なくとも1つのタスクに紐付いているか
□ 紐付けのないものはエラーとして報告
```

---

## 7. 実装チェックリスト

### spec-kit 改善項目

| # | 改善 | 対象フェーズ | 優先度 |
|---|------|-------------|:------:|
| 1 | Guardrails セクションを spec に追加 | spec 定義 | 高 |
| 2 | plan.md に Guardrails を必ず含める | spec → plan | 高 |
| 3 | tasks.md に Guardrail を紐付け | plan → tasks | 高 |
| 4 | required 属性の強制反映 | data-model 生成 | 高 |
| 5 | validation_contract の tasks 記述 | tasks 生成 | 中 |
| 6 | Query semantic の明示 | plan 生成 | 中 |
| 7 | FR 番号の自動紐付け | tasks 生成 | 中 |
| 8 | 紐付け漏れの自動検出 | 検証 | 低 |

### カタログ側の対応（完了）

| # | 改善 | ファイル | 状態 |
|---|------|----------|:----:|
| 1 | domain-ordered-queue パターン追加 | domain-ordered-queue.yaml | ✅ |
| 2 | 複合前提条件チェックパターン追加 | domain-validation-service.yaml | ✅ |
| 3 | 優先権判定パターン追加 | boundary-pattern.yaml | ✅ |
| 4 | validation_contract スキーマ追加 | domain-validation-service.yaml | ✅ |
| 5 | COMMON_MISTAKES.md 更新 | COMMON_MISTAKES.md | ✅ |
| 6 | LLM_PATTERN_INDEX.md 更新 | LLM_PATTERN_INDEX.md | ✅ |

---

## 8. サンプル: 図書館予約機能の Guardrails

### spec.yaml

```yaml
feature: reserve-book
description: "書籍の予約機能"

guardrails:
  - id: GR-001
    fr: FR-017
    rule: "Available なコピーがある間は予約不可"
    scope: ReservationValidationService
    test: "利用可能コピーがある状態で予約 → エラー"

  - id: GR-002
    fr: FR-018
    rule: "予約は Position で先着順管理"
    scope: Reservation Entity
    test: "新規予約の Position が最大値+1 になる"

  - id: GR-003
    fr: FR-021
    rule: "Ready 予約者が貸出優先権を持つ"
    scope: LoanBoundaryService
    test: "Ready 予約者がいる場合、他の会員は貸出不可"

entities:
  Reservation:
    attributes:
      - name: Position
        type: int
        required: true
        description: "予約順番（1から連番）"
      - name: Status
        type: ReservationStatus
        required: true
        values: [Pending, Ready, Fulfilled, Cancelled]
```

### plan.md（抜粋）

```markdown
## Guardrails（絶対遵守）

| ID | FR | ルール | 対象 |
|----|----|----|------|
| GR-001 | FR-017 | Available コピーがある間は予約不可 | ReservationValidationService |
| GR-002 | FR-018 | 予約は Position で先着順管理 | Reservation Entity |
| GR-003 | FR-021 | Ready 予約者が貸出優先権を持つ | LoanBoundaryService |

**警告**: これらのルールを満たさない実装は仕様違反です。
```

### tasks.md（抜粋）

```markdown
## Task 8: Implement ReservationValidationService.ValidateCanReserveAsync

**Guardrails**: GR-001
**関連 FR**: FR-017

**Validation Contract**:
```yaml
requires:
  - IBookCopyRepository.GetAvailableCopiesByBookIdAsync(bookId)
ensures:
  - Available なコピーが0件である (FR-017)
```

**実装条件**:
- [ ] IBookCopyRepository.GetAvailableCopiesByBookIdAsync を呼び出す
- [ ] 結果が1件以上なら ValidationResult.Failure を返す
- [ ] エラーメッセージ: "利用可能なコピーがあります。直接貸出してください。"
```

---

## 9. 期待効果

| 問題 | 対策 | 効果 |
|------|------|------|
| 重要ルールの伝播漏れ | Guardrails の強制伝播 | spec → plan → tasks で追跡可能 |
| 属性の欠落 | required 属性の強制反映 | Position 等が data-model から落ちない |
| リポジトリ呼び出し漏れ | validation_contract | requires に明示されたメソッドを必ず呼ぶ |
| Query の混同 | Query semantic の明示 | 正しい Repository メソッドを使用 |
| FR 忘却 | FR 番号の紐付け | タスク実行時に「何を満たすか」が明確 |

---

## カタログバージョン

- **バージョン**: v2025.12.07.2
- **作成日**: 2025-12-07
- **背景**: 図書館ドッグフーディングフィードバック（カタログの改善19, 20）
