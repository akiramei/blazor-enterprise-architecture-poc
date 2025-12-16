---
description: Execute the implementation planning workflow with catalog integration.
handoffs:
  - label: Create Tasks
    agent: speckit.tasks
    prompt: Break the plan into tasks
    send: true
  - label: Create Checklist
    agent: speckit.checklist
    prompt: Create a checklist for the following domain...
scripts:
  sh: scripts/bash/setup-plan.sh --json
  ps: scripts/powershell/setup-plan.ps1 -Json
agent_scripts:
  sh: scripts/bash/update-agent-context.sh __AGENT__
  ps: scripts/powershell/update-agent-context.ps1 -AgentType __AGENT__
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## このコマンドの目的

- SPEC の内容を元に、Vertical Slice + パターンカタログ前提の技術計画を作ること
- 選択されたパターンは後続の `/speckit.tasks`, `/speckit.implement` から再利用される
- Boundary セクション（UI がある場合）と Catalog Binding セクションが必須出力

## Outline

1. **Setup**: Run `{SCRIPT}` from repo root and parse JSON for FEATURE_SPEC, IMPL_PLAN, SPECS_DIR, BRANCH.

2. **Load context**: Read FEATURE_SPEC and `/memory/constitution.md`. Load IMPL_PLAN template.

3. **Execute plan workflow**: Follow the structure in IMPL_PLAN template to:
   - Fill Technical Context (mark unknowns as "NEEDS CLARIFICATION")
   - Fill Constitution Check section from constitution
   - Evaluate gates (ERROR if violations unjustified)
   - Phase 0: Generate research.md (resolve all NEEDS CLARIFICATION)
   - **Phase 0.5: Catalog Pattern Selection** (CATALOG EXTENSION - see below)
   - Phase 1: Generate data-model.md, contracts/, quickstart.md
   - **Phase 1.5: Design-Level COMMON_MISTAKES Check** (CATALOG EXTENSION - see below)
   - Re-evaluate Constitution Check post-design

4. **Stop and report**: Command ends after Phase 1.5 (before tasks).

## Phases

### Phase 0: Outline & Research

(Standard spec-kit phase - unchanged)

1. Extract unknowns from Technical Context
2. Generate and dispatch research agents
3. Consolidate findings in research.md

### Phase 0.25: Guardrails Integration (CRITICAL)

**This phase integrates guardrails from guardrails.yaml file.**

> **詳細**: `catalog/speckit-extensions/commands/speckit.guardrails.md` を参照

#### 0.25.0 guardrails.yaml 読み込み（先行フェーズで生成済みの場合）

**前提**: `/speckit.guardrails` が先行実行されている場合、`specs/{feature}/{slice}.guardrails.yaml` が存在する。

```
1. Check if specs/{feature}/{slice}.guardrails.yaml exists
2. If exists → Load and integrate into plan
3. If not exists → Extract from SPEC (legacy mode, see below)
```

**guardrails.yaml が存在する場合**:

```
Read: specs/{feature}/{slice}.guardrails.yaml

Integration:
1. canonical_routes → Plan の Guardrails セクションに「正解経路」として引用
2. forbidden_actions → Plan の制約として明記
3. spec_derived_guardrails → GR-XXX として列挙
4. acceptance_criteria → Plan のテスト要件に反映
```

**出力フォーマット（guardrails.yaml 統合時）**:

```markdown
## Guardrails (from guardrails.yaml)

### Canonical Routes (正解経路)

| ID | Operation | Path | Source |
|----|-----------|------|--------|
| CR-001 | 予約キャンセル | Handler → QueueService.DequeueAsync | domain-ordered-queue.yaml |

### Forbidden Actions (禁止事項)

| ID | Forbidden | Reason | Severity |
|----|-----------|--------|----------|
| FA-001 | reservation.Cancel() 直接呼び出し | Position繰り上げなし | critical |

### Spec-Derived Guardrails

| ID | FR Ref | Rule | Scope |
|----|--------|------|-------|
| GR-001 | FR-021 | Ready 予約者が最優先 | LoanBoundaryService |
```

#### 0.25.1 Legacy Mode: SPEC からの直接抽出

**guardrails.yaml が存在しない場合のみ実行**。

> **詳細**: `catalog/AI_GUARDRAILS.md` の「Phase 0.25: Guardrails 抽出」を参照

