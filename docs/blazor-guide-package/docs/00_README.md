# Blazor Enterprise Architecture Guide
## 中規模業務アプリケーションのための決定版アーキテクチャ

**Version**: 2.1.1 (修正版)  
**Target**: Blazor Server / Blazor WebAssembly (Hosted)  
**Team Size**: 5-20 developers  
**Project Scale**: Medium to Large Enterprise Applications

**v2.1.1 Hotfix** (2025-10-22):
- **[CRITICAL]** 型定義の修正: `ICommand<r>` → `ICommand<r>` (6箇所)
- **[CRITICAL]** 型定義の修正: `Task<r>` → `Task<r>` (1箇所)
- **[FIX]** 文字化けコメントの修正: 「更新」の表記を正常化 (2箇所)
- **[FIX]** 円記号の文字化け修正: Money.ToDisplayString (1箇所)
- 詳細は `CHANGELOG.md` を参照

**v2.1 Updates** (2025-10-22):
- **[P0]** CachingBehaviorの順序規約とキー安全性の明文化(08章)
- **[P0]** Idempotency-Keyのエンドツーエンド伝播パターン(08章)
- **[P1]** Store single-flight パターン追加(07章)
- **[P1]** SignalR通知のコアレス&デバウンス実装(07章)
- **[P1]** Query最適化チェックリストと画面専用DTO徹底(12章)
- **[P2]** CorrelationIdによる観測可能性の実装(12章)
- **[P2]** Blazor Server運用ガイド(安全策集約)(14章)

**v2.0 Updates** (2025-10):
- Transactionスコープの厳密化とPipeline登録の最適化
- Store並行制御パターンの強化(バージョニング + 差分判定)
- PageActionsコーディング規約の明文化
- Outbox Dispatcher の信頼性向上(Dead Letter対応)
- Authorization二重化戦略の追加
- Read最適化とキャッシュ無効化戦略の詳細化

---

## 📋 目次

### 各章へのリンク

1. **[イントロダクション](01_イントロダクション.md)** (5.2 KB)
   - このアーキテクチャが解決する課題
   - 対象読者と前提知識
   - ドキュメントの読み方

2. **[アーキテクチャ概要](02_アーキテクチャ概要.md)** (3.6 KB)
   - 設計原則
   - アーキテクチャの全体像
   - 主要な設計判断

3. **[採用技術とパターン](03_採用技術とパターン.md)** (11 KB)
   - 技術スタック
   - 採用パターン一覧
   - パターンの組み合わせ

4. **[全体アーキテクチャ図](04_全体アーキテクチャ図.md)** (13 KB)
   - システム全体図
   - データフロー
   - 責務分離

5. **[レイヤー構成と責務](05_レイヤー構成と責務.md)** (5.6 KB)
   - 4層アーキテクチャ
   - 各層の責務
   - 依存関係のルール

6. **[具体例: 商品管理機能](06_具体例_商品管理機能.md)** (16 KB)
   - 実装例による理解
   - コード例
   - ベストプラクティス

7. **[UI層の詳細設計](07_UI層の詳細設計.md)** (19 KB)
   - Blazor Componentの設計
   - Store パターン
   - PageActions パターン

8. **[Application層の詳細設計](08_Application層の詳細設計.md)** (16 KB)
   - Command/Query分離
   - Transaction管理
   - Authorization

9. **[Domain層の詳細設計](09_Domain層の詳細設計.md)** (13 KB)
   - Entityとvalue Object
   - Domain Service
   - Domain Event

10. **[Infrastructure層の詳細設計](10_Infrastructure層の詳細設計.md)** (17 KB)
    - Repository実装
    - データアクセス
    - 外部サービス連携

11. **[信頼性パターン](11_信頼性パターン.md)** (17 KB)
    - Outbox Pattern
    - リトライ戦略
    - エラーハンドリング

12. **[パフォーマンス最適化](12_パフォーマンス最適化.md)** (3.9 KB)
    - キャッシュ戦略
    - クエリ最適化
    - レンダリング最適化

13. **[テスト戦略](13_テスト戦略.md)** (7.3 KB)
    - Unit Test
    - Integration Test
    - E2E Test

