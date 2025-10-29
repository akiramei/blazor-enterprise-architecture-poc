using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using ProductCatalog.Infrastructure.Persistence;
using ProductCatalog.Web.Components;

namespace ProductCatalog.E2ETests;

/// <summary>
/// Playwright E2Eテストの基底クラス
///
/// 【パターン: E2Eテスト基盤】
///
/// 使用シナリオ:
/// - ブラウザの自動操作
/// - エンドツーエンドのユーザーシナリオテスト
/// - クロスブラウザテスト
///
/// 実装ガイド:
/// - IAsyncLifetimeでテスト前後のセットアップ/クリーンアップ
/// - WebApplicationで直接テストサーバー起動
/// - Playwrightでブラウザ制御
///
/// AI実装時の注意:
/// - Headlessモードで実行（CI環境）
/// - テスト完了後は必ずリソース解放
/// - ページ遷移は WaitForURLAsync で確実に待機
/// - 要素の表示待ちは WaitForSelectorAsync を使用
/// </summary>
public abstract class PlaywrightTestBase : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private WebApplication? _app;
    protected string BaseUrl { get; private set; } = string.Empty;
    protected IPage? Page { get; private set; }

    public async Task InitializeAsync()
    {
        // Playwrightセットアップ
        _playwright = await Playwright.CreateAsync();

        // ブラウザ起動（ヘッドレスモード）
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true, // CI環境用。ローカルデバッグ時はfalseに変更可能
            SlowMo = 0 // ミリ秒単位で操作を遅くする（デバッグ用）
        });

        // テストサーバー起動（Kestrelで実際のポートをリッスン）
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Test";
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        // Program.csと同じ設定を適用（簡易版）
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("TestDatabase"));

        _app = builder.Build();

        // 最小限のミドルウェア設定
        _app.UseStaticFiles();
        _app.UseAntiforgery();
        _app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        // サーバーを起動
        await _app.StartAsync();

        // サーバーアドレスを取得
        var addresses = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>();
        BaseUrl = addresses!.Addresses.First();

        // 新しいページを作成
        Page = await _browser.NewPageAsync();

        // デフォルトタイムアウト設定（30秒）
        Page.SetDefaultTimeout(30000);
    }

    public async Task DisposeAsync()
    {
        if (Page != null)
        {
            await Page.CloseAsync();
        }

        if (_browser != null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();

        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    /// <summary>
    /// 指定したURLに移動
    /// </summary>
    protected async Task GotoAsync(string path)
    {
        if (Page == null) throw new InvalidOperationException("Page is not initialized");
        await Page.GotoAsync($"{BaseUrl}{path}");
    }

    /// <summary>
    /// スクリーンショットを保存（デバッグ用）
    /// </summary>
    protected async Task<byte[]> TakeScreenshotAsync()
    {
        if (Page == null) throw new InvalidOperationException("Page is not initialized");
        return await Page.ScreenshotAsync();
    }
}
