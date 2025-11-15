# VSAとドメインモデルの独立性

## 🎯 結論

**VSAとドメインモデルは分離されており、ドメインモデルはVSAから独立している。**

---

## 📐 現在の正しい構造

```
src/PurchaseManagement/
├── Features/                        ← ★ VSA（垂直スライス）
│   ├── ApprovePurchaseRequest/
│   │   └── Application/             ← アプリケーション層のみ
│   │       └── ApprovePurchaseRequestHandler.cs
│   ├── GetPurchaseRequestById/
│   │   ├── Application/
│   │   └── UI/
│   └── SubmitPurchaseRequest/
│       ├── Application/
│       └── UI/
│
└── Shared/                          ← ★ ドメインモデル（VSAの外）
    └── Domain/
        └── PurchaseRequests/
            ├── PurchaseRequest.cs   ← エンティティ（独立）
            ├── ApprovalStep.cs      ← エンティティ（独立）
            └── Boundaries/          ← ドメインサービス（独立）
                ├── ApprovalBoundaryService.cs
                └── UIMetadata.cs
```

**重要なポイント:**
- ✅ ドメインモデルは `Features/` の**外**に配置
- ✅ `Features/` 内には**アプリケーション層とUI層のみ**
- ✅ ドメインモデルはVSAの構造に影響を受けない

---

## 🏛️ アーキテクチャの原則

### VSAの本質

```
VSA = 「アプリケーション層を機能ごとに垂直に切る」アーキテクチャ
     ≠ 「ドメインモデルを垂直に切る」アーキテクチャ
```

### レイヤー構造

```
┌─────────────────────────────────────────┐
│  UI Layer                                │
│  ├── Features/GetPurchaseRequestById/UI/ │ ← VSAで分離
│  └── Features/SubmitPurchaseRequest/UI/  │
└─────────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│  Application Layer                       │
│  ├── Features/ApprovePurchaseRequest/    │ ← VSAで分離
│  ├── Features/GetPurchaseRequestById/    │
│  └── Features/SubmitPurchaseRequest/     │
└─────────────────────────────────────────┘
                  ↓ 依存（一方向）
┌─────────────────────────────────────────┐
│  Domain Layer                            │
│  └── Shared/Domain/PurchaseRequests/     │ ← VSAの影響を受けない
│      ├── PurchaseRequest.cs              │
│      └── Boundaries/                     │
└─────────────────────────────────────────┘
```

**依存方向:**
- アプリケーション層（Features/） → ドメイン層（Shared/Domain/）
- ドメイン層はアプリケーション層を知らない（独立）

---

## ❌ 誤った設計：ドメインモデルがVSAに従属

もし以下のような構造だったら、**ドメインモデルの独立性に反している**：

```
❌ 間違った構造

src/PurchaseManagement/Features/
├── ApprovePurchaseRequest/
│   ├── Domain/                      ← ❌ ドメインモデルがスライス内
│   │   └── PurchaseRequest.cs       ← ❌ VSAに従属
│   └── Application/
│       └── ApprovePurchaseRequestHandler.cs
│
└── GetPurchaseRequestById/
    ├── Domain/                      ← ❌ 同じエンティティが重複？
    │   └── PurchaseRequest.cs       ← ❌ DRY原則違反
    └── Application/
        └── GetPurchaseRequestByIdHandler.cs
```

### 問題点

1. **❌ ドメインモデルがVSAに従属**
   - ドメインモデルがスライス（機能）の構造に影響されている
   - ビジネスルールが技術的構造に依存している

2. **❌ DRY原則違反**
   - `PurchaseRequest` エンティティが複数のスライスで重複
   - 同じドメインロジックが複数箇所に存在

3. **❌ ドメインの独立性が失われる**
   - ドメインモデルが特定の機能（スライス）に紐付く
   - ビジネスルールの変更がVSA構造に影響を与える

---

## ✅ 正しい設計：ドメインモデルはVSAから独立

### 構造

```
✅ 正しい構造

src/PurchaseManagement/
├── Shared/                          ← ★ ドメインモデル（独立）
│   └── Domain/
│       └── PurchaseRequests/
│           ├── PurchaseRequest.cs   ← エンティティ（1つだけ）
│           ├── ApprovalStep.cs
│           └── Boundaries/
│               ├── ApprovalBoundaryService.cs
│               └── UIMetadata.cs
│
└── Features/                        ← ★ VSA（アプリケーション層のみ）
    ├── ApprovePurchaseRequest/
    │   └── Application/
    │       └── ApprovePurchaseRequestHandler.cs
    │
    └── GetPurchaseRequestById/
        ├── Application/
        │   └── GetPurchaseRequestByIdHandler.cs
        └── UI/
            └── PurchaseRequestDetail.razor
```

