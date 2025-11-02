# 状態管理レイヤーの分離：理想と現状のギャップ分析

**目的**: AI開発テンプレートとして、「どの状態を毎回書くべきでないか」を明確にする

---

## 🎯 核心的な問い

> **新規プロジェクトで、どこを共通化し、どこだけ個別に作るべきか？**

この問いに答えるため、状態管理を2つのレイヤーに明確に分離します：

### レイヤー定義

| レイヤー | 説明 | スコープ | AI開発での扱い |
|---------|------|---------|--------------|
| **A. システムレベル（共通プラットフォーム）** | 全プロジェクトで共通の永続状態・セッション系状態 | 組織全体 | **テンプレート化**し、毎回コピーして使う |
| **B. ドメイン固有** | 個別案件のビジネスドメインに関わる状態 | プロジェクト個別 | プロジェクトごとに新規作成 |

**重要な設計原則**:

✅ **「どこを毎回書くべきでないか」= システムレベル層**
❌ **「毎回書いていい（書くべき）箇所」= ドメイン固有層**

---

## 📊 現状分析：ProductCatalogプロジェクト

### A. サーバーサイド（DB + バックエンドサービス）

#### 現状の状態管理

##### 1. DBテーブル

| テーブル名 | レイヤー | 説明 | 現状 |
|-----------|---------|------|------|
| **Identity系テーブル** | システムレベル | ユーザー、ロール、権限（ASP.NET Core Identity） | ✅ 理想的な分離 |
| **RefreshTokens** | システムレベル | JWT Refresh Token | ✅ 理想的な分離 |
| **AuditLogs** | システムレベル | 監査ログ（誰がいつ何をしたか） | ✅ 理想的な分離 |
| **OutboxMessages** | システムレベル | 統合イベント配信キュー | ✅ 理想的な分離 |
| **Products** | ドメイン固有 | 商品マスター | ✅ 理想的な分離 |
| **ProductImages** | ドメイン固有 | 商品画像 | ✅ 理想的な分離 |

**評価**: ✅ **サーバーサイドのDBスキーマは理想的な分離ができている**

##### 2. バックエンドサービス

| サービス名 | レイヤー | 説明 | ファイルパス |
|-----------|---------|------|------------|
| **IAppContext** | システムレベル | 統合コンテキスト（ユーザー、リクエスト、環境情報を一元管理） | `Infrastructure/Services/AppContext.cs` |
| **ICurrentUserService** | システムレベル | 現在のユーザー情報 | `Infrastructure/Services/CurrentUserService.cs` |
| **ICorrelationIdAccessor** | システムレベル | リクエスト追跡ID | `Infrastructure/Services/CorrelationIdAccessor.cs` |
| **IJwtTokenGenerator** | システムレベル | JWT生成・検証 | `Infrastructure/Authentication/JwtTokenGenerator.cs` |
| **IProductRepository** | ドメイン固有 | 商品リポジトリ | `Domain/Products/IProductRepository.cs` |
| **IProductNotificationService** | ドメイン固有 | 商品変更通知（SignalR） | `Web/Services/ProductNotificationService.cs` |

**評価**: ✅ **サーバーサイドのサービス層も理想的な分離ができている**

##### 3. Pipeline Behaviors（横断的関心事）

| Behavior名 | レイヤー | 説明 |
|-----------|---------|------|
| **MetricsBehavior** | システムレベル | 実行時間・成功率を計測 |
| **LoggingBehavior** | システムレベル | リクエスト/レスポンスをログ記録 |
| **ValidationBehavior** | システムレベル | FluentValidationで入力検証 |
| **AuthorizationBehavior** | システムレベル | ロールベース認可チェック |
| **IdempotencyBehavior** | システムレベル | 冪等性キーで重複実行を防止 |
| **CachingBehavior** | システムレベル | Queryの結果をキャッシュ |
| **AuditLogBehavior** | システムレベル | Commandの実行を監査ログに記録 |
| **TransactionBehavior** | システムレベル | Commandをトランザクション内で実行 |

**評価**: ✅ **Pipeline Behaviorsは完全にシステムレベルで実装されている**

---

### B. フロントエンド（Blazor Server）

#### 現状の状態管理

##### 1. 画面固有のStore（ドメイン層）

