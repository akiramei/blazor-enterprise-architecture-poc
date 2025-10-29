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
        // まずルートパスにアクセスして、基本的な動作を確認
        await GotoAsync("/");
        await Task.Delay(2000); // Blazor の初期化を待つ

        // 商品一覧ページに移動
        await GotoAsync("/products");
        await Task.Delay(3000); // SignalR接続とレンダリングを待つ

        // デバッグ: コンソールエラーをキャプチャ
        Page!.Console += (_, msg) => Console.WriteLine($"Console: {msg.Text}");
        Page.PageError += (_, err) => Console.WriteLine($"Page Error: {err}");

        // Assert
        // ページタイトルが表示されるまで待機
        try
        {
            await Page.WaitForSelectorAsync("h2", new() { Timeout = 10000 });
            var title = await Page.TextContentAsync("h2");
            title.Should().Contain("商品");

            // 新規作成ボタンが表示される
            var newButton = await Page.QuerySelectorAsync("text=新規作成");
            newButton.Should().NotBeNull();
        }
        catch (Exception ex)
        {
            // デバッグ情報を出力
            Console.WriteLine($"Error: {ex.Message}");
            var html = await Page.ContentAsync();
            Console.WriteLine($"HTML: {html.Substring(0, Math.Min(1000, html.Length))}");
            throw;
        }
    }

    [Fact]
    public async Task 商品を新規作成して一覧に表示される()
    {
        // Arrange
        var productName = $"E2Eテスト商品_{DateTime.Now:HHmmss}";
        var productDescription = "自動テストで作成された商品";
        var price = "5000";
        var stock = "100";

        // 商品一覧ページに移動
        await GotoAsync("/products");

        // Act
        // 新規作成ボタンをクリック
        await Page!.ClickAsync("text=新規作成");

        // 新規作成ページに遷移したことを確認
        await Page.WaitForURLAsync("**/products/new");

        // フォームに入力
        await Page.FillAsync("input[name='Name']", productName);
        await Page.FillAsync("textarea[name='Description']", productDescription);
        await Page.FillAsync("input[name='Price']", price);
        await Page.FillAsync("input[name='Stock']", stock);

        // 保存ボタンをクリック
        await Page.ClickAsync("button:has-text('保存')");

        // 一覧ページに戻ることを確認
        await Page.WaitForURLAsync("**/products");

        // Assert
        // 成功メッセージが表示される
        var successMessage = await Page.WaitForSelectorAsync(".alert-success", new PageWaitForSelectorOptions
        {
            Timeout = 5000
        });
        successMessage.Should().NotBeNull();

        // 作成した商品が一覧に表示される
        var productCell = await Page.WaitForSelectorAsync($"text={productName}", new PageWaitForSelectorOptions
        {
            Timeout = 5000
        });
        productCell.Should().NotBeNull();

        // 価格が表示される
        var priceCell = await Page.QuerySelectorAsync("text=¥5,000");
        priceCell.Should().NotBeNull();
    }

    [Fact]
    public async Task 商品詳細ページが表示される()
    {
        // Arrange
        // まず商品を作成
        var productName = $"詳細テスト商品_{DateTime.Now:HHmmss}";
        await CreateTestProductAsync(productName, "テスト説明", "3000", "50");

        // 一覧ページに移動
        await GotoAsync("/products");

        // Act
        // 作成した商品の詳細リンクをクリック
        var row = await Page!.WaitForSelectorAsync($"tr:has-text('{productName}')");
        row.Should().NotBeNull();

        // 行内の詳細リンクをクリック
        var detailLink = await row!.QuerySelectorAsync("text=詳細");
        await detailLink!.ClickAsync();

        // 詳細ページに遷移したことを確認
        await Page.WaitForURLAsync("**/products/*");

        // Assert
        // 商品名が表示される
        var nameElement = await Page.WaitForSelectorAsync($"text={productName}");
        nameElement.Should().NotBeNull();

        // 価格が表示される
        var priceElement = await Page.QuerySelectorAsync("text=¥3,000");
        priceElement.Should().NotBeNull();

        // 在庫数が表示される
        var stockElement = await Page.QuerySelectorAsync("text=50");
        stockElement.Should().NotBeNull();
    }

    [Fact]
    public async Task バリデーションエラーが表示される()
    {
        // Arrange
        await GotoAsync("/products/new");

        // Act
        // 必須項目を空のまま保存
        await Page!.ClickAsync("button:has-text('保存')");

        // Assert
        // バリデーションエラーメッセージが表示される
        var errorMessages = await Page.QuerySelectorAllAsync(".validation-message");
        errorMessages.Should().NotBeEmpty();

        // ページ遷移していないことを確認（エラー時は同じページに留まる）
        Page.Url.Should().Contain("/products/new");
    }

    [Fact]
    public async Task 商品を編集できる()
    {
        // Arrange
        var originalName = $"編集前商品_{DateTime.Now:HHmmss}";
        var updatedName = $"編集後商品_{DateTime.Now:HHmmss}";
        await CreateTestProductAsync(originalName, "編集前", "2000", "30");

        // 一覧ページに移動
        await GotoAsync("/products");

        // Act
        // 作成した商品の編集リンクをクリック
        var row = await Page!.WaitForSelectorAsync($"tr:has-text('{originalName}')");
        var editLink = await row!.QuerySelectorAsync("text=編集");
        await editLink!.ClickAsync();

        // 編集ページに遷移
        await Page.WaitForURLAsync("**/products/*/edit");

        // 商品名を変更
        await Page.FillAsync("input[name='Name']", updatedName);

        // 保存
        await Page.ClickAsync("button:has-text('保存')");

        // 詳細ページに遷移することを確認
        await Page.WaitForURLAsync("**/products/*");
        await Page.WaitForSelectorAsync(".alert-success");

        // Assert
        // 更新後の商品名が表示される
        var nameElement = await Page.WaitForSelectorAsync($"text={updatedName}");
        nameElement.Should().NotBeNull();

        // 一覧ページでも更新されていることを確認
        await GotoAsync("/products");
        var updatedCell = await Page.QuerySelectorAsync($"text={updatedName}");
        updatedCell.Should().NotBeNull();
    }

    /// <summary>
    /// テスト用商品を作成するヘルパーメソッド
    /// </summary>
    private async Task CreateTestProductAsync(string name, string description, string price, string stock)
    {
        await GotoAsync("/products/new");
        await Page!.FillAsync("input[name='Name']", name);
        await Page.FillAsync("textarea[name='Description']", description);
        await Page.FillAsync("input[name='Price']", price);
        await Page.FillAsync("input[name='Stock']", stock);
        await Page.ClickAsync("button:has-text('保存')");
        await Page.WaitForURLAsync("**/products");
    }
}
