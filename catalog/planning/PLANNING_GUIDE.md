# Planning Guide - 計画フェーズの進め方

このドキュメントは、AIがカタログを使って**計画フェーズ**を進める際のガイドラインです。

> **重要**: 計画フェーズと実装フェーズは明確に分離してください。
> 計画が承認されるまで、コードの生成を開始してはいけません。

---

## 計画フェーズの目的

計画フェーズでは、以下を決定します：

1. **どのパターンを適用するか**（パターン選択）
2. **どの順序で適用するか**（適用順序）
3. **どのファイルを作成/変更するか**（影響範囲）
4. **どの非機能要件パターンを含めるか**（運用考慮）

---

## 計画フェーズのフロー

```
┌─────────────────────────────────────────────────────────────┐
│ Step 1: 要求分析                                            │
│   └── ユーザー要求からキーワードを抽出                       │
│   └── ドメインを特定（図書館、EC、金融など）                 │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 2: 機能パターン選択                                    │
│   └── ai_decision_matrix でカテゴリを特定                   │
│   └── 該当する feature-* パターンを選択                     │
│   └── domain_hints で追加パターンを特定                     │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 3: 非機能要件パターン選択                              │
│   └── nonfunctional_pattern_hints でドメイン別推奨を確認    │
│   └── 運用要件に基づいてパターンを選択                      │
│   └── 適用しない場合は理由を記録                            │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 4: パターン組み合わせ・依存解決                        │
│   └── 選択したパターンの依存関係を確認                      │
│   └── 適用順序を決定                                        │
│   └── 影響範囲を評価                                        │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 5: 計画書作成                                          │
│   └── spec-template.yaml のフォーマットで作成               │
│   └── 適用パターン一覧を含める                              │
│   └── ファイル構成案を含める                                │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 6: ユーザー承認                                        │
│   └── 計画をユーザーに提示                                  │
│   └── 承認を得てから実装フェーズへ                          │
└─────────────────────────────────────────────────────────────┘
```

---

## Step 1: 要求分析

### やること

1. ユーザーの要求を読み、**キーワード**を抽出する
2. **ドメイン**を特定する（下表参照）

### ドメイン特定表

| キーワード | ドメイン | 参照先 |
|-----------|---------|--------|
| 図書館、貸出、返却、蔵書、会員 | 図書館・貸出管理 | nonfunctional_pattern_hints |
| 決済、支払い、口座、残高、振込 | 金融・決済 | nonfunctional_pattern_hints |
| 患者、診療、処方、カルテ | 医療・ヘルスケア | nonfunctional_pattern_hints |
| 商品、在庫、注文、カート | EC・在庫管理 | nonfunctional_pattern_hints |
| 予約、会議室、シフト、スケジュール | 予約・スケジュール系 | domain_hints |
| 承認、稟議、申請、ワークフロー | 承認・ワークフロー系 | domain_hints |
| マスタ、カタログ、顧客 | マスタ・カタログ系 | domain_hints |

### 出力

```markdown
## 要求分析

### ユーザー要求
「図書館貸出管理システムを作りたい」

### 抽出キーワード
- 図書館、貸出、返却、蔵書、会員

### 特定されたドメイン
- 図書館・貸出管理
- 予約・スケジュール系（貸出期間の管理）
```

---

## Step 2: 機能パターン選択

### やること

1. `catalog/index.json` の `ai_decision_matrix` を参照
2. 要求に合致する**機能パターン**を選択
3. `domain_hints` で**追加パターン**を特定

### 参照すべきファイル

| ファイル | 目的 |
|---------|------|
| `catalog/index.json` | ai_decision_matrix、domain_hints |
| 各パターンの YAML | ai_selection_hints で確認 |

### 出力

```markdown
## 機能パターン選択

### 主要パターン（CRUD機能）
| パターン | 目的 | 適用対象 |
|---------|------|---------|
| feature-create-entity | 新規作成 | Book, Member, Loan |
| feature-search-entity | 検索・一覧 | Book, Member, Loan |
| feature-update-entity | 更新 | Book, Member |
| feature-delete-entity | 削除 | Book, Member |

### ドメイン固有パターン（domain_hints より）
| パターン | 理由 |
|---------|------|
| domain-typed-id | BookId, MemberId, LoanId の型安全性 |
| domain-validation-service | 貸出可否チェック（在庫、重複） |
| domain-state-machine | 貸出状態の遷移管理 |
| boundary-pattern | 貸出/返却の操作可否判定 |
```

