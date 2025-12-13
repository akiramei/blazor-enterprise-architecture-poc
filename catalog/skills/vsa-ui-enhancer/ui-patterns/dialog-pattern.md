# ダイアログパターン（Dialog Pattern）

確認・入力ダイアログの UI パターンを定義する。

---

## MudBlazor ダイアログの基本

MudBlazor では `IDialogService` を使用してダイアログを表示する。

```csharp
@inject IDialogService DialogService
```

---

## 確認ダイアログ

### シンプルな確認

```csharp
private async Task HandleDelete()
{
    var confirmed = await DialogService.ShowMessageBox(
        "削除確認",
        "このアイテムを削除しますか？",
        yesText: "削除",
        cancelText: "キャンセル");

    if (confirmed == true)
    {
        await Actions.DeleteAsync();
    }
}
```

---

### 危険な操作の確認

```csharp
private async Task HandleDelete()
{
    var confirmed = await DialogService.ShowMessageBox(
        "削除確認",
        (MarkupString)"<p>この商品を削除しますか？</p><p><strong>この操作は取り消せません。</strong></p>",
        yesText: "削除",
        cancelText: "キャンセル",
        options: new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small
        });

    if (confirmed == true)
    {
        await Actions.DeleteAsync();
    }
}
```

---

### 3択の確認

```csharp
private async Task HandleSaveOrDiscard()
{
    var result = await DialogService.ShowMessageBox(
        "変更の保存",
        "変更内容を保存しますか？",
        yesText: "保存",
        noText: "破棄",
        cancelText: "キャンセル");

    if (result == true)
    {
        await Actions.SaveAsync();
    }
    else if (result == false)
    {
        await Actions.DiscardAsync();
    }
    // null の場合はキャンセル（何もしない）
}
```

---

## カスタムダイアログ

### ダイアログコンポーネントの作成

```razor
@* ConfirmDeleteDialog.razor *@
<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">
            <MudIcon Icon="@Icons.Material.Filled.Warning" Color="Color.Error" Class="mr-2" />
            削除確認
        </MudText>
    </TitleContent>

    <DialogContent>
        <MudText>
            「@ItemName」を削除しますか？
        </MudText>
        <MudText Typo="Typo.body2" Color="Color.Default" Class="mt-2">
            この操作は取り消せません。
        </MudText>
    </DialogContent>

    <DialogActions>
        <MudButton OnClick="Cancel">キャンセル</MudButton>
        <MudButton Color="Color.Error"
                   Variant="Variant.Filled"
                   OnClick="Submit">
            削除
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public string ItemName { get; set; } = string.Empty;

    private void Cancel() => MudDialog.Cancel();
    private void Submit() => MudDialog.Close(DialogResult.Ok(true));
}
```

### ダイアログの呼び出し

```csharp
private async Task HandleDelete(ProductDto product)
{
    var parameters = new DialogParameters<ConfirmDeleteDialog>
    {
        { x => x.ItemName, product.Name }
    };

    var dialog = await DialogService.ShowAsync<ConfirmDeleteDialog>("削除確認", parameters);
    var result = await dialog.Result;

    if (!result.Canceled)
    {
        await Actions.DeleteAsync(product.Id);
    }
}
```

---

## 入力ダイアログ

### テキスト入力

```razor
@* InputReasonDialog.razor *@
<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">却下理由</MudText>
    </TitleContent>

    <DialogContent>
        <MudTextField T="string"
                      @bind-Value="_reason"
                      Label="理由"
                      Required="true"
                      RequiredError="理由を入力してください"
                      Lines="3"
                      Variant="Variant.Outlined"
                      Class="mt-2" />
    </DialogContent>

    <DialogActions>
        <MudButton OnClick="Cancel">キャンセル</MudButton>
        <MudButton Color="Color.Primary"
                   Variant="Variant.Filled"
                   Disabled="@string.IsNullOrWhiteSpace(_reason)"
                   OnClick="Submit">
            確定
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    private string _reason = string.Empty;

    private void Cancel() => MudDialog.Cancel();
    private void Submit() => MudDialog.Close(DialogResult.Ok(_reason));
}
```

### 呼び出しと結果の取得

