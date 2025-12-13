# コンポーネントマッピング（Component Mapping）

HTML 要素から MudBlazor コンポーネントへの変換ルールを定義する。

---

## 入力コンポーネント

### テキスト入力

| HTML | MudBlazor | 備考 |
|------|-----------|------|
| `<input type="text">` | `<MudTextField T="string">` | |
| `<input type="password">` | `<MudTextField T="string" InputType="InputType.Password">` | |
| `<input type="email">` | `<MudTextField T="string" InputType="InputType.Email">` | |
| `<input type="tel">` | `<MudTextField T="string" InputType="InputType.Telephone">` | |
| `<textarea>` | `<MudTextField T="string" Lines="3">` | Lines で行数指定 |

**変換例:**

```razor
@* Before *@
<input type="text" @bind="model.Name" class="form-control" placeholder="商品名" />

@* After *@
<MudTextField T="string"
              @bind-Value="model.Name"
              Label="商品名"
              Variant="Variant.Outlined"
              Required="true" />
```

---

### 数値入力

| HTML | MudBlazor | 備考 |
|------|-----------|------|
| `<input type="number">` | `<MudNumericField T="int">` | 整数 |
| `<input type="number" step="0.01">` | `<MudNumericField T="decimal">` | 小数 |

**変換例:**

```razor
@* Before *@
<input type="number" @bind="model.Price" min="0" step="0.01" />

@* After *@
<MudNumericField T="decimal"
                 @bind-Value="model.Price"
                 Label="価格"
                 Min="0"
                 Adornment="Adornment.Start"
                 AdornmentText="¥"
                 Variant="Variant.Outlined" />
```

---

### 選択入力

| HTML | MudBlazor | 備考 |
|------|-----------|------|
| `<select>` | `<MudSelect T="...">` | 単一選択 |
| `<select multiple>` | `<MudSelect T="..." MultiSelection="true">` | 複数選択 |
| `<input type="radio">` | `<MudRadioGroup>` + `<MudRadio>` | |

**変換例:**

```razor
@* Before *@
<select @bind="model.CategoryId">
    <option value="">選択してください</option>
    @foreach (var cat in categories)
    {
        <option value="@cat.Id">@cat.Name</option>
    }
</select>

@* After *@
<MudSelect T="Guid?"
           @bind-Value="model.CategoryId"
           Label="カテゴリ"
           Variant="Variant.Outlined"
           AnchorOrigin="Origin.BottomCenter">
    <MudSelectItem T="Guid?" Value="@null">選択してください</MudSelectItem>
    @foreach (var cat in categories)
    {
        <MudSelectItem T="Guid?" Value="@cat.Id">@cat.Name</MudSelectItem>
    }
</MudSelect>
```

---

### チェックボックス / スイッチ

| HTML | MudBlazor | 備考 |
|------|-----------|------|
| `<input type="checkbox">` | `<MudCheckBox T="bool">` | |
| `<input type="checkbox">` (トグル用) | `<MudSwitch T="bool">` | ON/OFF 切り替え向け |

**変換例:**

```razor
@* Before *@
<input type="checkbox" @bind="model.IsActive" />
<label>有効</label>

@* After *@
<MudSwitch T="bool"
           @bind-Value="model.IsActive"
           Label="有効"
           Color="Color.Primary" />
```

---

### 日付・時刻入力

| HTML | MudBlazor | 備考 |
|------|-----------|------|
| `<input type="date">` | `<MudDatePicker>` | |
| `<input type="time">` | `<MudTimePicker>` | |
| `<input type="datetime-local">` | `<MudDatePicker>` + `<MudTimePicker>` | 組み合わせ |

**変換例:**

```razor
@* Before *@
<input type="date" @bind="model.DueDate" />

@* After *@
<MudDatePicker @bind-Date="model.DueDate"
               Label="期限日"
               DateFormat="yyyy/MM/dd"
               Variant="Variant.Outlined" />
```

---

## ボタン

| HTML | MudBlazor | 備考 |
|------|-----------|------|
| `<button type="submit">` | `<MudButton ButtonType="ButtonType.Submit">` | |
| `<button type="button">` | `<MudButton>` | |
| `<a href="...">` (ボタン風) | `<MudButton Href="...">` | |

### ボタンバリエーション

| 用途 | Color | Variant |
|------|-------|---------|
| プライマリアクション | `Color.Primary` | `Variant.Filled` |
| セカンダリアクション | `Color.Secondary` | `Variant.Outlined` |
| 危険なアクション | `Color.Error` | `Variant.Filled` |
| キャンセル | `Color.Default` | `Variant.Text` |

**変換例:**

