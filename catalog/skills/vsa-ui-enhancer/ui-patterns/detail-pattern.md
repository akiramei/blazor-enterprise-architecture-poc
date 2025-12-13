# 詳細パターン（Detail Pattern）

詳細表示 + 操作ボタンの UI パターンを定義する。

---

## 基本構造

```razor
@page "/products/{Id:guid}"
@inject ProductDetailStore Store
@inject ProductDetailActions Actions
@inject NavigationManager Navigation

<PageTitle>商品詳細</PageTitle>

<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-4">
    @if (Store.State.IsLoading)
    {
        <MudProgressLinear Indeterminate="true" Color="Color.Primary" />
    }
    else if (Store.State.Product is not null)
    {
        @* ヘッダー *@
        @* 詳細カード *@
        @* 操作ボタン *@
    }
    else
    {
        @* エラー or 見つからない *@
    }
</MudContainer>
```

---

## ヘッダー

### タイトル + 戻るボタン

```razor
<MudStack Row="true" Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center" Class="mb-4">
    <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
        <MudIconButton Icon="@Icons.Material.Filled.ArrowBack"
                       Href="/products"
                       Size="Size.Medium" />
        <MudText Typo="Typo.h5">@Store.State.Product.Name</MudText>
    </MudStack>

    <MudChip T="string"
             Color="@GetStatusColor(Store.State.Product.Status)"
             Size="Size.Medium">
        @Store.State.Product.Status.ToDisplayString()
    </MudChip>
</MudStack>
```

---

### ブレッドクラム

```razor
<MudBreadcrumbs Items="_breadcrumbs" Class="mb-4" />

@code {
    private List<BreadcrumbItem> _breadcrumbs = new()
    {
        new BreadcrumbItem("ホーム", href: "/"),
        new BreadcrumbItem("商品一覧", href: "/products"),
        new BreadcrumbItem("商品詳細", href: null, disabled: true)
    };
}
```

---

## 詳細カード

### 基本レイアウト

```razor
<MudCard>
    <MudCardHeader>
        <CardHeaderContent>
            <MudText Typo="Typo.h6">基本情報</MudText>
        </CardHeaderContent>
        <CardHeaderActions>
            <MudIconButton Icon="@Icons.Material.Filled.Edit"
                           Color="Color.Primary"
                           Href="@($"/products/{Id}/edit")" />
        </CardHeaderActions>
    </MudCardHeader>

    <MudCardContent>
        <MudGrid Spacing="3">
            <MudItem xs="12" md="6">
                <MudText Typo="Typo.caption" Color="Color.Default">商品名</MudText>
                <MudText Typo="Typo.body1">@Store.State.Product.Name</MudText>
            </MudItem>

            <MudItem xs="12" md="6">
                <MudText Typo="Typo.caption" Color="Color.Default">カテゴリ</MudText>
                <MudText Typo="Typo.body1">@Store.State.Product.CategoryName</MudText>
            </MudItem>

            <MudItem xs="12" md="6">
                <MudText Typo="Typo.caption" Color="Color.Default">価格</MudText>
                <MudText Typo="Typo.body1">@Store.State.Product.Price.ToString("C")</MudText>
            </MudItem>

            <MudItem xs="12" md="6">
                <MudText Typo="Typo.caption" Color="Color.Default">在庫数</MudText>
                <MudText Typo="Typo.body1">@Store.State.Product.Stock</MudText>
            </MudItem>

            <MudItem xs="12">
                <MudText Typo="Typo.caption" Color="Color.Default">説明</MudText>
                <MudText Typo="Typo.body1">
                    @(string.IsNullOrEmpty(Store.State.Product.Description) ? "（なし）" : Store.State.Product.Description)
                </MudText>
            </MudItem>
        </MudGrid>
    </MudCardContent>
</MudCard>
```

---

### 複数セクション

