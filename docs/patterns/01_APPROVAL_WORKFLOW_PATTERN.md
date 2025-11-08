# パターン詳細: 承認フロー・ワークフロー

## 📋 概要

多段階承認や状態遷移を伴う業務機能の実装パターンです。
稟議、購買申請、経費精算、休暇申請など、B2B業務アプリケーションで頻繁に必要となるワークフローに対応します。

**実装例:** 購買申請（PurchaseRequest）の承認フロー

---

## 🎯 解決する課題

### 従来の問題点

**❌ 状態遷移ロジックが分散:**
```csharp
// ❌ Handlerに状態遷移ロジックが散在
public class ApproveHandler
{
    public async Task Handle(ApproveCommand cmd)
    {
        if (request.Status == Status.Draft) // 状態チェックがハンドラーにある
            throw new Exception("下書きは承認できません");

        if (request.Status == Status.Rejected)
            throw new Exception("却下済みは承認できません");

        request.Status = Status.Approved; // 直接ステータス変更
    }
}
```

**❌ ビジネスルールの漏れ:**
- どの状態からどの状態に遷移できるかが明示的でない
- 承認ステップのロジックがRepositoryやHandlerに分散
- テストが困難（状態遷移パターンの網羅が難しい）

**❌ 拡張性の欠如:**
- 新しい状態の追加が困難
- 承認フローの変更（例: 3段階承認→2段階承認）が困難

### 本パターンの解決策

**✅ State Machine Patternで状態遷移をカプセル化:**
```csharp
// ✅ 許可された状態遷移を明示的に定義
private static readonly Dictionary<Status, List<Status>> _allowedTransitions = new()
{
    { Status.Draft, new() { Status.Submitted } },
    { Status.Submitted, new() { Status.PendingApproval, Status.Cancelled } },
    { Status.PendingApproval, new() { Status.Approved, Status.Rejected } }
};

// ✅ Domain層で状態遷移を保護
public void Approve(Guid approverId, string comment)
{
    _stateMachine.ValidateTransition(Status, PurchaseRequestStatus.Approved);
    // 承認ロジック...
}
```

**✅ Saga Pattern（疑似）で複数段階の処理:**
- 承認ステップごとの進行管理
- Domain Eventによる各ステップの追跡
- 補償トランザクション（キャンセル時のロールバック）

**✅ Domain Eventで疎結合な連携:**
- 承認完了時にメール通知（Outbox Pattern経由）
- 却下時のワークフローキャンセル処理
- 監査ログの自動記録

---

## 🏗️ アーキテクチャ

### BC構造

```
src/PurchaseManagement/                     # 新規BC
├── Features/
│   ├── SubmitPurchaseRequest/              # 購買申請提出
│   │   ├── Application/
│   │   │   ├── SubmitPurchaseRequestCommand.cs
│   │   │   ├── SubmitPurchaseRequestHandler.cs
│   │   │   └── SubmitPurchaseRequestValidator.cs
│   │   └── UI/
│   │       ├── Api/
│   │       │   └── SubmitPurchaseRequestEndpoint.cs
│   │       └── Components/
│   │           └── PurchaseRequestForm.razor
│   │
│   ├── ApprovePurchaseRequest/             # 購買申請承認
│   │   ├── Application/
│   │   │   ├── ApprovePurchaseRequestCommand.cs
│   │   │   ├── ApprovePurchaseRequestHandler.cs
│   │   │   └── ApprovePurchaseRequestValidator.cs
│   │   └── UI/
│   │       ├── Api/
│   │       └── Components/
│   │           └── ApprovalDialog.razor
│   │
│   ├── RejectPurchaseRequest/              # 購買申請却下
│   ├── CancelPurchaseRequest/              # 購買申請キャンセル
│   ├── GetPurchaseRequestById/             # 申請詳細取得
│   ├── GetPendingApprovals/                # 承認待ち一覧取得
│   └── GetMyPurchaseRequests/              # 自分の申請一覧取得
│
└── Shared/
    ├── Domain/
    │   └── PurchaseRequests/
    │       ├── PurchaseRequest.cs              # 集約ルート
    │       ├── PurchaseRequestId.cs            # ValueObject
    │       ├── PurchaseRequestNumber.cs        # ValueObject
    │       ├── PurchaseRequestStatus.cs        # 状態列挙型
    │       ├── PurchaseRequestItem.cs          # エンティティ（明細）
    │       ├── ApprovalStep.cs                 # エンティティ（承認ステップ）
    │       ├── ApprovalFlow.cs                 # ValueObject（承認フロー定義）
    │       ├── StateMachine/
    │       │   ├── IStateMachine.cs
    │       │   └── PurchaseRequestStateMachine.cs
    │       └── Events/
    │           ├── PurchaseRequestSubmittedEvent.cs
    │           ├── PurchaseRequestApprovedEvent.cs
    │           ├── PurchaseRequestRejectedEvent.cs
    │           └── PurchaseRequestCancelledEvent.cs
    │
    ├── Application/
    │   ├── DTOs/
    │   │   ├── PurchaseRequestDto.cs
    │   │   ├── PurchaseRequestDetailDto.cs
    │   │   └── ApprovalStepDto.cs
    │   └── Services/
    │       └── IApprovalFlowService.cs         # 承認フロー決定サービス
    │
    ├── Infrastructure/
    │   ├── Persistence/
    │   │   ├── Configurations/
    │   │   │   ├── PurchaseRequestConfiguration.cs
    │   │   │   ├── ApprovalStepConfiguration.cs
    │   │   │   └── PurchaseRequestItemConfiguration.cs
    │   │   └── Repositories/
    │   │       └── PurchaseRequestRepository.cs
    │   └── Services/
    │       └── ApprovalFlowService.cs          # 承認フロー決定ロジック実装
    │
    └── UI/
        ├── Store/
        │   ├── PurchaseRequestsStore.cs
        │   └── PendingApprovalsStore.cs
        └── Actions/
            ├── PurchaseRequestActions.cs
            └── ApprovalActions.cs
```