```csharp
private async Task HandleReject()
{
    var dialog = await DialogService.ShowAsync<InputReasonDialog>("却下理由");
    var result = await dialog.Result;

    if (!result.Canceled && result.Data is string reason)
    {
        await Actions.RejectAsync(reason);
    }
}
```

---

## フォームダイアログ

### 編集フォーム

```razor
@* EditProductDialog.razor *@
<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">商品編集</MudText>
    </TitleContent>

    <DialogContent>
        <EditForm Model="@_model" OnValidSubmit="@Submit">
            <DataAnnotationsValidator />

            <MudStack Spacing="3">
                <MudTextField T="string"
                              @bind-Value="_model.Name"
                              Label="商品名"
                              Required="true"
                              Variant="Variant.Outlined"
                              For="@(() => _model.Name)" />

                <MudNumericField T="decimal"
                                 @bind-Value="_model.Price"
                                 Label="価格"
                                 Required="true"
                                 Min="0"
                                 Variant="Variant.Outlined"
                                 For="@(() => _model.Price)" />

                <MudNumericField T="int"
                                 @bind-Value="_model.Stock"
                                 Label="在庫"
                                 Required="true"
                                 Min="0"
                                 Variant="Variant.Outlined"
                                 For="@(() => _model.Stock)" />
            </MudStack>
        </EditForm>
    </DialogContent>

    <DialogActions>
        <MudButton OnClick="Cancel">キャンセル</MudButton>
        <MudButton Color="Color.Primary"
                   Variant="Variant.Filled"
                   OnClick="Submit">
            保存
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public ProductEditModel? InitialModel { get; set; }

    private ProductEditModel _model = new();

    protected override void OnInitialized()
    {
        if (InitialModel is not null)
        {
            _model = InitialModel with { };  // コピー
        }
    }

    private void Cancel() => MudDialog.Cancel();
    private void Submit() => MudDialog.Close(DialogResult.Ok(_model));
}
```

---

## CanXxx() 連携

### ダイアログ内での Boundary チェック

```razor
@* ApprovalDialog.razor *@
<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">承認確認</MudText>
    </TitleContent>

    <DialogContent>
        @if (!_canApprove.IsAllowed)
        {
            <MudAlert Severity="Severity.Warning" Class="mb-4">
                @_canApprove.Reason
            </MudAlert>
        }

        <MudText>この申請を承認しますか？</MudText>

        <MudTextField T="string"
                      @bind-Value="_comment"
                      Label="コメント（任意）"
                      Lines="2"
                      Variant="Variant.Outlined"
                      Class="mt-4" />
    </DialogContent>

    <DialogActions>
        <MudButton OnClick="Cancel">キャンセル</MudButton>
        <MudTooltip Text="@(_canApprove.IsAllowed ? string.Empty : _canApprove.Reason)"
                    Disabled="@_canApprove.IsAllowed">
            <MudButton Color="Color.Success"
                       Variant="Variant.Filled"
                       Disabled="@(!_canApprove.IsAllowed)"
                       OnClick="Submit">
                承認
            </MudButton>
        </MudTooltip>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public BoundaryDecision CanApprove { get; set; } = BoundaryDecision.Allow();

    private BoundaryDecision _canApprove = BoundaryDecision.Allow();
    private string _comment = string.Empty;

    protected override void OnInitialized()
    {
        _canApprove = CanApprove;
    }

    private void Cancel() => MudDialog.Cancel();
    private void Submit() => MudDialog.Close(DialogResult.Ok(_comment));
}
```

---

## ダイアログオプション

### サイズ設定

```csharp
var options = new DialogOptions
{
    MaxWidth = MaxWidth.Small,      // ExtraSmall, Small, Medium, Large, ExtraLarge
    FullWidth = true,               // 幅いっぱいに広げる
    CloseButton = true,             // 閉じるボタン表示
    CloseOnEscapeKey = true,        // ESC で閉じる
    DisableBackdropClick = false,   // 背景クリックで閉じる
    NoHeader = false,               // ヘッダー非表示
    Position = DialogPosition.Center // Center, TopCenter, etc.
};

var dialog = await DialogService.ShowAsync<MyDialog>("タイトル", parameters, options);
```

---

### フルスクリーン（モバイル対応）

```csharp
var options = new DialogOptions
{
    FullScreen = true  // モバイルではフルスクリーン
};
```

