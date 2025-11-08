# パターン詳細: 集計・レポート系クエリ

## 📋 概要

リアルタイム集計、ダッシュボード表示、複雑な検索条件での絞り込みなど、
業務アプリケーションで頻繁に必要となる高度なクエリパターンを提供します。

**実装例:** 商品売上レポート、在庫回転率分析、売れ筋ランキング

---

## 🎯 解決する課題

### 従来の問題点

**❌ EF Core LINQでの非効率な集計:**
```csharp
// ❌ N+1問題、メモリ上での集計、パフォーマンス悪化
public async Task<SalesReport> GetSalesReport()
{
    var products = await _context.Products.ToListAsync(); // 全件取得
    var orders = await _context.Orders.ToListAsync(); // 全件取得

    var report = products.Select(p => new
    {
        Product = p,
        TotalSales = orders.Where(o => o.ProductId == p.Id).Sum(o => o.Amount) // メモリ上で集計
    }).ToList();

    return report;
}
```

**❌ 複雑な条件分岐:**
```csharp
// ❌ 動的クエリの構築が煩雑
public IQueryable<Product> Search(string? name, decimal? minPrice, decimal? maxPrice)
{
    var query = _context.Products.AsQueryable();

    if (!string.IsNullOrEmpty(name))
        query = query.Where(p => p.Name.Contains(name));

    if (minPrice.HasValue)
        query = query.Where(p => p.Price >= minPrice.Value);

    if (maxPrice.HasValue)
        query = query.Where(p => p.Price <= maxPrice.Value);

    return query;
}
```

**❌ ページング・ソートの実装漏れ:**
- ページング処理の実装が各所で重複
- ソート項目のSQL Injection脆弱性
- 総件数取得の最適化忘れ

### 本パターンの解決策

**✅ Dapper + Raw SQLで最適化:**
```csharp
// ✅ 1回のクエリで集計完了
var sql = @"
    SELECT
        p.Id, p.Name,
        SUM(oi.Quantity * oi.UnitPrice) AS TotalSales,
        COUNT(DISTINCT o.Id) AS OrderCount
    FROM Products p
    LEFT JOIN OrderItems oi ON p.Id = oi.ProductId
    LEFT JOIN Orders o ON oi.OrderId = o.Id
    WHERE o.OrderDate BETWEEN @StartDate AND @EndDate
    GROUP BY p.Id, p.Name
    ORDER BY TotalSales DESC
    LIMIT 100;
";
```

**✅ Dynamic Query Builderで安全な動的クエリ:**
```csharp
var queryBuilder = new QueryBuilder()
    .Select("p.*")
    .From("Products p")
    .WhereIf(!string.IsNullOrEmpty(name), "p.Name LIKE @Name")
    .WhereIf(minPrice.HasValue, "p.Price >= @MinPrice")
    .OrderBy(sortColumn, isDescending)
    .Paginate(page, pageSize);
```

**✅ Materialized Viewで事前集計:**
```sql
CREATE MATERIALIZED VIEW product_sales_summary AS
SELECT
    product_id,
    DATE_TRUNC('day', order_date) AS order_date,
    SUM(daily_sales) AS total_sales
FROM orders
GROUP BY product_id, DATE_TRUNC('day', order_date);
```

---

## 🏗️ アーキテクチャ

### BC構造

```
src/ProductCatalog/Features/              # 既存BCに追加
├── GetProductSalesReport/                 # 商品売上レポート
│   ├── Application/
│   │   ├── GetProductSalesReportQuery.cs
│   │   ├── GetProductSalesReportHandler.cs
│   │   └── DTOs/
│   │       ├── ProductSalesReportDto.cs
│   │       └── ProductSalesItemDto.cs
│   └── UI/
│       ├── Api/
│       │   └── GetProductSalesReportEndpoint.cs
│       └── Components/
│           └── ProductSalesChart.razor
│
├── GetInventoryTurnoverReport/            # 在庫回転率レポート
├── GetTopSellingProducts/                 # 売れ筋商品ランキング
├── GetLowStockProducts/                   # 在庫僅少商品一覧
└── SearchProductsAdvanced/                # 高度な商品検索

Shared/Infrastructure/Querying/            # 共通クエリ基盤
├── IQueryBuilder.cs                       # クエリビルダーインターフェース
├── PostgreSqlQueryBuilder.cs              # PostgreSQL実装
├── DapperExtensions.cs                    # Dapper拡張メソッド
└── Specifications/
    ├── ISpecification.cs                  # Specification抽象化
    ├── AndSpecification.cs
    ├── OrSpecification.cs
    └── NotSpecification.cs
```

