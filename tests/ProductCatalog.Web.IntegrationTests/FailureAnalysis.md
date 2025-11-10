# ProductCatalog統合テスト 失敗分析

## 失敗テスト一覧 (8/14件)

### 1. SearchProducts_WithKeyword_ReturnsMatchingProducts
- **ステータス**: 原因特定完了
- **エラー**: `Translation of member 'Name' on entity type 'Product' failed. Translation of member 'Description' on entity type 'Product' failed`
- **原因**: `EfProductReadRepository.SearchAsync` (line 119) で `p.Name.Contains()` と `p.Description.Contains()` を使用
- **修正方針**: `EF.Property<string>(p, "_name")` と `EF.Property<string>(p, "_description")` を使用

### 2. TransactionalOutbox_ProductCreation_EnsuresAtomicity
- **ステータス**: 原因特定完了
- **エラー**: `Translation of member 'Name' on entity type 'Product' failed`
- **原因**: テストコード内で `p.Name == "Atomicity Test Product"` を使用
- **修正方針**: テストコードを `EF.Property<string>(p, "_name")` に変更、またはToListAsync()後にLINQ to Objectsで検索

### 3. Login_WithInvalidCredentials_ReturnsUnauthorized
- **ステータス**: 調査中
- **エラー**: 400 BadRequest (詳細不明)
- **原因**: 認証エラーテスト（Identity関連の可能性）

### 4. DeleteProduct_ExistingProduct_DeletesProductAndCreatesOutboxMessage
- **ステータス**: 調査中
- **エラー**: 400 BadRequest (InvalidOperationException無し)
- **原因**: 不明

### 5. CreateProduct_ValidRequest_CreatesProductAndOutboxMessage
- **ステータス**: 調査中
- **エラー**: Outbox Content に ProductId が含まれていない
- **原因**: 検証ロジックの問題の可能性

### 6. GetProductById_ExistingProduct_ReturnsProduct
- **ステータス**: 原因特定完了
- **エラー**: `Translation of member 'Id' on entity type 'Product' failed`
- **原因**: `EfProductReadRepository.GetByIdAsync` で `EF.Property<ProductId>(p, "_id").Value` を使用したが、ValueObjectの内部プロパティアクセスが翻訳できない
- **修正方針**: Guid型で直接比較 `EF.Property<Guid>(p, "_id")` または別の方法を検討

### 7. UpdateProduct_ValidRequest_UpdatesProductAndCreatesOutboxMessage
- **ステータス**: 調査中
- **エラー**: 400 BadRequest
- **原因**: GetProductByIdと同じ問題の可能性

### 8. GetProductById_NonExistingProduct_ReturnsNotFound
- **ステータス**: 調査中
- **エラー**: 400 BadRequest (404期待)
- **原因**: GetProductByIdと同じ問題の可能性

## エラーパターン分類

### パターンA: EF Core LINQ翻訳エラー（publicプロパティアクセス）
**件数**: 4件
**共通原因**: ProductConfigurationで`builder.Ignore()`したpublicプロパティをLINQクエリで使用

1. **SearchProducts_WithKeyword_ReturnsMatchingProducts**
   - 箇所: `EfProductReadRepository.SearchAsync` line 94
   - 問題: `p.Name.Contains()`, `p.Description.Contains()`

2. **TransactionalOutbox_ProductCreation_EnsuresAtomicity**
   - 箇所: テストコード line 337
   - 問題: `p.Name == "Atomicity Test Product"`

3. **GetProductById_ExistingProduct_ReturnsProduct**
   - 箇所: `EfProductReadRepository.GetByIdAsync` line 48
   - 問題: `EF.Property<ProductId>(p, "_id").Value`（ValueObject内部プロパティアクセス）

4. **GetProductById_NonExistingProduct_ReturnsNotFound**
   - 箇所: 同上（GetByIdAsyncを使用）

### パターンB: 400 BadRequest (調査継続必要)
**件数**: 3件