---

## 💎 Domain層実装

### 1. PurchaseRequestStatus（状態定義）

```csharp
/// <summary>
/// 購買申請のステータス
/// </summary>
public enum PurchaseRequestStatus
{
    /// <summary>下書き（未提出）</summary>
    Draft = 0,

    /// <summary>申請中（提出済み、承認待ち）</summary>
    Submitted = 1,

    /// <summary>1次承認待ち</summary>
    PendingFirstApproval = 2,

    /// <summary>2次承認待ち</summary>
    PendingSecondApproval = 3,

    /// <summary>最終承認待ち</summary>
    PendingFinalApproval = 4,

    /// <summary>承認済み</summary>
    Approved = 5,

    /// <summary>却下</summary>
    Rejected = 6,

    /// <summary>キャンセル</summary>
    Cancelled = 7
}
```

### 2. IStateMachine（状態機械インターフェース）

```csharp
/// <summary>
/// 状態機械の抽象化
/// </summary>
public interface IStateMachine<TState> where TState : Enum
{
    /// <summary>
    /// 指定された状態遷移が許可されているか確認
    /// </summary>
    bool CanTransition(TState from, TState to);

    /// <summary>
    /// 状態遷移を検証（許可されていない場合は例外）
    /// </summary>
    void ValidateTransition(TState from, TState to);

    /// <summary>
    /// 現在の状態から遷移可能な状態のリストを取得
    /// </summary>
    IEnumerable<TState> GetAllowedTransitions(TState from);
}

/// <summary>
/// 無効な状態遷移例外
/// </summary>
public class InvalidStateTransitionException : DomainException
{
    public InvalidStateTransitionException(string message) : base(message) { }
}
```

### 3. PurchaseRequestStateMachine（状態遷移ロジック）

