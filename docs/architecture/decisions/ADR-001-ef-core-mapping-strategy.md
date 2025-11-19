# ADR-001: EF Coreマッピング戦略の見直し

## ステータス

提案中（Proposed）

## コンテキスト

ProductCatalog統合テストの修正中に、以下の技術的問題が発見されました:

### 問題1: Shadow Propertyの変更追跡が困難
```csharp
// 現在の実装
builder.Ignore(p => p.Name);           // 公開プロパティを無視
builder.Property("_name")              // プライベートフィールドを直接マッピング
    .HasColumnName("Name");
```

**課題:**
- EF Coreが変更を自動検出できない
- `Detach() → Update()`という回避策が必要
- LINQ式での型安全性が失われる（文字列指定）
- デバッグが困難

### 問題2: Versionの取得が複雑
```csharp
// Select projectionが必要
var result = await _context.Products
    .Select(p => new { Product = p, Version = EF.Property<long>(p, "Version") })
    .FirstOrDefaultAsync();
```

**課題:**
- すべてのクエリで`EF.Property<T>`を使用する必要がある
- 読み取り専用リポジトリとの整合性が取りにくい
- コード量が増加

## 決定事項

Shadow Propertyの使用を段階的に廃止し、以下の戦略を採用します:

### 戦略A: 公開プロパティ + private setter（推奨）

```csharp
// Domainエンティティ
public class Product : AggregateRoot<ProductId>
{
    // 公開プロパティとしてマッピング
    public string Name { get; private set; } = default!;
    public long Version { get; private set; } = default!;
    // 変更は専用メソッド経由
    public void ChangeName(string name)
    {
        // ビジネスルール検証
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("商品名は必須です");

        Name = name;
        RaiseDomainEvent(new ProductUpdatedDomainEvent(Id, name));
    }
}

// EF Core Configuration
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // シンプルなマッピング（バッキングフィールドは不要）
        builder.Property(p => p.Name)
            .HasColumnName("Name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Version)
            .IsConcurrencyToken()
            .IsRequired();
    }
}
```

**メリット:**
- EF Coreの変更追跡が自動的に機能
- LINQ式で型安全にアクセス可能
- デバッグが容易
- `EF.Property<T>()`が不要

**デメリット:**
- プライベートフィールドによる厳密なカプセル化ができない
- ただし、`private setter`により外部からの変更は防止可能

### 戦略B: 公開プロパティ + HasField（中間案）

```csharp
// プライベートフィールドは保持
private string _name = default!;
public string Name => _name;  // 読み取り専用プロパティ

// EF Core Configuration
builder.Property(p => p.Name)
    .HasField("_name")
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

**メリット:**
- フィールドによるカプセル化を維持
- EF Coreは自動的にバッキングフィールドにアクセス

**デメリット:**
- 設定が複雑
- リフレクション使用によるパフォーマンスへの影響

## 決定理由

**戦略Aを推奨する理由:**

1. **保守性**: シンプルな実装でトラブルシューティングが容易
2. **生産性**: `EF.Property<T>()`のような特殊な構文が不要
3. **一貫性**: 読み取り/書き込みリポジトリで同じアクセス方法
4. **DDD原則**: `private setter`により不変性は十分に保護される

Martin Fowlerの ["Anemic Domain Model"](https://martinfowler.com/bliki/AnemicDomainModel.html) の議論:
> "The fundamental horror of this anti-pattern is that it's so contrary to the basic idea of object-oriented design;
> which is to combine data and process together."

重要なのは**ビジネスロジックがドメインメソッドに集約されていること**であり、
プロパティがprivateフィールドかpublic setter制限かは二次的な問題。

## 影響範囲

### 短期的影響（今回の修正）
- ✅ ProductCatalogDbContext.SaveChangesAsync - Versionのnull安全性を改善
- ✅ CacheInvalidationService - キャッシュ管理を中央集約
- ✅ DiagnosticLoggingExtensions - トラブルシューティング時間を短縮

### 中期的影響（次のスプリント）
- ProductエンティティのリファクタリングShadow Property削除（推定: 4時間）
- ProductConfiguration の簡素化（推定: 2時間）
- ReadRepositoryの簡素化（推定: 2時間）
- **合計推定工数: 8時間**

### 長期的影響
- 他のエンティティ（PurchaseRequest等）への適用
- 統合テストの安定性向上
- 新規開発者のオンボーディング時間短縮

## 代替案

### 代替案1: 現状維持（Shadow Property継続）
**却下理由:**
- 技術的負債が蓄積
- トラブルシューティングコストが高い
- 新規開発者の学習曲線が急

### 代替案2: Dapperへの完全移行
**検討中:**
- 読み取り専用クエリはDapperで最適化
- 書き込み操作はEF Coreで集約管理
- ハイブリッドアプローチを継続評価

## 関連リソース

- [EF Core - Shadow and Indexer Properties](https://learn.microsoft.com/en-us/ef/core/modeling/shadow-properties)
- [DDD - Aggregate Design](https://www.dddcommunity.org/library/vernon_2011/)
- [Martin Fowler - Anemic Domain Model](https://martinfowler.com/bliki/AnemicDomainModel.html)

## 変更履歴

| 日付 | 変更内容 | 作成者 |
|------|---------|--------|
| 2025-11-10 | 初版作成 | Claude Code |

## レビュー

- [ ] アーキテクト承認
- [ ] テックリード承認
- [ ] チームレビュー完了

---

**注記**: この決定は、ProductCatalog統合テストの修正で得られた教訓に基づいています。
実装前に必ずチームでレビューを行ってください。