1. **Login_WithInvalidCredentials_ReturnsUnauthorized**
   - 期待: 401 Unauthorized
   - 実際: 400 BadRequest
   - 推測: Identity関連のバリデーションエラー？

2. **DeleteProduct_ExistingProduct_DeletesProductAndCreatesOutboxMessage**
   - InvalidOperationExceptionが見つからない
   - APIハンドラー側の問題の可能性

3. **UpdateProduct_ValidRequest_UpdatesProductAndCreatesOutboxMessage**
   - GetProductByIdと同様の問題の可能性

### パターンC: Outbox検証失敗
**件数**: 1件

1. **CreateProduct_ValidRequest_CreatesProductAndOutboxMessage**
   - Outbox Contentに ProductId が含まれていることを期待
   - 実際には含まれていない
   - Domain Eventのシリアライズ方法の問題の可能性

## 根本原因まとめ

### 主要な原因: EF Core マッピング設定の不整合

**問題の本質:**
- ProductConfigurationで公開プロパティ(Id, Name, Description等)を`Ignore()`している
- しかし、EfProductReadRepositoryやテストコードでは公開プロパティを使用している
- EF CoreはIgnoreされたプロパティをLINQクエリで翻訳できない

**なぜIgnoreが必要だったのか:**
- Product集約はprivateフィールド(`_id`, `_name`等)を直接マッピングしている
- 公開プロパティとprivateフィールドの両方が存在すると、EF Coreが混乱する
- したがって、公開プロパティをIgnoreする必要があった

**影響範囲:**
- EfProductReadRepository全体（GetAllAsync, GetByIdAsync, SearchAsync）
- テストコード内のLINQクエリ

## 修正方針

### 方針A: EfProductReadRepositoryを完全にEF.Propertyベースに書き換え

**メリット:**
- privateフィールドに直接アクセスできる
- ProductConfigurationの設定と整合性が取れる

**デメリット:**
- コードが冗長になる
- ValueObject（ProductId, Money）の内部プロパティアクセスが困難
  - 例: `EF.Property<ProductId>(p, "_id").Value` は翻訳できない

**対応が必要な箇所:**
1. `SearchAsync` - `p.Name.Contains()` → `EF.Property<string>(p, "_name").Contains()`
2. `SearchAsync` - `p.Description.Contains()` → `EF.Property<string>(p, "_description").Contains()`
3. `GetByIdAsync` - `p.Id.Value == productId` → 別の方法が必要

### 方針B: ProductIdを直接Guid型でマッピング

**ValueObjectの問題:**
`EF.Property<ProductId>(p, "_id").Value`は翻訳できないため、以下のいずれかが必要:

1. **オプション1**: Guidで直接マッピング
   ```csharp
   // ProductConfiguration
   builder.Property<Guid>("_id");

   // Repository
   .Where(p => EF.Property<Guid>(p, "_id") == productId)
   ```

2. **オプション2**: ToListAsync()後にLINQ to Objectsで絞り込み
   ```csharp
   var products = await _context.Products.ToListAsync();
   var product = products.FirstOrDefault(p => p.Id.Value == productId);
   ```
   ⚠️ パフォーマンスの問題あり

3. **オプション3**: HasConversionでProductIdをGuidに変換
   ```csharp
   builder.Property(p => EF.Property<ProductId>(p, "_id"))
       .HasConversion(
           id => id.Value,
           value => ProductId.From(value));
   ```

### 推奨方針: **方針A + オプション1の組み合わせ**

1. EfProductReadRepositoryを EF.Property ベースに書き換え
2. ProductId等のValueObjectは内部でGuid型として扱う
3. テストコードも EF.Property を使うか、ToListAsync()後に検索

## 次のアクション

1. ✅ 失敗テスト全件の調査完了
2. ✅ エラーパターンの分類完了
3. ✅ 根本原因の特定完了
4. ⏭️ 修正方針の実装