```razor
@* 基本情報 *@
<MudCard Class="mb-4">
    <MudCardHeader>
        <CardHeaderContent>
            <MudText Typo="Typo.h6">基本情報</MudText>
        </CardHeaderContent>
    </MudCardHeader>
    <MudCardContent>
        @* 基本情報フィールド *@
    </MudCardContent>
</MudCard>

@* 価格・在庫情報 *@
<MudCard Class="mb-4">
    <MudCardHeader>
        <CardHeaderContent>
            <MudText Typo="Typo.h6">価格・在庫</MudText>
        </CardHeaderContent>
    </MudCardHeader>
    <MudCardContent>
        @* 価格・在庫フィールド *@
    </MudCardContent>
</MudCard>

@* 履歴情報 *@
<MudCard>
    <MudCardHeader>
        <CardHeaderContent>
            <MudText Typo="Typo.h6">更新履歴</MudText>
        </CardHeaderContent>
    </MudCardHeader>
    <MudCardContent>
        <MudSimpleTable Dense="true">
            <tbody>
                <tr>
                    <td><MudText Typo="Typo.caption">作成日時</MudText></td>
                    <td>@Store.State.Product.CreatedAt.ToString("yyyy/MM/dd HH:mm")</td>
                </tr>
                <tr>
                    <td><MudText Typo="Typo.caption">更新日時</MudText></td>
                    <td>@Store.State.Product.UpdatedAt.ToString("yyyy/MM/dd HH:mm")</td>
                </tr>
            </tbody>
        </MudSimpleTable>
    </MudCardContent>
</MudCard>
```

---

### タブ形式

```razor
<MudTabs Elevation="2" Rounded="true" ApplyEffectsToContainer="true" PanelClass="pa-4">
    <MudTabPanel Text="基本情報">
        @* 基本情報 *@
    </MudTabPanel>

    <MudTabPanel Text="価格・在庫">
        @* 価格・在庫情報 *@
    </MudTabPanel>

    <MudTabPanel Text="履歴">
        @* 履歴情報 *@
    </MudTabPanel>
</MudTabs>
```

---

## 操作ボタン

### 固定フッター

```razor
<MudAppBar Bottom="true" Fixed="true" Color="Color.Surface" Elevation="4">
    <MudSpacer />
    <MudStack Row="true" Spacing="2">
        <MudButton Color="Color.Default"
                   Variant="Variant.Text"
                   Href="/products">
            一覧に戻る
        </MudButton>

        <MudButton Color="Color.Primary"
                   Variant="Variant.Outlined"
                   StartIcon="@Icons.Material.Filled.Edit"
                   Href="@($"/products/{Id}/edit")">
            編集
        </MudButton>

        @{
            var canDelete = Store.State.CanDelete;
        }
        <MudTooltip Text="@(canDelete.IsAllowed ? string.Empty : canDelete.Reason)"
                    Disabled="@canDelete.IsAllowed">
            <MudButton Color="Color.Error"
                       Variant="Variant.Filled"
                       StartIcon="@Icons.Material.Filled.Delete"
                       Disabled="@(!canDelete.IsAllowed)"
                       OnClick="@HandleDelete">
                削除
            </MudButton>
        </MudTooltip>
    </MudStack>
</MudAppBar>
```

---

### カード内アクション

```razor
<MudCard>
    <MudCardContent>
        @* 詳細情報 *@
    </MudCardContent>

    <MudCardActions Class="d-flex justify-end gap-2 pa-4">
        <MudButton Color="Color.Default"
                   Variant="Variant.Text"
                   Href="/products">
            一覧に戻る
        </MudButton>

        <MudButton Color="Color.Primary"
                   Variant="Variant.Outlined"
                   StartIcon="@Icons.Material.Filled.Edit"
                   Href="@($"/products/{Id}/edit")">
            編集
        </MudButton>

        @{
            var canDelete = Store.State.CanDelete;
        }
        <MudTooltip Text="@(canDelete.IsAllowed ? string.Empty : canDelete.Reason)"
                    Disabled="@canDelete.IsAllowed">
            <MudButton Color="Color.Error"
                       Variant="Variant.Filled"
                       StartIcon="@Icons.Material.Filled.Delete"
                       Disabled="@(!canDelete.IsAllowed)"
                       OnClick="@HandleDelete">
                削除
            </MudButton>
        </MudTooltip>
    </MudCardActions>
</MudCard>
```

---

### 複数操作（ワークフロー）

