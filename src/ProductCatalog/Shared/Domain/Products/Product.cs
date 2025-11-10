using Shared.Kernel;
using ProductCatalog.Shared.Domain.Products.Events;

namespace ProductCatalog.Shared.Domain.Products;

/// <summary>
/// 商品集約ルート
///
/// 【パターン: AggregateRoot（集約ルート）】
///
/// 使用シナリオ:
/// - 商品の基本情報管理（名前、説明、価格、在庫）
/// - 商品のライフサイクル管理（Draft → Published → Archived）
/// - 商品画像の管理（親子関係）
/// - ビジネスルールの保護
///
/// ビジネスルール:
/// - 在庫がある商品は削除できない
/// - 公開中の商品は50%以上の値下げはできない
/// - 在庫がない商品は公開できない
/// - アーカイブ済みの商品は公開できない
/// - 商品画像は最大10枚まで
///
/// 実装ガイド:
/// - すべてのフィールドは private
/// - 公開プロパティは読み取り専用（getter のみ）
/// - 変更はメソッド経由（ChangeXxx, AddXxx, RemoveXxx）
/// - ビジネスルールはメソッド内で検証
/// - ルール違反時は DomainException をスロー
/// - 重要な変更時はドメインイベントを発行
///
/// AI実装時の注意:
/// - AggregateRoot<ProductId> を継承
/// - 子エンティティ（ProductImage）は private コレクション
/// - 公開は IReadOnlyList で
/// - 画像の追加/削除は親集約（Product）のメソッド経由
/// - 状態遷移は Publish(), Archive() メソッド経由
/// </summary>
public sealed class Product : AggregateRoot<ProductId>
{
    private string _name = string.Empty;
    private string _description = string.Empty;
    private Money _price = null!;
    private int _stock;
    private ProductStatus _status;
    private readonly List<ProductImage> _images = new();
    private bool _isDeleted;

    // Public properties（読み取り専用）
    public string Name => _name;
    public string Description => _description;
    public Money Price => _price;
    public int Stock => _stock;
    public ProductStatus Status => _status;
    public IReadOnlyList<ProductImage> Images => _images.AsReadOnly();
    public bool IsDeleted => _isDeleted;

    /// <summary>
    /// EF Core用のパラメータレスコンストラクタ
    /// </summary>
    private Product()
    {
    }

