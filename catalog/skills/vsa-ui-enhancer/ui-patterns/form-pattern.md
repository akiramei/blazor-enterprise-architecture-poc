# フォームパターン（Form Pattern）

作成・編集フォームの UI パターンを定義する。

---

## 基本構造

```razor
@page "/products/create"
@inject CreateProductStore Store
@inject CreateProductActions Actions
@inject NavigationManager Navigation

<PageTitle>商品登録</PageTitle>

<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-4">
    <MudCard>
        <MudCardHeader>
            <CardHeaderContent>
                <MudText Typo="Typo.h5">商品登録</MudText>
            </CardHeaderContent>
        </MudCardHeader>

        <EditForm Model="@Store.State.Model" OnValidSubmit="@HandleSubmit">
            <DataAnnotationsValidator />

            <MudCardContent>
                @* フォームフィールド *@
            </MudCardContent>

            <MudCardActions Class="d-flex justify-end gap-2 pa-4">
                @* アクションボタン *@
            </MudCardActions>
        </EditForm>
    </MudCard>
</MudContainer>
```

---

## フォームフィールド配置

### 単一カラム（シンプルフォーム）

```razor
<MudCardContent>
    <MudStack Spacing="3">
        <MudTextField T="string"
                      @bind-Value="model.Name"
                      Label="商品名"
                      Required="true"
                      Variant="Variant.Outlined"
                      For="@(() => model.Name)" />

        <MudTextField T="string"
                      @bind-Value="model.Description"
                      Label="説明"
                      Lines="3"
                      Variant="Variant.Outlined"
                      For="@(() => model.Description)" />

        <MudNumericField T="decimal"
                         @bind-Value="model.Price"
                         Label="価格"
                         Required="true"
                         Min="0"
                         Adornment="Adornment.Start"
                         AdornmentText="¥"
                         Variant="Variant.Outlined"
                         For="@(() => model.Price)" />
    </MudStack>
</MudCardContent>
```

---

### 複数カラム（グリッドレイアウト）

```razor
<MudCardContent>
    <MudGrid Spacing="3">
        <MudItem xs="12" md="6">
            <MudTextField T="string"
                          @bind-Value="model.FirstName"
                          Label="姓"
                          Required="true"
                          Variant="Variant.Outlined"
                          For="@(() => model.FirstName)" />
        </MudItem>

        <MudItem xs="12" md="6">
            <MudTextField T="string"
                          @bind-Value="model.LastName"
                          Label="名"
                          Required="true"
                          Variant="Variant.Outlined"
                          For="@(() => model.LastName)" />
        </MudItem>

        <MudItem xs="12" md="6">
            <MudTextField T="string"
                          @bind-Value="model.Email"
                          Label="メールアドレス"
                          InputType="InputType.Email"
                          Required="true"
                          Variant="Variant.Outlined"
                          For="@(() => model.Email)" />
        </MudItem>

        <MudItem xs="12" md="6">
            <MudTextField T="string"
                          @bind-Value="model.Phone"
                          Label="電話番号"
                          InputType="InputType.Telephone"
                          Variant="Variant.Outlined"
                          For="@(() => model.Phone)" />
        </MudItem>
    </MudGrid>
</MudCardContent>
```

---

### セクション分割（複雑なフォーム）

```razor
<MudCardContent>
    @* 基本情報セクション *@
    <MudText Typo="Typo.subtitle1" Class="mb-2">基本情報</MudText>
    <MudGrid Spacing="3">
        <MudItem xs="12">
            <MudTextField T="string" @bind-Value="model.Name" Label="商品名" />
        </MudItem>
    </MudGrid>

    <MudDivider Class="my-4" />

    @* 価格情報セクション *@
    <MudText Typo="Typo.subtitle1" Class="mb-2">価格情報</MudText>
    <MudGrid Spacing="3">
        <MudItem xs="12" md="6">
            <MudNumericField T="decimal" @bind-Value="model.Price" Label="販売価格" />
        </MudItem>
        <MudItem xs="12" md="6">
            <MudNumericField T="decimal?" @bind-Value="model.CostPrice" Label="原価" />
        </MudItem>
    </MudGrid>

    <MudDivider Class="my-4" />

    @* 在庫情報セクション *@
    <MudText Typo="Typo.subtitle1" Class="mb-2">在庫情報</MudText>
    <MudGrid Spacing="3">
        <MudItem xs="12" md="6">
            <MudNumericField T="int" @bind-Value="model.Stock" Label="在庫数" />
        </MudItem>
        <MudItem xs="12" md="6">
            <MudNumericField T="int" @bind-Value="model.ReorderLevel" Label="発注点" />
        </MudItem>
    </MudGrid>
</MudCardContent>
```

---

## バリデーション表示

### フィールドレベルバリデーション

`For` パラメータでフィールドを指定すると、そのフィールドのエラーが表示される。

```razor
<MudTextField T="string"
              @bind-Value="model.Name"
              Label="商品名"
              Required="true"
              RequiredError="商品名は必須です"
              Variant="Variant.Outlined"
              For="@(() => model.Name)" />
```

---

### サマリーバリデーション

フォーム全体のエラーをまとめて表示。

```razor
<MudCardContent>
    <ValidationSummary />

    @* または MudBlazor スタイルで *@
    @if (Store.State.Errors.Any())
    {
        <MudAlert Severity="Severity.Error" Class="mb-4">
            <ul class="mb-0">
                @foreach (var error in Store.State.Errors)
                {
                    <li>@error</li>
                }
            </ul>
        </MudAlert>
    }

    @* フォームフィールド *@
</MudCardContent>
```

---

### サーバーサイドエラー表示

Command 実行後のエラーを表示。

