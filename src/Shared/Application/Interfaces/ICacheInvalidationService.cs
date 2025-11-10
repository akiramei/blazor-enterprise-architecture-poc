namespace Shared.Application.Interfaces;

/// <summary>
/// キャッシュ無効化サービスのインターフェース
///
/// 【目的】
/// - キャッシュキーの構築ロジックを中央集約
/// - Command Handlerでのキャッシュ無効化を簡素化
/// - マジックストリングを排除
///
/// 【使用例】
/// ```csharp
/// public class UpdateProductHandler
/// {
///     private readonly ICacheInvalidationService _cacheInvalidation;
///
///     public async Task<Result> Handle(UpdateProductCommand command, CancellationToken ct)
///     {
///         // ... ビジネスロジック
///
///         // キャッシュ無効化（簡潔！）
///         _cacheInvalidation.InvalidateProduct(command.ProductId);
///
///         return Result.Success();
///     }
/// }
/// ```
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    /// 特定商品のキャッシュを無効化
    /// （すべてのテナント/ユーザーのキャッシュを削除）
    /// </summary>
    void InvalidateProduct(Guid productId);

    /// <summary>
    /// 商品一覧のキャッシュを無効化
    /// </summary>
    void InvalidateProductList();

    /// <summary>
    /// パターンマッチングでキャッシュを無効化
    /// </summary>
    /// <param name="pattern">キャッシュキーのパターン（例: "GetProductByIdQuery:*:*:product_xxx"）</param>
    void InvalidateByPattern(string pattern);
}