```csharp
/// <summary>
/// 購買申請の状態遷移ロジック
/// </summary>
public class PurchaseRequestStateMachine : IStateMachine<PurchaseRequestStatus>
{
    // 許可された状態遷移の定義
    private static readonly Dictionary<PurchaseRequestStatus, List<PurchaseRequestStatus>> _allowedTransitions = new()
    {
        // 下書き → 申請中
        { PurchaseRequestStatus.Draft, new() { PurchaseRequestStatus.Submitted } },

        // 申請中 → 1次承認待ち or キャンセル
        { PurchaseRequestStatus.Submitted, new()
            {
                PurchaseRequestStatus.PendingFirstApproval,
                PurchaseRequestStatus.Cancelled
            }
        },

        // 1次承認待ち → 2次承認待ち or 最終承認待ち（承認フローによる） or 却下 or キャンセル
        { PurchaseRequestStatus.PendingFirstApproval, new()
            {
                PurchaseRequestStatus.PendingSecondApproval,
                PurchaseRequestStatus.PendingFinalApproval,
                PurchaseRequestStatus.Approved, // 1段階承認の場合
                PurchaseRequestStatus.Rejected,
                PurchaseRequestStatus.Cancelled
            }
        },

        // 2次承認待ち → 最終承認待ち or 承認済み or 却下
        { PurchaseRequestStatus.PendingSecondApproval, new()
            {
                PurchaseRequestStatus.PendingFinalApproval,
                PurchaseRequestStatus.Approved, // 2段階承認の場合
                PurchaseRequestStatus.Rejected
            }
        },

        // 最終承認待ち → 承認済み or 却下
        { PurchaseRequestStatus.PendingFinalApproval, new()
            {
                PurchaseRequestStatus.Approved,
                PurchaseRequestStatus.Rejected
            }
        },

        // 承認済み → 遷移なし（終端状態）
        { PurchaseRequestStatus.Approved, new() { } },

        // 却下 → 遷移なし（終端状態）
        { PurchaseRequestStatus.Rejected, new() { } },

        // キャンセル → 遷移なし（終端状態）
        { PurchaseRequestStatus.Cancelled, new() { } }
    };

    public bool CanTransition(PurchaseRequestStatus from, PurchaseRequestStatus to)
    {
        return _allowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public void ValidateTransition(PurchaseRequestStatus from, PurchaseRequestStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidStateTransitionException(
                $"状態遷移が許可されていません: {from} → {to}");
        }
    }

    public IEnumerable<PurchaseRequestStatus> GetAllowedTransitions(PurchaseRequestStatus from)
    {
        return _allowedTransitions.TryGetValue(from, out var allowed)
            ? allowed
            : Enumerable.Empty<PurchaseRequestStatus>();
    }
}
```

### 4. ApprovalFlow（承認フロー定義）

```csharp
/// <summary>
/// 承認フロー定義（ValueObject）
/// </summary>
public class ApprovalFlow : ValueObject
{
    public IReadOnlyList<ApprovalFlowStep> Steps { get; }

    public ApprovalFlow(IEnumerable<ApprovalFlowStep> steps)
    {
        var stepList = steps.ToList();

        if (stepList.Count == 0)
            throw new DomainException("承認フローには最低1つのステップが必要です");

        if (stepList.Count > 5)
            throw new DomainException("承認フローは最大5段階までです");

        // ステップ番号の連続性チェック
        for (int i = 0; i < stepList.Count; i++)
        {
            if (stepList[i].StepNumber != i + 1)
                throw new DomainException("承認ステップ番号が連続していません");
        }

        Steps = stepList.AsReadOnly();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        foreach (var step in Steps)
            yield return step;
    }

    /// <summary>
    /// 1段階承認フロー（例: 10万円未満）
    /// </summary>
    public static ApprovalFlow SingleStep(Guid approverId, string approverName)
    {
        return new ApprovalFlow(new[]
        {
            new ApprovalFlowStep(1, approverId, approverName, "Manager")
        });
    }

    /// <summary>
    /// 2段階承認フロー（例: 10万円以上50万円未満）
    /// </summary>
    public static ApprovalFlow TwoStep(
        Guid firstApproverId, string firstApproverName,
        Guid secondApproverId, string secondApproverName)
    {
        return new ApprovalFlow(new[]
        {
            new ApprovalFlowStep(1, firstApproverId, firstApproverName, "Manager"),
            new ApprovalFlowStep(2, secondApproverId, secondApproverName, "Director")
        });
    }

    /// <summary>
    /// 3段階承認フロー（例: 50万円以上）
    /// </summary>
    public static ApprovalFlow ThreeStep(
        Guid firstApproverId, string firstApproverName,
        Guid secondApproverId, string secondApproverName,
        Guid thirdApproverId, string thirdApproverName)
    {
        return new ApprovalFlow(new[]
        {
            new ApprovalFlowStep(1, firstApproverId, firstApproverName, "Manager"),
            new ApprovalFlowStep(2, secondApproverId, secondApproverName, "Director"),
            new ApprovalFlowStep(3, thirdApproverId, thirdApproverName, "Executive")
        });
    }
}

/// <summary>
/// 承認フローのステップ定義
/// </summary>
public record ApprovalFlowStep(
    int StepNumber,
    Guid ApproverId,
    string ApproverName,
    string ApproverRole
);
```

### 5. ApprovalStep（承認ステップエンティティ）