| Store名 | レイヤー | 状態の内容 | ファイルパス |
|--------|---------|-----------|------------|
| **ProductsStore** | ドメイン固有 | 商品一覧、読み込み状態、エラー | `Web/Features/Products/Store/ProductsStore.cs` |
| **ProductDetailStore** | ドメイン固有 | 商品詳細、読み込み状態 | `Web/Features/Products/Store/ProductDetailStore.cs` |
| **ProductEditStore** | ドメイン固有 | 商品編集、保存状態 | `Web/Features/Products/Store/ProductEditStore.cs` |
| **ProductSearchStore** | ドメイン固有 | 商品検索、フィルタ条件 | `Web/Features/Products/Store/ProductSearchStore.cs` |

**評価**: ✅ **ドメイン固有のStoreとして正しく分離されている**

##### 2. システムレベルの状態管理

| 状態の種類 | 理想的な実装場所 | 現状 | 実装ファイル |
|-----------|---------------|------|------------|
| **認証状態** | SessionStore/SessionProvider | ✅ **実装完了** | `Infrastructure/Providers/SessionProvider.razor` |
| **ログイン中ユーザー情報** | SessionStore | ✅ **実装完了** | `Infrastructure/Models/SessionState.cs` |
| **ロール・権限** | SessionStore | ✅ **実装完了** | `SessionState.IsInRole()`, `IsInAnyRole()` |
| **UIテーマ設定** | ThemeStore/ThemeProvider | ✅ **実装完了** | `Infrastructure/Providers/ThemeProvider.razor` |
| **言語・タイムゾーン** | PreferencesStore | ✅ **実装完了** | `Infrastructure/Stores/PreferencesStore.cs` |
| **サイドバー開閉状態** | LayoutStore | ✅ **実装完了** | `Infrastructure/Stores/LayoutStore.cs` |
| **グローバルトースト/モーダル** | NotificationStore | ✅ **実装完了** | `Infrastructure/Stores/NotificationStore.cs` |

**評価**: ✅ **フロントエンドのシステムレベル状態管理が理想的な分離を達成**

---

## 🏗️ 理想的なアーキテクチャ

### 原則1: DBスキーマのレイヤー分離

```
┌─────────────────────────────────────────────────────────────┐
│ データベース（PostgreSQL）                                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  【システムレベル（全プロジェクト共通）】                       │
│  ├─ IdentityUsers, IdentityRoles, IdentityUserRoles        │
│  ├─ RefreshTokens                                          │
│  ├─ AuditLogs                                              │
│  ├─ OutboxMessages                                         │
│  ├─ Notifications (通知キュー)                              │
│  ├─ UserPreferences (ユーザーごとのUI設定)                   │
│  └─ JobExecutions (バックグラウンドジョブ進行状況)            │
│                                                             │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━                 │
│                                                             │
│  【ドメイン固有（プロジェクトごとに変わる）】                   │
│  ├─ Products, ProductImages                                │
│  ├─ Orders, OrderItems                                     │
│  ├─ Invoices, InvoiceLines                                 │
│  └─ ... (ビジネスドメインのテーブル)                         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**AIテンプレートとしての指針**:

✅ **新規プロジェクトでやっていいこと**: ドメインモデル由来のテーブル追加のみ
❌ **毎回書いてはいけないこと**: システムレベルのテーブル（Identity, AuditLog等）

---

### 原則2: フロントエンド状態管理のレイヤー分離

```
┌─────────────────────────────────────────────────────────────┐
│ Blazor Serverアプリケーション                                │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  【システムレベル（共通プラットフォームSDK）】                  │
│  ├─ SessionProvider/SessionStore                           │
│  │   ├─ isAuthenticated                                    │
│  │   ├─ currentUser (Id, Name, Email, Roles)              │
│  │   └─ tenantId, organizationId                          │
│  │                                                         │
│  ├─ ThemeProvider/ThemeStore                               │
│  │   ├─ theme: 'light' | 'dark'                           │
│  │   ├─ fontSize: 'small' | 'medium' | 'large'            │
│  │   └─ setTheme(), toggleDarkMode()                      │
│  │                                                         │
│  ├─ PreferencesStore                                       │
│  │   ├─ language: 'ja' | 'en'                             │
│  │   ├─ timeZone: 'Asia/Tokyo'                            │
│  │   └─ dateFormat: 'yyyy/MM/dd'                          │
│  │                                                         │
│  ├─ LayoutStore                                            │
│  │   ├─ sidebarOpen: boolean                              │
│  │   └─ headerCollapsed: boolean                          │
│  │                                                         │
│  └─ NotificationStore                                      │
│      ├─ showToast(message, type)                          │
│      ├─ showModal(component, props)                       │
│      └─ showConfirm(message, onConfirm)                   │
│                                                             │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━                 │
│                                                             │
│  【ドメイン固有（プロジェクトごとに作成）】                     │
│  ├─ ProductsStore (商品一覧、フィルタ、選択状態)              │
│  ├─ InvoiceEditStore (請求書編集、一時計算結果)              │
│  ├─ OrderWizardStore (注文ウィザードのステップ状態)           │
│  └─ ... (業務画面固有の状態)                                │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**AIテンプレートとしての指針**:

✅ **新規プロジェクトでやっていいこと**: ドメイン固有のStore作成のみ
❌ **毎回書いてはいけないこと**: SessionStore, ThemeStore等のシステムレベル状態

---

## ✅ 実装完了：フロントエンド共通基盤

### 実装済みコンポーネント

| コンポーネント | 実装内容 | ファイルパス |
|------------|---------|------------|
| **SessionProvider** | 認証状態・ユーザー情報のCascadingValue | `Infrastructure/Providers/SessionProvider.razor` |
| **ThemeProvider** | ダークモード切り替え・システム設定連携 | `Infrastructure/Providers/ThemeProvider.razor` |
| **PreferencesStore** | 言語・タイムゾーン・日付フォーマット | `Infrastructure/Stores/PreferencesStore.cs` |
| **LayoutStore** | サイドバー・ナビゲーション状態 | `Infrastructure/Stores/LayoutStore.cs` |
| **NotificationStore** | トースト・モーダル管理 | `Infrastructure/Stores/NotificationStore.cs` |
| **LocalStorageService** | ブラウザストレージアクセス | `Infrastructure/Services/LocalStorageService.cs` |

**達成した成果**:
- ✅ システムレベルとドメイン固有を明確に分離
- ✅ 新規プロジェクトで即座に使えるテンプレート化
- ✅ バックエンドと同等の品質・一貫性を実現
- ✅ セキュリティ実装の統一化（認可チェック等）

---

## 🏆 到達した理想状態

### サーバーサイドとフロントエンドの統合

**以前**: サーバーサイドは理想的だが、フロントエンドから活用しづらい

```csharp
// 以前: 毎回DIして使う（煩雑）
@inject IAppContext AppContext

@code {
    private async Task LoadUserInfo()
    {
        var userId = AppContext.UserId;
        var isAdmin = AppContext.IsInRole("Admin");
        // ...
    }
}
```

**現在**: ✅ **Blazor固有のProviderで簡潔にアクセス可能**

```razor
@* 実装完了: CascadingParameterで簡潔に取得 *@
@code {
    [CascadingParameter]
    private SessionProvider SessionProvider { get; set; } = default!;

    private void OnClick()
    {
        var session = SessionProvider.State;

        if (session.IsAuthenticated)
        {
            Console.WriteLine($"UserId: {session.UserId}");
            Console.WriteLine($"UserName: {session.UserName}");

            if (session.IsInRole("Admin"))
            {
                // 管理者専用処理
            }
        }
    }
}
```

**使用例**（実装済み）:

```razor
<!-- Routes.razor -->
<SessionProvider>
    <ThemeProvider>
        <Router AppAssembly="typeof(Program).Assembly">
            <!-- アプリケーションコンテンツ -->
        </Router>
    </ThemeProvider>
</SessionProvider>
```

---

## 🎯 理想的な状態への移行

### ステップ1: 共通プラットフォームSDKの構築

#### 1-A. SessionProvider実装

```csharp
// Infrastructure/Blazor/Session/SessionProvider.razor
@using ProductCatalog.Application.Common.Interfaces

<CascadingValue Value="@Session">
    @ChildContent
</CascadingValue>

@code {
    [Inject] private IAppContext AppContext { get; set; } = default!;
    [Parameter] public RenderFragment ChildContent { get; set; } = default!;

    private SessionState Session { get; set; } = new();

    protected override void OnInitialized()
    {
        Session = new SessionState
        {
            UserId = AppContext.UserId,
            UserName = AppContext.UserName,
            IsAuthenticated = AppContext.IsAuthenticated,
            Roles = GetUserRoles(),
            TenantId = AppContext.TenantId
        };
    }

    private string[] GetUserRoles()
    {
        // AppContextからロール一覧を取得
        // TODO: IAppContextに GetRoles() メソッドを追加
        return Array.Empty<string>();
    }
}
```