1. SPEC から「〜のみ」「優先」「先着」などの重要ルールを抽出
2. Guardrails セクションとして plan.md に追加
3. 各 Guardrail に ID、FR番号、対象スコープ、違反時の問題を記載

**警告**: Guardrails が0件の場合、spec に前提条件が明示されていない可能性がある。

> **推奨**: guardrails.yaml が存在しない場合は、先に `/speckit.guardrails` を実行することを検討。

#### 0.25.2 Guardrail-FR Cross-Reference Check (U2 対策)

**ルール**: Guardrail に記載した機能は、対応する FR が Spec に存在しなければならない。

**手順**:
1. 各 Guardrail の FR 参照を確認
2. 参照先 FR が Spec に存在するか検証
3. 存在しない機能への言及があれば **WARNING**

**出力フォーマット**:
```markdown
### Guardrail-FR Cross-Reference

| Guardrail | FR Reference | FR Exists? | Status |
|-----------|--------------|:----------:|:------:|
| GR-001 | FR-016 | ✅ | OK |
| GR-005 | FR-027 | ✅ | OK |
| GR-XXX | (extension) | ❌ | ⚠️ WARNING: FR なし - 機能追加 or Guardrail 削除 |
```

**WARNING 時の対応**:
- 該当機能を FR に追加するか
- Guardrail から該当記述を削除するか
- Out of Scope として明記するか
を決定してから続行する。

### Phase 0.5: Catalog Pattern Selection (CATALOG EXTENSION)

**This phase is added by the catalog. DO NOT skip.**

> **Skills ヒント**: パターン選択時に `vsa-pattern-selector` の知識が自動的に適用される
> 可能性があります。Feature Slices、Query Patterns、Domain Patterns の選択基準は
> Skills が提供します。

#### 0.5.0: Unsupported Intents Scan (FIRST - NEW)

**MUST scan unsupported_intents BEFORE pattern selection.**

1. Read `catalog/index.json` の `ai_decision_matrix.unsupported_intents`
2. 要求文に該当キーワードがあれば **STOP**
3. ユーザーにインフラ前提を確認してから続行

**該当キーワード例**:
- 通知/メール/リマインダー → SMTP等の選定を確認
- バッチ/定期実行/ジョブ → スケジューラ選定を確認
- PDF/帳票/印刷 → 帳票ライブラリ選定を確認

**STOP時の出力**:
⚠️ カタログ外機能を検出しました。
- 検出キーワード: {keyword}
- 必要な確認: {action from unsupported_intents}
- 続行前に上記を明確にしてください。

#### 0.5.1: Read catalog index (renamed from 1.)

1. **Read catalog index**:
   ```
   Read: catalog/index.json
   Read: catalog/DECISION_FLOWCHART.md
   Read: catalog/CHARACTERISTICS_CATALOG.md
   ```

2. **Extract characteristics from feature spec**:
   - Operation type: `op:mutates-state`, `op:read-only`
   - Cross-cutting: `xcut:auth`, `xcut:audit`, `xcut:validation`
   - Structure: `struct:single-aggregate`, `struct:state-machine`

3. **Match patterns from catalog**:
   - Use `ai_decision_matrix` in index.json to find pattern IDs
   - For each matched pattern, read: `catalog/patterns/{pattern-id}.yaml`
   - Note `ai_guidance.common_mistakes` from each pattern

4. **Generate Catalog Binding section** in plan.md:

   ```markdown
   ## Catalog Binding

   | Requirement | Pattern ID | Status | must_read_checklist 引用（実装時に記入） |
   |-------------|-----------|--------|----------------------------------------|
   | 機能作成 | feature-create-entity | matched | （実装時に引用を記入） |
   | 入力検証 | validation-behavior | auto-applied | （実装時に引用を記入） |
   | 状態遷移 | domain-state-machine | matched | （実装時に引用を記入） |

   ### 引用記入ルール（実装フェーズで使用）

   実装開始前に、各パターンの `must_read_checklist` から引用を記入すること。
   引用することで「読んだつもり」を防止する。

   **引用例:**
   ```
   | validation-behavior | auto-applied | > Handler内でSaveChangesAsyncを呼ばない |
   ```

   ### Unmatched Requirements
   - {requirement}: {reason why no pattern matches}

   ### Creative Areas
   - Domain Model: {entities to design}
   - Validation Logic: {rules to implement}
   - UI Layout: {screens to design}
   ```

