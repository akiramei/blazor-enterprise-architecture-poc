# Verification Rules Guide

## 概要

ガードレールフェーズで生成された `guardrails.yaml` の `verification_rules` を、
静的検査およびテスト生成に活用するためのガイド。

### このガイドの目的

1. **静的検査ルール展開** - `grep_prohibition` を検証スクリプトに変換
2. **spec-test-template.yaml 連携** - `acceptance_criteria` からテスト生成
3. **tasks.md への自動展開** - Guardrail ID をタスクに紐付け

---

## Phase 2: Verification Rules（Plan直後）

### ワークフロー位置

```
specify → guardrails → plan → 【verification】→ tasks → implement
                              ↑ここで実行（Plan内で自動）
```

**入力**: `guardrails.yaml` + `plan.md`
**出力**:
- 静的検査スクリプト（オプション）
- テスト生成ヒント
- tasks.md への Guardrail ID 紐付け

---

## 1. 静的検査ルール展開

### 1.1 grep_prohibition タイプ

`guardrails.yaml` の `verification_rules.static_checks` で定義された
`grep_prohibition` ルールを検証スクリプトに変換する。

#### 入力例

```yaml
verification_rules:
  static_checks:
    - id: SC-001
      type: "grep_prohibition"
      pattern: "reservation\\.Cancel\\(\\)"
      exclude_paths: ["*QueueService.cs", "*Tests.cs"]
      message: "Entity.Cancel()の直接呼び出しは禁止。QueueService.DequeueAsync()を使用してください。"

    - id: SC-002
      type: "grep_prohibition"
      pattern: "CheckAndPromoteNextAsync.*Cancel"
      exclude_paths: ["*Tests.cs"]
      message: "Cancel後のCheckAndPromoteNextAsyncは禁止。DequeueAsyncを使用してください。"
```

#### PowerShell 出力

```powershell
# verification/check-guardrails.ps1
# Generated from: specs/{feature}/{slice}.guardrails.yaml

param(
    [string]$SourcePath = "src"
)

$violations = @()

# SC-001: Entity.Cancel()の直接呼び出しは禁止
Write-Host "Checking SC-001: Entity.Cancel() direct call..." -ForegroundColor Cyan
$sc001 = Get-ChildItem -Path $SourcePath -Recurse -Filter "*.cs" |
    Where-Object {
        $_.FullName -notmatch "QueueService" -and
        $_.FullName -notmatch "Tests"
    } |
    Select-String -Pattern "reservation\.Cancel\(\)"

if ($sc001) {
    $violations += @{
        Id = "SC-001"
        Message = "Entity.Cancel()の直接呼び出しは禁止。QueueService.DequeueAsync()を使用してください。"
        Files = $sc001
    }
}

# SC-002: Cancel後のCheckAndPromoteNextAsyncは禁止
Write-Host "Checking SC-002: CheckAndPromoteNextAsync after Cancel..." -ForegroundColor Cyan
$sc002 = Get-ChildItem -Path $SourcePath -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch "Tests" } |
    Select-String -Pattern "CheckAndPromoteNextAsync.*Cancel"

if ($sc002) {
    $violations += @{
        Id = "SC-002"
        Message = "Cancel後のCheckAndPromoteNextAsyncは禁止。DequeueAsyncを使用してください。"
        Files = $sc002
    }
}

# 結果出力
if ($violations.Count -gt 0) {
    Write-Host "`n❌ GUARDRAIL VIOLATIONS FOUND:" -ForegroundColor Red
    foreach ($v in $violations) {
        Write-Host "`n[$($v.Id)] $($v.Message)" -ForegroundColor Yellow
        $v.Files | ForEach-Object { Write-Host "  - $_" -ForegroundColor Gray }
    }
    exit 1
} else {
    Write-Host "`n✅ All guardrail checks passed." -ForegroundColor Green
    exit 0
}
```

#### Bash 出力

```bash
#!/bin/bash
# verification/check-guardrails.sh
# Generated from: specs/{feature}/{slice}.guardrails.yaml