```razor
@* Before *@
<button type="submit" class="btn btn-primary" disabled="@isSubmitting">
    保存
</button>
<button type="button" class="btn btn-secondary" @onclick="Cancel">
    キャンセル
</button>

@* After *@
<MudButton ButtonType="ButtonType.Submit"
           Color="Color.Primary"
           Variant="Variant.Filled"
           Disabled="@isSubmitting">
    @if (isSubmitting)
    {
        <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
    }
    保存
</MudButton>
<MudButton Color="Color.Default"
           Variant="Variant.Text"
           OnClick="@Cancel">
    キャンセル
</MudButton>
```

---

## テーブル

| HTML | MudBlazor | 備考 |
|------|-----------|------|
| `<table>` (シンプル) | `<MudSimpleTable>` | 静的データ向け |
| `<table>` (データ一覧) | `<MudTable T="...">` | ページング、ソート対応 |
| `<table>` (高機能) | `<MudDataGrid T="...">` | フィルタ、グループ化対応 |

**変換例:**

```razor
@* Before *@
<table class="table">
    <thead>
        <tr>
            <th>商品名</th>
            <th>価格</th>
            <th>操作</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var item in products)
        {
            <tr>
                <td>@item.Name</td>
                <td>@item.Price.ToString("C")</td>
                <td><button @onclick="() => Edit(item.Id)">編集</button></td>
            </tr>
        }
    </tbody>
</table>

@* After *@
<MudTable Items="@products" Hover="true" Striped="true" Dense="true">
    <HeaderContent>
        <MudTh>商品名</MudTh>
        <MudTh>価格</MudTh>
        <MudTh>操作</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="商品名">@context.Name</MudTd>
        <MudTd DataLabel="価格">@context.Price.ToString("C")</MudTd>
        <MudTd>
            <MudIconButton Icon="@Icons.Material.Filled.Edit"
                           Size="Size.Small"
                           OnClick="@(() => Edit(context.Id))" />
        </MudTd>
    </RowTemplate>
</MudTable>
```

---

## レイアウト

| HTML | MudBlazor | 備考 |
|------|-----------|------|
| `<div class="card">` | `<MudCard>` | |
| `<div class="row">` | `<MudGrid>` | |
| `<div class="col-*">` | `<MudItem xs="*">` | |
| `<div class="alert">` | `<MudAlert>` | |
| `<span class="badge">` | `<MudChip>` | |

**変換例:**

```razor
@* Before *@
<div class="card">
    <div class="card-header">商品情報</div>
    <div class="card-body">
        <div class="row">
            <div class="col-6">...</div>
            <div class="col-6">...</div>
        </div>
    </div>
</div>

@* After *@
<MudCard>
    <MudCardHeader>
        <CardHeaderContent>
            <MudText Typo="Typo.h6">商品情報</MudText>
        </CardHeaderContent>
    </MudCardHeader>
    <MudCardContent>
        <MudGrid>
            <MudItem xs="12" md="6">...</MudItem>
            <MudItem xs="12" md="6">...</MudItem>
        </MudGrid>
    </MudCardContent>
</MudCard>
```

---

## フィードバック

| HTML | MudBlazor | 備考 |
|------|-----------|------|
| `<div class="alert alert-success">` | `<MudAlert Severity="Severity.Success">` | |
| `<div class="alert alert-danger">` | `<MudAlert Severity="Severity.Error">` | |
| `<div class="alert alert-warning">` | `<MudAlert Severity="Severity.Warning">` | |
| `<div class="alert alert-info">` | `<MudAlert Severity="Severity.Info">` | |
| `<div class="spinner">` | `<MudProgressCircular>` | |
| `<div class="progress">` | `<MudProgressLinear>` | |

---

## アイコン

MudBlazor は Material Icons を使用。

```razor
@* アイコンボタン *@
<MudIconButton Icon="@Icons.Material.Filled.Delete" Color="Color.Error" />
<MudIconButton Icon="@Icons.Material.Filled.Edit" Color="Color.Primary" />
<MudIconButton Icon="@Icons.Material.Filled.Add" Color="Color.Success" />

@* ボタン内アイコン *@
<MudButton StartIcon="@Icons.Material.Filled.Save" Color="Color.Primary">
    保存
</MudButton>
```

---

## 変換チェックリスト

```
□ input[type="text"] → MudTextField
□ select → MudSelect
□ button → MudButton（適切な Color/Variant）
□ table → MudTable / MudDataGrid
□ div.card → MudCard
□ div.alert → MudAlert
□ ローディング → MudProgressCircular
□ アイコン → Icons.Material.Filled.*
```