---

## 💎 共通インフラ実装

### 1. IQueryBuilder（クエリビルダー）

```csharp
/// <summary>
/// 動的SQLクエリビルダー
/// </summary>
public interface IQueryBuilder
{
    IQueryBuilder Select(string columns);
    IQueryBuilder From(string table);
    IQueryBuilder Join(string table, string condition);
    IQueryBuilder LeftJoin(string table, string condition);
    IQueryBuilder Where(string condition);
    IQueryBuilder WhereIf(bool condition, string whereClause);
    IQueryBuilder And(string condition);
    IQueryBuilder Or(string condition);
    IQueryBuilder GroupBy(string columns);
    IQueryBuilder Having(string condition);
    IQueryBuilder OrderBy(string column, bool descending = false);
    IQueryBuilder Paginate(int page, int pageSize);
    (string Sql, DynamicParameters Parameters) Build();
}
```

### 2. PostgreSqlQueryBuilder（PostgreSQL実装）

```csharp
public class PostgreSqlQueryBuilder : IQueryBuilder
{
    private readonly StringBuilder _select = new();
    private readonly StringBuilder _from = new();
    private readonly List<string> _joins = new();
    private readonly List<string> _wheres = new();
    private readonly StringBuilder _groupBy = new();
    private readonly StringBuilder _having = new();
    private readonly List<string> _orderBy = new();
    private int? _limit;
    private int? _offset;
    private readonly DynamicParameters _parameters = new();

    public IQueryBuilder Select(string columns)
    {
        _select.Append(columns);
        return this;
    }

    public IQueryBuilder From(string table)
    {
        _from.Append(table);
        return this;
    }

    public IQueryBuilder Join(string table, string condition)
    {
        _joins.Add($"INNER JOIN {table} ON {condition}");
        return this;
    }

    public IQueryBuilder LeftJoin(string table, string condition)
    {
        _joins.Add($"LEFT JOIN {table} ON {condition}");
        return this;
    }

    public IQueryBuilder Where(string condition)
    {
        _wheres.Add(condition);
        return this;
    }

    public IQueryBuilder WhereIf(bool condition, string whereClause)
    {
        if (condition)
            _wheres.Add(whereClause);
        return this;
    }

    public IQueryBuilder And(string condition)
    {
        if (_wheres.Count > 0)
            _wheres.Add(condition);
        return this;
    }

    public IQueryBuilder Or(string condition)
    {
        if (_wheres.Count > 0)
            _wheres[^1] = $"({_wheres[^1]} OR {condition})";
        return this;
    }

    public IQueryBuilder GroupBy(string columns)
    {
        _groupBy.Append(columns);
        return this;
    }

    public IQueryBuilder Having(string condition)
    {
        _having.Append(condition);
        return this;
    }

    public IQueryBuilder OrderBy(string column, bool descending = false)
    {
        // SQL Injection対策: カラム名をホワイトリストでチェック
        var allowedColumns = new[] { "Name", "Price", "Stock", "CreatedAt", "TotalSales" };
        if (!allowedColumns.Contains(column))
            throw new ArgumentException($"Invalid sort column: {column}");

        _orderBy.Add($"{column} {(descending ? "DESC" : "ASC")}");
        return this;
    }

    public IQueryBuilder Paginate(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 1000) pageSize = 1000; // 最大1000件

        _limit = pageSize;
        _offset = (page - 1) * pageSize;
        return this;
    }

    public (string Sql, DynamicParameters Parameters) Build()
    {
        var sql = new StringBuilder();

        // SELECT
        sql.Append("SELECT ");
        sql.Append(_select.Length > 0 ? _select.ToString() : "*");

        // FROM
        if (_from.Length == 0)
            throw new InvalidOperationException("FROM clause is required");
        sql.Append(" FROM ").Append(_from);

        // JOINs
        foreach (var join in _joins)
        {
            sql.Append(' ').Append(join);
        }

        // WHERE
        if (_wheres.Count > 0)
        {
            sql.Append(" WHERE ").Append(string.Join(" AND ", _wheres));
        }

        // GROUP BY
        if (_groupBy.Length > 0)
        {
            sql.Append(" GROUP BY ").Append(_groupBy);
        }

        // HAVING
        if (_having.Length > 0)
        {
            sql.Append(" HAVING ").Append(_having);
        }

        // ORDER BY
        if (_orderBy.Count > 0)
        {
            sql.Append(" ORDER BY ").Append(string.Join(", ", _orderBy));
        }

        // LIMIT/OFFSET
        if (_limit.HasValue)
        {
            sql.Append(" LIMIT ").Append(_limit.Value);
        }

        if (_offset.HasValue)
        {
            sql.Append(" OFFSET ").Append(_offset.Value);
        }

        sql.Append(';');

        return (sql.ToString(), _parameters);
    }

    public IQueryBuilder AddParameter(string name, object value)
    {
        _parameters.Add(name, value);
        return this;
    }
}
```

