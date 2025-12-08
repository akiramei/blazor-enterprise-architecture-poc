# Constitution Additions for Catalog-Driven Development

> **使い方**: このファイルの内容を `memory/constitution.md` に追記してください。

---

## Catalog-Driven Development (NON-NEGOTIABLE)

すべての実装はカタログのパターンを参照して行う。

- カタログに存在するパターンを独自実装してはならない
- `catalog/index.json` でパターンを検索し、該当パターンの YAML を読む
- パターンが存在しない場合のみ、新規実装を検討する
- カタログの変更が必要な場合は「提案」として説明し、直接変更しない

## Catalog Integration Rules

### Pattern Selection

1. `catalog/DECISION_FLOWCHART.md` でパターンカテゴリを特定
2. `catalog/index.json` の `ai_decision_matrix` でパターン ID を取得
3. 該当パターンの YAML を読み込む
4. `ai_guidance.common_mistakes` を確認して回避

### Plan Phase Requirements

Plan フェーズ（/speckit.plan）では以下を必須とする：

- **Guardrails セクション**：絶対に破ってはいけないビジネスルールを列挙
- Catalog Binding セクション：使用するパターン ID を列挙
- Creative Areas セクション：パターンでカバーできない創造的領域を明示
- Boundary セクション：UI がある場合は Intent と Entity.CanXxx() を設計
- **Attribute Enforcement Check**：spec の属性が data-model から落ちていないか確認
- **Query Semantics**：Query と Repository メソッドの対応を明示

## Guardrails（絶対遵守ルール）

### Guardrails とは

- 「絶対に破ってはいけないビジネスルール」
- 違反すると仕様違反となるもの
- FR 番号で追跡可能

### Guardrails の識別条件

spec を読む際に以下をチェック：

- 「〜のみ」「〜だけ」「〜の場合のみ」という文言
- 「優先」「先着」「順番」という文言
- 複数の条件を満たす必要がある操作
- 状態に依存する操作可否判定

### Guardrails の伝播ルール

1. **spec → plan**: Guardrails セクションとして抽出
2. **plan → tasks**: 各 Guardrail を最低1つのタスクに紐付け
3. **tasks → implement**: Acceptance Criteria に Guardrail チェックを含める

### 違反時の扱い

- Guardrail が紐付いていないタスクがある場合 → **エラー**
- Guardrail を満たさない実装 → **仕様違反**

## Validation Contract（検証契約）

ValidationService 実装時は以下を必須とする：

```yaml
validation_contract:
  service: "{ServiceName}"
  method: "{MethodName}"
  requires:
    - "{IRepository}.{Method}({params}) - {説明}"
  ensures:
    - "{条件} ({FR番号})"
```

- **requires**: 呼び出すべきリポジトリメソッド（省略禁止）
- **ensures**: 保証すべき条件（FR 番号付き）

## Query Semantics（クエリの意味）

Query 実装時は以下を明示：

- Query の意味（semantic）
- 使用すべき Repository メソッド
- 使用禁止の Repository メソッド

これにより、Query と Repository の混同を防ぐ。

## Spec/Plan Consistency (SSOT) - NON-NEGOTIABLE

Spec.md は仕様の唯一の真実の源泉（Single Source of Truth）である。
Plan.md は Spec を「どう実現するか」を決めるが、「何を実現するか」を変更してはならない。

### MUST

- Plan で追加した制約・デフォルト値は Spec の Assumptions に反映する
- Spec に定義された Enum 値・属性を Plan から落とさない
- 曖昧さ解消の決定は "Unknowns Resolved" セクションに記録する
- Optional 機能の実装判断は "Optional Rule Decisions" に記録する

### MUST NOT

- Spec に明記されていない制約を Plan で暗黙的に追加する（記録なしで）
- Spec の Enum 値を Plan で勝手に削除・変更する
- Spec の Enum 値を「要約」「解釈」して省略する
- 決定の理由を記録せずにデフォルト値を使用する

## Enum Value Enforcement (CRITICAL - 予防的チェック)

Spec の Enum 値は Plan の data-model に **完全一致** で反映されなければならない。

### MUST