```csharp
/// <summary>
/// 承認ステップ（エンティティ）
/// </summary>
public class ApprovalStep : Entity<Guid>
{
    public int StepNumber { get; init; }
    public Guid ApproverId { get; init; }
    public string ApproverName { get; init; } = string.Empty;
    public string ApproverRole { get; init; } = string.Empty;
    public ApprovalStepStatus Status { get; private set; }
    public string? Comment { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }

    public bool IsPending => Status == ApprovalStepStatus.Pending;
    public bool IsApproved => Status == ApprovalStepStatus.Approved;
    public bool IsRejected => Status == ApprovalStepStatus.Rejected;

    /// <summary>
    /// 承認
    /// </summary>
    public void Approve(string comment)
    {
        if (Status != ApprovalStepStatus.Pending)
            throw new DomainException("このステップは既に処理されています");

        Status = ApprovalStepStatus.Approved;
        Comment = comment;
        ApprovedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 却下
    /// </summary>
    public void Reject(string reason)
    {
        if (Status != ApprovalStepStatus.Pending)
            throw new DomainException("このステップは既に処理されています");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("却下理由は必須です");

        Status = ApprovalStepStatus.Rejected;
        Comment = reason;
        RejectedAt = DateTime.UtcNow;
    }
}

public enum ApprovalStepStatus
{
    Pending = 0,    // 承認待ち
    Approved = 1,   // 承認済み
    Rejected = 2    // 却下
}
```

### 6. PurchaseRequest（集約ルート）

