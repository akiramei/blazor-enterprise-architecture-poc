# AI / spec-kit 開発ワークフロー（プロジェクト共通）

このドキュメントは、各プロジェクトに `catalog/` をベンダリングして spec-kit で開発する際の
**運用ルールの最小セット**です。  
AI も人間も、このファイルと `catalog/` を前提に作業します。

> AI 実装詳細の正本は `catalog/` にあります。本ファイルは「使い方」と「ガードレール」の要約です。

---

## 1. 目的

- 要求仕様から **ドメインモデルとUIを創造的に設計**しつつ、  
  **アーキテクチャは catalog のベストプラクティスを再現**する。
- プロジェクト間で構造と実装スタイルを揃え、参加メンバーの学習コストと手戻りを最小化する。

---

## 2. 標準フロー（spec-kit）

1. `/spec-kit.specify`  
   - 要求仕様（自然言語）を渡す。  
   - AI は SPEC を作成し、Intent / Boundary / domain_rules を明文化する。
2. `/spec-kit.plan`  
   - `catalog/index.json` と `catalog/DECISION_FLOWCHART.md` に従い、  
     適用パターンと順序を決める（**UIがある場合は Boundary を最初に設計**）。
3. `/spec-kit.tasks`  
   - 計画をタスクに分解し、影響範囲と作成ファイルを明確化する。
4. `/spec-kit.implement`  
   - 生成対象パターン YAML の `implementation.template` を使って実装する。

> 途中で迷いが出たら止めて確認する（`catalog/AI_USAGE_GUIDE.md` のレッドフラグ参照）。

---

## 3. AI の創造性の範囲（Creative Boundary）

**創造してよい領域**
- Domain Model（Entity / VO / DomainService / DomainEvent / Boundary）
- UI（画面構成、入力/表示、ユーザー体験の工夫）
- ドメインルールの解釈・具体化

**創造してはいけない領域（固定）**
- アーキテクチャ選定（VSA / CQRS / Pipeline Behaviors / Result パターン）
- プロジェクト構造・配置ルール
- catalog に存在するパターンの独自実装

---

## 4. 実装前プリフライト（必須チェックリスト）

**新規機能 / UI を含む場合は特に厳守**

- [ ] `catalog/AI_USAGE_GUIDE.md` を読んだ
- [ ] `catalog/index.json` の ai_decision_matrix を確認した
- [ ] `catalog/COMMON_MISTAKES.md` を読んだ（NEVER DO を把握）
- [ ] UIがある場合 `catalog/patterns/boundary-pattern.yaml` を読んだ
- [ ] 適用パターン YAML の `implementation.template` と `wiring` を確認した
- [ ] evidence の実装例を参照した

**絶対禁止（抜粋）**
- Handler 内で `SaveChangesAsync()` を呼ばない（TransactionBehavior が担当）
- 例外で業務エラーを伝播しない（`Result<T>` を返す）
- BoundaryService に if 文で業務ロジックを書かない（Entity.CanXxx() に委譲）

---

## 5. ランタイム/依存のルール

- **catalog の runtime_requirements が最優先**。  
  例: `.NET 10 / net10.0 / パッケージ 10.x`。
- プロジェクト側が別バージョンの場合は **先に移行計画を立てて承認**し、  
  その後に段階的に揃える。

---

## 6. レッドフラグ（止まって確認）

- パターン YAML で表現できない要求が出た
- 複数パターンの候補が並び判断できない
- 既存コードと catalog の規約が矛盾している
- Boundary / Intent / CanXxx() の設計に自信がない

**迷ったら聞く。間違って進めるより安い。**