5. **Boundary modeling** (if UI is involved):

   > **Skills ヒント**: UI がある場合、`vsa-boundary-modeler` の知識が自動的に適用される
   > 可能性があります。Intent 定義、Entity.CanXxx() 設計、BoundaryService の責務分離は
   > Skills が提供します。

   - Read: `catalog/patterns/boundary-pattern.yaml`
   - List all Intents (user intentions)
   - Design Entity.CanXxx() methods for each Intent

   ```markdown
   ## Boundary

   ### Intents
   | Intent | Entity.CanXxx() | Decision Logic |
   |--------|-----------------|----------------|

   ### Conversation Script
   | User Intent | System Response |
   |-------------|-----------------|
   ```

**Output**: Plan with Catalog Binding, Boundary sections complete

### Phase 1: Design & Contracts

(Standard spec-kit phase with catalog awareness)

1. Extract entities from feature spec → data-model.md
   - Include **Entity.CanXxx() methods from Boundary section**
   - **属性の強制反映チェック（CRITICAL - NEW）**

2. Generate API contracts from functional requirements
   - Use MediatR patterns from catalog (not REST endpoints)
   - **Query Semantics の明示（CRITICAL - NEW）**

3. Agent context update

#### 1.1 属性の強制反映チェック（FR-018 対策）

> **詳細**: `catalog/AI_GUARDRAILS.md` の「属性の強制反映チェック」を参照

**ルール**: spec に明記された属性は data-model から落としてはならない。

特に注意すべき属性: Position, Order, Status, State, Priority, Limit

#### 1.1.1 Enum Value Enforcement Check（CRITICAL - 予防的チェック）

**ルール**: Spec の Enum 値は Plan の data-model に **完全一致** で反映されなければならない。

**背景**: 属性の存在チェックだけでは不十分。Enum の「値」が欠落するミスを予防する。

**手順**:

1. Spec の Key Entities セクションから Status/Enum 定義を抽出:
   ```
   例: Has status (Waiting, Ready, Completed, Cancelled)
   ```

2. Plan の data-model から対応する Enum 定義を抽出:
   ```
   例: Status: ReservationStatus (Waiting, Ready, Cancelled)
   ```

3. **完全一致を検証**:
   - Spec の全ての値が Plan に存在するか
   - 欠落があれば **ERROR**

**出力フォーマット**:

```markdown
### Enum Value Enforcement Check

| Entity | Enum | Spec Values | Plan Values | Status |
|--------|------|-------------|-------------|:------:|
| BookCopy | BookCopyStatus | Available, OnLoan, Reserved, Inactive | Available, OnLoan, Reserved, Inactive | ✅ OK |
| Member | MemberStatus | Active, Suspended | Active, Suspended | ✅ OK |
| Loan | LoanStatus | OnLoan, Returned, Overdue | OnLoan, Returned | ⚠️ Overdue? |
| Reservation | ReservationStatus | Waiting, Ready, Completed, Cancelled | Waiting, Ready, Cancelled | ❌ **ERROR: Completed 欠落** |
```

**エラー時の動作**:
- ❌ がある場合 → **ERROR: Plan 生成を停止**
- 欠落した値を Plan に追加してから再開
- **tasks フェーズに進むことを禁止**

**注意**:
- Spec で定義された Enum 値を「要約」「解釈」してはならない
- 値の追加は許可（Plan で詳細化）、削除は禁止

#### 1.2 Query Semantics の明示（Query バグ対策）

> **詳細**: `catalog/AI_GUARDRAILS.md` の「Query Semantics の明示」を参照

**ルール**: Query の意味（semantic）を plan で明示し、使用すべき Repository メソッドを指定する。

### Phase 1.4: UI-IR Schema Generation (UI がある場合のみ)

**目的**: Boundary から UI 中間表現を生成し、画面品質を設計段階で保証する

> **思想**: UI は「完成品」ではなく「組み替え可能な構造」として出力する。
> AI は論理的完全性を担保し、レイアウトは人間に委ねる。

#### 1.4.1 UI-IR 生成条件チェック

以下の**いずれか**の条件を満たす場合に実行：

| 条件 | 判定方法 |
|------|---------|
| Spec に UI 層が含まれる | `characteristics` に `layer:ui` または `layer:full-slice` がある |
| Boundary セクションが存在 | Plan の Boundary セクションに Intent が定義されている |
| UI 関連の FR がある | Spec の FR に「画面」「一覧」「フォーム」「ボタン」等の記述がある |