### 利点

1. **✅ ドメインモデルがVSAから独立**
   - `PurchaseRequest` エンティティはVSAの構造に依存しない
   - ビジネスルールは技術的関心事から分離

2. **✅ DRY原則を守る**
   - `PurchaseRequest` エンティティは1つだけ
   - 複数のスライスが同じドメインモデルを使用

3. **✅ 柔軟性が高い**
   - スライスを追加してもドメインモデルは変更不要
   - ドメインロジックの変更がVSA構造に影響しない

---

## 🔍 実例：依存方向の確認

### スライス1：承認機能

```csharp
// ✅ アプリケーション層（VSA内）
// src/PurchaseManagement/Features/ApprovePurchaseRequest/Application/ApprovePurchaseRequestHandler.cs

using PurchaseManagement.Shared.Domain.PurchaseRequests;  // ← ドメインモデルを参照
using PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;

public class ApprovePurchaseRequestHandler : IRequestHandler<ApprovePurchaseRequestCommand, Result>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly IApprovalBoundary _approvalBoundary;

    public async Task<Result> Handle(ApprovePurchaseRequestCommand command)
    {
        // ドメインモデルを使用
        var request = await _repository.GetByIdAsync(command.RequestId);

        // ドメインサービスを使用
        var eligibility = _approvalBoundary.CheckEligibility(request, command.ApproverId);

        if (!eligibility.IsEligible)
            return Result.Failure(eligibility.Reasons);

        // ドメインロジックを実行
        var result = request.Approve(command.ApproverId);

        if (result.IsSuccess)
            await _repository.UpdateAsync(request);

        return result;
    }
}
```

**依存方向:**
```
ApprovePurchaseRequestHandler（アプリケーション層）
  ↓ 依存
PurchaseRequest（ドメイン層）
ApprovalBoundary（ドメイン層）
```

---

### スライス2：詳細表示機能

```csharp
// ✅ アプリケーション層（VSA内）
// src/PurchaseManagement/Features/GetPurchaseRequestById/Application/GetPurchaseRequestByIdHandler.cs

using PurchaseManagement.Shared.Domain.PurchaseRequests;  // ← 同じドメインモデルを参照
using PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;

public class GetPurchaseRequestByIdHandler : IRequestHandler<GetPurchaseRequestByIdQuery, Result<PurchaseRequestDto>>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly IApprovalBoundary _approvalBoundary;

    public async Task<Result<PurchaseRequestDto>> Handle(GetPurchaseRequestByIdQuery query)
    {
        // 同じドメインモデルを使用
        var request = await _repository.GetByIdAsync(query.Id);

        if (request is null)
            return Result.Failure<PurchaseRequestDto>(DomainError.NotFound);

        // 同じドメインサービスを使用
        var context = _approvalBoundary.GetContext(request, query.CurrentUserId);

        return Result.Success(MapToDto(request, context));
    }
}
```

**依存方向:**
```
GetPurchaseRequestByIdHandler（アプリケーション層）
  ↓ 依存
PurchaseRequest（ドメイン層） ← ★ 同じエンティティを使用
ApprovalBoundary（ドメイン層） ← ★ 同じドメインサービスを使用
```

---

### ドメインモデル（独立）

```csharp
// ✅ ドメイン層（VSAの外）
// src/PurchaseManagement/Shared/Domain/PurchaseRequests/PurchaseRequest.cs

namespace PurchaseManagement.Shared.Domain.PurchaseRequests;

public sealed class PurchaseRequest : AggregateRoot<Guid>
{
    public PurchaseRequestStatus Status { get; private set; }
    public List<ApprovalStep> ApprovalSteps { get; private set; }

    // ✅ ビジネスルール（VSAに依存しない）
    public Result Approve(Guid approverId)
    {
        // ステータスチェック
        if (Status != PurchaseRequestStatus.PendingFirstApproval &&
            Status != PurchaseRequestStatus.PendingSecondApproval &&
            Status != PurchaseRequestStatus.PendingFinalApproval)
        {
            return Result.Failure(DomainError.InvalidState("このステータスでは承認できません"));
        }

        // 承認ステップの更新
        var currentStep = ApprovalSteps.FirstOrDefault(s => s.Status == ApprovalStepStatus.Pending);
        if (currentStep is null)
            return Result.Failure(DomainError.InvalidState("承認ステップが見つかりません"));

        currentStep.Approve(approverId);

        // ステータス遷移
        UpdateStatusBasedOnApprovalSteps();

        return Result.Success();
    }
}
```