#### 1-B. ThemeProvider実装

```csharp
// Infrastructure/Blazor/Theme/ThemeProvider.razor
<CascadingValue Value="@Theme">
    @ChildContent
</CascadingValue>

@code {
    [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;
    [Parameter] public RenderFragment ChildContent { get; set; } = default!;

    private ThemeState Theme { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        var savedTheme = await LocalStorage.GetItemAsync<string>("theme") ?? "light";
        Theme = new ThemeState
        {
            Current = savedTheme,
            Toggle = ToggleTheme
        };
    }

    private async Task ToggleTheme()
    {
        var newTheme = Theme.Current == "light" ? "dark" : "light";
        Theme = Theme with { Current = newTheme };
        await LocalStorage.SetItemAsync("theme", newTheme);
        StateHasChanged();
    }
}
```

#### 1-C. NotificationProvider実装

```csharp
// Infrastructure/Blazor/Notifications/NotificationProvider.razor
<CascadingValue Value="@Notifications">
    @ChildContent
</CascadingValue>

<!-- Toast表示領域 -->
<div class="toast-container">
    @foreach (var toast in _toasts)
    {
        <div class="toast toast-@toast.Type">@toast.Message</div>
    }
</div>

@code {
    [Parameter] public RenderFragment ChildContent { get; set; } = default!;

    private NotificationState Notifications { get; set; } = default!;
    private List<ToastMessage> _toasts = new();

    protected override void OnInitialized()
    {
        Notifications = new NotificationState
        {
            ShowToast = ShowToast,
            ShowModal = ShowModal,
            ShowConfirm = ShowConfirm
        };
    }

    private void ShowToast(string message, string type = "info")
    {
        var toast = new ToastMessage { Message = message, Type = type };
        _toasts.Add(toast);
        StateHasChanged();

        // 3秒後に自動削除
        Task.Delay(3000).ContinueWith(_ =>
        {
            _toasts.Remove(toast);
            InvokeAsync(StateHasChanged);
        });
    }

    private Task ShowModal(RenderFragment content) { /* TODO */ return Task.CompletedTask; }
    private Task ShowConfirm(string message, Action onConfirm) { /* TODO */ return Task.CompletedTask; }
}
```

---

### ステップ2: App.razorで全Providerを統合

```razor
<!-- App.razor -->
<SessionProvider>
    <ThemeProvider>
        <NotificationProvider>
            <Router>
                <!-- 既存のルーティング -->
            </Router>
        </NotificationProvider>
    </ThemeProvider>
</SessionProvider>
```

---

### ステップ3: 画面側での使用例

```razor
@page "/products"

<h3>商品一覧</h3>

<!-- セッション情報の表示 -->
<p>ログイン中: @Session.UserName</p>

<!-- ロールベース表示 -->
@if (Session.IsInRole("Admin"))
{
    <button @onclick="DeleteProduct">削除</button>
}

<!-- テーマ切り替え -->
<button @onclick="Theme.Toggle">
    @(Theme.Current == "light" ? "ダークモード" : "ライトモード")
</button>

<!-- 通知表示 -->
<button @onclick="() => Notifications.ShowToast('保存しました', 'success')">
    保存
</button>

@code {
    [CascadingParameter] public SessionState Session { get; set; } = default!;
    [CascadingParameter] public ThemeState Theme { get; set; } = default!;
    [CascadingParameter] public NotificationState Notifications { get; set; } = default!;

    // ドメイン固有の状態管理
    [Inject] private ProductsStore Store { get; set; } = default!;

    private void DeleteProduct()
    {
        // ドメイン固有のロジック
        Store.DeleteAsync(productId);
    }
}
```

---

## 📦 AIテンプレートとして提供すべきもの

### 1. 共通プラットフォームSDK（NuGetパッケージ化）

```
ProductCatalog.Platform.Blazor/
├── Session/
│   ├── SessionProvider.razor
│   ├── SessionState.cs
│   └── ISessionService.cs
├── Theme/
│   ├── ThemeProvider.razor
│   ├── ThemeState.cs
│   └── IThemeService.cs
├── Notifications/
│   ├── NotificationProvider.razor
│   ├── NotificationState.cs
│   └── Toast.razor
├── Layout/
│   ├── LayoutProvider.razor
│   └── LayoutState.cs
└── Preferences/
    ├── PreferencesProvider.razor
    └── PreferencesState.cs
```

