# 一覧パターン（List Pattern）

データ一覧表示の UI パターンを定義する。

---

## 基本構造

```razor
@page "/products"
@inject ProductListStore Store
@inject ProductListActions Actions

<PageTitle>商品一覧</PageTitle>

<MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="mt-4">
    @* ヘッダー（タイトル + アクション） *@
    <MudStack Row="true" Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center" Class="mb-4">
        <MudText Typo="Typo.h5">商品一覧</MudText>
        <MudButton Color="Color.Primary"
                   Variant="Variant.Filled"
                   StartIcon="@Icons.Material.Filled.Add"
                   Href="/products/create">
            新規登録
        </MudButton>
    </MudStack>

    @* 検索・フィルター *@
    @* テーブル *@
</MudContainer>
```

---

## 検索・フィルター

### シンプルな検索

```razor
<MudCard Class="mb-4">
    <MudCardContent>
        <MudGrid>
            <MudItem xs="12" md="6">
                <MudTextField T="string"
                              @bind-Value="Store.State.SearchTerm"
                              Label="検索"
                              Placeholder="商品名で検索..."
                              Variant="Variant.Outlined"
                              Adornment="Adornment.Start"
                              AdornmentIcon="@Icons.Material.Filled.Search"
                              Immediate="true"
                              DebounceInterval="300"
                              OnDebounceIntervalElapsed="@HandleSearch" />
            </MudItem>
        </MudGrid>
    </MudCardContent>
</MudCard>
```

---

### 複数条件フィルター

```razor
<MudExpansionPanels Class="mb-4">
    <MudExpansionPanel Text="検索条件" IsInitiallyExpanded="@Store.State.HasActiveFilters">
        <MudGrid Spacing="3">
            <MudItem xs="12" md="4">
                <MudTextField T="string"
                              @bind-Value="Store.State.SearchTerm"
                              Label="キーワード"
                              Variant="Variant.Outlined" />
            </MudItem>

            <MudItem xs="12" md="4">
                <MudSelect T="Guid?"
                           @bind-Value="Store.State.CategoryId"
                           Label="カテゴリ"
                           Variant="Variant.Outlined"
                           Clearable="true">
                    @foreach (var category in Store.State.Categories)
                    {
                        <MudSelectItem T="Guid?" Value="@category.Id">@category.Name</MudSelectItem>
                    }
                </MudSelect>
            </MudItem>

            <MudItem xs="12" md="4">
                <MudSelect T="ProductStatus?"
                           @bind-Value="Store.State.Status"
                           Label="ステータス"
                           Variant="Variant.Outlined"
                           Clearable="true">
                    @foreach (var status in Enum.GetValues<ProductStatus>())
                    {
                        <MudSelectItem T="ProductStatus?" Value="@status">@status.ToDisplayString()</MudSelectItem>
                    }
                </MudSelect>
            </MudItem>

            <MudItem xs="12" Class="d-flex justify-end gap-2">
                <MudButton Color="Color.Default"
                           Variant="Variant.Text"
                           OnClick="@HandleClearFilters">
                    クリア
                </MudButton>
                <MudButton Color="Color.Primary"
                           Variant="Variant.Filled"
                           OnClick="@HandleSearch">
                    検索
                </MudButton>
            </MudItem>
        </MudGrid>
    </MudExpansionPanel>
</MudExpansionPanels>
```

---

## テーブル表示

### MudTable（基本）

