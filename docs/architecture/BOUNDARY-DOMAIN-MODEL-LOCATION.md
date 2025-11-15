# バウンダリーパターン - ドメインモデルの配置

## 📍 場所

バウンダリーのドメインモデルは以下に配置されています：

```
src/PurchaseManagement/Shared/Domain/PurchaseRequests/Boundaries/
```

---

## 🏗️ ディレクトリ構造

```
src/PurchaseManagement/
└── Shared/
    └── Domain/
        └── PurchaseRequests/
            └── Boundaries/              ← バウンダリードメインモデル
                ├── ApprovalAction.cs
                ├── ApprovalBoundaryService.cs
                ├── ApprovalContext.cs
                ├── ApprovalEligibility.cs
                ├── ApprovalIntent.cs
                ├── BoundaryDecision.cs
                ├── CompositeApprovalCommandFactory.cs
                ├── FilteringBoundaryService.cs
                ├── FilterOptions.cs
                ├── IApprovalBoundary.cs
                ├── IApprovalCommandFactory.cs
                ├── IFilteringBoundary.cs
                ├── IntentContext.cs
                ├── ISubmissionBoundary.cs
                ├── SubmissionBoundaryService.cs
                ├── SubmissionContext.cs
                ├── SubmissionEligibility.cs
                └── UIMetadata.cs
```

---

## 📦 ファイル分類

### 1. **承認バウンダリー（Approval Boundary）**

#### インターフェース
- **`IApprovalBoundary.cs`** - 承認バウンダリーのインターフェース
- **`IApprovalCommandFactory.cs`** - 承認コマンドファクトリーのインターフェース

#### ドメインサービス
- **`ApprovalBoundaryService.cs`** - 承認バウンダリーサービス（実装）
- **`CompositeApprovalCommandFactory.cs`** - 複数ファクトリーの統合

#### 値オブジェクト
- **`ApprovalAction.cs`** - 承認アクションの値オブジェクト（UIメタデータ含む）
- **`ApprovalContext.cs`** - 承認コンテキスト（画面表示用データ）
- **`ApprovalEligibility.cs`** - 承認資格の判定結果
- **`ApprovalIntent.cs`** - 承認意図（Intent-Command分離パターン）
- **`BoundaryDecision.cs`** - バウンダリー判定結果（型安全）
- **`IntentContext.cs`** - Intent一覧とメタデータ

#### UIポリシープッシュ
- **`UIMetadata.cs`** - UIメタデータ（RenderingHints, AccessibilityInfo等）

---

### 2. **提出バウンダリー（Submission Boundary）**

#### インターフェース
- **`ISubmissionBoundary.cs`** - 提出バウンダリーのインターフェース

#### ドメインサービス
- **`SubmissionBoundaryService.cs`** - 提出バウンダリーサービス（実装）

#### 値オブジェクト
- **`SubmissionContext.cs`** - 提出コンテキスト（画面表示用データ）
- **`SubmissionEligibility.cs`** - 提出資格の判定結果

---

### 3. **フィルタリングバウンダリー（Filtering Boundary）**

#### インターフェース
- **`IFilteringBoundary.cs`** - フィルタリングバウンダリーのインターフェース

#### ドメインサービス
- **`FilteringBoundaryService.cs`** - フィルタリングバウンダリーサービス（実装）

#### 値オブジェクト
- **`FilterOptions.cs`** - フィルタ・ソートオプション

---

## 🎯 なぜ `Shared/Domain` に配置されているか？

### 理由1: **ドメイン知識のカプセル化**

バウンダリーパターンは**ドメインロジックそのもの**です：
- 承認可否の判定ロジック
- ステータス遷移ルール
- UIポリシー（どの色、どのアイコンを使うか）

これらは純粋なビジネスルールであり、`Domain` 層に属します。

### 理由2: **複数の機能スライスで共有**

バウンダリーサービスは複数の機能から利用されます：

```
PurchaseManagement/
├── Features/
│   ├── ApprovePurchaseRequest/      ← ApprovalBoundary を使用
│   ├── RejectPurchaseRequest/       ← ApprovalBoundary を使用
│   ├── GetPurchaseRequestById/      ← ApprovalBoundary を使用
│   ├── SubmitPurchaseRequest/       ← SubmissionBoundary を使用
│   └── GetPurchaseRequests/         ← FilteringBoundary を使用
└── Shared/
    └── Domain/
        └── PurchaseRequests/
            └── Boundaries/          ← 共有ドメインロジック
```

### 理由3: **VSAの原則に準拠**

VSA（Vertical Slice Architecture）では：
- **機能固有のロジック** → `Features/{機能名}/`
- **共有ドメインロジック** → `Shared/Domain/`

バウンダリーは複数機能で共有されるため、`Shared/Domain/` が適切です。

---

## 🔍 各ファイルの責務

