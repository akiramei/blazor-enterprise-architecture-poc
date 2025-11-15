# 境界コンテキスト（BC）間の整合性ガイドライン

## 📋 概要

本ドキュメントは、複数の境界コンテキスト（BC）間でドメイン表現やバウンダリーパターンの整合性を保つためのガイドラインです。

---

## 🎯 目的

### なぜ整合性が必要か

1. **AI支援の効率化**
   - Claude Codeなどのコード生成AIが、BC間でパターンを認識しやすくなる
   - 一貫性のあるコードベースでAIの提案精度が向上

2. **開発者の認知負荷軽減**
   - BC間を移動してもパターンが同じなら、学習コストが低い
   - 新規参加者のオンボーディングが容易

3. **保守性の向上**
   - BC横断の変更（例: セキュリティ修正）を一貫して適用できる
   - リファクタリングのパターンが明確

---

## 🏗️ BC間で統一すべきパターン

### 1. **バウンダリーパターン**

#### ディレクトリ構造の統一

```
src/{BC名}/
└── Shared/
    └── Domain/
        └── {集約名}/
            └── Boundaries/          ← 必ずこの構造
                ├── I{目的}Boundary.cs
                ├── {目的}BoundaryService.cs
                ├── {目的}Context.cs
                ├── {目的}Eligibility.cs
                └── UIMetadata.cs    ← UI Policy Push用
```

**例:**

```
✅ PurchaseManagement/Shared/Domain/PurchaseRequests/Boundaries/
   ├── IApprovalBoundary.cs
   ├── ApprovalBoundaryService.cs
   ├── ApprovalContext.cs
   └── ApprovalEligibility.cs

✅ ProductCatalog/Shared/Domain/Products/Boundaries/
   ├── IPricingBoundary.cs
   ├── PricingBoundaryService.cs
   ├── PricingContext.cs
   └── PricingEligibility.cs
```

#### インターフェース命名規則

```csharp
// パターン: I{目的}Boundary
public interface IApprovalBoundary { }      // 承認
public interface IPricingBoundary { }       // 価格設定
public interface IInventoryBoundary { }     // 在庫管理
```

#### メソッドシグネチャの統一

```csharp
// 全BCで統一
public interface I{目的}Boundary
{
    // 1. 資格チェック（必須）
    {目的}Eligibility CheckEligibility(
        {集約}Entity entity,
        Guid currentUserId
    );

    // 2. コンテキスト取得（必須）
    {目的}Context GetContext(
        {集約}Entity entity,
        Guid currentUserId
    );

    // 3. Intent一覧（オプション - Intent-Command分離パターン採用時）
    IntentContext GetIntentContext(
        {集約}Entity entity,
        Guid currentUserId
    );
}
```

---

### 2. **値オブジェクトの命名規則**

#### Context（コンテキスト）

```csharp
// パターン: {目的}Context
public record ApprovalContext      // 承認コンテキスト
{
    public required {集約} Entity { get; init; }
    public BoundaryDecision Decision { get; init; }
    public UIMetadata? UIMetadata { get; init; }
    public StatusDisplayInfo StatusDisplay { get; init; }
}
```

#### Eligibility（資格）

```csharp
// パターン: {目的}Eligibility
public record ApprovalEligibility
{
    public bool CanPerform { get; init; }        // 統一: CanPerform
    public IReadOnlyList<DomainError> BlockingReasons { get; init; }

    public static ApprovalEligibility Eligible(...);
    public static ApprovalEligibility NotEligible(...);
}
```

#### BoundaryDecision（判定結果）

```csharp
// 全BCで同じ構造を使用
public sealed record BoundaryDecision
{
    public bool IsAllowed { get; init; }
    public IReadOnlyList<{目的}Action> AllowedActions { get; init; }
    public IReadOnlyList<DomainError> BlockingReasons { get; init; }
    public DecisionContext Context { get; init; }
}
```

---

### 3. **UIポリシープッシュパターン**

#### UIMetadataの構造統一

```csharp
// 全BCで同じ構造
public sealed record UIMetadata
{
    public required RenderingHints Rendering { get; init; }
    public required AccessibilityInfo Accessibility { get; init; }
    public IReadOnlyList<InteractionHint> Interactions { get; init; }
    public LayoutHints? Layout { get; init; }
}

// ファクトリメソッドのパターン統一
public static UIMetadata For{エンティティ}Status({エンティティ}Status status);
public static UIMetadata For{サブエンティティ}({サブエンティティ}Status status);
```