**追加条件**（上記を満たした上で）：
- Entity.CanXxx() メソッドが設計されている（disabled_when に必要）

**条件を満たさない場合**: このフェーズをスキップして Phase 1.5 へ進む

**判定ログ出力**（1行形式）:
```
UI-IR phase: run=true (layer=ui, boundary=true, ui_fr=true)
UI-IR phase: run=false (layer=backend, boundary=false, ui_fr=false)
```

> **参照**: UI-IR 実装時の入力要件は `catalog/skills/vsa-ui-enhancer/input-requirements.md` を参照

#### 1.4.2 UI-IR テンプレート読み込み

```
Read: catalog/scaffolds/ui-ir-template.yaml
Read: catalog/scaffolds/ui-ir-schema.yaml
Read: catalog/scaffolds/ui-ir-lint-rules.yaml
```

#### 1.4.2.5 UI Maturity Assessment (vNext)

**目的**: モデルの成熟度を判定し、許可される UI 語彙を決定する

> **設計思想**: モデルが語っていないことを、UI が先取りしてはいけない。
> 成熟度に応じて UI 語彙を制限することで、AI 生成の破綻を防ぐ。

##### Step 1: 成熟度ゲート判定

| Gate | 条件 | チェック項目 |
|------|------|-------------|
| boundary→entity | Entity 属性確定 | data-model.md に属性定義あり |
| boundary→entity | 入力項目確定 | form_fields が埋まっている |
| entity→view | 関心領域安定 | concerns が前回 plan から変更なし |
| entity→view | 状態遷移確定 | Entity.CanXxx() が全操作に対応 |

**判定アルゴリズム**:
```
if 属性未確定:
  level = boundary
elif concerns 未安定:
  level = entity
elif stability 証拠あり:
  level = view
else:
  level = entity
```

##### Step 2: uiPolicy 自動設定（defaults 注入）

成熟度に応じて `maturity_defaults` を `uiPolicy` に物理的に注入：

```yaml
# boundary の場合
uiPolicy:
  allowed_widgets: [inline-sections, card, list, flow, simple-list]
  denied_widgets: [tab, master-detail, stepper, accordion, data-grid]

# entity の場合
uiPolicy:
  allowed_widgets: [inline-sections, card, list, flow, simple-list, accordion, data-grid, grouping]
  denied_widgets: [tab, master-detail, stepper]

# view の場合（stability 必須）
uiPolicy:
  allowed_widgets: [全て許可]
  denied_widgets: []
  # 注意: stability.concerns_unchanged_since が必須
```

##### Step 3: structure 構築

concerns を定義し、information_blocks と紐付け：

```yaml
structure:
  subject: "{Aggregate Root}"
  concerns:
    - id: overview
      name: "基本情報"
      blocks: [blk-core]
      exclusivity: false
    - id: history
      name: "履歴"
      blocks: [blk-history]
      exclusivity: true
      comparability:
        mode: rows
        key: createdAt
```

- 各 concern の `exclusivity` を評価
- `comparability` があれば mode と key を設定
- information_blocks への参照を設定

##### Step 4: Lint 実行

```
ui-ir-lint --schema ui-ir-schema.yaml --rules ui-ir-lint-rules.yaml screen.ui-ir.yaml
```

**出力形式**:
```
UI-IR lint: status=pass, rules_checked=8, violations=0
UI-IR lint: status=fail, rules_checked=8, violations=2 (MATURITY-001, BLOCK-002)
UI-IR lint: status=pass_with_waivers, rules_checked=8, waivers=1 (MATURITY-001)
```

**Lint ルール一覧**:

| ID | 名称 | Severity |
|----|------|----------|
| MATURITY-001 | 成熟度超過 | error |
| MATURITY-002 | 安定性未達で view | error |
| STRUCTURE-001 | 排他性違反 | error |
| STRUCTURE-002 | 比較性違反 | warning |
| STRUCTURE-003 | 排他 concern のブロック共有 | warning |
| BLOCK-001 | 孤立ブロック | warning |
| BLOCK-002 | 参照切れ | error |
| OVERRIDE-001 | 未承認 override | error |
| OVERRIDE-002 | violation_id なし override | error |

**エラー時の対応**:
- error があれば自己修正または uiPolicyOverrides で waive
- waive には approved_by（承認者）が必須