**重要なポイント:**
- ✅ ドメインモデルは `Features/` を知らない
- ✅ ビジネスルールは純粋（技術的関心事なし）
- ✅ VSAの構造変更の影響を受けない

---

## 📊 レイヤーごとの分離

| レイヤー | VSAで分離される？ | 配置場所 | 理由 |
|---------|------------------|---------|------|
| **UI層** | ✅ YES | `Features/{機能}/UI/` | 機能ごとに画面が異なる |
| **Application層** | ✅ YES | `Features/{機能}/Application/` | 機能ごとにハンドラーが異なる |
| **Domain層** | ❌ NO | `Shared/Domain/` | エンティティは機能を超えて共有 |
| **Infrastructure層** | ❌ NO | `Shared/Infrastructure/` | DBアクセスは共通 |

---

## 🎯 設計ガイドライン

### ガイドライン1: 「ドメインモデルはBC内で1つ」

```
PurchaseRequest エンティティは PurchaseManagement BC 内で1つだけ存在する。

配置場所: src/PurchaseManagement/Shared/Domain/PurchaseRequests/PurchaseRequest.cs

スライス（Features）が10個に増えても、PurchaseRequest は1つのまま。
```

---

### ガイドライン2: 「Features/ にはアプリケーション層とUI層のみ」

```
Features/{機能}/
├── Application/  ← ✅ OK（ハンドラー、コマンド、クエリ）
├── UI/           ← ✅ OK（Razorコンポーネント）
└── Domain/       ← ❌ NG（ドメインモデルを置かない）
```

**理由:**
- ドメインモデルは機能（スライス）を超えて共有される
- ドメインモデルをスライス内に配置すると、独立性が失われる

---

### ガイドライン3: 「依存方向は一方向」

```
Features/ (アプリケーション層・UI層)
  ↓ 依存（一方向）
Shared/Domain/ (ドメイン層)

✅ アプリケーション層はドメイン層を参照できる
❌ ドメイン層はアプリケーション層を参照できない（独立）
```

---

### ガイドライン4: 「新しいスライスを追加してもドメインモデルは変更しない」

```
スライス追加前:
Features/
├── ApprovePurchaseRequest/
└── GetPurchaseRequestById/

Shared/Domain/PurchaseRequests/
└── PurchaseRequest.cs  ← ドメインモデル

---

スライス追加後:
Features/
├── ApprovePurchaseRequest/
├── GetPurchaseRequestById/
└── UpdatePurchaseRequest/  ← ★ 新スライス追加

Shared/Domain/PurchaseRequests/
└── PurchaseRequest.cs  ← ★ 変更なし（独立）
```

**ドメインモデルはVSAの構造変更の影響を受けない**

---

## 🔄 変更の影響範囲

### ケース1: ビジネスルールの変更

**例:** 承認ロジックの変更（2段階承認 → 3段階承認）

**影響範囲:**
- ✅ `Shared/Domain/PurchaseRequests/PurchaseRequest.cs` のみ変更
- ✅ `Features/` 内のスライスは変更不要（ドメインモデルを使うだけ）

**理由:**
- ドメインモデルが独立しているため、ビジネスルール変更が局所化

---

### ケース2: 新機能の追加

**例:** 「申請の差し戻し」機能を追加

**影響範囲:**
1. **ドメイン層:** `PurchaseRequest.cs` に `Return()` メソッドを追加
2. **アプリケーション層:** `Features/ReturnPurchaseRequest/` を新規作成
3. **UI層:** `Features/ReturnPurchaseRequest/UI/` を新規作成

**理由:**
- ドメインモデルは独立しているため、新機能追加が容易

---

### ケース3: UI画面の追加

**例:** 承認画面のレイアウト変更

**影響範囲:**
- ✅ `Features/ApprovePurchaseRequest/UI/` のみ変更
- ✅ ドメイン層は変更不要

**理由:**
- UI層とドメイン層が分離しているため、UI変更がドメインに影響しない

---

## 🚫 アンチパターン：スライス内ドメインモデル

### 誤った実装例

