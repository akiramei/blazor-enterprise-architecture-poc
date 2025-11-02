# フロントエンド共通基盤（Infrastructure）

このディレクトリには、**プロジェクトテンプレートとして再利用可能なフロントエンド共通基盤**が含まれています。

## 設計方針

### ✅ システムレベル vs ドメイン固有

| 分類 | 責任範囲 | 実装場所 | 例 |
|------|---------|---------|-----|
| **システムレベル** | すべてのプロジェクトで共通 | `Infrastructure/` | セッション、テーマ、レイアウト、通知 |
| **ドメイン固有** | ビジネスロジック固有 | `Features/` | Products, Orders, Customers |

### 🎯 目的

- **毎プロジェクトで書く必要のないコード**をテンプレート化
- 新規プロジェクトで**即座に使える**状態管理を提供
- バックエンドの共通基盤（Pipeline Behaviors等）と同等の品質

---

## 📁 ディレクトリ構成

```
Infrastructure/
├── Models/              # 状態モデル（record型、不変）
│   ├── SessionState.cs
│   ├── ThemeState.cs
│   ├── PreferencesState.cs
│   ├── LayoutState.cs
│   └── NotificationState.cs
├── Providers/           # Cascading Provider（Razor Component）
│   ├── SessionProvider.razor
│   └── ThemeProvider.razor
├── Stores/              # 状態管理ストア（並行制御対応）
│   ├── PreferencesStore.cs
│   ├── LayoutStore.cs
│   └── NotificationStore.cs
└── Services/
    └── LocalStorageService.cs
```

---

## 🚀 実装済み機能

### 1. SessionProvider（認証状態管理）

現在のユーザー情報を簡単にアクセス可能にする。

**特徴:**
- IAppContextのフロントエンド版
- AuthenticationStateProviderと統合
- CascadingValueで全コンポーネントから利用可能

**使用例:**

```razor
@code {
    [CascadingParameter]
    private SessionProvider SessionProvider { get; set; } = default!;

    protected override void OnInitialized()
    {
        var session = SessionProvider.State;

        if (session.IsAuthenticated)
        {
            var userId = session.UserId;
            var userName = session.UserName;
            var isAdmin = session.IsInRole("Admin");
        }
    }
}
```

### 2. ThemeProvider（ダークモード切り替え）

アプリケーション全体のテーマを管理。

**特徴:**
- ダークモード/ライトモードの切り替え
- システム設定に従うオプション
- LocalStorageに永続化

**使用例:**

```razor
@code {
    [CascadingParameter]
    private ThemeProvider ThemeProvider { get; set; } = default!;

    private async Task ToggleTheme()
    {
        await ThemeProvider.ToggleThemeAsync();
    }

    private bool IsDarkMode => ThemeProvider.State.Mode == ThemeMode.Dark;
}
```

### 3. PreferencesStore（ユーザー設定）

言語・タイムゾーン・日付フォーマット等のユーザー設定を管理。

**特徴:**
- カルチャ設定（多言語対応）
- タイムゾーン設定
- 日付・時刻フォーマット
- デフォルトページサイズ

**使用例:**

```csharp
@inject PreferencesStore PreferencesStore

@code {
    protected override async Task OnInitializedAsync()
    {
        await PreferencesStore.InitializeAsync();

        var prefs = PreferencesStore.GetState();
        var culture = prefs.GetCultureInfo();
        var timeZone = prefs.GetTimeZoneInfo();
    }

    private async Task ChangeCulture()
    {
        await PreferencesStore.SetCultureAsync("en-US");
    }
}
```

### 4. LayoutStore（レイアウト状態）

サイドバー、ナビゲーションメニュー等のUI要素の状態を管理。

**特徴:**
- サイドバーの表示/非表示
- サイドバーのピン留め
- レスポンシブ対応（画面サイズ検知）

**使用例:**

```csharp
@inject LayoutStore LayoutStore

@code {
    protected override async Task OnInitializedAsync()
    {
        await LayoutStore.InitializeAsync();

        LayoutStore.OnChangeAsync += StateHasChanged;
    }

    private async Task ToggleSidebar()
    {
        await LayoutStore.ToggleSidebarAsync();
    }

    private bool IsSidebarOpen => LayoutStore.GetState().IsSidebarOpen;
}
```

### 5. NotificationStore（通知管理）