```razor
<MudTable Items="@Store.State.Items"
          Hover="true"
          Striped="true"
          Dense="true"
          Loading="@Store.State.IsLoading"
          LoadingProgressColor="Color.Primary">
    <HeaderContent>
        <MudTh>商品名</MudTh>
        <MudTh>カテゴリ</MudTh>
        <MudTh Style="text-align: right">価格</MudTh>
        <MudTh Style="text-align: right">在庫</MudTh>
        <MudTh>ステータス</MudTh>
        <MudTh Style="width: 120px">操作</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="商品名">@context.Name</MudTd>
        <MudTd DataLabel="カテゴリ">@context.CategoryName</MudTd>
        <MudTd DataLabel="価格" Style="text-align: right">@context.Price.ToString("C")</MudTd>
        <MudTd DataLabel="在庫" Style="text-align: right">@context.Stock</MudTd>
        <MudTd DataLabel="ステータス">
            <MudChip T="string"
                     Color="@GetStatusColor(context.Status)"
                     Size="Size.Small">
                @context.Status.ToDisplayString()
            </MudChip>
        </MudTd>
        <MudTd>
            @* 操作ボタン *@
        </MudTd>
    </RowTemplate>
    <NoRecordsContent>
        <MudText>データがありません</MudText>
    </NoRecordsContent>
</MudTable>
```

---

### MudTable（サーバーサイドページング）

```razor
<MudTable ServerData="@LoadDataAsync"
          @ref="_table"
          Hover="true"
          Striped="true"
          Dense="true"
          Loading="@_isLoading"
          LoadingProgressColor="Color.Primary">
    <HeaderContent>
        <MudTh><MudTableSortLabel SortLabel="name" T="ProductDto">商品名</MudTableSortLabel></MudTh>
        <MudTh><MudTableSortLabel SortLabel="price" T="ProductDto">価格</MudTableSortLabel></MudTh>
        <MudTh>操作</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="商品名">@context.Name</MudTd>
        <MudTd DataLabel="価格">@context.Price.ToString("C")</MudTd>
        <MudTd>@* 操作ボタン *@</MudTd>
    </RowTemplate>
    <PagerContent>
        <MudTablePager PageSizeOptions="new int[] { 10, 25, 50, 100 }"
                       RowsPerPageString="表示件数"
                       InfoFormat="{first_item}-{last_item} / {all_items}件" />
    </PagerContent>
</MudTable>

@code {
    private MudTable<ProductDto>? _table;
    private bool _isLoading;

    private async Task<TableData<ProductDto>> LoadDataAsync(TableState state, CancellationToken ct)
    {
        _isLoading = true;
        StateHasChanged();

        var query = new GetProductsQuery(
            SearchTerm: Store.State.SearchTerm,
            Page: state.Page + 1,
            PageSize: state.PageSize,
            SortLabel: state.SortLabel,
            SortDirection: state.SortDirection == SortDirection.Ascending ? "asc" : "desc"
        );

        var result = await Actions.SearchAsync(query, ct);

        _isLoading = false;

        return new TableData<ProductDto>
        {
            Items = result.Items,
            TotalItems = result.TotalCount
        };
    }
}
```

---

### MudDataGrid（高機能）

```razor
<MudDataGrid Items="@Store.State.Items"
             Filterable="true"
             FilterMode="DataGridFilterMode.Simple"
             Sortable="true"
             Groupable="false"
             Dense="true"
             Hover="true"
             Striped="true">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="商品名" />
        <PropertyColumn Property="x => x.CategoryName" Title="カテゴリ" />
        <PropertyColumn Property="x => x.Price" Title="価格" Format="C" />
        <PropertyColumn Property="x => x.Stock" Title="在庫" />
        <TemplateColumn Title="ステータス" Sortable="false" Filterable="false">
            <CellTemplate>
                <MudChip T="string" Color="@GetStatusColor(context.Item.Status)" Size="Size.Small">
                    @context.Item.Status.ToDisplayString()
                </MudChip>
            </CellTemplate>
        </TemplateColumn>
        <TemplateColumn Title="操作" Sortable="false" Filterable="false">
            <CellTemplate>
                @* 操作ボタン *@
            </CellTemplate>
        </TemplateColumn>
    </Columns>
    <PagerContent>
        <MudDataGridPager T="ProductDto" PageSizeOptions="new int[] { 10, 25, 50 }" />
    </PagerContent>
</MudDataGrid>
```

---

## 操作ボタン

### アイコンボタン（行内）