- Spec の Key Entities から全ての Status/Enum 定義を抽出する
- Plan の data-model に全ての Enum 値を含める
- Enum Value Enforcement Check テーブルを出力する
- 欠落がある場合は ERROR で停止する

### チェックフォーマット

```markdown
| Entity | Enum | Spec Values | Plan Values | Status |
|--------|------|-------------|-------------|:------:|
| Reservation | ReservationStatus | Waiting, Ready, Completed, Cancelled | Waiting, Ready, Completed, Cancelled | ✅ OK |
```

### Rationale

属性の存在チェックだけでは Enum の「値」の欠落を検出できない。
Fulfill() メソッドがあるのに Completed 状態がない、という矛盾を防止する。
詳細は `spec-plan-consistency.md` および `decision-guide.md` を参照。

## Decision Documentation

曖昧な仕様に対する意思決定は必ず記録する。

### Unknowns Resolved フォーマット

```markdown
| 項目 | Spec の状態 | 決定 | 理由 | Spec 反映 |
|------|------------|------|------|:---------:|
| 予約上限 | 明記なし | 3件 | 図書館業界の標準 | Y |
```

### Optional Rule Decisions フォーマット

```markdown
| Rule | Spec Location | キーワード | 決定 | 理由 |
|------|---------------|-----------|------|------|
| 延滞ブロック | L132 | optional | MVP 見送り | 明示的に無効化 |
```

## Expiration Rule（期限付き状態のルール） - NON-NEGOTIABLE

期限付きの状態（ExpiresAt プロパティを持つ状態）には、必ず期限処理のオーケストレーションが存在しなければならない。

### 背景

図書館ドッグフーディングで発見された問題：
- Ready 状態に ExpiresAt を設定
- IsExpired() メソッドも実装
- **しかし、どこからも呼ばれていない**
- 結果：期限切れの予約が永遠に Ready のまま、キューが詰まる

### MUST

期限付き状態には、以下のいずれかのトリガーを用意する：

| トリガー方式 | 説明 | ユースケース |
|-------------|------|-------------|
| 関連操作時チェック | 返却時、貸出時などにチェック | リアルタイム性が低くてよい場合 |
| バックグラウンドジョブ | 定期的にチェック（1時間ごと等） | 期限切れを確実に検知したい場合 |
| コマンド実行時チェック | 各 Command 実行前にチェック | 即座に反映が必要な場合 |

### 実装パターン

```csharp
// ❌ NG: IsExpired() を定義するだけ
public bool IsExpired()
{
    return Status == ReservationStatus.Ready
        && ExpiresAt.HasValue
        && DateTime.UtcNow > ExpiresAt.Value;
}

// ✅ OK: 期限切れ処理を呼び出すトリガーがある
// 方式1: 関連操作時
public async Task<Result<Unit>> Handle(ReturnBookCommand request, ...)
{
    // 返却処理...

    // 期限切れチェック
    await _queueService.ExpireAndPromoteIfNeededAsync(bookId, ct);
}

// 方式2: バックグラウンドジョブ
public class ReservationExpirationJob : IHostedService
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var expired = await _repository.GetExpiredReadyReservationsAsync(ct);
        foreach (var r in expired)
        {
            r.Expire();
            await _queueService.PromoteNextAsync(r.BookId, ct);
        }
    }
}
```

### チェックリスト

Plan フェーズで ExpiresAt を定義した場合：

- [ ] 期限切れをチェックするトリガーが設計されているか？
- [ ] 期限切れ時のアクション（Cancel, Expire, PromoteNext等）が定義されているか？
- [ ] IsExpired() を呼ぶコードが存在するか？

### 違反時の扱い

- ExpiresAt を定義したのにトリガーがない → **Plan 不完全（ERROR）**
- IsExpired() がどこからも呼ばれていない → **実装不完全（ERROR）**

## Technology Stack (Catalog Patterns)

このカタログで使用する技術スタック：

- .NET 8 / C# 12
- Blazor Server (InteractiveServer)
- Entity Framework Core
- MediatR (CQRS)
- FluentValidation
- Result<T> Pattern for error handling
- Pipeline Behaviors for cross-cutting concerns
