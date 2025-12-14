# Catalog Backlog - 未解決の課題と改善提案

このファイルは、ドッグフーディングで発見されたカタログの課題を追跡します。

---

## 優先度: High

### BACKLOG-001: layer-infrastructure - DB設定パターンの明確化

**出典**: Library8 ドッグフーディング (UI強化10.md, UI強化11.md)

**問題**:
- DB接続設定のパターンが不明確
- SQLite/SQL Serverの選択基準がない
- プロバイダーと接続文字列の整合性チェックが自動化されていない

**現状の対応**:
- `catalog/checklists/infrastructure-setup.yaml` に CHK-DB-001 を追加
- `catalog/troubleshooting/blazor-runtime-errors.yaml` に ERR-DB-001 を追加

**追加で必要な改善**:
```yaml
# catalog/patterns/layer-infrastructure.yaml に追加すべき内容
db_provider_selection:
  criteria:
    - "開発環境のみ・軽量": SQLite
    - "本番環境・スケーラブル": SQL Server / PostgreSQL

  connection_string_templates:
    sqlite: "Data Source={app_name}.db"
    sqlserver: "Server=...;Database=...;Trusted_Connection=True"
    postgres: "Host=...;Database=...;Username=...;Password=..."
```

**ステータス**: 部分対応（チェックリスト追加済み、パターン未整備）

---

### BACKLOG-002: layer-ui - ホスティング責務とUI責務の分離

**出典**: Library8 ドッグフーディング (UI強化11.md)

**問題**:
- UIプロジェクトが「ホスティング」と「プレゼンテーション」の2つの責務を持つ
- `dotnet run --project src/UI` でUIがエントリーポイントになることの違和感
- UIは交換可能であるべき（Blazor → MAUI、Console等）

**現状の構造**:
```
src/UI/
├── Program.cs          ← ホスティング設定（DI, DB, ミドルウェア）
├── Components/         ← UI表示責務
└── Features/           ← 画面実装
```

**推奨構造案**:
```
src/
├── Host.Web/           ← エントリーポイント（Program.cs）
│   └── Program.cs      ← DI, ミドルウェア, DB設定
├── UI.Blazor/          ← Blazorコンポーネントのみ（RCL）
└── ...
```

**追加で必要な改善**:
- `catalog/scaffolds/project-structure.yaml` の更新
- Host/UI 分離のガイドライン追加
- プロジェクト名の命名規約（UI → WebApp or Host）

**現状の対応**:
- `catalog/scaffolds/project-structure.yaml` を更新（Host/UI分離オプションを追加）
- `catalog/layers/layer-ui-hosting-separation.yaml` を追加（ガイドライン/依存関係ルール）
- `catalog/index.json` にパターン登録（layer-ui-hosting-separation）
- **コード改修完了** (2025-12-14):
  - `src/Application` を3プロジェクトに分割
  - `src/Host.Web/` - ホスティング（Program/DI/ミドルウェア/API/Hub）
  - `src/UI.Blazor/` - Razor/UI（Components + Features/*.razor + Store/PageActions）
  - `src/Application.Features/` - Command/Query/Handler/Validator + DTO/IF
  - ルーティング分離: `RouteAssemblyRegistry.cs` で UI アセンブリを管理
  - 実行: `dotnet run --project src/Host.Web/Host.Web.csproj`

**ステータス**: 完了（カタログ整備 + コード改修）

---

### BACKLOG-003: Blazor固有パターン - MudBlazor型制約

**出典**: Library8 ドッグフーディング (UI強化11.md)

**問題**:
- MudBlazorの型制約（T=必須）が未カタログ化
- ジェネリックコンポーネントの型パラメータ不一致エラーが頻発
- Nullable型とNon-nullable型の混在による問題

**現状の対応**:
- `catalog/troubleshooting/blazor-runtime-errors.yaml` に ERR-MUD-003 を追加

**追加で必要な改善**:
```yaml
# catalog/patterns/mudblazor-components.yaml (新規)
generic_component_rules:
  - rule: "親と子のジェネリック型を一致させる"
    example: |
      <MudSelect T="Status?">
        <MudSelectItem T="Status?" Value="...">
      </MudSelect>

  - rule: "Nullable型を使う場合は全コンポーネントに適用"
    example: |
      @bind-Value="_status" where _status is Status?
```

**ステータス**: 部分対応（エラーパターン追加済み、パターン未整備）

---

## 優先度: Medium

### BACKLOG-004: EF + DDD ミスマッチの体系的対応

**出典**: Library8 ドッグフーディング (UI強化11.md)

**問題**:
- DomainEvent が EF のナビゲーションプロパティとして検出される
- DDD パターンと EF Core の相性問題が体系化されていない

**現状の対応**:
- `catalog/troubleshooting/blazor-runtime-errors.yaml` に ERR-EF-001 を追加

**追加で必要な改善**:
- `catalog/patterns/ef-ddd-integration.yaml` の作成
- OnModelCreating のスキャフォールドにIgnore設定を含める
- Entity基底クラスの設計ガイドライン

**ステータス**: 部分対応

---

### BACKLOG-005: Unit型の命名規約

**出典**: Library8 ドッグフーディング (UI強化11.md)

**問題**:
- MediatR.Unit と独自の Unit 型が衝突
- Result.Fail<Unit> でコンパイルエラー

**現状の対応**:
- `catalog/troubleshooting/blazor-runtime-errors.yaml` に ERR-TYPE-001 を追加

**推奨対応**:
- `catalog/kernel/result-pattern.yaml` で UnitResult を正式名称とする
- 既存プロジェクトの Unit → UnitResult 移行ガイド

**ステータス**: 部分対応

---

### BACKLOG-006: validation_contract の必須化

**出典**: Library8 ドッグフーディング (UI強化11.md - ERR-EF-002)

**問題**:
- Handler が使用する Repository メソッドが事前に定義されていない
- 設計と実装の不整合が実装時に発覚

**現状の対応**:
- `speckit.tasks.md` で ValidationService タスクに validation_contract を推奨

**追加で必要な改善**:
- すべての Handler タスクに requires セクションを必須化
- plan フェーズで Repository インターフェースを事前設計

**ステータス**: 部分対応

---

## 優先度: Low

### BACKLOG-007: UI-IR 自動生成ツール

**出典**: Library8 ドッグフーディング (UI強化11.md)

**問題**:
- UI-IR は概念として存在するが、実際の YAML ファイルが生成されない
- 手動で UI-IR を作成する手間

**現状の対応**:
- `speckit.tasks.md` に UI-IR-to-Task Mapping を追加
- `speckit.implement.md` に UI-IR Gate を追加

**追加で必要な改善**:
- UI-IR YAML を自動生成するスクリプト/ツール
- Boundary セクションから UI-IR への変換ロジック

**ステータス**: ゲート追加済み、自動生成ツール未整備

---

## 完了済み

### [完了] インフラチェックリスト
- `catalog/checklists/infrastructure-setup.yaml` 作成済み

### [完了] エラーパターンカタログ
- `catalog/troubleshooting/blazor-runtime-errors.yaml` 作成済み

### [完了] UI-IR ワークフロー統合
- `speckit.tasks.md`, `speckit.implement.md` 更新済み

### [完了] プロセスゲート定義
- `INTEGRATION_WITH_SPEC.md` に Process Gates セクション追加済み

---

## 更新履歴

| 日付 | 変更内容 |
|------|---------|
| 2025-12-14 | 初版作成（Library8 ドッグフーディングフィードバック反映） |