SOURCE_PATH="${1:-src}"
VIOLATIONS=0

echo "=== Guardrail Verification ==="

# SC-001: Entity.Cancel()の直接呼び出しは禁止
echo "Checking SC-001: Entity.Cancel() direct call..."
SC001=$(find "$SOURCE_PATH" -name "*.cs" \
    ! -path "*QueueService*" \
    ! -path "*Tests*" \
    -exec grep -l "reservation\.Cancel()" {} \;)

if [ -n "$SC001" ]; then
    echo "❌ SC-001: Entity.Cancel()の直接呼び出しは禁止"
    echo "$SC001" | while read f; do echo "  - $f"; done
    VIOLATIONS=$((VIOLATIONS + 1))
fi

# SC-002: Cancel後のCheckAndPromoteNextAsyncは禁止
echo "Checking SC-002: CheckAndPromoteNextAsync after Cancel..."
SC002=$(find "$SOURCE_PATH" -name "*.cs" \
    ! -path "*Tests*" \
    -exec grep -l "CheckAndPromoteNextAsync.*Cancel" {} \;)

if [ -n "$SC002" ]; then
    echo "❌ SC-002: Cancel後のCheckAndPromoteNextAsyncは禁止"
    echo "$SC002" | while read f; do echo "  - $f"; done
    VIOLATIONS=$((VIOLATIONS + 1))
fi

# 結果
if [ $VIOLATIONS -gt 0 ]; then
    echo ""
    echo "❌ $VIOLATIONS guardrail violation(s) found."
    exit 1
else
    echo ""
    echo "✅ All guardrail checks passed."
    exit 0
fi
```

### 1.2 call_sequence_check タイプ

呼び出しシーケンスを検証するルール。

#### 入力例

```yaml
verification_rules:
  static_checks:
    - id: SC-003
      type: "call_sequence_check"
      forbidden_sequence: ["Cancel()", "CheckAndPromoteNextAsync"]
      allowed_sequence: ["DequeueAsync"]
      scope: "*CommandHandler.cs"
      message: "Cancel後のCheckAndPromoteNextAsyncは禁止。DequeueAsyncを使用してください。"
```

#### 検証ロジック

```powershell
# call_sequence_check の検証
function Test-CallSequence {
    param(
        [string]$FilePath,
        [string[]]$ForbiddenSequence,
        [string[]]$AllowedSequence
    )

    $content = Get-Content $FilePath -Raw

    # 禁止シーケンスの検出
    $forbiddenPattern = $ForbiddenSequence -join ".*"
    if ($content -match $forbiddenPattern) {
        # 許可シーケンスが存在するか確認
        $allowedFound = $false
        foreach ($allowed in $AllowedSequence) {
            if ($content -match $allowed) {
                $allowedFound = $true
                break
            }
        }

        if (-not $allowedFound) {
            return $false  # 違反
        }
    }

    return $true  # OK
}
```

---

## 2. spec-test-template.yaml 連携

### 2.1 acceptance_criteria からテスト生成

`guardrails.yaml` の `acceptance_criteria` を
`spec-test-template.yaml` と連携してテストを生成する。

#### 入力例

```yaml
acceptance_criteria:
  - id: AC-001
    gr_ref: "GR-001"
    criterion: "Ready予約者がいる場合、他の会員は貸出不可"
    test_derivation:
      given:
        - "Book A に Ready 予約（Member X）がある"
        - "BookCopy A1 が Available"
      when: "Member Y が Book A を借りようとする"
      then: "Deny(予約者に優先権があります)"

  - id: AC-002
    source: "domain-ordered-queue.yaml"
    criterion: "Cancel後、Position=2がPosition=1になること"
    test_derivation:
      given:
        - "Reservation R1 (Position=1, Status=Waiting)"
        - "Reservation R2 (Position=2, Status=Waiting)"
      when: "R1 を DequeueAsync でキャンセル"
      then:
        - "R1.Status == Cancelled"
        - "R1.Position == null"
        - "R2.Position == 1"
        - "R2.Status == Ready"