#### 1.4.3 画面ごとに UI-IR スキーマ填充

各画面について以下を定義：

| セクション | 内容 |
|-----------|------|
| screen | id, name, primary_user_intent |
| data.bindings | Query/Command マッピング |
| main_actions | priority, frequency, error_cost, is_destructive |
| secondary_actions | 補助操作 |
| information_blocks | 表示情報の優先度 |
| form_fields | 入力項目（該当する場合） |

#### 1.4.4 Derivation Rules 適用

confirmation_level を自動算出：

```
if error_cost in [Negligible, Low] and reversibility == Reversible:
  confirmation_level = None
elif error_cost == Medium:
  confirmation_level = Simple
elif error_cost == High:
  confirmation_level = Detailed
elif error_cost == Critical or reversibility == Irreversible:
  confirmation_level = DoubleConfirm
```

#### 1.4.5 UX 自己評価

ux_review.mandatory_checks 全10項目を検証：

**基本チェック（v0.1）**:
```
□ UX-001: Primary アクションは画面に1つのみか
□ UX-002: Irreversible/error_cost>=High に確認があるか
□ UX-003: Critical importance がファーストビューにあるか
□ UX-004: Entity.CanXxx() が UI の disabled に反映されているか
□ UX-005: VeryHigh/High 操作が1クリック以内か
```

**成熟度チェック（vNext）**:
```
□ UX-006: maturity.level に対して allowed_widgets が適切か
□ UX-007: view レベルの場合、stability.concerns_unchanged_since が設定されているか
□ UX-008: exclusivity=true の concern が複数ある場合、exclusive-switch が許可されているか
□ UX-009: comparability が定義されている concern に対して comparison affordance が許可されているか
□ UX-010: information_blocks がすべて concerns から参照されているか
```

違反があれば UI-IR を自己修正。
成熟度違反（UX-006〜010）は ui-ir-lint-rules.yaml の Lint ルールでも検証される。

#### 1.4.6 出力

**plan.md に要約セクション追加**:

```markdown
## UI-IR Summary

| Screen | Maturity | Primary Action | Confirmation | Lint Status |
|--------|----------|---------------|--------------|-------------|
| ProductSearch | entity | 検索 | None | pass |
| ProductDelete | entity | 削除 | DoubleConfirm | pass |
| BookDetail | view | 表示 | None | pass_with_waivers |

### Maturity Assessment

| Screen | Level | Gate Evidence | Stability |
|--------|-------|---------------|-----------|
| ProductSearch | entity | form_fields 定義済み | - |
| BookDetail | view | concerns 安定 | plan#3 |

### Lint Results

| Screen | Status | Violations | Waivers |
|--------|--------|------------|---------|
| ProductSearch | pass | 0 | 0 |
| BookDetail | pass_with_waivers | 0 | 1 (MATURITY-001) |

詳細: specs/{feature}/{slice}.ui-ir.yaml
```

**別ファイル出力**:

```
specs/{feature}/{slice}.ui-ir.yaml
```

**Output**: Plan with UI-IR Summary section (including Maturity Assessment), separate .ui-ir.yaml file

### Phase 1.5: Design-Level COMMON_MISTAKES Check (CRITICAL - AUTO)

**This check is MANDATORY and runs AUTOMATICALLY after Phase 1.**

> **詳細**: `catalog/AI_GUARDRAILS.md` の「Phase 1.5: Self-correction Loop」を参照

> **Skills ヒント**: 実装計画チェック時に `vsa-implementation-guard` の知識が自動的に適用される
> 可能性があります。禁止事項、必須パターンは Skills が提供します。

**要約**:
1. `catalog/COMMON_MISTAKES.md` を読む
2. 計画を各ミスパターンと照合
3. 違反があれば自己修正ループで自動修正（ユーザー許可不要）
4. 修正版を出力
5. 全チェック通過 → tasks へ進む / 自動修正不可 → STOP

**Output**: Corrected plan with Design-Level Check section complete

### Phase 1.75: Spec/Plan Consistency Check (SSOT - NEW)

**This phase ensures SSOT (Single Source of Truth) principle is maintained.**

> **詳細**: `catalog/speckit-extensions/spec-plan-consistency.md` を参照

1. **Scan for unresolved decisions**:
   - Check for "Unknowns Resolved" section
   - Each decision should have "Spec 反映: Y" or documented reason