---

## ローディング状態

### 送信中のローディング

```razor
<DialogActions>
    <MudButton OnClick="Cancel" Disabled="@_isSubmitting">キャンセル</MudButton>
    <MudButton Color="Color.Primary"
               Variant="Variant.Filled"
               Disabled="@_isSubmitting"
               OnClick="Submit">
        @if (_isSubmitting)
        {
            <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
        }
        保存
    </MudButton>
</DialogActions>

@code {
    private bool _isSubmitting;

    private async Task Submit()
    {
        _isSubmitting = true;
        StateHasChanged();

        try
        {
            // 処理実行
            await Task.Delay(1000);  // 実際の処理
            MudDialog.Close(DialogResult.Ok(true));
        }
        finally
        {
            _isSubmitting = false;
        }
    }
}
```

---

## 完全な例

### 削除確認ダイアログ

```razor
@* DeleteConfirmDialog.razor *@
<MudDialog>
    <TitleContent>
        <MudStack Row="true" AlignItems="AlignItems.Center">
            <MudIcon Icon="@Icons.Material.Filled.Warning" Color="Color.Error" />
            <MudText Typo="Typo.h6">削除確認</MudText>
        </MudStack>
    </TitleContent>

    <DialogContent>
        <MudText>
            「<strong>@ItemName</strong>」を削除しますか？
        </MudText>

        @if (!string.IsNullOrEmpty(WarningMessage))
        {
            <MudAlert Severity="Severity.Warning" Class="mt-4">
                @WarningMessage
            </MudAlert>
        }

        <MudText Typo="Typo.body2" Color="Color.Default" Class="mt-4">
            この操作は取り消せません。
        </MudText>
    </DialogContent>

    <DialogActions>
        <MudButton OnClick="Cancel" Disabled="@_isDeleting">
            キャンセル
        </MudButton>
        <MudButton Color="Color.Error"
                   Variant="Variant.Filled"
                   Disabled="@_isDeleting"
                   OnClick="Delete">
            @if (_isDeleting)
            {
                <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
            }
            削除
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public string ItemName { get; set; } = string.Empty;
    [Parameter] public string? WarningMessage { get; set; }
    [Parameter] public Func<Task<bool>>? OnDelete { get; set; }

    private bool _isDeleting;

    private void Cancel() => MudDialog.Cancel();

    private async Task Delete()
    {
        if (OnDelete is null)
        {
            MudDialog.Close(DialogResult.Ok(true));
            return;
        }

        _isDeleting = true;
        StateHasChanged();

        try
        {
            var success = await OnDelete();
            if (success)
            {
                MudDialog.Close(DialogResult.Ok(true));
            }
        }
        finally
        {
            _isDeleting = false;
            StateHasChanged();
        }
    }
}
```

### 呼び出し側

```csharp
private async Task HandleDelete(ProductDto product)
{
    var canDelete = product.CanDelete();

    if (!canDelete.IsAllowed)
    {
        await DialogService.ShowMessageBox(
            "削除できません",
            canDelete.Reason,
            yesText: "OK");
        return;
    }

    var parameters = new DialogParameters<DeleteConfirmDialog>
    {
        { x => x.ItemName, product.Name },
        { x => x.WarningMessage, product.HasOrders ? "この商品には注文履歴があります。" : null },
        { x => x.OnDelete, async () => await Actions.DeleteAsync(product.Id) }
    };

    var options = new DialogOptions
    {
        CloseButton = true,
        MaxWidth = MaxWidth.Small
    };

    var dialog = await DialogService.ShowAsync<DeleteConfirmDialog>("削除確認", parameters, options);
    var result = await dialog.Result;

    if (!result.Canceled)
    {
        Navigation.NavigateTo("/products");
    }
}
```

---

## チェックリスト

```
□ 危険な操作には確認ダイアログがあるか？
□ ダイアログにタイトルがあるか？
□ キャンセルボタンがあるか？
□ CanXxx() の結果でボタンが制御されているか？
□ ローディング中はボタンが Disabled になるか？
□ 入力ダイアログにバリデーションがあるか？
□ 適切なサイズ（MaxWidth）が設定されているか？
□ ESC キーで閉じられるか？
```