    /// <summary>
    /// コンストラクタ（内部用）
    /// </summary>
    private Product(ProductId id, string name, string description, Money price, int stock)
        : base(id)
    {
        // コンストラクタ内では直接フィールドに代入（イベント発行は行わない）
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("商品名は必須です");
        }
        if (name.Length > 200)
        {
            throw new DomainException("商品名は200文字以内である必要があります");
        }
        _name = name;

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("説明は必須です");
        }
        if (description.Length > 2000)
        {
            throw new DomainException("説明は2000文字以内である必要があります");
        }
        _description = description;

        if (price == null || price.Amount <= 0)
        {
            throw new DomainException("価格は0より大きい値である必要があります");
        }
        _price = price;

        if (stock < 0)
        {
            throw new DomainException("在庫数は0以上である必要があります");
        }
        _stock = stock;

        _status = ProductStatus.Draft;  // 初期状態は下書き
        _isDeleted = false;
    }

    /// <summary>
    /// ファクトリーメソッド
    ///
    /// 【パターン: ファクトリーメソッド】
    /// new Product() を外部から呼べないようにし、
    /// 必ずこのメソッド経由で作成することで、
    /// 初期化時のビジネスルールを保証する
    /// </summary>
    public static Product Create(string name, string description, Money price, int initialStock)
    {
        if (initialStock < 0)
        {
            throw new DomainException("初期在庫は0以上である必要があります");
        }

        var product = new Product(ProductId.New(), name, description, price, initialStock);

        // ドメインイベントを発行（統合イベント配信、通知などに使用）
        product.RaiseDomainEvent(new ProductCreatedDomainEvent(product.Id, name, price, initialStock));

        return product;
    }

    /// <summary>
    /// 商品名を変更
    /// </summary>
    public void ChangeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("商品名は必須です");
        }

        if (name.Length > 200)
        {
            throw new DomainException("商品名は200文字以内である必要があります");
        }

        _name = name;

        // 商品更新イベントを発行
        RaiseDomainEvent(new ProductUpdatedDomainEvent(Id, name));
    }

    /// <summary>
    /// 説明を変更
    /// </summary>
    public void ChangeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("説明は必須です");
        }

        if (description.Length > 2000)
        {
            throw new DomainException("説明は2000文字以内である必要があります");
        }

        _description = description;
    }

    /// <summary>
    /// 価格を変更
    ///
    /// 【パターン: 状態に応じたビジネスルール】
    /// 公開中の商品は、50%以上の値下げを禁止
    /// </summary>
    public void ChangePrice(Money price)
    {
        if (price == null)
        {
            throw new ArgumentNullException(nameof(price));
        }

        if (price.Amount <= 0)
        {
            throw new DomainException("価格は0より大きい必要があります");
        }

        // ビジネスルール: 公開中の商品は50%以上の値下げを禁止
        if (_status == ProductStatus.Published && _price != null)
        {
            var discountRate = 1 - (price.Amount / _price.Amount);
            if (discountRate > 0.5m)
            {
                throw new DomainException(
                    $"公開中の商品は50%以上の値下げはできません。" +
                    $"現在価格: {_price.Amount:C}, 変更後価格: {price.Amount:C}, 値下げ率: {discountRate:P0}");
            }
        }

        var oldPrice = _price;
        _price = price;

        // 価格変更イベントを発行
        if (oldPrice != null && oldPrice.Amount != price.Amount)
        {
            RaiseDomainEvent(new ProductPriceChangedDomainEvent(Id, oldPrice, price));
        }
    }

    /// <summary>
    /// 在庫を変更
    /// </summary>
    public void ChangeStock(int stock)
    {
        if (stock < 0)
        {
            throw new DomainException("在庫数は0以上である必要があります");
        }

        _stock = stock;
    }

    /// <summary>
    /// 在庫を追加
    /// </summary>
    public void AddStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("追加数は0より大きい必要があります");
        }

        _stock += quantity;
    }

    /// <summary>
    /// 在庫を減少
    /// </summary>
    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("減少数は0より大きい必要があります");
        }

        if (_stock < quantity)
        {
            throw new DomainException($"在庫が不足しています。現在在庫: {_stock}, 要求数: {quantity}");
        }

        _stock -= quantity;
    }

    /// <summary>
    /// 在庫を予約（注文確定時などに使用）
    ///
    /// 【パターン: ビジネス用語でメソッドを命名】
    /// ReduceStock ではなく ReserveStock という
    /// ビジネス的に意味のある名前を使用
    /// </summary>
    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("予約数は0より大きい必要があります");
        }

        if (_stock < quantity)
        {
            throw new DomainException($"在庫が不足しているため予約できません。現在在庫: {_stock}, 予約要求: {quantity}");
        }

        _stock -= quantity;
    }

    /// <summary>
    /// 商品を公開
    ///
    /// 【パターン: 状態遷移メソッド】
    /// Draft → Published への遷移
    /// </summary>
    public void Publish()
    {
        if (_status == ProductStatus.Published)
        {
            throw new DomainException("既に公開済みです");
        }

        if (_status == ProductStatus.Archived)
        {
            throw new DomainException("アーカイブ済みの商品は公開できません");
        }

        if (_stock <= 0)
        {
            throw new DomainException("在庫がない商品は公開できません");
        }

        _status = ProductStatus.Published;

        // ドメインイベントを発行
        RaiseDomainEvent(new ProductPublishedDomainEvent(Id));
    }

    /// <summary>
    /// 商品をアーカイブ
    ///
    /// 【パターン: 状態遷移メソッド】
    /// Published → Archived への遷移
    /// または Draft → Archived への遷移
    /// </summary>
    public void Archive()
    {
        if (_status == ProductStatus.Archived)
        {
            throw new DomainException("既にアーカイブ済みです");
        }

        _status = ProductStatus.Archived;
    }

    /// <summary>
    /// 商品を再公開（アーカイブ状態から公開に戻す）
    /// </summary>
    public void Republish()
    {
        if (_status != ProductStatus.Archived)
        {
            throw new DomainException("アーカイブ状態の商品のみ再公開できます");
        }

        if (_stock <= 0)
        {
            throw new DomainException("在庫がない商品は公開できません");
        }

        _status = ProductStatus.Published;
        RaiseDomainEvent(new ProductPublishedDomainEvent(Id));
    }

    /// <summary>
    /// 商品を削除（論理削除）
    ///
    /// 【パターン: ビジネスルール検証】
    /// </summary>
    public void Delete()
    {
        // ビジネスルール: 在庫がある商品は削除できない
        if (_stock > 0)
        {
            throw new DomainException($"在庫がある商品は削除できません。現在在庫: {_stock}");
        }

        if (_isDeleted)
        {
            throw new DomainException("既に削除されています");
        }

        _isDeleted = true;

        // ドメインイベントを発行
        RaiseDomainEvent(new ProductDeletedDomainEvent(Id, _name));
    }

    /// <summary>
    /// 画像を追加
    ///
    /// 【パターン: 子エンティティの管理】
    /// 親集約経由で子エンティティを追加
    /// </summary>
    public void AddImage(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("画像URLは必須です", nameof(url));
        }

        // ビジネスルール: 画像は最大10枚まで
        if (_images.Count >= 10)
        {
            throw new DomainException("商品画像は最大10枚までです");
        }

        // 表示順序は既存の画像数
        var displayOrder = _images.Any() ? _images.Max(i => i.DisplayOrder) + 1 : 0;

        var image = ProductImage.Create(ProductImageId.New(), url, displayOrder);
        _images.Add(image);
    }

    /// <summary>
    /// 画像を削除
    ///
    /// 【パターン: 子エンティティの管理】
    /// 親集約経由で子エンティティを削除
    /// </summary>
    public void RemoveImage(ProductImageId imageId)
    {
        if (imageId == null)
        {
            throw new ArgumentNullException(nameof(imageId));
        }

        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image == null)
        {
            throw new DomainException("指定された画像が見つかりません");
        }

        _images.Remove(image);

        // 表示順序を再設定
        ReorderImages();
    }

    /// <summary>
    /// 画像の表示順序を変更
    /// </summary>
    public void ReorderImage(ProductImageId imageId, int newDisplayOrder)
    {
        if (imageId == null)
        {
            throw new ArgumentNullException(nameof(imageId));
        }

        if (newDisplayOrder < 0 || newDisplayOrder >= _images.Count)
        {
            throw new DomainException($"表示順序は0以上{_images.Count}未満である必要があります");
        }

        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image == null)
        {
            throw new DomainException("指定された画像が見つかりません");
        }

        var oldOrder = image.DisplayOrder;
        if (oldOrder == newDisplayOrder)
        {
            return;  // 変更なし
        }

        _images.Remove(image);
        _images.Insert(newDisplayOrder, image);

        // 全画像の表示順序を再設定
        ReorderImages();
    }

    /// <summary>
    /// 画像の表示順序を0から連番で振り直す
    /// </summary>
    private void ReorderImages()
    {
        for (var i = 0; i < _images.Count; i++)
        {
            _images[i].ChangeDisplayOrder(i);
        }
    }
}