### ApprovalBoundaryService.cs
```csharp
// 場所: src/PurchaseManagement/Shared/Domain/PurchaseRequests/Boundaries/
public class ApprovalBoundaryService : IApprovalBoundary
{
    // 承認資格のチェック
    public ApprovalEligibility CheckEligibility(PurchaseRequest request, Guid userId);

    // 承認コンテキストの取得（UIメタデータ含む）
    public ApprovalContext GetContext(PurchaseRequest request, Guid userId);

    // Intent一覧の取得
    public IntentContext GetIntentContext(PurchaseRequest request, Guid userId);

    // IntentをCommandに変換
    public object CreateCommandFromIntent(ApprovalIntent intent, ...);
}
```

**責務:**
- ドメインルールに基づく承認可否の判定
- UIに必要な情報の提供（メタデータ含む）
- Intent-Command変換

---

### ApprovalAction.cs
```csharp
// 場所: src/PurchaseManagement/Shared/Domain/PurchaseRequests/Boundaries/
public sealed record ApprovalAction
{
    public ApprovalActionType Type { get; init; }      // Approve, Reject, Cancel
    public bool IsEnabled { get; init; }
    public string Label { get; init; }                 // "承認", "却下"
    public string Icon { get; init; }                  // "bi-check-circle"
    public string ColorTheme { get; init; }            // "btn-success"
    // ... UIメタデータ
}
```

**責務:**
- アクションの型安全な表現
- UIレンダリング情報の提供
- アクセシビリティ情報の保持

---

### BoundaryDecision.cs
```csharp
// 場所: src/PurchaseManagement/Shared/Domain/PurchaseRequests/Boundaries/
public sealed record BoundaryDecision
{
    public bool IsAllowed { get; init; }
    public IReadOnlyList<ApprovalAction> AllowedActions { get; init; }
    public IReadOnlyList<DomainError> BlockingReasons { get; init; }
    public DecisionContext Context { get; init; }
}
```

**責務:**
- バウンダリー判定結果の型安全な表現
- 許可/拒否の理由の構造化
- 可観測性のためのコンテキスト情報

---

### UIMetadata.cs
```csharp
// 場所: src/PurchaseManagement/Shared/Domain/PurchaseRequests/Boundaries/
public sealed record UIMetadata
{
    public RenderingHints Rendering { get; init; }      // CSS, Icon, EmphasisLevel
    public AccessibilityInfo Accessibility { get; init; } // ARIA属性
    public IReadOnlyList<InteractionHint> Interactions { get; init; }
    public LayoutHints? Layout { get; init; }
}
```

**責務:**
- ドメイン知識をUIレンダリング情報としてプッシュ
- アクセシビリティ情報の自動生成
- レイアウトヒントの提供

---

## 🏛️ アーキテクチャ上の位置づけ

### レイヤー構造

```
┌─────────────────────────────────────────┐
│  UI Layer (Blazor)                       │
│  - PurchaseRequestDetail.razor           │ ← UIメタデータを使用
└─────────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│  Application Layer (CQRS)                │
│  - ApprovePurchaseRequestHandler         │
│  - GetPurchaseRequestByIdHandler         │
└─────────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│  Domain Layer (Shared/Domain)            │ ← ★ バウンダリーはここ
│  - Boundaries/                           │
│    - ApprovalBoundaryService             │
│    - UIMetadata                          │
│    - BoundaryDecision                    │
└─────────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│  Domain Layer (Entities)                 │
│  - PurchaseRequest                       │
│  - ApprovalStep                          │
└─────────────────────────────────────────┘
```

### 依存関係

```
UI層
  ↓ 依存
Application層 (Features/)
  ↓ 依存
Shared/Domain (Boundaries/) ← バウンダリードメインモデル
  ↓ 依存
Domain層 (Entities)
```

---

## 📚 関連ドキュメント

- **バウンダリーパターンの詳細:** `docs/architecture/BOUNDARY-PATTERN.md` (要作成)
- **UIポリシープッシュ:** `docs/architecture/UI-POLICY-PUSH-DESIGNER-BENEFITS.md`
- **Intent-Command分離:** コミットメッセージ `feat: Intent-Command分離パターン実装`

---

## 🎓 まとめ

### バウンダリーのドメインモデルは:

✅ **場所:** `src/PurchaseManagement/Shared/Domain/PurchaseRequests/Boundaries/`

✅ **理由:**
1. ドメイン知識のカプセル化（ビジネスルール）
2. 複数機能スライスでの共有
3. VSAの原則に準拠

✅ **構成:**
- インターフェース（IApprovalBoundary等）
- ドメインサービス（ApprovalBoundaryService等）
- 値オブジェクト（ApprovalAction, BoundaryDecision等）
- UIポリシープッシュ（UIMetadata）

✅ **アーキテクチャ:**
- Domain層に属する
- Application層とUI層から利用される
- エンティティ層に依存する