```razor
@if (!string.IsNullOrEmpty(Store.State.ErrorMessage))
{
    <MudAlert Severity="Severity.Error" Class="mb-4" ShowCloseIcon="true" CloseIconClicked="@ClearError">
        @Store.State.ErrorMessage
    </MudAlert>
}
```

---

## アクションボタン

### 基本パターン

```razor
<MudCardActions Class="d-flex justify-end gap-2 pa-4">
    <MudButton Color="Color.Default"
               Variant="Variant.Text"
               Href="/products">
        キャンセル
    </MudButton>

    <MudButton ButtonType="ButtonType.Submit"
               Color="Color.Primary"
               Variant="Variant.Filled"
               Disabled="@Store.State.IsSubmitting">
        @if (Store.State.IsSubmitting)
        {
            <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
        }
        保存
    </MudButton>
</MudCardActions>
```

---

### CanXxx() 連携

編集可否を Entity で判定する場合。

```razor
@{
    var canSave = Store.State.Entity?.CanSave() ?? BoundaryDecision.Deny("読み込み中");
    var isDisabled = !canSave.IsAllowed || Store.State.IsSubmitting;
}

<MudCardActions Class="d-flex justify-end gap-2 pa-4">
    <MudButton Color="Color.Default" Variant="Variant.Text" Href="/products">
        キャンセル
    </MudButton>

    <MudTooltip Text="@(canSave.IsAllowed ? string.Empty : canSave.Reason)"
                Disabled="@canSave.IsAllowed">
        <MudButton ButtonType="ButtonType.Submit"
                   Color="Color.Primary"
                   Variant="Variant.Filled"
                   Disabled="@isDisabled">
            @if (Store.State.IsSubmitting)
            {
                <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
            }
            保存
        </MudButton>
    </MudTooltip>
</MudCardActions>
```

---

## 完全な例

### 作成フォーム

```razor
@page "/products/create"
@inject CreateProductStore Store
@inject CreateProductActions Actions
@inject NavigationManager Navigation

<PageTitle>商品登録</PageTitle>

<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-4">
    <MudCard>
        <MudCardHeader>
            <CardHeaderContent>
                <MudText Typo="Typo.h5">商品登録</MudText>
            </CardHeaderContent>
        </MudCardHeader>

        <EditForm Model="@Store.State.Model" OnValidSubmit="@HandleSubmit">
            <DataAnnotationsValidator />

            <MudCardContent>
                @if (!string.IsNullOrEmpty(Store.State.ErrorMessage))
                {
                    <MudAlert Severity="Severity.Error" Class="mb-4">
                        @Store.State.ErrorMessage
                    </MudAlert>
                }

                <MudStack Spacing="3">
                    <MudTextField T="string"
                                  @bind-Value="Store.State.Model.Name"
                                  Label="商品名"
                                  Required="true"
                                  Variant="Variant.Outlined"
                                  For="@(() => Store.State.Model.Name)" />

                    <MudTextField T="string"
                                  @bind-Value="Store.State.Model.Description"
                                  Label="説明"
                                  Lines="3"
                                  Variant="Variant.Outlined"
                                  For="@(() => Store.State.Model.Description)" />

                    <MudNumericField T="decimal"
                                     @bind-Value="Store.State.Model.Price"
                                     Label="価格"
                                     Required="true"
                                     Min="0"
                                     Adornment="Adornment.Start"
                                     AdornmentText="¥"
                                     Variant="Variant.Outlined"
                                     For="@(() => Store.State.Model.Price)" />

                    <MudNumericField T="int"
                                     @bind-Value="Store.State.Model.Stock"
                                     Label="在庫数"
                                     Required="true"
                                     Min="0"
                                     Variant="Variant.Outlined"
                                     For="@(() => Store.State.Model.Stock)" />

                    <MudSelect T="Guid?"
                               @bind-Value="Store.State.Model.CategoryId"
                               Label="カテゴリ"
                               Variant="Variant.Outlined"
                               AnchorOrigin="Origin.BottomCenter">
                        <MudSelectItem T="Guid?" Value="@null">選択してください</MudSelectItem>
                        @foreach (var category in Store.State.Categories)
                        {
                            <MudSelectItem T="Guid?" Value="@category.Id">@category.Name</MudSelectItem>
                        }
                    </MudSelect>
                </MudStack>
            </MudCardContent>

            <MudCardActions Class="d-flex justify-end gap-2 pa-4">
                <MudButton Color="Color.Default"
                           Variant="Variant.Text"
                           Href="/products">
                    キャンセル
                </MudButton>

                <MudButton ButtonType="ButtonType.Submit"
                           Color="Color.Primary"
                           Variant="Variant.Filled"
                           Disabled="@Store.State.IsSubmitting">
                    @if (Store.State.IsSubmitting)
                    {
                        <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
                    }
                    登録
                </MudButton>
            </MudCardActions>
        </EditForm>
    </MudCard>
</MudContainer>

@code {
    protected override async Task OnInitializedAsync()
    {
        Store.OnChangeAsync += StateHasChangedAsync;
        await Actions.InitializeAsync();
    }

    private async Task HandleSubmit()
    {
        var result = await Actions.CreateAsync();
        if (result.IsSuccess)
        {
            Navigation.NavigateTo($"/products/{result.Value}");
        }
    }

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
□ MudCard でフォームを囲んでいるか？
□ EditForm + DataAnnotationsValidator を使用しているか？
□ MudTextField/MudNumericField/MudSelect を使用しているか？
□ For パラメータでバリデーションを紐付けているか？
□ ローディング中はボタンを Disabled にしているか？
□ エラーメッセージを MudAlert で表示しているか？
□ キャンセルボタンがあるか？
□ レスポンシブ対応（MudGrid + MudItem）しているか？
```