**使い方**: 新規プロジェクトで NuGet参照 → App.razorで組み込むだけ

---

### 2. サーバーサイド共通基盤（プロジェクトテンプレート）

```
ProductCatalog.Platform.Infrastructure/
├── Authentication/
│   ├── JwtSettings.cs
│   ├── JwtTokenGenerator.cs
│   └── RefreshToken.cs
├── Services/
│   ├── AppContext.cs
│   ├── CurrentUserService.cs
│   └── CorrelationIdAccessor.cs
├── Behaviors/
│   ├── MetricsBehavior.cs
│   ├── LoggingBehavior.cs
│   ├── ValidationBehavior.cs
│   ├── AuthorizationBehavior.cs
│   ├── IdempotencyBehavior.cs
│   ├── CachingBehavior.cs
│   ├── AuditLogBehavior.cs
│   └── TransactionBehavior.cs
└── Persistence/
    ├── BaseDbContext.cs (Identity, AuditLog, OutboxMessage含む)
    └── Configurations/
        ├── AuditLogConfiguration.cs
        ├── RefreshTokenConfiguration.cs
        └── OutboxMessageConfiguration.cs
```

**使い方**: dotnet new プロジェクトテンプレート化 → 新規プロジェクトで即利用

---

### 3. ドメイン層のみを実装すればいいガイド

```markdown
# 新規プロジェクト開発ガイド

## ✅ あなたが書くべきコード

### サーバーサイド
- ドメインエンティティ（Product, Order等）
- Command/Query（CreateProduct, GetProducts等）
- Handler（ビジネスロジック）
- Repository実装

### フロントエンド
- ドメイン固有のStore（ProductsStore, OrdersStore等）
- 画面コンポーネント（ProductList.razor等）
- Actions（ユーザー操作）

## ❌ 書いてはいけないコード（共通基盤が提供済み）

### サーバーサイド
- 認証・認可（ASP.NET Core Identity, JWT）
- 監査ログ（AuditLogBehavior）
- バリデーション（ValidationBehavior）
- トランザクション（TransactionBehavior）
- 冪等性保証（IdempotencyBehavior）

### フロントエンド
- SessionProvider（ログイン状態管理）
- ThemeProvider（ダークモード切り替え）
- NotificationProvider（トースト/モーダル）
- LayoutProvider（サイドバー状態）
```

---

## 🎓 まとめ

### 現状の評価

| レイヤー | サーバーサイド | フロントエンド |
|---------|------------|------------|
| **システムレベル** | ✅ 理想的な分離 | ✅ **理想的な分離** |
| **ドメイン固有** | ✅ 適切に分離 | ✅ 適切に分離 |

### 到達した理想状態

**サーバーサイド**: ✅ 理想的な状態 → テンプレート化可能
**フロントエンド**: ✅ **システムレベルの状態管理が完備** → テンプレート化可能

### 移行によるメリット

1. **開発速度向上**: ドメインモデリングにのみ集中できる
2. **品質向上**: システムレベルの実装が統一される
3. **保守性向上**: 共通基盤のバグ修正が全プロジェクトに波及
4. **工業化**: 「受託開発」から「プロダクト+ドメイン差分の組み立て」へ

### 実装完了項目

- ✅ **Phase 1**: SessionProvider, ThemeProvider, NotificationProvider実装 **（完了）**
  - SessionProvider.razor
  - ThemeProvider.razor
  - PreferencesStore.cs
  - LayoutStore.cs
  - NotificationStore.cs
  - LocalStorageService.cs
  - theme.js（テーマ切り替えJSモジュール）

### 次のアクション

1. **Phase 2**: NuGetパッケージ化（`ProductCatalog.Platform.Blazor`）
2. **Phase 3**: プロジェクトテンプレート化（`dotnet new productcatalog`）
3. **Phase 4**: ドキュメント整備（新規プロジェクト開発ガイド）
4. **Phase 5**: 実装ドキュメントの充実化（使用例、ベストプラクティス）

---

**作成日**: 2025-11-02
**最終更新**: 2025-11-02
**対象プロジェクト**: ProductCatalog
**ステータス**: ✅ **Phase 1 実装完了** → 次フェーズ: NuGetパッケージ化/テンプレート化