```csharp
/// <summary>
/// 購買申請（集約ルート）
/// </summary>
public class PurchaseRequest : AggregateRoot<Guid>
{
    private readonly PurchaseRequestStateMachine _stateMachine = new();
    private readonly List<ApprovalStep> _approvalSteps = new();
    private readonly List<PurchaseRequestItem> _items = new();

    // 基本情報
    public PurchaseRequestNumber RequestNumber { get; private set; } = null!;
    public Guid RequesterId { get; private set; }
    public string RequesterName { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    // 状態
    public PurchaseRequestStatus Status { get; private set; }

    // 日時
    public DateTime CreatedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    // 承認情報
    public IReadOnlyList<ApprovalStep> ApprovalSteps => _approvalSteps.AsReadOnly();
    public ApprovalStep? CurrentApprovalStep => _approvalSteps.FirstOrDefault(s => s.IsPending);

    // 明細
    public IReadOnlyList<PurchaseRequestItem> Items => _items.AsReadOnly();
    public Money TotalAmount => new(_items.Sum(i => i.Amount.Value));

    // ビジネスルール: 金額制限
    private const decimal MaxRequestAmount = 1_000_000m; // 100万円

    #region ファクトリメソッド

    /// <summary>
    /// 購買申請を作成（下書き状態）
    /// </summary>
    public static PurchaseRequest Create(
        Guid requesterId,
        string requesterName,
        string title,
        string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("タイトルは必須です");

        var request = new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = PurchaseRequestNumber.Generate(),
            RequesterId = requesterId,
            RequesterName = requesterName,
            Title = title,
            Description = description,
            Status = PurchaseRequestStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        return request;
    }

    #endregion

    #region 明細操作

    /// <summary>
    /// 明細を追加
    /// </summary>
    public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (Status != PurchaseRequestStatus.Draft)
            throw new DomainException("明細の追加は下書き状態でのみ可能です");

        if (quantity <= 0)
            throw new DomainException("数量は1以上を指定してください");

        if (unitPrice <= 0)
            throw new DomainException("単価は0円より大きい金額を指定してください");

        var item = PurchaseRequestItem.Create(productId, productName, unitPrice, quantity);
        _items.Add(item);

        // 金額上限チェック
        if (TotalAmount.Value > MaxRequestAmount)
            throw new DomainException($"購買申請の合計金額は{MaxRequestAmount:N0}円までです");
    }

    /// <summary>
    /// 明細を削除
    /// </summary>
    public void RemoveItem(Guid itemId)
    {
        if (Status != PurchaseRequestStatus.Draft)
            throw new DomainException("明細の削除は下書き状態でのみ可能です");

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            throw new DomainException("明細が見つかりません");

        _items.Remove(item);
    }

    #endregion

    #region ワークフロー操作

    /// <summary>
    /// 申請提出
    /// </summary>
    public void Submit(ApprovalFlow approvalFlow)
    {
        _stateMachine.ValidateTransition(Status, PurchaseRequestStatus.Submitted);

        if (_items.Count == 0)
            throw new DomainException("明細が1件もありません");

        // 承認フローを設定
        foreach (var step in approvalFlow.Steps)
        {
            _approvalSteps.Add(new ApprovalStep
            {
                Id = Guid.NewGuid(),
                StepNumber = step.StepNumber,
                ApproverId = step.ApproverId,
                ApproverName = step.ApproverName,
                ApproverRole = step.ApproverRole,
                Status = ApprovalStepStatus.Pending
            });
        }

        Status = PurchaseRequestStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;

        // Domain Event発行
        AddDomainEvent(new PurchaseRequestSubmittedEvent(
            Id,
            RequestNumber.Value,
            RequesterId,
            RequesterName,
            TotalAmount.Value,
            SubmittedAt.Value
        ));
    }

    /// <summary>
    /// 承認
    /// </summary>
    public void Approve(Guid approverId, string comment)
    {
        // 現在の承認ステップを取得
        var currentStep = CurrentApprovalStep;
        if (currentStep is null)
            throw new DomainException("承認待ちのステップがありません");

        // 承認者チェック
        if (currentStep.ApproverId != approverId)
            throw new DomainException("このステップの承認者ではありません");

        // 承認ステップを完了
        currentStep.Approve(comment);

        // 次のステップがあるか確認
        var nextStep = _approvalSteps.FirstOrDefault(s => s.StepNumber == currentStep.StepNumber + 1);
        if (nextStep is not null)
        {
            // 次の承認ステップへ
            var nextStatus = GetNextApprovalStatus(nextStep.StepNumber);
            _stateMachine.ValidateTransition(Status, nextStatus);
            Status = nextStatus;
        }
        else
        {
            // 最終承認完了
            _stateMachine.ValidateTransition(Status, PurchaseRequestStatus.Approved);
            Status = PurchaseRequestStatus.Approved;
            ApprovedAt = DateTime.UtcNow;

            // Domain Event発行
            AddDomainEvent(new PurchaseRequestApprovedEvent(
                Id,
                RequestNumber.Value,
                RequesterId,
                approverId,
                TotalAmount.Value,
                ApprovedAt.Value
            ));
        }
    }

    /// <summary>
    /// 却下
    /// </summary>
    public void Reject(Guid approverId, string reason)
    {
        var currentStep = CurrentApprovalStep;
        if (currentStep is null)
            throw new DomainException("承認待ちのステップがありません");

        if (currentStep.ApproverId != approverId)
            throw new DomainException("このステップの承認者ではありません");

        // 承認ステップを却下
        currentStep.Reject(reason);

        _stateMachine.ValidateTransition(Status, PurchaseRequestStatus.Rejected);
        Status = PurchaseRequestStatus.Rejected;
        RejectedAt = DateTime.UtcNow;

        // Domain Event発行
        AddDomainEvent(new PurchaseRequestRejectedEvent(
            Id,
            RequestNumber.Value,
            RequesterId,
            approverId,
            reason,
            RejectedAt.Value
        ));
    }

    /// <summary>
    /// キャンセル（申請者のみ可能）
    /// </summary>
    public void Cancel(Guid userId)
    {
        if (userId != RequesterId)
            throw new DomainException("申請者のみキャンセルできます");

        // キャンセル可能な状態かチェック
        if (Status == PurchaseRequestStatus.Draft)
            throw new DomainException("下書きはキャンセルできません（削除してください）");

        if (Status == PurchaseRequestStatus.Approved)
            throw new DomainException("承認済みの申請はキャンセルできません");

        if (Status == PurchaseRequestStatus.Rejected)
            throw new DomainException("既に却下されています");

        if (Status == PurchaseRequestStatus.Cancelled)
            throw new DomainException("既にキャンセルされています");

        _stateMachine.ValidateTransition(Status, PurchaseRequestStatus.Cancelled);
        Status = PurchaseRequestStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;

        // Domain Event発行
        AddDomainEvent(new PurchaseRequestCancelledEvent(
            Id,
            RequestNumber.Value,
            RequesterId,
            CancelledAt.Value
        ));
    }

    #endregion

    #region ヘルパーメソッド

    private PurchaseRequestStatus GetNextApprovalStatus(int stepNumber)
    {
        return stepNumber switch
        {
            1 => PurchaseRequestStatus.PendingFirstApproval,
            2 => PurchaseRequestStatus.PendingSecondApproval,
            3 => PurchaseRequestStatus.PendingFinalApproval,
            _ => throw new DomainException($"無効なステップ番号: {stepNumber}")
        };
    }

    #endregion
}
```