```razor
<MudTd>
    <MudStack Row="true" Spacing="1">
        <MudTooltip Text="詳細">
            <MudIconButton Icon="@Icons.Material.Filled.Visibility"
                           Size="Size.Small"
                           Href="@($"/products/{context.Id}")" />
        </MudTooltip>

        <MudTooltip Text="編集">
            <MudIconButton Icon="@Icons.Material.Filled.Edit"
                           Size="Size.Small"
                           Color="Color.Primary"
                           Href="@($"/products/{context.Id}/edit")" />
        </MudTooltip>

        @{
            var canDelete = context.CanDelete();
        }
        <MudTooltip Text="@(canDelete.IsAllowed ? "削除" : canDelete.Reason)">
            <MudIconButton Icon="@Icons.Material.Filled.Delete"
                           Size="Size.Small"
                           Color="@(canDelete.IsAllowed ? Color.Error : Color.Default)"
                           Disabled="@(!canDelete.IsAllowed)"
                           OnClick="@(() => HandleDelete(context))" />
        </MudTooltip>
    </MudStack>
</MudTd>
```

---

### メニューボタン（複数操作）

```razor
<MudTd>
    <MudMenu Icon="@Icons.Material.Filled.MoreVert" Size="Size.Small" Dense="true">
        <MudMenuItem Href="@($"/products/{context.Id}")">
            <MudIcon Icon="@Icons.Material.Filled.Visibility" Size="Size.Small" Class="mr-2" />
            詳細
        </MudMenuItem>

        <MudMenuItem Href="@($"/products/{context.Id}/edit")">
            <MudIcon Icon="@Icons.Material.Filled.Edit" Size="Size.Small" Class="mr-2" />
            編集
        </MudMenuItem>

        <MudDivider />

        @{
            var canDelete = context.CanDelete();
        }
        <MudMenuItem Disabled="@(!canDelete.IsAllowed)"
                     OnClick="@(() => HandleDelete(context))">
            <MudIcon Icon="@Icons.Material.Filled.Delete"
                     Size="Size.Small"
                     Color="@(canDelete.IsAllowed ? Color.Error : Color.Default)"
                     Class="mr-2" />
            @(canDelete.IsAllowed ? "削除" : canDelete.Reason)
        </MudMenuItem>
    </MudMenu>
</MudTd>
```

---

## ステータス表示

### 色分けヘルパー

```csharp
@code {
    private Color GetStatusColor(ProductStatus status) => status switch
    {
        ProductStatus.Active => Color.Success,
        ProductStatus.Inactive => Color.Default,
        ProductStatus.OutOfStock => Color.Warning,
        ProductStatus.Discontinued => Color.Error,
        _ => Color.Default
    };

    private string GetStatusIcon(ProductStatus status) => status switch
    {
        ProductStatus.Active => Icons.Material.Filled.CheckCircle,
        ProductStatus.Inactive => Icons.Material.Filled.PauseCircle,
        ProductStatus.OutOfStock => Icons.Material.Filled.Warning,
        ProductStatus.Discontinued => Icons.Material.Filled.Cancel,
        _ => Icons.Material.Filled.Help
    };
}
```

---

## 空状態・エラー状態

### データなし

```razor
<NoRecordsContent>
    <MudStack AlignItems="AlignItems.Center" Class="pa-8">
        <MudIcon Icon="@Icons.Material.Filled.SearchOff" Size="Size.Large" Color="Color.Default" />
        <MudText Typo="Typo.h6" Color="Color.Default">データがありません</MudText>
        <MudText Typo="Typo.body2" Color="Color.Default">
            検索条件を変更するか、新規登録してください
        </MudText>
        <MudButton Color="Color.Primary"
                   Variant="Variant.Filled"
                   StartIcon="@Icons.Material.Filled.Add"
                   Href="/products/create"
                   Class="mt-4">
            新規登録
        </MudButton>
    </MudStack>
</NoRecordsContent>
```

---

### エラー表示

