# Common Mistakes Summary - VSA 実装ミス要約

このファイルは `catalog/COMMON_MISTAKES.md` の要約版です。
詳細は元ファイルを参照してください。

---

## 計画フェーズでの Boundary モデリング忘却

**UIがある機能を計画する際、Boundary モデリングを忘れる問題が頻発。**

### 計画が不完全とみなされる条件

| 条件 | 判定 |
|-----|------|
| UI があるのに Boundary セクションがない | ❌ 不完全 |
| Intent（ユーザーの意図）が定義されていない | ❌ 不完全 |
| Entity.CanXxx() の設計がない | ❌ 不完全 |

### 必須確認事項

```
□ boundary-pattern.yaml を読んだか？
□ Boundary セクションを定義したか？
□ Intent を列挙したか？
□ Entity.CanXxx() を設計したか？
```

---

## 絶対禁止事項（NEVER DO）

| No | 禁止事項 | 理由 |
|:--:|---------|------|
| 1 | Handler 内で SaveChangesAsync() | TransactionBehavior が自動実行 |
| 2 | Singleton で DbContext 注入 | Captive Dependency 問題 |
| 3 | HandleAsync メソッド名 | MediatR 規約は Handle |
| 4 | throw で例外伝播 | Result<T> を使用 |
| 5 | BoundaryService に業務ロジック | Entity.CanXxx() に委譲 |
| 6 | Validator で DB アクセス | 形式検証のみ |
| 7 | Value Object の .Value 比較 | インスタンス同士で比較 |

---

## BoundaryService の責務

### やること
- Repository からデータ取得
- Entity.CanXxx() の呼び出し
- 存在チェック（null 判定）のみ

### やらないこと
- 業務ルールの判定（if 文で状態チェック）
- ドメインロジックの実装

### Entity.CanXxx() テンプレート

```csharp
public BoundaryDecision CanPay()
{
    return Status switch
    {
        OrderStatus.Pending => BoundaryDecision.Allow(),
        OrderStatus.Paid => BoundaryDecision.Deny("既に支払い済みです"),
        _ => BoundaryDecision.Deny("この状態では支払いできません")
    };
}
```

---

## FluentValidation の範囲

| 検証内容 | 配置場所 |
|---------|---------|
| 入力値の形式（空文字、長さ、範囲） | ValidationBehavior |
| データの存在確認 | Handler 内 |
| ビジネスルール（重複チェック等） | ドメインサービス |

---

## ケアレスミス集

| カテゴリ | 問題 | 対策 |
|---------|------|------|
| 型の衝突 | MediatR.Unit vs 独自 Unit | using エイリアス |
| 引数順序 | Query/Command の引数誤り | 名前付き引数を使用 |
| プロパティ名 | DTO 名と Entity 名の不一致 | DTO 定義を確認 |
| namespace | using 文の不足 | IDE 補完を活用 |

---

## ドッグフーディングで発見されたミス

### 1. クエリの条件分岐ミス（コピペバグ）

```csharp
// ❌ バグ: 両分岐が同じメソッド
if (request.ActiveOnly == true)
    loans = await _repo.GetOverdueLoansAsync(ct);
else
    loans = await _repo.GetOverdueLoansAsync(ct);  // 同じ！
```

**対策**: コピペ後は必ずメソッド名を確認

### 2. 複合前提条件の検証漏れ（FR-017）

仕様: 「予約は全コピー貸出中の場合のみ可能」

**対策**: ValidationService で全前提条件を列挙

### 3. 優先権のある操作可否判定の漏れ（FR-021）

仕様: 「Ready状態の予約者に優先権」

**対策**: CanXxx() に優先権エンティティを渡す

### 4. 順序付きキュー（Position）の実装漏れ（FR-018）

仕様: 「予約は先着順（Position）で管理」

**対策**: Entity に Position フィールドを追加

---

## 実装前チェックリスト

```
□ catalog/index.json を読んだか？
□ 該当パターンの YAML を読んだか？
□ Handler 内で SaveChangesAsync を呼んでいないか？
□ すべてのサービスは Scoped で登録しているか？
□ Value Object はインスタンス同士で比較しているか？
□ 操作可否判定は Boundary 経由か？
□ 複数エンティティをまたぐ検証はドメインサービスに委譲しているか？
□ FluentValidation は DB アクセスを伴わない形式検証のみか？
```

---

## 参照

完全版: `catalog/COMMON_MISTAKES.md`