**例:**

```csharp
// PurchaseManagement BC
UIMetadata.ForRequestStatus(PurchaseRequestStatus.Approved);
UIMetadata.ForApprovalStep(ApprovalStepStatus.Pending);

// ProductCatalog BC
UIMetadata.ForProductStatus(ProductStatus.Active);
UIMetadata.ForPriceChangeStatus(PriceChangeStatus.Pending);
```

---

### 4. **Intent-Command分離パターン**

#### Intent列挙型の命名

```csharp
// パターン: {目的}Intent
public enum ApprovalIntent        // 承認意図
{
    PerformFirstApproval,
    PerformSecondApproval,
    SendBackForRevision,
    RejectPermanently
}

public enum PricingIntent         // 価格設定意図
{
    ApplyDiscountPrice,
    RevertToOriginalPrice,
    SchedulePriceChange
}
```

#### IntentContextの構造統一

```csharp
// 全BCで同じ構造
public record IntentContext
{
    public {集約}Entity? Request { get; init; }
    public AvailableIntent[] AvailableIntents { get; init; }
    public Guid CurrentUserId { get; init; }

    public bool HasAvailableIntents =>
        AvailableIntents.Any(i => i.IsEnabled);

    public bool CanExecute({目的}Intent intent) =>
        AvailableIntents.Any(i => i.Intent == intent && i.IsEnabled);
}
```

---

### 5. **可観測性（Observability）**

#### 構造化ログのパターン統一

```csharp
// 全BCで統一されたログパターン
public class {目的}BoundaryService
{
    private readonly ILogger<{目的}BoundaryService> _logger;

    public {目的}Eligibility CheckEligibility(...)
    {
        if (!isEligible)
        {
            _logger.LogWarning(
                "CheckEligibility denied: {Reason}. " +
                "EntityId={EntityId}, UserId={UserId}, Status={Status}",
                reason, entityId, userId, status
            );
        }
        else
        {
            _logger.LogInformation(
                "CheckEligibility approved. " +
                "EntityId={EntityId}, UserId={UserId}",
                entityId, userId
            );
        }
    }

    public {目的}Context GetContext(...)
    {
        _logger.LogInformation(
            "GetContext completed. " +
            "EntityId={EntityId}, UserId={UserId}, Status={Status}, " +
            "IsAllowed={IsAllowed}, AllowedActionsCount={AllowedActionsCount}",
            entityId, userId, status, decision.IsAllowed, decision.AllowedActions.Count
        );
    }
}
```

---

## 🚧 BC境界の物理的な保護

### 1. **プロジェクト参照ルール**

#### 禁止される参照

```xml
<!-- ❌ BC間の直接参照は禁止 -->
<ProjectReference Include="..\PurchaseManagement\Shared\Domain\..." />
```

#### 許可される参照

```xml
<!-- ✅ BC内の参照はOK -->
<ProjectReference Include="..\..\..\Shared\Domain\..." />

<!-- ✅ 全BC共通の参照はOK -->
<ProjectReference Include="..\..\..\..\..\Shared\Kernel\..." />
```

### 2. **アーキテクチャテストによる保護**

```csharp
// tests/ArchitectureTests/BCBoundaryTests.cs
[Fact]
public void PurchaseManagement_ShouldNotReference_ProductCatalog()
{
    // Arrange
    var purchaseManagementAssembly = typeof(PurchaseRequest).Assembly;

    // Act
    var references = purchaseManagementAssembly.GetReferencedAssemblies();

    // Assert
    references.Should().NotContain(r =>
        r.Name.Contains("ProductCatalog"),
        "PurchaseManagement BCはProductCatalog BCを参照してはいけない"
    );
}

[Fact]
public void ProductCatalog_ShouldNotReference_PurchaseManagement()
{
    // Arrange
    var productCatalogAssembly = typeof(Product).Assembly;

    // Act
    var references = productCatalogAssembly.GetReferencedAssemblies();

    // Assert
    references.Should().NotContain(r =>
        r.Name.Contains("PurchaseManagement"),
        "ProductCatalog BCはPurchaseManagement BCを参照してはいけない"
    );
}
```