トースト通知・モーダルダイアログの表示を管理。

**特徴:**
- トースト通知のキュー管理
- 自動消去タイマー
- モーダルダイアログの状態管理
- 型安全なコールバック

**使用例:**

```csharp
@inject NotificationStore NotificationStore

@code {
    private async Task ShowSuccess()
    {
        await NotificationStore.ShowSuccessAsync(
            "成功",
            "データを保存しました");
    }

    private async Task ShowError()
    {
        await NotificationStore.ShowErrorAsync(
            "エラー",
            "データの保存に失敗しました");
    }

    private async Task ShowConfirm()
    {
        await NotificationStore.ShowConfirmAsync(
            "確認",
            "本当に削除しますか？",
            onConfirm: async () => await DeleteAsync(),
            onCancel: async () => await Task.CompletedTask);
    }
}
```

---

## 🔧 セットアップ

### 1. DI登録（Program.cs）

```csharp
// Infrastructure Services
builder.Services.AddScoped<ProductCatalog.Web.Infrastructure.Services.LocalStorageService>();

// Infrastructure Stores（システムレベル状態管理）
builder.Services.AddScoped<ProductCatalog.Web.Infrastructure.Stores.PreferencesStore>();
builder.Services.AddScoped<ProductCatalog.Web.Infrastructure.Stores.LayoutStore>();
builder.Services.AddScoped<ProductCatalog.Web.Infrastructure.Stores.NotificationStore>();
```

### 2. Providerの配置（Routes.razor）

```razor
@using ProductCatalog.Web.Infrastructure.Providers

<SessionProvider>
    <ThemeProvider>
        <Router AppAssembly="typeof(Program).Assembly">
            <!-- ルーティング設定 -->
        </Router>
    </ThemeProvider>
</SessionProvider>
```

### 3. JavaScriptモジュール配置

`wwwroot/Infrastructure/theme.js` を配置（ThemeProvider用）

---

## 📊 状態管理パターン

すべてのStoreは以下のパターンを採用：

### ✅ 不変オブジェクト（record型）

```csharp
public sealed record PreferencesState
{
    public string Culture { get; init; }
    public string TimeZoneId { get; init; }
    // ...
}
```

### ✅ 並行制御（SemaphoreSlim）

```csharp
private readonly SemaphoreSlim _gate = new(1, 1);

public async Task SetCultureAsync(string culture, CancellationToken ct = default)
{
    await _gate.WaitAsync(ct);
    try
    {
        var newState = _state with { Culture = culture };
        await SetStateAsync(newState);
    }
    finally
    {
        _gate.Release();
    }
}
```

### ✅ 状態変更通知

```csharp
public event Func<Task>? OnChangeAsync;

private async Task SetStateAsync(PreferencesState newState)
{
    _state = newState;

    if (OnChangeAsync != null)
    {
        await OnChangeAsync.Invoke();
    }
}
```

---

## 🎨 設計の意図

### バックエンドとの一貫性

| バックエンド | フロントエンド |
|------------|--------------|
| Pipeline Behaviors（横断的関心事） | Infrastructure Stores（共通状態管理） |
| IProductRepository（ドメイン固有） | ProductsStore（ドメイン固有） |
| IAppContext（横断的コンテキスト） | SessionProvider（横断的コンテキスト） |

### 新規プロジェクトでの使い方

1. **コピー**: `Infrastructure/` ディレクトリをコピー
2. **DI登録**: Program.csに登録コードを追加
3. **Provider配置**: Routes.razorに配置
4. **即座に使用可能**: すべてのコンポーネントから利用可能

---

## ⚠️ 使い分けガイド

### ✅ Infrastructure に配置すべきもの

- セッション・認証状態
- テーマ・UI設定
- 言語・地域設定
- 通知・アラート
- レイアウト状態

### ❌ Infrastructure に配置すべきでないもの

- ビジネスロジック（→ Features/）
- ドメイン固有の状態（→ Features/[Domain]/Store/）
- API呼び出しロジック（→ Features/[Domain]/Actions/）

---

## 📚 関連ドキュメント

- バックエンド共通基盤: `docs/architecture/`
- 状態管理パターン: `docs/frontend/state-management.md`（TBD）
- 設計方針全体: `docs/architecture/STATE-MANAGEMENT-LAYERS.md`