---

## Step 3: 非機能要件パターン選択

### やること

1. `catalog/index.json` の `nonfunctional_pattern_hints` を参照
2. 特定されたドメインの**推奨パターン**を確認
3. 適用する/しないを決定し、**理由を記録**

### 参照すべきファイル

| ファイル | 目的 |
|---------|------|
| `catalog/index.json` | nonfunctional_pattern_hints.domains |
| `catalog/COMMON_MISTAKES.md` | 実装後チェックリスト（ドメイン別推奨） |

### 出力

```markdown
## 非機能要件パターン選択

### ドメイン: 図書館・貸出管理

| パターン | 推奨度 | 適用 | 理由 |
|---------|-------|------|------|
| logging-behavior | high | ✅ 適用 | 運用トラブルシューティングに必須 |
| audit-log-behavior | high | ✅ 適用 | 貸出履歴の監査証跡（誰が何をいつ借りたか） |
| concurrency-control | medium | ✅ 適用 | 同一書籍の同時貸出競合防止 |
| caching-behavior | low | ❌ 不適用 | 書籍情報は頻繁に変更されないが、初期リリースでは不要 |

### 適用しないパターンの理由

| パターン | 理由 |
|---------|------|
| authorization-behavior | 初期リリースでは認証なし（内部ツール想定） |
| idempotency-behavior | 貸出操作は状態で制御されるため不要 |
```

---

## Step 4: パターン組み合わせ・依存解決

### やること

1. 選択したパターンの**依存関係**を確認
2. **適用順序**を決定
3. **影響範囲**（作成/変更するファイル）を評価

### 依存関係の確認方法

各パターンYAMLの `dependencies.patterns` を確認：

```yaml
# feature-create-entity.yaml
dependencies:
  patterns:
    - validation-behavior
    - transaction-behavior
    - idempotency-behavior
```

### 適用順序の原則

```
1. Kernel層（基盤）
   └── Result, ValueObject, Entity

2. Domain層
   └── TypedId, Entity, ValueObject
   └── StateMachine, ValidationService
   └── Boundary（CanXxx メソッド）

3. Infrastructure層（Behaviors）
   └── ValidationBehavior, TransactionBehavior
   └── LoggingBehavior, AuditLogBehavior

4. Application層
   └── Command, Handler, Validator

5. UI層
   └── Store, PageActions, Component
```

### 出力

```markdown
## パターン組み合わせ・依存解決

### 適用順序

| 順序 | レイヤー | パターン | 依存先 |
|:---:|---------|---------|--------|
| 1 | Kernel | result-pattern | なし |
| 2 | Kernel | value-object | なし |
| 3 | Kernel | entity-base | value-object |
| 4 | Domain | domain-typed-id | value-object |
| 5 | Domain | domain-state-machine | entity-base |
| 6 | Domain | boundary-pattern | entity-base |
| 7 | Infrastructure | validation-behavior | なし |
| 8 | Infrastructure | transaction-behavior | なし |
| 9 | Infrastructure | logging-behavior | なし |
| 10 | Infrastructure | audit-log-behavior | なし |
| 11 | Application | feature-create-entity | validation, transaction |
| ... | ... | ... | ... |

### 影響範囲（作成するファイル）

```
src/
├── Kernel/                          # 既存（変更なし）
├── Domain/LibraryManagement/
│   ├── Books/
│   │   ├── BookId.cs               # 新規
│   │   ├── Book.cs                 # 新規
│   │   └── BookStatus.cs           # 新規
│   ├── Members/
│   │   ├── MemberId.cs             # 新規
│   │   └── Member.cs               # 新規
│   ├── Loans/
│   │   ├── LoanId.cs               # 新規
│   │   ├── Loan.cs                 # 新規
│   │   └── LoanStatus.cs           # 新規
│   └── Boundaries/
│       └── LoanBoundaryService.cs  # 新規
├── Application/
│   ├── Features/
│   │   ├── CreateBook/             # 新規（一式）
│   │   ├── BorrowBook/             # 新規（一式）
│   │   └── ReturnBook/             # 新規（一式）
│   └── Infrastructure/
│       └── LibraryManagement/      # 新規
└── ...
```
```