2. **Verify Spec → Plan attribute preservation**:
   - Extract Enum values from spec.md Key Entities
   - Verify all values are present in plan.md Data Model
   - **CRITICAL** if any value is missing (e.g., ReservationStatus Completed)

3. **Verify Plan → Spec constraint reflection**:
   - Scan Plan for numeric constraints (e.g., "3件", "24時間")
   - Scan Plan for phrases: "〜をデフォルトとする", "specに明記なし"
   - **WARNING** if constraint not in spec.md Assumptions

4. **Terminology Consistency Check (T1 対策)**:
   - Extract key terms from Spec (especially time-related properties, status names)
   - Compare with Plan terminology for the same concepts
   - **WARNING** if same concept uses different names

   **出力フォーマット**:
   ```markdown
   ### Terminology Consistency Check

   | Concept | Spec Term | Plan Term | Status |
   |---------|-----------|-----------|:------:|
   | 予約準備完了時刻 | ReadyAt | ReadyAt | ✅ OK |
   | 予約有効期限 | ExpiresAt | ExpiresAt | ✅ OK |
   | 予約有効期限 | - | ReadyAt | ⚠️ WARNING: 用語揺れ |
   ```

   **チェック対象**:
   - 時間関連プロパティ: *At, *Date, *Time, *Until, *Expires
   - 状態関連: Status, State, Phase
   - 数量関連: Count, Limit, Max, Quantity

   **WARNING 時の対応**:
   - Spec の用語に統一する（Spec が SSOT）
   - 用語集（Glossary）への登録を検討

5. **Generate Unknowns Resolved section** (if not exists):
   ```markdown
   ## Unknowns Resolved

   | 項目 | Spec の状態 | 決定 | 理由 | Spec 反映 |
   |------|------------|------|------|:---------:|
   | 予約上限 | 明記なし | 3件 | 図書館業界の標準 | Y |
   ```

6. **Output consistency warnings**:
   - List any Plan constraints not reflected in Spec
   - Recommend adding to Spec.Assumptions before `/speckit.tasks`

**Output**: Plan with SSOT Check complete, warnings listed

---

## Key Rules

- Use absolute paths
- ERROR on gate failures or unresolved clarifications
- **NEVER skip Guardrails Extraction phase** - 重要ルールの伝播漏れを防ぐ
- **NEVER skip Catalog Binding phase** - Constitution requires it
- **NEVER skip Design-Level Check phase** - Runs AUTOMATICALLY after Phase 1
- **NEVER ask user permission to fix violations** - Self-correct automatically
- **NEVER implement patterns that exist in catalog** - use templates
- **NEVER drop attributes from spec** - 属性の強制反映チェックを実行
- **NEVER drop Enum values from spec** - Enum Value Enforcement Check を実行（予防的チェック）
- **ALWAYS define Query Semantics** - Query と Repository の対応を明示
- If Boundary section is empty for UI features → ERROR
- If Design-Level Check cannot auto-correct → ERROR (do not proceed to tasks)
- If Attribute Enforcement Check fails → ERROR (do not proceed)
- **If Enum Value Enforcement Check fails → ERROR (do not proceed to tasks)** - Spec の Enum 値欠落は禁止
- If Guardrails section is empty → WARNING (confirm with spec)
- **ALWAYS output corrected plan** if violations were found and fixed

---

## Task Generation Guidelines (for speckit.tasks handoff)

When handing off to `speckit.tasks`, ensure:

1. **Catalog Binding is complete** - Pattern IDs are assigned to requirements
2. **Each task maps to a Pattern** - Use the mapping below

### Task-to-Pattern Mapping

| Task Type | Pattern ID |
|-----------|-----------|
| Handler実装 | transaction-behavior |
| Validator実装 | validation-behavior |
| Entity実装（CanXxx） | boundary-pattern |
| BoundaryService実装 | boundary-pattern |
| UI実装 | boundary-pattern |
| StateMachine実装 | domain-state-machine |
| ValidationService実装 | domain-validation-service |

### Task Format Requirement

Each task in tasks.md MUST include:
- **Pattern:** {pattern-id}
- **YAML:** catalog/patterns/{pattern-id}.yaml
- **Catalog Constraints:** quoted from must_read_checklist
- **Acceptance Criteria:** derived from constraints

See: `catalog/speckit-extensions/commands/speckit.tasks.md` for details