```csharp
// ❌ 間違った配置
// src/PurchaseManagement/Features/ApprovePurchaseRequest/Domain/PurchaseRequest.cs

namespace PurchaseManagement.Features.ApprovePurchaseRequest.Domain;

public class PurchaseRequest  // ← ❌ スライス内のドメインモデル
{
    // このスライス専用のドメインロジック？
    public Result Approve(Guid approverId)
    {
        // ...
    }
}
```

```csharp
// ❌ 間違った配置
// src/PurchaseManagement/Features/GetPurchaseRequestById/Domain/PurchaseRequest.cs

namespace PurchaseManagement.Features.GetPurchaseRequestById.Domain;

public class PurchaseRequest  // ← ❌ 重複したドメインモデル
{
    // 同じエンティティが別のスライスにも？
}
```

### 問題点

1. **❌ DRY原則違反**
   - 同じ `PurchaseRequest` が複数のスライスに存在
   - ビジネスルールの重複

2. **❌ 整合性の欠如**
   - スライスAの `PurchaseRequest` とスライスBの `PurchaseRequest` が異なる可能性
   - ドメインロジックの不一致

3. **❌ 保守性の低下**
   - ビジネスルール変更時、すべてのスライスを修正
   - 変更漏れのリスク

---

## 📐 正しい実装：共有ドメインモデル

### 正しい配置

```csharp
// ✅ 正しい配置
// src/PurchaseManagement/Shared/Domain/PurchaseRequests/PurchaseRequest.cs

namespace PurchaseManagement.Shared.Domain.PurchaseRequests;

public sealed class PurchaseRequest : AggregateRoot<Guid>  // ← ✅ 1つだけ
{
    // すべてのスライスで共有されるドメインロジック
    public Result Approve(Guid approverId) { /* ... */ }
    public Result Reject(Guid approverId, string reason) { /* ... */ }
    public Result Cancel(Guid userId, string reason) { /* ... */ }
    public Result Submit() { /* ... */ }
}
```

### 各スライスからの使用

```csharp
// ✅ スライス1：承認機能
// src/PurchaseManagement/Features/ApprovePurchaseRequest/Application/ApprovePurchaseRequestHandler.cs

using PurchaseManagement.Shared.Domain.PurchaseRequests;  // ← 共有ドメインモデルを参照

public class ApprovePurchaseRequestHandler
{
    public async Task<Result> Handle(ApprovePurchaseRequestCommand command)
    {
        var request = await _repository.GetByIdAsync(command.RequestId);
        return request.Approve(command.ApproverId);  // ← 共有ドメインロジック使用
    }
}
```

```csharp
// ✅ スライス2：却下機能
// src/PurchaseManagement/Features/RejectPurchaseRequest/Application/RejectPurchaseRequestHandler.cs

using PurchaseManagement.Shared.Domain.PurchaseRequests;  // ← 同じドメインモデルを参照

public class RejectPurchaseRequestHandler
{
    public async Task<Result> Handle(RejectPurchaseRequestCommand command)
    {
        var request = await _repository.GetByIdAsync(command.RequestId);
        return request.Reject(command.ApproverId, command.Reason);  // ← 同じドメインロジック使用
    }
}
```

---

## 🎓 まとめ

### VSAとドメインモデルの関係

| 項目 | VSA（垂直スライス） | ドメインモデル |
|------|---------------------|----------------|
| **配置場所** | `Features/{機能}/` | `Shared/Domain/` |
| **スコープ** | 1機能のみ | BC全体 |
| **分離される層** | アプリケーション層、UI層 | ドメイン層（分離されない） |
| **依存方向** | ドメイン層に依存 | 独立（他に依存しない） |

---

### 設計原則

> **ドメインモデルはVSAから独立している。**
>
> **VSAは「アプリケーション層を機能ごとに分離」するが、ドメインモデルは分離しない。**
>
> **ドメインモデルは `Shared/Domain/` に1つだけ配置し、すべてのスライスから参照される。**
>
> **この設計により、ビジネスルールの一貫性と保守性が保たれる。**

---

## 📚 関連ドキュメント

- **BC-VSA-Slice関係:** `docs/architecture/VSA-BC-SLICE-BOUNDARY-RELATIONSHIP.md`
- **Shared vs Kernel:** `docs/architecture/SHARED-VS-KERNEL-DISTINCTION.md`
- **BC整合性ガイドライン:** `docs/architecture/BC-CONSISTENCY-GUIDELINES.md`