---

## Step 5: 計画書作成

### やること

1. `catalog/scaffolds/spec-template.yaml` のフォーマットを使用
2. 上記の分析結果をまとめる
3. **Boundaryセクション**を必ず含める

### 計画書テンプレート

```markdown
# 機能計画書: {機能名}

## 1. 概要
- 目的: {この機能が解決する課題}
- スコープ: {含む/含まない}

## 2. 要求分析
- ユーザー要求: {元の要求}
- 抽出キーワード: {キーワード}
- 特定ドメイン: {ドメイン}

## 3. Boundary設計（★必須）
- Intent: {ユーザーの意図}
- Entity.CanXxx(): {操作可否判定}
- BoundaryService: {委譲のみ}

## 4. 適用パターン

### 機能パターン
| パターン | 目的 | 適用対象 |
|---------|------|---------|

### ドメインパターン
| パターン | 理由 |
|---------|------|

### 非機能要件パターン
| パターン | 適用 | 理由 |
|---------|------|------|

## 5. 適用順序
| 順序 | パターン | 依存先 |
|:---:|---------|--------|

## 6. 影響範囲
{ファイル構成}

## 7. 実装順序
1. {最初に実装するもの}
2. {次に実装するもの}
...
```

---

## Step 6: ユーザー承認

### やること

1. 計画書をユーザーに提示
2. 以下の確認を求める：
   - パターン選択は適切か
   - 非機能要件パターンの取捨選択は妥当か
   - 影響範囲は想定通りか
3. 承認を得てから実装フェーズへ

### 承認確認のテンプレート

```markdown
## 計画の確認

上記の計画について確認させてください：

1. **適用パターン**: {n}個の機能パターン、{m}個の非機能要件パターンを適用します
2. **作成ファイル数**: 約{x}ファイル
3. **適用しないパターン**: {パターン名}（理由: {理由}）

この計画で実装を進めてよろしいですか？
```

---

## 計画フェーズのチェックリスト

計画を完了する前に確認：

```
□ 要求分析
  □ キーワードを抽出した
  □ ドメインを特定した

□ パターン選択
  □ ai_decision_matrix を参照した
  □ domain_hints を参照した
  □ nonfunctional_pattern_hints を参照した

□ Boundary設計（UIがある場合）
  □ boundary-pattern.yaml を読んだ
  □ Intent を列挙した
  □ Entity.CanXxx() を設計した

□ パターン組み合わせ
  □ 依存関係を確認した
  □ 適用順序を決定した
  □ 影響範囲を評価した

□ 計画書
  □ spec-template のフォーマットで作成した
  □ 適用パターン一覧を含めた
  □ 非機能要件パターンの取捨選択理由を記載した

□ 承認
  □ ユーザーに計画を提示した
  □ 承認を得た
```

---

## よくある計画ミス

| ミス | 解決策 |
|-----|--------|
| パターン選択をスキップして実装を始める | 必ず計画フェーズを完了してから実装 |
| Boundaryセクションを省略する | UIがある場合は必須 |
| 非機能要件パターンを考慮しない | nonfunctional_pattern_hints を必ず確認 |
| 依存関係を無視して適用順序を決める | 各パターンの dependencies を確認 |
| 計画書なしで実装を始める | spec-template で計画書を作成 |

---

## 関連ドキュメント

- [IMPLEMENTATION_GUIDE.md](../implementation/IMPLEMENTATION_GUIDE.md) - 実装フェーズの進め方
- [spec-template.yaml](../scaffolds/spec-template.yaml) - 仕様書テンプレート
- [COMMON_MISTAKES.md](../COMMON_MISTAKES.md) - よくあるミス
- [pattern-combinations.yaml](pattern-combinations.yaml) - パターン組み合わせ例

---

**最終更新**: 2025-11-27
**カタログバージョン**: v2025.11.27.1