### 7. PurchaseRequestItem（明細エンティティ）

```csharp
/// <summary>
/// 購買申請明細
/// </summary>
public class PurchaseRequestItem : Entity<Guid>
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Money UnitPrice { get; init; } = null!;
    public int Quantity { get; init; }
    public Money Amount { get; init; } = null!;

    public static PurchaseRequestItem Create(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        return new PurchaseRequestItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName,
            UnitPrice = new Money(unitPrice),
            Quantity = quantity,
            Amount = new Money(unitPrice * quantity)
        };
    }
}
```

### 8. Domain Events

```csharp
/// <summary>
/// 購買申請が提出されたイベント
/// </summary>
public record PurchaseRequestSubmittedEvent(
    Guid RequestId,
    string RequestNumber,
    Guid RequesterId,
    string RequesterName,
    decimal TotalAmount,
    DateTime SubmittedAt
) : DomainEvent;

/// <summary>
/// 購買申請が承認されたイベント
/// </summary>
public record PurchaseRequestApprovedEvent(
    Guid RequestId,
    string RequestNumber,
    Guid RequesterId,
    Guid ApproverId,
    decimal TotalAmount,
    DateTime ApprovedAt
) : DomainEvent;

/// <summary>
/// 購買申請が却下されたイベント
/// </summary>
public record PurchaseRequestRejectedEvent(
    Guid RequestId,
    string RequestNumber,
    Guid RequesterId,
    Guid ApproverId,
    string Reason,
    DateTime RejectedAt
) : DomainEvent;

/// <summary>
/// 購買申請がキャンセルされたイベント
/// </summary>
public record PurchaseRequestCancelledEvent(
    Guid RequestId,
    string RequestNumber,
    Guid RequesterId,
    DateTime CancelledAt
) : DomainEvent;
```

---

## 🔧 Application層実装

### SubmitPurchaseRequest（申請提出）

```csharp
// Command
[Authorize(Roles = "User,Manager")]
public record SubmitPurchaseRequestCommand : ICommand<Result<Guid>>
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required List<PurchaseRequestItemDto> Items { get; init; }
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}

public record PurchaseRequestItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity
);

// Validator
public class SubmitPurchaseRequestValidator : AbstractValidator<SubmitPurchaseRequestCommand>
{
    public SubmitPurchaseRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("タイトルは必須です")
            .MaximumLength(200).WithMessage("タイトルは200文字以内で入力してください");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("説明は2000文字以内で入力してください");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("明細は最低1件必要です")
            .Must(items => items.Count <= 100).WithMessage("明細は100件までです");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.ProductName).NotEmpty().MaximumLength(200);
            item.RuleFor(i => i.UnitPrice).GreaterThan(0).WithMessage("単価は0円より大きい金額を指定してください");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("数量は1以上を指定してください");
        });
    }
}

// Handler
public class SubmitPurchaseRequestHandler : ICommandHandler<SubmitPurchaseRequestCommand, Result<Guid>>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly IApprovalFlowService _approvalFlowService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SubmitPurchaseRequestHandler> _logger;

    public async Task<Result<Guid>> Handle(SubmitPurchaseRequestCommand cmd, CancellationToken ct)
    {
        try
        {
            // 1. 購買申請を作成
            var request = PurchaseRequest.Create(
                _currentUserService.UserId!.Value,
                _currentUserService.UserName!,
                cmd.Title,
                cmd.Description
            );

            // 2. 明細を追加
            foreach (var item in cmd.Items)
            {
                request.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);
            }

            // 3. 承認フローを決定（金額に応じて自動判定）
            var approvalFlow = await _approvalFlowService.DetermineFlowAsync(
                request.TotalAmount.Value,
                ct
            );

            // 4. 申請提出
            request.Submit(approvalFlow);

            // 5. 永続化
            await _repository.SaveAsync(request, ct);

            _logger.LogInformation(
                "Purchase request submitted: RequestId={RequestId}, RequestNumber={RequestNumber}, TotalAmount={TotalAmount}",
                request.Id, request.RequestNumber.Value, request.TotalAmount.Value);

            return Result.Success(request.Id);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Failed to submit purchase request: {Message}", ex.Message);
            return Result.Failure<Guid>(ex.Message);
        }
    }
}
```

