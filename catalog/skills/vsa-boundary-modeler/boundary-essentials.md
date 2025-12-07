# Boundary Essentials - Boundary パターンの要点

このファイルは `catalog/patterns/boundary-pattern.yaml` の要点をまとめたものです。

---

## Robustness Analysis との対応

| 分析モデル | 実装先 | 責務 |
|-----------|--------|------|
| Boundary | IBoundary (Domain) | 画面契約の定義 |
| Boundary | BoundaryService | データ取得・ViewModel 構築 |
| **Control** | **Entity.CanXxx()** | **業務ルール判定**（状態ベース） |
| Control | Domain Service | 複数集約にまたがる判定 |
| Entity | Entity | 状態と振る舞い |

**重要**: Control の業務ロジックは Entity または Domain Service に実装する。
BoundaryService は Control ではない。

---

## 責務分離

| 判定内容 | 配置場所 | 理由 |
|---------|---------|------|
| 「重要タグは赤で表示」 | UI 層（見た目） | プレゼンテーション |
| 「WIP 制限でカード追加不可」 | Domain 層（Boundary） | ビジネスルール |
| 「承認権限がない」 | Domain 層（Boundary） | ビジネスルール |
| 「下書き状態なら編集可能」 | Domain 層（Boundary） | ビジネスルール |

---

## ファイル構造

```
src/Domain/{BoundedContext}/{Aggregate}/
├── {Aggregate}.cs                    # 集約ルート ★CanXxx() を含む
└── Boundaries/
    ├── {Aggregate}Intent.cs          # 操作の意図（enum）
    ├── I{Aggregate}Boundary.cs       # Boundary インターフェース
    ├── BoundaryDecision.cs           # 判定結果
    └── {Aggregate}BoundaryService.cs # Boundary 実装（委譲のみ）
```

---

## トリガーフレーズ

以下のフレーズが出たら Boundary パターンを検討：

- 「操作可否」「〇〇できるか」
- 「ボタン活性」「ボタン非活性」
- 「権限チェック」
- 「CanCreate」「CanUpdate」「CanDelete」
- 「優先権」「Ready 状態の人のみ」

---

## AI が忘却しやすいポイント

1. **Boundary セクションの欠落**
   - 計画フェーズで Boundary を設計しない
   - 「後から追加すればいい」と判断する

2. **BoundaryService への業務ロジック配置**
   - if 文で状態をチェックする
   - Entity.CanXxx() に委譲しない

3. **優先権の考慮漏れ**
   - Ready 状態の予約者を考慮しない
   - CanXxx() に優先権エンティティを渡さない

---

## 関連パターン

| パターン | 関係 |
|---------|------|
| domain-state-machine | 状態遷移の判定に使用 |
| feature-approval-workflow | 承認権限の判定 |
| domain-validation-service | 複数エンティティをまたぐ検証 |