```razor
@if (!string.IsNullOrEmpty(Store.State.ErrorMessage))
{
    <MudAlert Severity="Severity.Error" Class="mb-4">
        @Store.State.ErrorMessage
        <MudButton Color="Color.Error"
                   Variant="Variant.Text"
                   Size="Size.Small"
                   OnClick="@HandleRetry"
                   Class="ml-4">
            再試行
        </MudButton>
    </MudAlert>
}
```

---

## 完全な例

```razor
@page "/products"
@inject ProductListStore Store
@inject ProductListActions Actions
@implements IDisposable

<PageTitle>商品一覧</PageTitle>

<MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="mt-4">
    <MudStack Row="true" Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center" Class="mb-4">
        <MudText Typo="Typo.h5">商品一覧</MudText>
        <MudButton Color="Color.Primary"
                   Variant="Variant.Filled"
                   StartIcon="@Icons.Material.Filled.Add"
                   Href="/products/create">
            新規登録
        </MudButton>
    </MudStack>

    <MudCard Class="mb-4">
        <MudCardContent>
            <MudTextField T="string"
                          @bind-Value="Store.State.SearchTerm"
                          Label="検索"
                          Placeholder="商品名で検索..."
                          Variant="Variant.Outlined"
                          Adornment="Adornment.Start"
                          AdornmentIcon="@Icons.Material.Filled.Search"
                          Immediate="true"
                          DebounceInterval="300"
                          OnDebounceIntervalElapsed="@HandleSearch" />
        </MudCardContent>
    </MudCard>

    @if (!string.IsNullOrEmpty(Store.State.ErrorMessage))
    {
        <MudAlert Severity="Severity.Error" Class="mb-4">
            @Store.State.ErrorMessage
        </MudAlert>
    }

    <MudTable Items="@Store.State.Items"
              Hover="true"
              Striped="true"
              Dense="true"
              Loading="@Store.State.IsLoading"
              LoadingProgressColor="Color.Primary">
        <HeaderContent>
            <MudTh>商品名</MudTh>
            <MudTh Style="text-align: right">価格</MudTh>
            <MudTh Style="text-align: right">在庫</MudTh>
            <MudTh>ステータス</MudTh>
            <MudTh Style="width: 120px">操作</MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd DataLabel="商品名">@context.Name</MudTd>
            <MudTd DataLabel="価格" Style="text-align: right">@context.Price.ToString("C")</MudTd>
            <MudTd DataLabel="在庫" Style="text-align: right">@context.Stock</MudTd>
            <MudTd DataLabel="ステータス">
                <MudChip T="string" Color="@GetStatusColor(context.Status)" Size="Size.Small">
                    @context.Status.ToDisplayString()
                </MudChip>
            </MudTd>
            <MudTd>
                <MudStack Row="true" Spacing="1">
                    <MudIconButton Icon="@Icons.Material.Filled.Visibility"
                                   Size="Size.Small"
                                   Href="@($"/products/{context.Id}")" />
                    <MudIconButton Icon="@Icons.Material.Filled.Edit"
                                   Size="Size.Small"
                                   Color="Color.Primary"
                                   Href="@($"/products/{context.Id}/edit")" />
                </MudStack>
            </MudTd>
        </RowTemplate>
        <NoRecordsContent>
            <MudText>データがありません</MudText>
        </NoRecordsContent>
    </MudTable>
</MudContainer>

@code {
    protected override async Task OnInitializedAsync()
    {
        Store.OnChangeAsync += StateHasChangedAsync;
        await Actions.LoadAsync();
    }

    private async Task HandleSearch()
    {
        await Actions.SearchAsync();
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
□ ヘッダーにタイトルと新規登録ボタンがあるか？
□ 検索/フィルター機能があるか？
□ MudTable/MudDataGrid を使用しているか？
□ ローディング状態が表示されるか？
□ データなしの状態が適切に表示されるか？
□ エラー状態が適切に表示されるか？
□ 操作ボタンに CanXxx() が適用されているか？
□ ステータスが色分けされているか？
□ ページングがあるか？（データ量が多い場合）
```