### ApprovePurchaseRequest（承認）

```csharp
// Command
[Authorize(Roles = "Manager,Director,Executive")]
public record ApprovePurchaseRequestCommand : ICommand<Result>
{
    public required Guid RequestId { get; init; }
    public required string Comment { get; init; }
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}

// Validator
public class ApprovePurchaseRequestValidator : AbstractValidator<ApprovePurchaseRequestCommand>
{
    public ApprovePurchaseRequestValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("コメントは必須です")
            .MaximumLength(500).WithMessage("コメントは500文字以内で入力してください");
    }
}

// Handler
public class ApprovePurchaseRequestHandler : ICommandHandler<ApprovePurchaseRequestCommand, Result>
{
    private readonly IPurchaseRequestRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ApprovePurchaseRequestHandler> _logger;

    public async Task<Result> Handle(ApprovePurchaseRequestCommand cmd, CancellationToken ct)
    {
        var request = await _repository.GetByIdAsync(cmd.RequestId, ct);
        if (request is null)
            return Result.Failure("購買申請が見つかりません");

        try
        {
            request.Approve(_currentUserService.UserId!.Value, cmd.Comment);
            await _repository.SaveAsync(request, ct);

            _logger.LogInformation(
                "Purchase request approved: RequestId={RequestId}, ApproverId={ApproverId}",
                request.Id, _currentUserService.UserId);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Failed to approve purchase request: RequestId={RequestId}, Message={Message}",
                cmd.RequestId, ex.Message);
            return Result.Failure(ex.Message);
        }
    }
}
```

---

## 🧪 テスト戦略

### Unit Test（State Machine）

```csharp
public class PurchaseRequestStateMachineTests
{
    private readonly PurchaseRequestStateMachine _stateMachine = new();

    [Theory]
    [InlineData(PurchaseRequestStatus.Draft, PurchaseRequestStatus.Submitted, true)]
    [InlineData(PurchaseRequestStatus.Draft, PurchaseRequestStatus.Approved, false)]
    [InlineData(PurchaseRequestStatus.Submitted, PurchaseRequestStatus.PendingFirstApproval, true)]
    [InlineData(PurchaseRequestStatus.Approved, PurchaseRequestStatus.Rejected, false)]
    public void CanTransition_ValidatesCorrectly(
        PurchaseRequestStatus from,
        PurchaseRequestStatus to,
        bool expected)
    {
        // Act
        var result = _stateMachine.CanTransition(from, to);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ValidateTransition_ThrowsException_WhenInvalidTransition()
    {
        // Arrange
        var from = PurchaseRequestStatus.Approved;
        var to = PurchaseRequestStatus.Rejected;

        // Act & Assert
        Assert.Throws<InvalidStateTransitionException>(() =>
            _stateMachine.ValidateTransition(from, to));
    }
}
```

### Unit Test（PurchaseRequest集約）

