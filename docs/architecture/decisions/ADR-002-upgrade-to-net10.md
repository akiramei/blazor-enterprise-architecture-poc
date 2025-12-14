# ADR-002: ランタイムを .NET 10 / net10.0 に統一する

## ステータス

提案中（Proposed）

## コンテキスト

カタログ `catalog/index.json` の `runtime_requirements` では、  
**ターゲットフレームワーク net10.0 / SDK 10.0.100+ / パッケージ 10.x** を必須としています。

一方、このサンプル実装（VSASample）は現状以下の状態です：

- `src/Host.Web/Host.Web.csproj`（旧 `src/Application/Application.csproj`）が `net8.0`
- EF Core / Blazor / Identity など主要パッケージが 8.x
- これは過去のサンプル互換のために残っていると推測され、  
  カタログ駆動開発の「再現性」要件と齟齬があります。

## 決定事項

本リポジトリおよび今後ベンダリングされる各プロジェクトは、  
**段階的に .NET 10 / net10.0 / 依存 10.x に移行し、catalog と一致させる。**

## 決定理由

1. **再現性の担保**: catalog のテンプレート/パターンは net10 前提で検証されている。
2. **プロジェクト横断の統一**: 参加メンバーが同じ構造・同じ前提で作業できる。
3. **将来互換性**: LTS の基準点を揃えることで移行コストを下げる。

## 移行方針（高レベル）

> 具体作業は feature 追加とは別に計画フェーズを設ける。

1. **現状棚卸し**
   - `TargetFramework` / `PackageReference` の一覧化
   - BC 別 DbContext / TransactionBehavior / テストの依存確認
2. **SDK/TFM 更新**
   - `.sln` 全プロジェクトの `TargetFramework` を `net10.0` に統一
3. **パッケージ更新**
   - `Microsoft.*`, `EntityFrameworkCore.*`, `AspNetCore.Components.*`, `Identity.*` を 10.x に更新
   - 互換のない API を差分修正
4. **ビルド/テスト**
   - `dotnet build`, `dotnet test`
   - サンプルアプリの動作確認
5. **ドキュメント更新**
   - README / ガイドの TF/SDK 記載を net10 に合わせる

## 影響範囲

- 全プロジェクト（`src/Host.Web`, `src/UI.Blazor`, `src/Application.Features`, `src/Domain/*`, `src/Shared/*`, `src/Kernel`）
- EF Core / Identity / Hangfire / Serilog / SignalR など主要依存
- テストプロジェクト（`tests/`）の SDK/依存

## リスクと対策

- **破壊的変更**: パッケージ更新に伴う API 差分。  
  → 最初に CI 相当のビルド/テストを通す小刻みなPRで対応。
- **移行と機能追加の競合**  
  → 移行フェーズを別タスクで凍結期間を設ける。

## 関連リソース

- `catalog/AI_USAGE_GUIDE.md`（runtime_requirements）
- `catalog/index.json`

## 変更履歴

| 日付 | 変更内容 | 作成者 |
|------|---------|--------|
| 2025-12-12 | 初版作成 | Codex CLI |

## レビュー

- [ ] アーキテクト承認
- [ ] テックリード承認
- [ ] チームレビュー完了