### 3. Specification Pattern（仕様パターン）

```csharp
/// <summary>
/// 仕様パターン抽象基底クラス
/// </summary>
public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public Func<T, bool> ToFunc() => ToExpression().Compile();

    public bool IsSatisfiedBy(T entity) => ToFunc()(entity);

    public Specification<T> And(Specification<T> other)
    {
        return new AndSpecification<T>(this, other);
    }

    public Specification<T> Or(Specification<T> other)
    {
        return new OrSpecification<T>(this, other);
    }

    public Specification<T> Not()
    {
        return new NotSpecification<T>(this);
    }
}

// AND仕様
public class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.AndAlso(
            Expression.Invoke(leftExpr, parameter),
            Expression.Invoke(rightExpr, parameter)
        );

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}

// OR仕様
public class OrSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public OrSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.OrElse(
            Expression.Invoke(leftExpr, parameter),
            Expression.Invoke(rightExpr, parameter)
        );

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}

// NOT仕様
public class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _spec;

    public NotSpecification(Specification<T> spec)
    {
        _spec = spec;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var expr = _spec.ToExpression();
        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.Not(Expression.Invoke(expr, parameter));

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
```

### 4. Product用Specification実装例

```csharp
// 商品名に指定文字列を含む
public class ProductNameContainsSpecification : Specification<Product>
{
    private readonly string _name;

    public ProductNameContainsSpecification(string name)
    {
        _name = name;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product => product.Name.Contains(_name);
    }
}

// 価格範囲
public class ProductPriceRangeSpecification : Specification<Product>
{
    private readonly decimal? _minPrice;
    private readonly decimal? _maxPrice;

    public ProductPriceRangeSpecification(decimal? minPrice, decimal? maxPrice)
    {
        _minPrice = minPrice;
        _maxPrice = maxPrice;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product =>
            (!_minPrice.HasValue || product.Price.Value >= _minPrice.Value) &&
            (!_maxPrice.HasValue || product.Price.Value <= _maxPrice.Value);
    }
}

// 在庫あり
public class ProductInStockSpecification : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return product => product.Stock > 0;
    }
}

// 使用例
public async Task<IEnumerable<Product>> SearchProducts(
    string nameFilter,
    decimal? minPrice,
    decimal? maxPrice,
    bool inStockOnly)
{
    var spec = new ProductNameContainsSpecification(nameFilter)
        .And(new ProductPriceRangeSpecification(minPrice, maxPrice));

    if (inStockOnly)
        spec = spec.And(new ProductInStockSpecification());

    return await _repository.FindAsync(spec.ToExpression());
}
```

---

## 🔧 機能スライス実装例

### GetProductSalesReport（商品売上レポート）

#### Query

```csharp
/// <summary>
/// 商品売上レポート取得クエリ
/// </summary>
public record GetProductSalesReportQuery : IQuery<Result<ProductSalesReportDto>>, ICacheableQuery
{
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public Guid? CategoryId { get; init; }
    public string? ProductNameFilter { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string SortBy { get; init; } = "TotalSales";
    public bool IsDescending { get; init; } = true;

    public string GetCacheKey() =>
        $"product-sales-report:{StartDate:yyyyMMdd}:{EndDate:yyyyMMdd}:{CategoryId}:{ProductNameFilter}:{Page}:{PageSize}:{SortBy}:{IsDescending}";

    public int CacheDurationMinutes => 30; // 30分キャッシュ
}
```

#### DTOs