```csharp
public class PurchaseRequestTests
{
    [Fact]
    public void Create_CreatesValidPurchaseRequest()
    {
        // Act
        var request = PurchaseRequest.Create(
            Guid.NewGuid(),
            "John Doe",
            "Office Supplies",
            "Purchase office supplies for Q4"
        );

        // Assert
        request.Status.Should().Be(PurchaseRequestStatus.Draft);
        request.Title.Should().Be("Office Supplies");
        request.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_AddsItemSuccessfully()
    {
        // Arrange
        var request = PurchaseRequest.Create(Guid.NewGuid(), "John Doe", "Test", "Test");

        // Act
        request.AddItem(Guid.NewGuid(), "Laptop", 150000m, 1);

        // Assert
        request.Items.Should().HaveCount(1);
        request.TotalAmount.Value.Should().Be(150000m);
    }

    [Fact]
    public void AddItem_ThrowsException_WhenNotInDraftStatus()
    {
        // Arrange
        var request = PurchaseRequest.Create(Guid.NewGuid(), "John Doe", "Test", "Test");
        request.AddItem(Guid.NewGuid(), "Laptop", 150000m, 1);
        var approvalFlow = ApprovalFlow.SingleStep(Guid.NewGuid(), "Manager");
        request.Submit(approvalFlow);

        // Act & Assert
        Assert.Throws<DomainException>(() =>
            request.AddItem(Guid.NewGuid(), "Mouse", 3000m, 1));
    }

    [Fact]
    public void Submit_TransitionsToSubmittedStatus()
    {
        // Arrange
        var request = PurchaseRequest.Create(Guid.NewGuid(), "John Doe", "Test", "Test");
        request.AddItem(Guid.NewGuid(), "Laptop", 150000m, 1);
        var approvalFlow = ApprovalFlow.SingleStep(Guid.NewGuid(), "Manager");

        // Act
        request.Submit(approvalFlow);

        // Assert
        request.Status.Should().Be(PurchaseRequestStatus.Submitted);
        request.ApprovalSteps.Should().HaveCount(1);
        request.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public void Approve_CompletesApprovalStep()
    {
        // Arrange
        var approverId = Guid.NewGuid();
        var request = PurchaseRequest.Create(Guid.NewGuid(), "John Doe", "Test", "Test");
        request.AddItem(Guid.NewGuid(), "Laptop", 150000m, 1);
        var approvalFlow = ApprovalFlow.SingleStep(approverId, "Manager");
        request.Submit(approvalFlow);

        // Act
        request.Approve(approverId, "Approved");

        // Assert
        request.Status.Should().Be(PurchaseRequestStatus.Approved);
        request.ApprovalSteps.First().IsApproved.Should().BeTrue();
        request.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reject_TransitionsToRejectedStatus()
    {
        // Arrange
        var approverId = Guid.NewGuid();
        var request = PurchaseRequest.Create(Guid.NewGuid(), "John Doe", "Test", "Test");
        request.AddItem(Guid.NewGuid(), "Laptop", 150000m, 1);
        var approvalFlow = ApprovalFlow.SingleStep(approverId, "Manager");
        request.Submit(approvalFlow);

        // Act
        request.Reject(approverId, "Insufficient budget");

        // Assert
        request.Status.Should().Be(PurchaseRequestStatus.Rejected);
        request.ApprovalSteps.First().IsRejected.Should().BeTrue();
        request.RejectedAt.Should().NotBeNull();
    }
}
```

### Integration Test（ワークフロー全体）

```csharp
public class PurchaseRequestWorkflowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CompletePurchaseRequestWorkflow_SingleStepApproval()
    {
        // Arrange: 申請者でログイン
        var requesterClient = _factory.CreateAuthenticatedClient("requester@example.com", "User");

        // Act 1: 購買申請を提出
        var submitCommand = new SubmitPurchaseRequestCommand
        {
            Title = "Office Supplies",
            Description = "Test",
            Items = new List<PurchaseRequestItemDto>
            {
                new(Guid.NewGuid(), "Laptop", 150000m, 1)
            }
        };

        var submitResponse = await requesterClient.PostAsJsonAsync("/api/purchase-requests/submit", submitCommand);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var requestId = await submitResponse.Content.ReadFromJsonAsync<Guid>();

        // Arrange: 承認者でログイン
        var approverClient = _factory.CreateAuthenticatedClient("manager@example.com", "Manager");

        // Act 2: 購買申請を承認
        var approveCommand = new ApprovePurchaseRequestCommand
        {
            RequestId = requestId,
            Comment = "Approved"
        };

        var approveResponse = await approverClient.PostAsJsonAsync("/api/purchase-requests/approve", approveCommand);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: 承認済み状態になっているか確認
        var getResponse = await requesterClient.GetAsync($"/api/purchase-requests/{requestId}");
        var purchaseRequest = await getResponse.Content.ReadFromJsonAsync<PurchaseRequestDetailDto>();
        purchaseRequest!.Status.Should().Be(PurchaseRequestStatus.Approved);
    }
}
```

---

## 📝 まとめ

### このパターンで実現できること

✅ **明示的な状態遷移:** State Machine Patternで許可された状態遷移を定義
✅ **ビジネスルールの保護:** Domain層で状態遷移ロジックをカプセル化
✅ **複数段階の承認フロー:** 金額に応じた柔軟な承認フロー設定
✅ **Domain Eventによる疎結合:** 承認完了時の通知処理を分離
✅ **テストの容易性:** 状態遷移パターンを網羅的にテスト可能

### 適用可能なシナリオ

- 購買申請・稟議
- 経費精算
- 休暇申請
- 契約承認
- 見積承認
- 注文承認

---

**作成日:** 2025-11-07
**最終更新:** 2025-11-07
**ステータス:** ✅ 設計完了