14. **[ベストプラクティス](14_ベストプラクティス.md)** (3.0 KB)
    - コーディング規約
    - チーム開発のヒント
    - よくある落とし穴

15. **[まとめ](15_まとめ.md)** (45 KB)
    - アーキテクチャの振り返り
    - 今後の発展
    - 補足資料

16. **[3層アーキテクチャからの移行ガイド](16_3層アーキテクチャからの移行ガイド.md)** (NEW - 30 KB)
    - WPF/WinForms経験者向け
    - ServiceクラスとMediatRの比較
    - ViewModelとStore/Actionsの比較
    - 段階的な学習パス

17. **[横断的関心事の詳細設計](../../architecture/cross-cutting-concerns.md)** (24 KB)
    - AppContext - 統合コンテキスト
    - 監査ログ（Audit Log）
    - メトリクス収集（OpenTelemetry）
    - Pipeline Behaviors全体像

---

## 📦 ファイル構成

```
blazor-architecture-guide/
├── 00_README.md                      (このファイル)
├── 01_イントロダクション.md
├── 02_アーキテクチャ概要.md
├── 03_採用技術とパターン.md
├── 04_全体アーキテクチャ図.md
├── 05_レイヤー構成と責務.md
├── 06_具体例_商品管理機能.md
├── 07_UI層の詳細設計.md
├── 08_Application層の詳細設計.md
├── 09_Domain層の詳細設計.md
├── 10_Infrastructure層の詳細設計.md
├── 11_信頼性パターン.md
├── 12_パフォーマンス最適化.md
├── 13_テスト戦略.md
├── 14_ベストプラクティス.md
├── 15_まとめ.md
├── 16_3層アーキテクチャからの移行ガイド.md  (NEW)
└── ../../architecture/
    └── cross-cutting-concerns.md     (17章: 横断的関心事)
```

## 🚀 推奨される読み方

### 🎯 3層アーキテクチャ経験者（推奨）
**WPF/WinForms + RESTful Web API 経験者向けの最短パス**
1. **[3層アーキテクチャからの移行ガイド](16_3層アーキテクチャからの移行ガイド.md)** ← まずはここから！
2. [イントロダクション](01_イントロダクション.md) - 1.4節の段階的な学習パス参照
3. [具体例: 商品管理機能](06_具体例_商品管理機能.md) で実装パターンを確認

### 初めて読む方
1. [イントロダクション](01_イントロダクション.md) → [アーキテクチャ概要](02_アーキテクチャ概要.md)
2. [全体アーキテクチャ図](04_全体アーキテクチャ図.md) で全体像を把握
3. [具体例: 商品管理機能](06_具体例_商品管理機能.md) で実装イメージを理解
4. 各層の詳細設計(7-10章)を順番に読む

### 特定の課題を解決したい方
- **3層アーキテクチャから移行** → [移行ガイド](16_3層アーキテクチャからの移行ガイド.md)
- **状態管理に悩んでいる** → [UI層の詳細設計](07_UI層の詳細設計.md)
- **トランザクション管理** → [Application層の詳細設計](08_Application層の詳細設計.md)
- **エラーハンドリング** → [信頼性パターン](11_信頼性パターン.md)
- **パフォーマンス改善** → [パフォーマンス最適化](12_パフォーマンス最適化.md)
- **ログ・監査・メトリクス** → [横断的関心事の詳細設計](../../architecture/cross-cutting-concerns.md)

### 実装を始める方
1. [レイヤー構成と責務](05_レイヤー構成と責務.md) で基本構造を理解
2. [具体例: 商品管理機能](06_具体例_商品管理機能.md) をテンプレートとして利用
3. [ベストプラクティス](14_ベストプラクティス.md) を参照しながら実装

---

## 📝 注意事項

このドキュメントは、中規模(5-20人)のチームで開発する業務アプリケーションを想定しています。
小規模プロジェクトや大規模エンタープライズでは、一部のパターンを簡略化または強化する必要があります。

---

**完全版ドキュメント**: [blazor-architecture-guide-complete-fixed.md](../blazor-architecture-guide-complete-fixed.md)