```

#### 生成テスト例

```csharp
using Xunit;
using FluentAssertions;

namespace Library.Tests.Guardrails;

/// <summary>
/// ガードレール由来テスト
/// Generated from: specs/reservation/cancel-reservation.guardrails.yaml
/// </summary>
public class CancelReservationGuardrailTests
{
    // ================================================================
    // AC-001: Ready予約者がいる場合、他の会員は貸出不可
    // ================================================================

    [Fact(DisplayName = "AC-001: Ready予約者がいる場合、他の会員は貸出不可")]
    public async Task AC001_ReadyReservation_ShouldBlockOtherMembers()
    {
        // Arrange (Given)
        // - Book A に Ready 予約（Member X）がある
        // - BookCopy A1 が Available
        var bookId = BookId.From(Guid.NewGuid());
        var memberX = MemberId.From(Guid.NewGuid());
        var memberY = MemberId.From(Guid.NewGuid());

        var reservation = Reservation.Create(bookId, memberX);
        reservation.MakeReady();

        var bookCopy = BookCopy.Create(bookId, "A1");

        // Act (When)
        // - Member Y が Book A を借りようとする
        var decision = bookCopy.CanBorrow(memberY, readyReserverId: memberX);

        // Assert (Then)
        // - Deny(予約者に優先権があります)
        decision.IsAllowed.Should().BeFalse();
        decision.Reason.Should().Contain("予約者に優先権があります");
    }

    // ================================================================
    // AC-002: Cancel後、Position=2がPosition=1になること
    // ================================================================

    [Fact(DisplayName = "AC-002: Cancel後、Position=2がPosition=1になること")]
    public async Task AC002_Cancel_ShouldPromoteFollowingPositions()
    {
        // Arrange (Given)
        // - Reservation R1 (Position=1, Status=Waiting)
        // - Reservation R2 (Position=2, Status=Waiting)
        var bookId = BookId.From(Guid.NewGuid());

        var r1 = Reservation.Create(bookId, MemberId.From(Guid.NewGuid()));
        r1.EnqueueAt(1);

        var r2 = Reservation.Create(bookId, MemberId.From(Guid.NewGuid()));
        r2.EnqueueAt(2);

        var repo = new InMemoryReservationRepository();
        await repo.AddAsync(r1);
        await repo.AddAsync(r2);

        var queueService = new ReservationQueueService(repo);

        // Act (When)
        // - R1 を DequeueAsync でキャンセル
        await queueService.DequeueAsync(r1.Id);

        // Assert (Then)
        var updatedR1 = await repo.GetByIdAsync(r1.Id);
        var updatedR2 = await repo.GetByIdAsync(r2.Id);

        // - R1.Status == Cancelled
        updatedR1!.Status.Should().Be(ReservationStatus.Cancelled);
        // - R1.Position == null
        updatedR1.Position.Should().BeNull();
        // - R2.Position == 1
        updatedR2!.Position.Should().Be(1);
        // - R2.Status == Ready
        updatedR2.Status.Should().Be(ReservationStatus.Ready);
    }
}
```

### 2.2 test_generation_hints の活用

```yaml
verification_rules:
  test_generation_hints:
    - guardrail_id: "GR-001"
      test_type: "boundary_decision"
      template_ref: "spec-test-template.yaml#boundary_test"

    - guardrail_id: "AC-002"
      test_type: "orchestration"
      template_ref: "spec-test-template.yaml#orchestration_rules"
```

| test_type | 説明 | テンプレート参照 |
|-----------|------|-----------------|
| `boundary_decision` | BoundaryDecision のテスト | `#boundary_test` |
| `orchestration` | オーケストレーションルールのテスト | `#orchestration_rules` |
| `state_transition` | 状態遷移のテスト | `#state_transition` |

---

## 3. tasks.md への Guardrail ID 紐付け

### 3.1 自動展開フォーマット

`/speckit.tasks` 実行時に、`guardrails.yaml` を読み込み、
各タスクに関連する Guardrail ID を自動付与する。

#### タスク出力例

