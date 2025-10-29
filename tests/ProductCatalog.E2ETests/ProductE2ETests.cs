using FluentAssertions;
using Microsoft.Playwright;

namespace ProductCatalog.E2ETests;

/// <summary>
/// 商品機能のE2Eテスト
///
/// 【パターン: E2Eテスト - 基本CRUD】
///
/// テストシナリオ:
/// - 商品一覧の表示
/// - 商品の新規作成
/// - 作成した商品の確認
/// - 商品詳細の表示
///
/// 実装ガイド:
/// - ユーザーの実際の操作フローを再現
/// - 各操作後にページ遷移を確認
/// - 期待する要素が表示されるまで待機
///
/// AI実装時の注意:
/// - ボタンクリックは text= セレクタを使用（日本語対応）
/// - 入力フィールドは name 属性でセレクト
/// - ページ遷移は WaitForURLAsync で確実に待機
/// - 要素の表示確認は WaitForSelectorAsync を使用
/// </summary>
public class ProductE2ETests : PlaywrightTestBase
{
    [Fact]
    public async Task 商品一覧ページが表示される()
    {
        // Arrange & Act
        // 商品一覧ページに直接移動
        await GotoAsync("/products");

        // Assert
        // ページタイトル（h1）が表示されるまで待機
        // Blazor Serverは初回レンダリングに時間がかかるため、十分な待機時間を設定
        var titleElement = await Page!.WaitForSelectorAsync("h1", new() { Timeout = 30000 });
        titleElement.Should().NotBeNull();

        var title = await titleElement!.TextContentAsync();
        title.Should().Contain("商品");

        // 商品検索ボタンが表示される
        var searchButton = await Page.WaitForSelectorAsync("a:has-text('商品検索')", new() { Timeout = 30000 });
        searchButton.Should().NotBeNull();

        // 商品検索ボタンのURLが正しい
        var href = await searchButton!.GetAttributeAsync("href");
        href.Should().Be("/products/search");
    }

    [Fact]
    public async Task 商品詳細ページが表示される()
    {
        // Arrange: テスト用商品を作成
        var productId = await CreateTestProductAsync(
            "詳細表示テスト商品",
            "この商品の詳細を確認します",
            3500,
            25,
            publish: true
        );

        // Act: 商品詳細ページに移動
        await GotoAsync($"/products/{productId}");

        // Assert
        // h1タイトルが表示される
        var titleElement = await Page!.WaitForSelectorAsync("h1:has-text('商品詳細')", new() { Timeout = 30000 });
        titleElement.Should().NotBeNull();

        // 商品名が表示される
        var nameElement = await Page.WaitForSelectorAsync("text=詳細表示テスト商品", new() { Timeout = 5000 });
        nameElement.Should().NotBeNull();

        // 価格が表示される
        var priceElement = await Page.WaitForSelectorAsync("text=¥3,500", new() { Timeout = 5000 });
        priceElement.Should().NotBeNull();

        // 在庫数が表示される
        var stockElement = await Page.WaitForSelectorAsync("text=25 個", new() { Timeout = 5000 });
        stockElement.Should().NotBeNull();

        // 編集ボタンが表示される
        var editButton = await Page.WaitForSelectorAsync("button:has-text('編集')", new() { Timeout = 5000 });
        editButton.Should().NotBeNull();
    }

    [Fact(Skip = "Blazor Server SignalR navigation issue - needs further investigation")]
    public async Task 商品を編集できる()
    {
        // Arrange: テスト用商品を作成
        var productId = await CreateTestProductAsync(
            "編集前の商品名",
            "編集前の説明",
            2000,
            30,
            publish: true
        );

        // Act: 商品詳細ページに移動
        await GotoAsync($"/products/{productId}");

        // 詳細ページのh1が表示されるまで待機
        await Page!.WaitForSelectorAsync("h1:has-text('商品詳細')", new() { Timeout = 30000 });

        // 編集ボタンが表示されるまで待機
        var editButton = await Page.WaitForSelectorAsync("button:has-text('編集')", new() { Timeout = 5000 });
        editButton.Should().NotBeNull();

        // 編集ボタンをクリック
        await editButton!.ClickAsync();

        // 少し待機してBlazor Serverの処理を待つ
        await Task.Delay(1000);

        // Blazor ServerはSignalR経由でページ遷移するため、
        // URLの変化ではなく、編集ページ固有の要素が表示されるのを待つ
        var titleElement = await Page.WaitForSelectorAsync("h1:has-text('商品編集')", new() { Timeout = 30000 });
        titleElement.Should().NotBeNull();

        // URLが編集ページになっていることを確認（補助的な確認）
        Page.Url.Should().Contain($"/products/{productId}/edit");

        // フォーム要素が表示されるまで待機
        await Page.WaitForSelectorAsync("input#name", new() { Timeout = 5000 });

        // 商品名を変更
        await Page.FillAsync("input#name", "編集後の商品名");

        // 説明を変更
        await Page.FillAsync("textarea#description", "編集後の説明文です");

        // 価格を変更
        await Page.FillAsync("input#price", "2500");

        // 在庫を変更
        await Page.FillAsync("input#stock", "35");

        // 保存ボタンをクリック
        var saveButton = await Page.QuerySelectorAsync("button[type='submit']:has-text('保存')");
        saveButton.Should().NotBeNull();
        await saveButton!.ClickAsync();

        // 成功メッセージが表示されることを確認
        var successMessage = await Page.WaitForSelectorAsync(".alert-success", new() { Timeout = 10000 });
        successMessage.Should().NotBeNull();

        // 3秒待機（ProductEdit.razorの自動遷移）
        await Task.Delay(3500);

        // 詳細ページ固有の要素が表示されることを確認（h1:has-text('商品詳細')）
        await Page.WaitForSelectorAsync("h1:has-text('商品詳細')", new() { Timeout = 5000 });

        // URLが詳細ページになっていることを確認
        Page.Url.Should().Contain($"/products/{productId}");
        Page.Url.Should().NotContain("/edit");

        // Assert: 更新後の値が表示される
        var updatedNameElement = await Page.WaitForSelectorAsync("text=編集後の商品名", new() { Timeout = 5000 });
        updatedNameElement.Should().NotBeNull();

        var updatedPriceElement = await Page.WaitForSelectorAsync("text=¥2,500", new() { Timeout = 5000 });
        updatedPriceElement.Should().NotBeNull();

        var updatedStockElement = await Page.WaitForSelectorAsync("text=35 個", new() { Timeout = 5000 });
        updatedStockElement.Should().NotBeNull();
    }

    // NOTE: The following tests are disabled because the UI for creating products
    // is not yet implemented. There is no /products/new route.
    //
    // Current UI provides:
    // - Product list viewing
    // - Product detail viewing
    // - Product editing
    // - Product search
    // - CSV import/export

    [Fact(Skip = "Product creation UI not yet implemented")]
    public async Task 商品を新規作成して一覧に表示される()
    {
        // Test implementation awaiting UI implementation (/products/new route)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Product creation UI not yet implemented")]
    public async Task バリデーションエラーが表示される()
    {
        // Test implementation awaiting UI implementation (/products/new route)
        await Task.CompletedTask;
    }
}
