# カタログ改善計画

**作成日**: 2025-11-25
**目的**: カタログ駆動開発の品質を 7.2/10 → 9.0/10 に向上

---

## 実施項目一覧

| # | 優先度 | 項目 | 作業内容 | 成果物 |
|:-:|:------:|------|---------|--------|
| 1 | 高 | CachingBehavior カタログ化 | 既存実装からYAML作成 | `catalog/patterns/caching-behavior.yaml` |
| 2 | 高 | パス定義統一 | テンプレート内パスを修正 | 複数YAML修正 |
| 3 | 中 | Query-by-ID パターン追加 | GetProductById を参照に作成 | `catalog/patterns/query-get-by-id.yaml` |
| 4 | 中 | 認証系パターン追加 | Login/2FA を参照に作成 | `catalog/features/feature-authentication.yaml` |
| 5 | 中 | テストファイルevidence更新 | "未実装"→実際のパス or 削除 | 複数YAML修正 |
| 6 | 低 | Decision Matrix 拡張 | 新インテント追加 | `catalog/index.json` 更新 |
| 7 | 低 | UI/Infrastructure パターン拡充 | 不足パターン追加 | 複数YAML追加 |

---

## 1. CachingBehavior カタログ化（高優先度）

### 現状
- 実装: `src/Shared/Infrastructure/Behaviors/CachingBehavior.cs` ✅ 存在
- カタログ: ❌ 未作成
- 他パターンから参照されている（`query-get-list.yaml` 等の `works_with`）

### 作業内容
1. `catalog/patterns/caching-behavior.yaml` を新規作成
2. `catalog/index.json` の `patterns` 配列に追加

### テンプレート構造
```yaml
id: caching-behavior
version: 1.0.0
name: CachingBehavior
category: pipeline-behavior
order_hint: 350  # Transaction(400) の前
intent: Query結果をキャッシュし、パフォーマンスを向上
```

### 関連インターフェース
- `ICacheableQuery` - キャッシュ対象のマーカーインターフェース
- `GetCacheKey()` - キャッシュキー生成メソッド
- `CacheDurationMinutes` - キャッシュ有効期間

---

## 2. テンプレートのパス定義統一（高優先度）

### 現状
対象ファイル（`{BoundedContext}/Shared/` 形式が残存）:
- `catalog/features/feature-create-entity.yaml`

### 正しいパス形式
| パス種別 | 旧形式 | 新形式 |
|---------|-------|-------|
| BC固有Store | `src/{BoundedContext}/Shared/UI/Store/` | `src/Application/Infrastructure/{BoundedContext}/UI/Store/` |
| BC固有Actions | `src/{BoundedContext}/Shared/UI/Actions/` | `src/Application/Infrastructure/{BoundedContext}/UI/Actions/` |
| Feature UI | `Features/{Feature}/UI/{Feature}.razor` | `Features/{Feature}/{Feature}.razor` |

### 作業内容
1. 全YAMLファイルをスキャン
2. 旧形式パスを新形式に置換

---

## 3. Query-by-ID パターン追加（中優先度）

### 現状
- 実装: `GetProductById`, `GetPurchaseRequestById` ✅ 存在
- カタログ: ❌ `query-get-by-id.yaml` 未作成

### 作業内容
1. `catalog/patterns/query-get-by-id.yaml` を新規作成
2. `catalog/index.json` に追加

### 参照実装
```
src/Application/Features/GetProductById/
├── GetProductByIdQuery.cs
├── GetProductByIdQueryHandler.cs
└── ProductDetail.razor
```

---

## 4. 認証系パターン追加（中優先度）

### 現状
実装されているが未カタログ化の機能:
- `Login` - ログイン認証
- `RefreshToken` - トークン更新
- `Enable2FA` / `Disable2FA` / `Verify2FA` - 二要素認証
- `Account` - アカウント管理

### 作業内容
1. `catalog/features/feature-authentication.yaml` を新規作成
2. 認証フロー全体を1パターンとして定義

### 参照実装
```
src/Application/Features/Account/
├── Login/
├── RefreshToken/
├── Enable2FA/
├── Disable2FA/
└── Verify2FA/
```

---

## 5. テストファイルevidence更新（中優先度）

### 現状
以下のパターンで `test_file: "未実装"` が設定されている:
- `validation-behavior.yaml`
- `command-create.yaml`
- `transaction-behavior.yaml`
- `metrics-behavior.yaml`
- `logging-behavior.yaml`
- `authorization-behavior.yaml`
- `idempotency-behavior.yaml`
- `feature-create-entity.yaml`

### 選択肢
A. テストを実装し、パスを更新
B. `test_file` を削除（evidenceとして不要）
C. `test_file: null` として明示的に未実装を示す

### 推奨: 選択肢 B または C
- テスト実装は別タスクとして分離
- 現時点では evidence から除外 or null

---

## 6. Decision Matrix 拡張（低優先度）

### 現状のインテント（6個）
1. 新機能追加
2. 重複チェック・競合チェック
3. 空き検索・複合検索
4. システム全体の機能追加
5. 既存ファイルの修正
6. パターン理解・学習

### 追加候補インテント
| インテント | カテゴリ | トリガーキーワード |
|-----------|---------|------------------|
| 認証・認可の実装 | feature-slice | ログイン, 認証, 2FA, OAuth |
| バルク操作 | feature-slice | 一括, バルク, 大量, まとめて |
| ダッシュボード・集計 | query-pattern | 集計, 統計, レポート, グラフ |
| キャッシュ最適化 | pipeline-behavior | キャッシュ, 高速化, 負荷軽減 |

---

## 7. UI/Infrastructure パターン拡充（低優先度）

### 現状
- `ui-pattern`: 2個（realtime-notification, undo-redo）
- `infrastructure-pattern`: 1個（concurrency-control）

### 追加候補
| パターン名 | カテゴリ | 参照実装 |
|-----------|---------|---------|
| Form Validation Pattern | ui-pattern | Blazor EditForm連携 |
| Error Handling Pattern | ui-pattern | エラー表示・リトライ |
| Repository Pattern | infrastructure-pattern | EfProductRepository |

---

## 実施順序

```
Phase 1: 高優先度（構造の整合性）
├── 1. CachingBehavior カタログ化
└── 2. パス定義統一

Phase 2: 中優先度（カバレッジ向上）
├── 3. Query-by-ID パターン追加
├── 4. 認証系パターン追加
└── 5. テストファイルevidence更新

Phase 3: 低優先度（利便性向上）
├── 6. Decision Matrix 拡張
└── 7. UI/Infrastructure パターン拡充

Phase 4: 検証
└── 検証スクリプト実行・品質確認
```

---

## 完了条件

- [ ] 検証スクリプト: Evidence Found >= 85, Missing = 0
- [ ] 全 Pipeline Behavior がカタログ化されている
- [ ] テンプレート内のパス定義が実装と一致
- [ ] Decision Matrix が主要ユースケースをカバー