```markdown
## Task 3: CancelReservationCommandHandler 実装

**Guardrails:** FA-001, FA-002, CR-001
**関連 FR:** FR-018

### Canonical Route (CR-001)

> CancelReservationCommandHandler
>   → IReservationQueueService.DequeueAsync(reservationId, ct)
>     → Reservation.Cancel()
>     → PromotePositions(後続)
>     → PromoteNext(新しい先頭)

### Forbidden Actions

> - ❌ **FA-001**: reservation.Cancel() を直接呼ぶ
>   - 理由: Position繰り上げが行われない
> - ❌ **FA-002**: CheckAndPromoteNextAsync() で代用する
>   - 理由: Position再インデックスが行われない

### Acceptance Criteria

- [ ] **CR-001** の正解経路に従っている
- [ ] **FA-001** を違反していない（Entity.Cancel()直接呼び出しなし）
- [ ] **FA-002** を違反していない（CheckAndPromoteNextAsync使用なし）
- [ ] **AC-002** のテストが通る（Position繰り上げ確認）
```

### 3.2 Guardrail-to-Task マッピングルール

| Guardrail ID | マッピング対象 |
|-------------|---------------|
| `FA-XXX` | 禁止事項に該当する操作を含むタスク |
| `CR-XXX` | 正解経路の操作を実装するタスク |
| `GR-XXX` | ビジネスルールを実装するタスク |
| `AC-XXX` | 受け入れ条件に関連するタスク |

### 3.3 マッピング検証

```
✅ すべての Guardrail が最低1つのタスクに紐付けられている
❌ Guardrail ID が紐付けられていないタスクがある → WARNING
❌ Guardrail が1つもタスクに紐付けられていない → ERROR
```

---

## 4. 実行タイミング

### 4.1 Plan フェーズ内での自動実行

`/speckit.plan` 実行時に、以下が自動実行される:

1. `guardrails.yaml` の読み込み
2. `canonical_routes` を Plan の Guardrails セクションに引用
3. `forbidden_actions` を Plan の制約として明記
4. `test_generation_hints` を Plan のテストセクションに反映

### 4.2 Tasks フェーズでの自動展開

`/speckit.tasks` 実行時に、以下が自動実行される:

1. `guardrails.yaml` の読み込み
2. 各タスクへの Guardrail ID 自動紐付け
3. Acceptance Criteria への Guardrail チェック項目追加

### 4.3 Implement フェーズでの参照

`/speckit.implement` 実行時に、以下が提示される:

1. `canonical_routes` を「この操作の正解経路」として表示
2. `negative_examples` を「やってはいけない例」として表示
3. 実装後の自己検証（`forbidden_actions` パターン検索）

---

## 5. CI/CD 統合

### 5.1 pre-commit hook

```yaml
# .pre-commit-config.yaml
repos:
  - repo: local
    hooks:
      - id: guardrail-check
        name: Guardrail Verification
        entry: pwsh -File verification/check-guardrails.ps1
        language: system
        types: [csharp]
        pass_filenames: false
```

### 5.2 GitHub Actions

```yaml
# .github/workflows/guardrail-check.yml
name: Guardrail Check

on: [push, pull_request]

jobs:
  check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Run Guardrail Verification
        run: |
          chmod +x verification/check-guardrails.sh
          ./verification/check-guardrails.sh src
```

---

## 6. 参照ファイル

| ファイル | 役割 |
|---------|------|
| `catalog/scaffolds/guardrails-template.yaml` | ガードレール定義テンプレート |
| `catalog/scaffolds/spec-test-template.yaml` | テスト生成テンプレート |
| `catalog/speckit-extensions/commands/speckit.guardrails.md` | ガードレール抽出コマンド |
| `catalog/speckit-extensions/commands/speckit.tasks.md` | タスク分解コマンド |
| `catalog/COMMON_MISTAKES.md` | 頻出ミス参照 |

---

## 変更履歴

| バージョン | 日付 | 変更内容 |
|-----------|------|---------|
| 1.0.0 | 2025-12-17 | 初版リリース |
