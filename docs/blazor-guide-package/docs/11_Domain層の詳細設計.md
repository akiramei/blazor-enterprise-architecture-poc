# 11. Domain層の詳細設計

[← 目次に戻る](00_README.md)

---

## 11. Domain層の詳細設計

### 9.1 Aggregate Root(集約ルート)

#### **設計原則**

- **Consistency Boundary**: トランザクション境界
- **Business Rules**: ビジネスルール保護
- **Domain Events**: 状態変更を通知

#### **実装例**

```csharp
/// <summary>
/// 商品集約ルート
/// </summary>
public sealed class Product : AggregateRoot<ProductId>
{
    // Private フィールド(カプセル化)
    private string _name;
    private string _description;
    private Money _price;
    private int _stock;
    private ProductStatus _status;
    private readonly List<ProductImage> _images = new();
    
    // Public読み取り専用プロパティ
    public string Name => _name;
    public string Description => _description;
    public Money Price => _price;
    public int Stock => _stock;
    public ProductStatus Status => _status;
    public IReadOnlyList<ProductImage> Images => _images.AsReadOnly();
    
    // EF Core用プライベートコンストラクタ
    private Product() { }
    
    // ファクトリメソッド
    public static Product Create(
        ProductId id,
        string name,
        string description,
        Money price,
        int initialStock)
    {
        // ビジネスルール検証
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("商品名は必須です");
        
        if (name.Length > 200)
            throw new DomainException("商品名は200文字以内で入力してください");
        
        if (price.Amount <= 0)
            throw new DomainException("価格は0より大きい値を設定してください");
        
        if (initialStock < 0)
            throw new DomainException("初期在庫は0以上の値を設定してください");
        
        var product = new Product
        {
            Id = id,
            _name = name,
            _description = description,
            _price = price,
            _stock = initialStock,
            _status = ProductStatus.Draft
        };
        
        // ドメインイベント発行
        product.RaiseDomainEvent(new ProductCreatedEvent(id, name, price));
        
        return product;
    }
    
    /// <summary>
    /// 商品名を変更
    /// </summary>
    public void ChangeName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("商品名は必須です");
        
        if (newName.Length > 200)
            throw new DomainException("商品名は200文字以内で入力してください");
        
        if (_name == newName)
            return;  // 変更なし
        
        var oldName = _name;
        _name = newName;
        
        RaiseDomainEvent(new ProductNameChangedEvent(Id, oldName, newName));
    }
    
    /// <summary>
    /// 価格を変更
    /// </summary>
    public void ChangePrice(Money newPrice)
    {
        if (newPrice.Amount <= 0)
            throw new DomainException("価格は0より大きい値を設定してください");
        
        // ビジネスルール制約: 公開中の商品は50%以上の値下げを禁止
        if (_status == ProductStatus.Published)
        {
            var discountRate = 1 - (newPrice.Amount / _price.Amount);
            if (discountRate > 0.5m)
                throw new DomainException("公開中の商品は50%以上の値下げはできません");
        }
        
        var oldPrice = _price;
        _price = newPrice;
        
        RaiseDomainEvent(new ProductPriceChangedEvent(Id, oldPrice, newPrice));
    }
    
    /// <summary>
    /// 在庫を追加
    /// </summary>
    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("追加数量は1以上を指定してください");
        
        _stock += quantity;
        
        RaiseDomainEvent(new StockAddedEvent(Id, quantity, _stock));
    }
    
    /// <summary>
    /// 在庫を引当
    /// </summary>
    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("引当数量は1以上を指定してください");
        
        if (_stock < quantity)
            throw new DomainException($"在庫が不足しています(在庫: {_stock}, 必要: {quantity})");
        
        _stock -= quantity;
        
        RaiseDomainEvent(new StockReservedEvent(Id, quantity, _stock));
    }
    
    /// <summary>
    /// 画像を追加
    /// </summary>
    public void AddImage(string url, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("画像URLは必須です");
        
        // ビジネスルール制約: 商品画像は最大10枚まで登録可能
        if (_images.Count >= 10)
            throw new DomainException("画像は最大10枚まで登録できます");
        
        var image = ProductImage.Create(url, displayOrder);
        _images.Add(image);
        
        RaiseDomainEvent(new ProductImageAddedEvent(Id, url));
    }
    
    /// <summary>
    /// 公開
    /// </summary>
    public void Publish()
    {
        // ビジネスルール制約: 公開には最低1枚の画像が必須
        if (!_images.Any())
            throw new DomainException("画像が登録されていないため公開できません");
        
        // ビジネスルール: 価格が設定されている必要がある
        if (_price.Amount <= 0)
            throw new DomainException("価格が設定されていないため公開できません");
        
        if (_status == ProductStatus.Published)
            return;  // 既に公開済み
        
        _status = ProductStatus.Published;
        
        RaiseDomainEvent(new ProductPublishedEvent(Id));
    }
    
    /// <summary>
    /// 削除
    /// </summary>
    public void Delete()
    {
        // ビジネスルール: 在庫がある商品は削除不可
        if (_stock > 0)
            throw new DomainException("在庫がある商品は削除できません");
        
        // ビジネスルール: 公開中の商品は削除不可
        if (_status == ProductStatus.Published)
            throw new DomainException("公開中の商品は削除できません。先に非公開にしてください。");
        
        _status = ProductStatus.Deleted;
        
        RaiseDomainEvent(new ProductDeletedEvent(Id));
    }
}

/// <summary>
/// 商品ステータス
/// </summary>
public enum ProductStatus
{
    Draft = 0,      // 下書き
    Published = 1,  // 公開中
    Archived = 2,   // アーカイブ
    Deleted = 99    // 削除済み
}
```

