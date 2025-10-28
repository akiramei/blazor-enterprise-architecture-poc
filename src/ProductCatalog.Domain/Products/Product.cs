using ProductCatalog.Domain.Common;
using ProductCatalog.Domain.Products.Events;

namespace ProductCatalog.Domain.Products;

/// <summary>
/// 商品集約ルート
/// </summary>
public sealed class Product : Entity
{
    private ProductId _id = null!;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private Money _price = null!;
    private int _stock;
    private bool _isDeleted;

    // EF Core用のプライベートコンストラクタ
    private Product()
    {
    }

    public Product(ProductId id, string name, string description, Money price, int stock)
    {
        _id = id ?? throw new ArgumentNullException(nameof(id));
        ChangeName(name);
        ChangeDescription(description);
        ChangePrice(price);
        ChangeStock(stock);
        _isDeleted = false;
    }

    // Public properties（読み取り専用）
    public ProductId Id => _id;
    public string Name => _name;
    public string Description => _description;
    public Money Price => _price;
    public int Stock => _stock;
    public bool IsDeleted => _isDeleted;

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
    /// </summary>
    public void ChangePrice(Money price)
    {
        _price = price ?? throw new ArgumentNullException(nameof(price));

        if (price.Amount <= 0)
        {
            throw new DomainException("価格は0より大きい必要があります");
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
            throw new DomainException("在庫が不足しています");
        }

        _stock -= quantity;
    }

    /// <summary>
    /// 商品を削除（ビジネスルール適用）
    /// </summary>
    public void Delete()
    {
        if (_stock > 0)
        {
            throw new DomainException("在庫がある商品は削除できません");
        }

        if (_isDeleted)
        {
            throw new DomainException("既に削除されています");
        }

        _isDeleted = true;

        // ドメインイベントを発行
        RaiseDomainEvent(new ProductDeletedDomainEvent(_id, _name));
    }

    /// <summary>
    /// ファクトリーメソッド
    /// </summary>
    public static Product Create(string name, string description, Money price, int initialStock)
    {
        return new Product(ProductId.New(), name, description, price, initialStock);
    }
}