```csharp
public record ProductSalesReportDto
{
    public required PagedResult<ProductSalesItemDto> Items { get; init; }
    public required SalesSummaryDto Summary { get; init; }
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

public record ProductSalesItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public decimal TotalSales { get; init; }
    public int TotalQuantity { get; init; }
    public decimal AverageUnitPrice { get; init; }
    public int OrderCount { get; init; }
    public decimal SalesGrowthRate { get; init; } // 前期比成長率
}

public record SalesSummaryDto
{
    public decimal TotalRevenue { get; init; }
    public int TotalOrders { get; init; }
    public int TotalProducts { get; init; }
    public decimal AverageOrderValue { get; init; }
    public decimal TopProductSalesPercentage { get; init; } // TOP商品の売上比率
}
```

#### Handler

```csharp
public class GetProductSalesReportHandler : IQueryHandler<GetProductSalesReportQuery, Result<ProductSalesReportDto>>
{
    private readonly IDbConnection _connection;
    private readonly ILogger<GetProductSalesReportHandler> _logger;

    public async Task<Result<ProductSalesReportDto>> Handle(
        GetProductSalesReportQuery query,
        CancellationToken ct)
    {
        try
        {
            // 1. 動的クエリ構築
            var queryBuilder = new PostgreSqlQueryBuilder()
                .Select(@"
                    p.Id AS ProductId,
                    p.Name AS ProductName,
                    c.Name AS CategoryName,
                    COALESCE(SUM(oi.Quantity * oi.UnitPrice), 0) AS TotalSales,
                    COALESCE(SUM(oi.Quantity), 0) AS TotalQuantity,
                    COALESCE(AVG(oi.UnitPrice), 0) AS AverageUnitPrice,
                    COUNT(DISTINCT o.Id) AS OrderCount")
                .From("Products p")
                .LeftJoin("Categories c", "p.CategoryId = c.Id")
                .LeftJoin("OrderItems oi", "p.Id = oi.ProductId")
                .LeftJoin("Orders o", "oi.OrderId = o.Id AND o.OrderDate BETWEEN @StartDate AND @EndDate")
                .Where("p.IsDeleted = false")
                .WhereIf(query.CategoryId.HasValue, "p.CategoryId = @CategoryId")
                .WhereIf(!string.IsNullOrEmpty(query.ProductNameFilter), "p.Name ILIKE @ProductNameFilter")
                .GroupBy("p.Id, p.Name, c.Name")
                .OrderBy(query.SortBy, query.IsDescending)
                .Paginate(query.Page, query.PageSize)
                .AddParameter("StartDate", query.StartDate)
                .AddParameter("EndDate", query.EndDate);

            if (query.CategoryId.HasValue)
                queryBuilder.AddParameter("CategoryId", query.CategoryId.Value);

            if (!string.IsNullOrEmpty(query.ProductNameFilter))
                queryBuilder.AddParameter("ProductNameFilter", $"%{query.ProductNameFilter}%");

            var (sql, parameters) = queryBuilder.Build();

            // 2. データ取得
            var items = (await _connection.QueryAsync<ProductSalesItemDto>(sql, parameters)).ToList();

            // 3. 総件数取得（ページング用）
            var countSql = @"
                SELECT COUNT(DISTINCT p.Id)
                FROM Products p
                LEFT JOIN Orders o ON o.ProductId = p.Id AND o.OrderDate BETWEEN @StartDate AND @EndDate
                WHERE p.IsDeleted = false
            ";
            var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

            // 4. サマリー計算
            var summary = new SalesSummaryDto
            {
                TotalRevenue = items.Sum(i => i.TotalSales),
                TotalOrders = items.Sum(i => i.OrderCount),
                TotalProducts = items.Count,
                AverageOrderValue = items.Any() && items.Sum(i => i.OrderCount) > 0
                    ? items.Sum(i => i.TotalSales) / items.Sum(i => i.OrderCount)
                    : 0,
                TopProductSalesPercentage = items.Any() && items.Sum(i => i.TotalSales) > 0
                    ? (items.FirstOrDefault()?.TotalSales ?? 0) / items.Sum(i => i.TotalSales) * 100
                    : 0
            };

            // 5. ページング結果作成
            var pagedResult = new PagedResult<ProductSalesItemDto>(
                items,
                totalCount,
                query.Page,
                query.PageSize
            );

            var report = new ProductSalesReportDto
            {
                Items = pagedResult,
                Summary = summary
            };

            _logger.LogInformation(
                "Product sales report generated: Period={StartDate} to {EndDate}, Products={ProductCount}, Revenue={TotalRevenue}",
                query.StartDate, query.EndDate, items.Count, summary.TotalRevenue);

            return Result.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate product sales report");
            return Result.Failure<ProductSalesReportDto>("レポート生成に失敗しました");
        }
    }
}
```

