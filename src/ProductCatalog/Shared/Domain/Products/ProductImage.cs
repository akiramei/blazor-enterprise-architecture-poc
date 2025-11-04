using Shared.Kernel;

namespace ProductCatalog.Shared.Domain.Products;

/// <summary>
/// 商品画像エンティティ（子エンティティ）
///
/// 【パターン: 子エンティティ】
///
/// 使用シナリオ:
/// - 親集約（Product）の一部として管理されるエンティティ
/// - 親なしでは存在意義がないエンティティ
/// - 親集約の整合性を保つために親経由でのみ操作したいエンティティ
///
/// 実装ガイド:
/// - 子エンティティは Entity を継承（AggregateRoot ではない）
/// - 親集約（Product）からのみアクセス可能にする
/// - 外部から直接インスタンス化させない（内部ファクトリメソッドまたは親集約経由）
/// - 親集約が不変条件を保護する（例: 画像は最大10枚まで）
///
/// AI実装時の注意:
/// - 子エンティティのコレクションは親集約で private List として保持
/// - 公開プロパティは IReadOnlyList で公開
/// - 追加/削除は親集約のメソッド経由（Product.AddImage(), Product.RemoveImage()）
/// - Repository は親集約のみを対象とする（子エンティティ専用のRepositoryは作らない）
/// </summary>
public sealed class ProductImage : Entity
{
    private ProductImageId _id = null!;
    private string _url = string.Empty;
    private int _displayOrder;

    /// <summary>
    /// 画像ID
    /// </summary>
    public ProductImageId Id => _id;

    /// <summary>
    /// 画像URL
    /// </summary>
    public string Url => _url;

    /// <summary>
    /// 表示順序（0から開始）
    /// </summary>
    public int DisplayOrder => _displayOrder;

    /// <summary>
    /// EF Core用のパラメータレスコンストラクタ
    /// </summary>
    private ProductImage()
    {
    }

    /// <summary>
    /// コンストラクタ（内部用）
    /// </summary>
    private ProductImage(ProductImageId id, string url, int displayOrder)
    {
        _id = id ?? throw new ArgumentNullException(nameof(id));
        ChangeUrl(url);
        ChangeDisplayOrder(displayOrder);
    }

    /// <summary>
    /// ファクトリメソッド（内部用）
    ///
    /// 【パターン: 内部ファクトリメソッド】
    /// 外部から直接new ProductImage()できないようにし、
    /// 親集約（Product）経由でのみ作成可能にする
    /// </summary>
    internal static ProductImage Create(ProductImageId id, string url, int displayOrder)
    {
        return new ProductImage(id, url, displayOrder);
    }

    /// <summary>
    /// 画像URLを変更
    /// </summary>
    internal void ChangeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DomainException("画像URLは必須です");
        }

        if (url.Length > 500)
        {
            throw new DomainException("画像URLは500文字以内である必要があります");
        }

        // 簡易的なURL検証
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            throw new DomainException("有効なURLを指定してください");
        }

        _url = url;
    }

    /// <summary>
    /// 表示順序を変更
    /// </summary>
    internal void ChangeDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new DomainException("表示順序は0以上である必要があります");
        }

        _displayOrder = displayOrder;
    }
}