### 3. **ディレクトリ構造の可視化**

```
src/
├── PurchaseManagement/          ← BC境界
│   ├── Features/
│   └── Shared/
│       └── Domain/              ← BC内共有（外部参照禁止）
│
├── ProductCatalog/              ← BC境界
│   ├── Features/
│   └── Shared/
│       └── Domain/              ← BC内共有（外部参照禁止）
│
└── Shared/                      ← 全BC共通（参照OK）
    └── Kernel/
```

---

## 📐 ドメイン責務の境界線

### UIメタデータはどこまでドメインの責務か？

#### ✅ ドメインの責務（Domain/Boundaries/）

| 情報 | 理由 | 例 |
|------|------|---|
| **意味論的な色** | ビジネス上の意味を表現 | "success" (成功), "danger" (危険) |
| **意味論的なアイコン** | 状態の意味を表現 | "check-circle" (承認), "x-circle" (却下) |
| **強調レベル** | ビジネス上の重要度 | EmphasisLevel.High (要対応) |
| **確認要否** | 操作のリスクレベル | RequiresConfirmation = true |
| **ARIA属性** | アクセシビリティはドメイン要件 | Role="alert" (重要通知) |

**コード例:**

```csharp
// ✅ ドメイン層で定義
public static UIMetadata ForRequestStatus(PurchaseRequestStatus status)
{
    return status switch
    {
        PurchaseRequestStatus.Approved => new UIMetadata
        {
            Rendering = new RenderingHints
            {
                BadgeColorClass = "bg-success",  // 意味論（成功）
                IconClass = "bi-check-circle",   // 意味論（チェック）
                EmphasisLevel = EmphasisLevel.Low
            },
            // ...
        }
    };
}
```

#### ❌ UI層の責務（UI/Styles/, UI/Components/）

| 情報 | 理由 | 例 |
|------|------|---|
| **具体的な色コード** | デザインシステムの詳細 | #28a745 (緑の具体的な色) |
| **アニメーション** | UXの演出 | transition: 0.3s ease |
| **レイアウト** | 画面構成 | grid-template-columns: 1fr 2fr |
| **フォント** | タイポグラフィ | font-family: 'Arial' |

**コード例:**

```css
/* ✅ UI層で定義 */
.bg-success {
    background-color: #28a745;  /* 具体的な色 */
    transition: all 0.3s ease;  /* アニメーション */
}
```

### 境界線の判断フロー

```
この情報はビジネスルールか？
  ├─ YES → ドメイン層（Boundaries/UIMetadata）
  │        例: "重要なのでHighレベル"
  │            "危険なので確認ダイアログ必要"
  │
  └─ NO → この情報はビジュアルデザインか？
           ├─ YES → UI層（CSS, Component）
           │        例: "緑色は#28a745"
           │            "0.3秒でフェードイン"
           │
           └─ 判断できない → デフォルト: ドメイン層
                              （後でUI層に移動可能）
```

---

## 📝 仕様変更時の対応ガイド

### ケース1: ステータスの色変更

**要求:** 「承認済みのバッジを緑から青に変更したい」

#### ❌ 間違った対応
```csharp
// ドメイン層を変更（不要）
BadgeColorClass = "bg-primary"  // success → primary
```

#### ✅ 正しい対応
```css
/* UI層のCSSのみ変更 */
.bg-success {
    background-color: #007bff;  /* 緑 → 青 */
}
```

**理由:** 「成功=緑」はデザインシステムの問題。ドメインは「成功」という意味論のみを扱う。

---

### ケース2: 新しい強調レベルの追加

**要求:** 「緊急対応が必要な状態を追加したい」

#### ✅ 正しい対応

```csharp
// 1. ドメイン層で列挙型を拡張
public enum EmphasisLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3  // ← 追加
}

// 2. UIメタデータで使用
public static UIMetadata ForRequestStatus(PurchaseRequestStatus status)
{
    return status switch
    {
        PurchaseRequestStatus.OverdueApproval => new UIMetadata
        {
            Rendering = new RenderingHints
            {
                EmphasisLevel = EmphasisLevel.Critical  // ← 新レベル使用
            }
        }
    };
}
```