```razor
@{
    var canApprove = Store.State.CanApprove;
    var canReject = Store.State.CanReject;
    var canCancel = Store.State.CanCancel;
}

<MudCard Class="mt-4">
    <MudCardHeader>
        <CardHeaderContent>
            <MudText Typo="Typo.h6">操作</MudText>
        </CardHeaderContent>
    </MudCardHeader>

    <MudCardContent>
        <MudStack Row="true" Spacing="2" Justify="Justify.FlexEnd">
            <MudTooltip Text="@(canApprove.IsAllowed ? string.Empty : canApprove.Reason)"
                        Disabled="@canApprove.IsAllowed">
                <MudButton Color="Color.Success"
                           Variant="Variant.Filled"
                           StartIcon="@Icons.Material.Filled.Check"
                           Disabled="@(!canApprove.IsAllowed || Store.State.IsProcessing)"
                           OnClick="@HandleApprove">
                    @if (Store.State.IsProcessing)
                    {
                        <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
                    }
                    承認
                </MudButton>
            </MudTooltip>

            <MudTooltip Text="@(canReject.IsAllowed ? string.Empty : canReject.Reason)"
                        Disabled="@canReject.IsAllowed">
                <MudButton Color="Color.Error"
                           Variant="Variant.Outlined"
                           StartIcon="@Icons.Material.Filled.Close"
                           Disabled="@(!canReject.IsAllowed || Store.State.IsProcessing)"
                           OnClick="@HandleReject">
                    却下
                </MudButton>
            </MudTooltip>

            <MudTooltip Text="@(canCancel.IsAllowed ? string.Empty : canCancel.Reason)"
                        Disabled="@canCancel.IsAllowed">
                <MudButton Color="Color.Default"
                           Variant="Variant.Text"
                           Disabled="@(!canCancel.IsAllowed || Store.State.IsProcessing)"
                           OnClick="@HandleCancel">
                    取消
                </MudButton>
            </MudTooltip>
        </MudStack>
    </MudCardContent>
</MudCard>
```

---

## 状態表示

### アラート

```razor
@if (!Store.State.CanEdit.IsAllowed)
{
    <MudAlert Severity="Severity.Warning" Class="mb-4">
        @Store.State.CanEdit.Reason
    </MudAlert>
}
```

---

### タイムライン

```razor
<MudCard Class="mt-4">
    <MudCardHeader>
        <CardHeaderContent>
            <MudText Typo="Typo.h6">ステータス履歴</MudText>
        </CardHeaderContent>
    </MudCardHeader>
    <MudCardContent>
        <MudTimeline TimelinePosition="TimelinePosition.Start">
            @foreach (var history in Store.State.StatusHistory)
            {
                <MudTimelineItem Color="@GetStatusColor(history.Status)" Size="Size.Small">
                    <ItemContent>
                        <MudText Typo="Typo.body2">@history.Status.ToDisplayString()</MudText>
                        <MudText Typo="Typo.caption" Color="Color.Default">
                            @history.ChangedAt.ToString("yyyy/MM/dd HH:mm") - @history.ChangedBy
                        </MudText>
                    </ItemContent>
                </MudTimelineItem>
            }
        </MudTimeline>
    </MudCardContent>
</MudCard>
```

---

## エラー・見つからない状態

```razor
@if (!string.IsNullOrEmpty(Store.State.ErrorMessage))
{
    <MudAlert Severity="Severity.Error" Class="mb-4">
        @Store.State.ErrorMessage
    </MudAlert>
}
else if (Store.State.Product is null && !Store.State.IsLoading)
{
    <MudCard>
        <MudCardContent>
            <MudStack AlignItems="AlignItems.Center" Class="pa-8">
                <MudIcon Icon="@Icons.Material.Filled.SearchOff" Size="Size.Large" Color="Color.Default" />
                <MudText Typo="Typo.h6" Color="Color.Default">商品が見つかりません</MudText>
                <MudButton Color="Color.Primary"
                           Variant="Variant.Filled"
                           Href="/products"
                           Class="mt-4">
                    一覧に戻る
                </MudButton>
            </MudStack>
        </MudCardContent>
    </MudCard>
}
```

---

## 完全な例

