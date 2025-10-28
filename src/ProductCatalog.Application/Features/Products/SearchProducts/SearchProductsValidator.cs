using FluentValidation;

namespace ProductCatalog.Application.Features.Products.SearchProducts;

/// <summary>
/// 商品検索Validator
///
/// 【パターン: Search Query Validator（検索条件検証）】
///
/// 責務:
/// - 検索パラメータの形式検証
/// - ページング情報の妥当性チェック
/// - 範囲指定の論理検証（MinPrice < MaxPrice）
///
/// 実装ガイド:
/// - AbstractValidator<TQuery> を継承
/// - ページング必須項目の検証（Page、PageSize）
/// - OrderByのホワイトリストはHandlerで検証（ここでは範囲のみ）
/// - フィルタ条件はnull許容（任意）
///
/// AI実装時の注意:
/// - Pageは1始まり（0以下は不正）
/// - PageSizeは1〜100の範囲が推奨
/// - MinPrice/MaxPriceの大小関係を検証
/// - OrderByの値そのものはHandlerで検証（SQLインジェクション対策）
///
/// 検索系Validatorの特徴:
/// ✅ フィルタ条件: null許容（任意指定）
/// ✅ ページング: 必須検証
/// ✅ ソート項目: Handlerで検証（ホワイトリスト）
/// </summary>
public sealed class SearchProductsValidator : AbstractValidator<SearchProductsQuery>
{
    public SearchProductsValidator()
    {
        // Page検証
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("ページ番号は1以上である必要があります");

        // PageSize検証
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("ページサイズは1〜100の範囲で指定してください");

        // MinPrice検証（指定された場合）
        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinPrice.HasValue)
            .WithMessage("最低価格は0以上である必要があります");

        // MaxPrice検証（指定された場合）
        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxPrice.HasValue)
            .WithMessage("最高価格は0以上である必要があります");

        // MinPrice < MaxPrice の検証
        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice.Value <= x.MaxPrice.Value)
            .WithMessage("最低価格は最高価格以下である必要があります")
            .WithName("MinPrice");

        // OrderBy検証（空文字でないこと）
        RuleFor(x => x.OrderBy)
            .NotEmpty()
            .WithMessage("ソート項目は必須です");
    }
}
