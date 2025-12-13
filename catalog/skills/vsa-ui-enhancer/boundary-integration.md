# Boundary 連携（Boundary Integration）

Domain Entity の `CanXxx()` メソッドと UI の連携パターンを定義する。

---

## 基本概念

### BoundaryDecision とは

操作可否の判定結果を表す値オブジェクト。

```csharp
public sealed record BoundaryDecision
{
    public bool IsAllowed { get; init; }
    public string? Reason { get; init; }

    public static BoundaryDecision Allow() => new() { IsAllowed = true };
    public static BoundaryDecision Deny(string reason) => new() { IsAllowed = false, Reason = reason };
}
```

### Entity.CanXxx() パターン

操作可否の判定ロジックは Entity が持つ。

```csharp
public class Order : AggregateRoot<OrderId>
{
    public BoundaryDecision CanCancel()
    {
        return Status switch
        {
            OrderStatus.Pending => BoundaryDecision.Allow(),
            OrderStatus.Shipped => BoundaryDecision.Deny("発送済みのためキャンセルできません"),
            OrderStatus.Delivered => BoundaryDecision.Deny("配達完了後はキャンセルできません"),
            OrderStatus.Cancelled => BoundaryDecision.Deny("既にキャンセル済みです"),
            _ => BoundaryDecision.Deny("この状態ではキャンセルできません")
        };
    }
}
```

---

## UI 連携パターン

### 1. ボタン活性/非活性 + ツールチップ

**最も基本的なパターン。** 操作不可の場合は理由をツールチップで表示。

```razor
@{
    var canCancel = order.CanCancel();
}

<MudTooltip Text="@(canCancel.IsAllowed ? string.Empty : canCancel.Reason)"
            Disabled="@canCancel.IsAllowed">
    <MudButton Color="Color.Error"
               Variant="Variant.Outlined"
               Disabled="@(!canCancel.IsAllowed)"
               OnClick="@HandleCancel">
        キャンセル
    </MudButton>
</MudTooltip>
```

**ポイント:**
- `Disabled` は `!canCancel.IsAllowed` で制御
- ツールチップは許可時は空文字（表示しない）
- ツールチップ自体も `Disabled` で制御（許可時は無効化）

---

### 2. ローディング状態との組み合わせ

送信中のローディング表示を追加。

```razor
@{
    var canSubmit = entity.CanSubmit();
    var isDisabled = !canSubmit.IsAllowed || isSubmitting;
}

<MudTooltip Text="@(canSubmit.IsAllowed ? string.Empty : canSubmit.Reason)"
            Disabled="@canSubmit.IsAllowed">
    <MudButton ButtonType="ButtonType.Submit"
               Color="Color.Primary"
               Variant="Variant.Filled"
               Disabled="@isDisabled">
        @if (isSubmitting)
        {
            <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
        }
        送信
    </MudButton>
</MudTooltip>
```

---

### 3. アイコンボタン（一覧内操作）

テーブル行内の操作ボタン。

```razor
<MudTable Items="@orders" Hover="true">
    <HeaderContent>
        <MudTh>注文番号</MudTh>
        <MudTh>ステータス</MudTh>
        <MudTh>操作</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd>@context.OrderNumber</MudTd>
        <MudTd>@context.Status</MudTd>
        <MudTd>
            @{
                var canCancel = context.CanCancel();
            }
            <MudTooltip Text="@(canCancel.IsAllowed ? "キャンセル" : canCancel.Reason)">
                <MudIconButton Icon="@Icons.Material.Filled.Cancel"
                               Color="@(canCancel.IsAllowed ? Color.Error : Color.Default)"
                               Disabled="@(!canCancel.IsAllowed)"
                               OnClick="@(() => HandleCancel(context.Id))" />
            </MudTooltip>
        </MudTd>
    </RowTemplate>
</MudTable>
```

---

### 4. 複数操作のボタングループ

複数の操作ボタンをまとめて表示。

```razor
@{
    var canApprove = request.CanApprove(currentUserId);
    var canReject = request.CanReject(currentUserId);
    var canCancel = request.CanCancel(currentUserId);
}

<MudButtonGroup Variant="Variant.Outlined">
    <MudTooltip Text="@(canApprove.IsAllowed ? string.Empty : canApprove.Reason)"
                Disabled="@canApprove.IsAllowed">
        <MudButton Color="Color.Success"
                   Disabled="@(!canApprove.IsAllowed)"
                   OnClick="@HandleApprove">
            <MudIcon Icon="@Icons.Material.Filled.Check" Class="mr-1" />
            承認
        </MudButton>
    </MudTooltip>

    <MudTooltip Text="@(canReject.IsAllowed ? string.Empty : canReject.Reason)"
                Disabled="@canReject.IsAllowed">
        <MudButton Color="Color.Error"
                   Disabled="@(!canReject.IsAllowed)"
                   OnClick="@HandleReject">
            <MudIcon Icon="@Icons.Material.Filled.Close" Class="mr-1" />
            却下
        </MudButton>
    </MudTooltip>

    <MudTooltip Text="@(canCancel.IsAllowed ? string.Empty : canCancel.Reason)"
                Disabled="@canCancel.IsAllowed">
        <MudButton Color="Color.Default"
                   Disabled="@(!canCancel.IsAllowed)"
                   OnClick="@HandleCancel">
            取消
        </MudButton>
    </MudTooltip>
</MudButtonGroup>
```

