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

    [Fact]
    public async Task 商品編集ページが表示される()
    {
        // Arrange: テスト用商品を作成
        var productId = await CreateTestProductAsync(
            "編集ページ表示テスト商品",
            "これは編集ページの表示をテストします",
            2000,
            30,
            publish: true
        );

        // Act: 編集ページに直接ナビゲート
        await GotoAsync($"/products/{productId}/edit");

        // Assert
        // h1タイトルが表示される
        var titleElement = await Page!.WaitForSelectorAsync("h1:has-text('商品編集')", new() { Timeout = 30000 });
        titleElement.Should().NotBeNull();

        // フォーム要素が表示される
        var nameInput = await Page.WaitForSelectorAsync("input#name", new() { Timeout = 5000 });
        nameInput.Should().NotBeNull();

        var descriptionInput = await Page.WaitForSelectorAsync("textarea#description", new() { Timeout = 5000 });
        descriptionInput.Should().NotBeNull();

        var priceInput = await Page.WaitForSelectorAsync("input#price", new() { Timeout = 5000 });
        priceInput.Should().NotBeNull();

        var stockInput = await Page.WaitForSelectorAsync("input#stock", new() { Timeout = 5000 });
        stockInput.Should().NotBeNull();

        // 商品IDとバージョン（readonly）が表示される
        var productIdField = await Page.WaitForSelectorAsync("input[value='" + productId + "']", new() { Timeout = 5000 });
        productIdField.Should().NotBeNull();

        // 保存ボタンが表示される
        var saveButton = await Page.WaitForSelectorAsync("button[type='submit']:has-text('保存')", new() { Timeout = 5000 });
        saveButton.Should().NotBeNull();

        // キャンセルボタンが表示される
        var cancelButton = await Page.WaitForSelectorAsync("button:has-text('キャンセル')", new() { Timeout = 5000 });
        cancelButton.Should().NotBeNull();
    }

    // NOTE: 商品作成UIは未実装のため、テストは実装していません
    //
    // 将来的に /products/new ルートが実装されたら、以下のテストケースを追加できます：
    // - 商品を新規作成して一覧に表示される
    // - バリデーションエラーが表示される
    // - 必須項目の検証
    //
    // 現在利用可能な機能:
    // - 商品一覧表示
    // - 商品詳細表示
    // - 商品編集ページ表示
    // - 商品検索
    // - CSVインポート/エクスポート
}