```css
/* 3. UI層でスタイル定義 */
.emphasis-critical {
    background-color: #ff0000;
    animation: blink 1s infinite;
}
```

**理由:** 強調レベルはビジネス上の重要度 → ドメイン層の責務

---

### ケース3: アイコンセットの変更

**要求:** 「Bootstrap IconsからFont Awesomeに変更したい」

#### ✅ 正しい対応（段階的移行）

```csharp
// 1. ドメイン層で抽象的な名前に変更（オプション）
public sealed record RenderingHints
{
    public string IconClass { get; init; }  // "check-circle" (抽象名)
}

public static UIMetadata ForRequestStatus(PurchaseRequestStatus status)
{
    IconClass = "check-circle"  // Bootstrap固有の "bi-" プレフィックスを削除
}
```

```razor
<!-- 2. UI層でマッピング -->
@{
    var iconClass = uiMeta.Rendering.IconClass;
    var fontAwesomeIcon = iconClass switch
    {
        "check-circle" => "fa-check-circle",
        "x-circle" => "fa-times-circle",
        _ => iconClass
    };
}
<i class="fas @fontAwesomeIcon"></i>
```

**理由:** アイコンセットの選択はデザインシステムの問題。ドメインは「チェックアイコン」という意味のみ。

---

## 🔄 継続的な改善プロセス

### 1. **定期的なレビュー**

#### 四半期ごとのパターンレビュー

```markdown
# BC間整合性レビュー（Q1 2025）

## レビュー項目
- [ ] 新しいBCは既存パターンに準拠しているか？
- [ ] バウンダリーの命名規則は統一されているか？
- [ ] UIメタデータの責務分担は適切か？
- [ ] アーキテクチャテストは通っているか？

## 改善事項
- ProductCatalog BCにUIMetadataを追加
- 全BCで構造化ログのフォーマットを統一
```

### 2. **パターンカタログの維持**

#### docs/architecture/PATTERN-CATALOG.md

```markdown
# バウンダリーパターンカタログ

## 実装済みBC

### PurchaseManagement
- ApprovalBoundary
- SubmissionBoundary
- FilteringBoundary

### ProductCatalog
- PricingBoundary
- InventoryBoundary

## 共通パターン
- Intent-Command分離: ✅ 全BCで採用
- UI Policy Push: ✅ 全BCで採用
- 構造化ログ: ✅ 全BCで採用
```

### 3. **AI支援の最適化**

#### .claude/commands/new-bc.md

```markdown
新しい境界コンテキストを作成する際は、以下のガイドラインに従ってください：

1. **ディレクトリ構造**
   - src/{BC名}/Features/
   - src/{BC名}/Shared/Domain/{集約名}/Boundaries/

2. **必須ファイル**
   - I{目的}Boundary.cs
   - {目的}BoundaryService.cs
   - {目的}Context.cs
   - {目的}Eligibility.cs
   - UIMetadata.cs

3. **参照**
   - PurchaseManagement/Shared/Domain/PurchaseRequests/Boundaries/ を参考実装として使用
```

---

## 📊 チェックリスト

### 新しいBC追加時

- [ ] ディレクトリ構造が統一パターンに準拠
- [ ] インターフェース命名が `I{目的}Boundary`
- [ ] メソッドシグネチャが統一パターンに準拠
- [ ] UIMetadataを実装
- [ ] 構造化ログを実装
- [ ] アーキテクチャテストを追加
- [ ] 他BCからの参照がないことを確認

### 既存BC修正時

- [ ] 変更が他BCにも適用すべきか検討
- [ ] パターンカタログを更新
- [ ] 全BCで同様の問題がないか確認

---

## 🎓 まとめ

### 整合性の3原則

1. **パターンの統一**
   - BC間で同じパターンを使う
   - 新規開発者やAIが迷わない

2. **境界の明示**
   - BC内Sharedは物理的に隔離
   - アーキテクチャテストで保護

3. **責務の明確化**
   - ドメイン = ビジネスルール + 意味論
   - UI = ビジュアルデザイン + レイアウト

### 継続的改善

> BC間の整合性は一度作って終わりではなく、
> 新しいBCの追加や既存BCの進化に合わせて、
> 継続的にガイドラインを改善していくプロセスです。