```razor
@page "/products/{Id:guid}"
@inject ProductDetailStore Store
@inject ProductDetailActions Actions
@inject NavigationManager Navigation
@inject IDialogService DialogService
@implements IDisposable

<PageTitle>商品詳細</PageTitle>

<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-4">
    @if (Store.State.IsLoading)
    {
        <MudProgressLinear Indeterminate="true" Color="Color.Primary" />
    }
    else if (Store.State.Product is not null)
    {
        <MudStack Row="true" Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center" Class="mb-4">
            <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
                <MudIconButton Icon="@Icons.Material.Filled.ArrowBack" Href="/products" />
                <MudText Typo="Typo.h5">@Store.State.Product.Name</MudText>
            </MudStack>
            <MudChip T="string" Color="@GetStatusColor(Store.State.Product.Status)" Size="Size.Medium">
                @Store.State.Product.Status.ToDisplayString()
            </MudChip>
        </MudStack>

        @if (!Store.State.CanEdit.IsAllowed)
        {
            <MudAlert Severity="Severity.Warning" Class="mb-4">
                @Store.State.CanEdit.Reason
            </MudAlert>
        }

        <MudCard>
            <MudCardHeader>
                <CardHeaderContent>
                    <MudText Typo="Typo.h6">基本情報</MudText>
                </CardHeaderContent>
                <CardHeaderActions>
                    @if (Store.State.CanEdit.IsAllowed)
                    {
                        <MudIconButton Icon="@Icons.Material.Filled.Edit"
                                       Color="Color.Primary"
                                       Href="@($"/products/{Id}/edit")" />
                    }
                </CardHeaderActions>
            </MudCardHeader>

            <MudCardContent>
                <MudGrid Spacing="3">
                    <MudItem xs="12" md="6">
                        <MudText Typo="Typo.caption" Color="Color.Default">商品名</MudText>
                        <MudText Typo="Typo.body1">@Store.State.Product.Name</MudText>
                    </MudItem>
                    <MudItem xs="12" md="6">
                        <MudText Typo="Typo.caption" Color="Color.Default">価格</MudText>
                        <MudText Typo="Typo.body1">@Store.State.Product.Price.ToString("C")</MudText>
                    </MudItem>
                    <MudItem xs="12" md="6">
                        <MudText Typo="Typo.caption" Color="Color.Default">在庫数</MudText>
                        <MudText Typo="Typo.body1">@Store.State.Product.Stock</MudText>
                    </MudItem>
                    <MudItem xs="12">
                        <MudText Typo="Typo.caption" Color="Color.Default">説明</MudText>
                        <MudText Typo="Typo.body1">
                            @(string.IsNullOrEmpty(Store.State.Product.Description) ? "（なし）" : Store.State.Product.Description)
                        </MudText>
                    </MudItem>
                </MudGrid>
            </MudCardContent>

            <MudCardActions Class="d-flex justify-end gap-2 pa-4">
                <MudButton Color="Color.Default" Variant="Variant.Text" Href="/products">
                    一覧に戻る
                </MudButton>

                @if (Store.State.CanEdit.IsAllowed)
                {
                    <MudButton Color="Color.Primary"
                               Variant="Variant.Outlined"
                               StartIcon="@Icons.Material.Filled.Edit"
                               Href="@($"/products/{Id}/edit")">
                        編集
                    </MudButton>
                }

                @{
                    var canDelete = Store.State.CanDelete;
                }
                <MudTooltip Text="@(canDelete.IsAllowed ? string.Empty : canDelete.Reason)"
                            Disabled="@canDelete.IsAllowed">
                    <MudButton Color="Color.Error"
                               Variant="Variant.Filled"
                               StartIcon="@Icons.Material.Filled.Delete"
                               Disabled="@(!canDelete.IsAllowed)"
                               OnClick="@HandleDelete">
                        削除
                    </MudButton>
                </MudTooltip>
            </MudCardActions>
        </MudCard>
    }
    else
    {
        <MudCard>
            <MudCardContent>
                <MudStack AlignItems="AlignItems.Center" Class="pa-8">
                    <MudIcon Icon="@Icons.Material.Filled.SearchOff" Size="Size.Large" Color="Color.Default" />
                    <MudText Typo="Typo.h6" Color="Color.Default">商品が見つかりません</MudText>
                    <MudButton Color="Color.Primary" Variant="Variant.Filled" Href="/products" Class="mt-4">
                        一覧に戻る
                    </MudButton>
                </MudStack>
            </MudCardContent>
        </MudCard>
    }
</MudContainer>

@code {
    [Parameter] public Guid Id { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Store.OnChangeAsync += StateHasChangedAsync;
        await Actions.LoadAsync(Id);
    }

    private async Task HandleDelete()
    {
        var confirmed = await DialogService.ShowMessageBox(
            "削除確認",
            "この商品を削除しますか？この操作は取り消せません。",
            yesText: "削除", cancelText: "キャンセル");

        if (confirmed == true)
        {
            var result = await Actions.DeleteAsync();
            if (result.IsSuccess)
            {
                Navigation.NavigateTo("/products");
            }
        }
    }

    private Color GetStatusColor(ProductStatus status) => status switch
    {
        ProductStatus.Active => Color.Success,
        ProductStatus.Inactive => Color.Default,
        _ => Color.Default
    };

    private Task StateHasChangedAsync()
    {
        StateHasChanged();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Store.OnChangeAsync -= StateHasChangedAsync;
    }
}
```

---

## チェックリスト

```
□ 戻るボタンがあるか？
□ ステータスがバッジで表示されているか？
□ 詳細情報が見やすく配置されているか？
□ CanXxx() で操作ボタンが制御されているか？
□ 不許可理由がツールチップで表示されているか？
□ ローディング状態が表示されるか？
□ 見つからない場合の表示があるか？
□ 削除時に確認ダイアログがあるか？
```