### GetTopSellingProducts（売れ筋ランキング）

```csharp
// Query
public record GetTopSellingProductsQuery : IQuery<Result<IEnumerable<TopSellingProductDto>>>, ICacheableQuery
{
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public int Top { get; init; } = 10; // TOP10

    public string GetCacheKey() => $"top-selling-products:{StartDate:yyyyMMdd}:{EndDate:yyyyMMdd}:{Top}";
    public int CacheDurationMinutes => 60; // 1時間キャッシュ
}

// DTO
public record TopSellingProductDto
{
    public int Rank { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public decimal TotalSales { get; init; }
    public int TotalQuantity { get; init; }
    public decimal SalesShare { get; init; } // 全体売上に占める割合
}

// Handler
public class GetTopSellingProductsHandler : IQueryHandler<GetTopSellingProductsQuery, Result<IEnumerable<TopSellingProductDto>>>
{
    private readonly IDbConnection _connection;

    public async Task<Result<IEnumerable<TopSellingProductDto>>> Handle(
        GetTopSellingProductsQuery query,
        CancellationToken ct)
    {
        // WITH句を使った効率的なランキングクエリ
        var sql = @"
            WITH sales_data AS (
                SELECT
                    p.Id AS ProductId,
                    p.Name AS ProductName,
                    p.ImageUrl,
                    SUM(oi.Quantity * oi.UnitPrice) AS TotalSales,
                    SUM(oi.Quantity) AS TotalQuantity
                FROM
                    Products p
                    INNER JOIN OrderItems oi ON p.Id = oi.ProductId
                    INNER JOIN Orders o ON oi.OrderId = o.Id
                WHERE
                    o.OrderDate BETWEEN @StartDate AND @EndDate
                    AND p.IsDeleted = false
                GROUP BY
                    p.Id, p.Name, p.ImageUrl
            ),
            total_sales AS (
                SELECT SUM(TotalSales) AS GrandTotal FROM sales_data
            )
            SELECT
                ROW_NUMBER() OVER (ORDER BY sd.TotalSales DESC) AS Rank,
                sd.ProductId,
                sd.ProductName,
                sd.ImageUrl,
                sd.TotalSales,
                sd.TotalQuantity,
                ROUND((sd.TotalSales / ts.GrandTotal * 100)::numeric, 2) AS SalesShare
            FROM
                sales_data sd
                CROSS JOIN total_sales ts
            ORDER BY
                sd.TotalSales DESC
            LIMIT @Top;
        ";

        var parameters = new
        {
            query.StartDate,
            query.EndDate,
            query.Top
        };

        var items = await _connection.QueryAsync<TopSellingProductDto>(sql, parameters);

        return Result.Success(items);
    }
}
```

---

## 📊 Materialized View活用パターン

### PostgreSQL Materialized View定義

```sql
-- 商品売上サマリー（日次集計）
CREATE MATERIALIZED VIEW product_sales_summary AS
SELECT
    p.id AS product_id,
    p.name AS product_name,
    c.id AS category_id,
    c.name AS category_name,
    DATE_TRUNC('day', o.order_date) AS order_date,
    SUM(oi.quantity * oi.unit_price) AS daily_sales,
    SUM(oi.quantity) AS daily_quantity,
    COUNT(DISTINCT o.id) AS daily_orders,
    AVG(oi.unit_price) AS avg_unit_price
FROM
    products p
    INNER JOIN categories c ON p.category_id = c.id
    INNER JOIN order_items oi ON p.id = oi.product_id
    INNER JOIN orders o ON oi.order_id = o.id
WHERE
    o.order_date >= CURRENT_DATE - INTERVAL '90 days'
    AND p.is_deleted = false
GROUP BY
    p.id, p.name, c.id, c.name, DATE_TRUNC('day', o.order_date);

-- インデックス作成
CREATE INDEX idx_product_sales_summary_date ON product_sales_summary(order_date);
CREATE INDEX idx_product_sales_summary_product ON product_sales_summary(product_id);
CREATE INDEX idx_product_sales_summary_category ON product_sales_summary(category_id);

-- 定期更新ジョブ（PostgreSQL拡張またはHangfireで実行）
-- 毎日深夜1時に実行
REFRESH MATERIALIZED VIEW CONCURRENTLY product_sales_summary;
```