### 9.2 Entity(エンティティ)

```csharp
/// <summary>
/// 商品画像エンティティ(集約内)
/// </summary>
public sealed class ProductImage : Entity<Guid>
{
    public string Url { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTime UploadedAt { get; private set; }
    
    private ProductImage() { }
    
    public static ProductImage Create(string url, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("画像URLは必須です");
        
        if (displayOrder < 0)
            throw new DomainException("表示順序は0以上の値を設定してください");
        
        return new ProductImage
        {
            Id = Guid.NewGuid(),
            Url = url,
            DisplayOrder = displayOrder,
            UploadedAt = DateTime.UtcNow
        };
    }
    
    public void ChangeDisplayOrder(int newOrder)
    {
        if (newOrder < 0)
            throw new DomainException("表示順序は0以上の値を設定してください");
        
        DisplayOrder = newOrder;
    }
}
```

### 9.3 Value Object(値オブジェクト)

```csharp
/// <summary>
/// 商品IDバリューオブジェクト
/// </summary>
public sealed record ProductId(Guid Value)
{
    public static ProductId New() => new(Guid.NewGuid());
    
    public static ProductId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("商品IDは空にできません");
        
        return new ProductId(value);
    }
    
    public override string ToString() => Value.ToString();
}

/// <summary>
/// 金額バリューオブジェクト
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    public Money(decimal amount, string currency = "JPY")
    {
        if (amount < 0)
            throw new DomainException("金額は0以上の値を設定してください");
        
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("通貨コードは必須です");
        
        Amount = amount;
        Currency = currency;
    }
    
    // 演算子オーバーロード
    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new DomainException("異なる通貨同士の計算はできません");
        
        return new Money(a.Amount + b.Amount, a.Currency);
    }
    
    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new DomainException("異なる通貨同士の計算はできません");
        
        return new Money(a.Amount - b.Amount, a.Currency);
    }
    
    public static Money operator *(Money money, decimal multiplier)
    {
        return new Money(money.Amount * multiplier, money.Currency);
    }
    
    public Money ApplyDiscount(decimal discountRate)
    {
        if (discountRate < 0 || discountRate > 1)
            throw new DomainException("割引率は0〜1の範囲で指定してください");
        
        return new Money(Amount * (1 - discountRate), Currency);
    }
    
    public string ToDisplayString() => $"¥{Amount:N0}";
}
```

### 9.4 Domain Event(ドメインイベント)

```csharp
/// <summary>
/// ドメインイベント基底クラス
/// </summary>
public abstract record DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// 商品作成イベント
/// </summary>
public sealed record ProductCreatedEvent(
    ProductId ProductId,
    string Name,
    Money Price
) : DomainEvent;

/// <summary>
/// 商品削除イベント
/// </summary>
public sealed record ProductDeletedEvent(ProductId ProductId) : DomainEvent;

/// <summary>
/// 価格変更イベント
/// </summary>
public sealed record ProductPriceChangedEvent(
    ProductId ProductId,
    Money OldPrice,
    Money NewPrice
) : DomainEvent;

/// <summary>
/// 在庫引当イベント
/// </summary>
public sealed record StockReservedEvent(
    ProductId ProductId,
    int Quantity,
    int RemainingStock
) : DomainEvent;
```

### 9.5 Aggregate Root基底クラス

```csharp
/// <summary>
/// 集約ルート基底クラス
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
{
    private readonly List<DomainEvent> _domainEvents = new();
    
    /// <summary>
    /// ドメインイベント一覧を取得
    /// </summary>
    public IReadOnlyList<DomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();
    
    /// <summary>
    /// ドメインイベントを発行
    /// </summary>
    protected void RaiseDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    /// <summary>
    /// ドメインイベントをクリア
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

/// <summary>
/// エンティティ基底クラス
/// </summary>
public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;
    
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;
        
        if (ReferenceEquals(this, other))
            return true;
        
        if (GetType() != other.GetType())
            return false;
        
        return Id?.Equals(other.Id) ?? false;
    }
    
    public override int GetHashCode()
    {
        return (GetType().ToString() + Id).GetHashCode();
    }
    
    public static bool operator ==(Entity<TId>? a, Entity<TId>? b)
    {
        if (a is null && b is null)
            return true;
        
        if (a is null || b is null)
            return false;
        
        return a.Equals(b);
    }
    
    public static bool operator !=(Entity<TId>? a, Entity<TId>? b)
    {
        return !(a == b);
    }
}
```

### 9.6 Domain Exception

```csharp
/// <summary>
/// ドメイン例外
/// ビジネスルール違反を表す
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
    
    public DomainException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
```

---