---

## 状態表示パターン

### 1. ステータスバッジ（Chip）

状態に応じた色分け表示。

```razor
@{
    var (color, icon) = GetStatusDisplay(order.Status);
}

<MudChip T="string"
         Color="@color"
         Icon="@icon"
         Size="Size.Small">
    @order.Status.ToDisplayString()
</MudChip>

@code {
    private (Color color, string icon) GetStatusDisplay(OrderStatus status) => status switch
    {
        OrderStatus.Pending => (Color.Warning, Icons.Material.Filled.HourglassEmpty),
        OrderStatus.Confirmed => (Color.Info, Icons.Material.Filled.CheckCircle),
        OrderStatus.Shipped => (Color.Primary, Icons.Material.Filled.LocalShipping),
        OrderStatus.Delivered => (Color.Success, Icons.Material.Filled.Done),
        OrderStatus.Cancelled => (Color.Error, Icons.Material.Filled.Cancel),
        _ => (Color.Default, Icons.Material.Filled.Help)
    };
}
```

---

### 2. アラート表示

重要な状態変化をアラートで表示。

```razor
@if (!canEdit.IsAllowed)
{
    <MudAlert Severity="Severity.Warning" Class="mb-4">
        @canEdit.Reason
    </MudAlert>
}
```

---

### 3. 条件付きセクション表示

操作可能な場合のみセクションを表示。

```razor
@{
    var canManage = entity.CanManage(currentUserId);
}

@if (canManage.IsAllowed)
{
    <MudCard Class="mt-4">
        <MudCardHeader>
            <CardHeaderContent>
                <MudText Typo="Typo.h6">管理操作</MudText>
            </CardHeaderContent>
        </MudCardHeader>
        <MudCardContent>
            @* 管理操作のUI *@
        </MudCardContent>
    </MudCard>
}
```

---

## 複合パターン

### 編集フォームの保護

編集可能かどうかで UI 全体を切り替え。

```razor
@{
    var canEdit = product.CanEdit();
}

@if (canEdit.IsAllowed)
{
    <EditForm Model="@model" OnValidSubmit="@HandleSubmit">
        <MudCard>
            <MudCardContent>
                <MudTextField T="string" @bind-Value="model.Name" Label="商品名" />
                <MudNumericField T="decimal" @bind-Value="model.Price" Label="価格" />
            </MudCardContent>
            <MudCardActions>
                <MudButton ButtonType="ButtonType.Submit" Color="Color.Primary">
                    保存
                </MudButton>
            </MudCardActions>
        </MudCard>
    </EditForm>
}
else
{
    <MudAlert Severity="Severity.Info">
        @canEdit.Reason
    </MudAlert>

    <MudCard Class="mt-4">
        <MudCardContent>
            <MudText><strong>商品名:</strong> @product.Name</MudText>
            <MudText><strong>価格:</strong> @product.Price.ToString("C")</MudText>
        </MudCardContent>
    </MudCard>
}
```

---

## Store との連携

### BoundaryDecision を Store で管理

```csharp
// State
public sealed record OrderDetailState
{
    public Order? Order { get; init; }
    public BoundaryDecision CanCancel { get; init; } = BoundaryDecision.Deny("読み込み中");
    public BoundaryDecision CanShip { get; init; } = BoundaryDecision.Deny("読み込み中");
    public bool IsLoading { get; init; }
}

// Store
public sealed class OrderDetailStore
{
    public async Task LoadAsync(OrderId orderId, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(orderId, ct);
        if (order == null)
        {
            _state = _state with { ErrorMessage = "注文が見つかりません" };
            return;
        }

        _state = _state with
        {
            Order = order,
            CanCancel = order.CanCancel(),
            CanShip = order.CanShip()
        };
    }
}
```

### Razor での使用

```razor
@inject OrderDetailStore Store

<MudTooltip Text="@(Store.State.CanCancel.IsAllowed ? string.Empty : Store.State.CanCancel.Reason)">
    <MudButton Disabled="@(!Store.State.CanCancel.IsAllowed)"
               OnClick="@HandleCancel">
        キャンセル
    </MudButton>
</MudTooltip>
```

---

## チェックリスト

```
□ CanXxx() の結果でボタンの Disabled を制御しているか？
□ 不許可理由をツールチップで表示しているか？
□ ローディング状態も Disabled 条件に含めているか？
□ ステータスに応じた色分けがされているか？
□ 複数操作がある場合、それぞれに CanXxx() を適用しているか？
□ 編集不可の場合、読み取り専用表示に切り替えているか？
```

---

## アンチパターン

### やってはいけないこと

```razor
@* ❌ UI 側で業務ロジックを判定 *@
@if (order.Status == OrderStatus.Pending)
{
    <MudButton OnClick="@HandleCancel">キャンセル</MudButton>
}

@* ✅ Entity の CanXxx() を使用 *@
@{
    var canCancel = order.CanCancel();
}
@if (canCancel.IsAllowed)
{
    <MudButton OnClick="@HandleCancel">キャンセル</MudButton>
}
```

**理由:** 業務ロジックは Entity に集約する。UI は `CanXxx()` の結果を使うだけ。