### Materialized View使用例

```csharp
// Materialized Viewを使用した高速レポート取得
public async Task<Result<ProductSalesReportDto>> HandleUsingMaterializedView(
    GetProductSalesReportQuery query,
    CancellationToken ct)
{
    var sql = @"
        SELECT
            product_id AS ProductId,
            product_name AS ProductName,
            category_name AS CategoryName,
            SUM(daily_sales) AS TotalSales,
            SUM(daily_quantity) AS TotalQuantity,
            AVG(avg_unit_price) AS AverageUnitPrice,
            SUM(daily_orders) AS OrderCount
        FROM
            product_sales_summary
        WHERE
            order_date BETWEEN @StartDate AND @EndDate
        GROUP BY
            product_id, product_name, category_name
        ORDER BY
            TotalSales DESC
        LIMIT @Limit OFFSET @Offset;
    ";

    var parameters = new
    {
        query.StartDate,
        query.EndDate,
        Limit = query.PageSize,
        Offset = (query.Page - 1) * query.PageSize
    };

    var items = await _connection.QueryAsync<ProductSalesItemDto>(sql, parameters);

    // サマリー計算...

    return Result.Success(report);
}
```

---

## 🧪 テスト戦略

### Unit Test（Specification Pattern）

```csharp
public class ProductSpecificationTests
{
    [Fact]
    public void ProductNameContainsSpecification_FiltersCorrectly()
    {
        // Arrange
        var products = new[]
        {
            new Product { Name = "Laptop" },
            new Product { Name = "Desktop Computer" },
            new Product { Name = "Mouse" }
        };

        var spec = new ProductNameContainsSpecification("top");

        // Act
        var filtered = products.Where(spec.ToFunc()).ToList();

        // Assert
        filtered.Should().HaveCount(2);
        filtered.Should().Contain(p => p.Name == "Laptop");
        filtered.Should().Contain(p => p.Name == "Desktop Computer");
    }

    [Fact]
    public void AndSpecification_CombinesCorrectly()
    {
        // Arrange
        var products = new[]
        {
            new Product { Name = "Laptop", Price = new Money(150000) },
            new Product { Name = "Desktop", Price = new Money(200000) },
            new Product { Name = "Mouse", Price = new Money(3000) }
        };

        var nameSpec = new ProductNameContainsSpecification("top");
        var priceSpec = new ProductPriceRangeSpecification(100000, 180000);
        var combinedSpec = nameSpec.And(priceSpec);

        // Act
        var filtered = products.Where(combinedSpec.ToFunc()).ToList();

        // Assert
        filtered.Should().ContainSingle();
        filtered.First().Name.Should().Be("Laptop");
    }
}
```

### Integration Test（集計クエリ）

```csharp
public class GetProductSalesReportIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetProductSalesReport_ReturnsCorrectData()
    {
        // Arrange: テストデータ投入
        await SeedTestDataAsync();

        var query = new GetProductSalesReportQuery
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            Page = 1,
            PageSize = 50
        };

        // Act
        var result = await _mediator.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Items.Should().NotBeEmpty();
        result.Value.Summary.TotalRevenue.Should().BeGreaterThan(0);
    }
}
```

---

## 📝 まとめ

### このパターンで実現できること

✅ **高速な集計クエリ:** Dapper + Raw SQLで最適化
✅ **動的クエリ構築:** Query Builderで安全に条件を組み立て
✅ **再利用可能な条件:** Specification Patternで条件の組み合わせ
✅ **事前集計:** Materialized Viewでリアルタイム性と速度のバランス
✅ **SQL Injection対策:** パラメータ化クエリとカラム名ホワイトリスト

### 適用可能なシナリオ

- 商品売上レポート
- 在庫回転率分析
- 売れ筋ランキング
- 顧客行動分析
- ダッシュボード表示
- KPIモニタリング

---

**作成日:** 2025-11-07
**最終更新:** 2025-11-07
**ステータス:** ✅ 設計完了
