using FluentValidation;

namespace ProductCatalog.Application.Features.Products.UpdateProduct;

/// <summary>
/// 商品更新Validator
///
/// 【パターン: Validator（入力検証）】
///
/// 責務:
/// - 入力値の形式検証（null、範囲、長さ、フォーマットなど）
/// - ビジネスルールの検証は含めない（Domain層に委譲）
///
/// 実装ガイド:
/// - AbstractValidator<TCommand> を継承
/// - RuleFor() でフィールドごとに検証ルールを定義
/// - WithMessage() でユーザーフレンドリーなエラーメッセージを設定
/// - 複雑な検証は Must() を使用
///
/// AI実装時の注意:
/// - Validatorは「入力値そのもの」の検証のみ
/// - ビジネスルールはDomain層（例: 「在庫がある商品は価格を下げられない」など）
/// - データベースアクセスが必要な検証はValidatorで行わない
/// - 検証失敗時は自動的に400 Bad Requestが返される（ValidationBehavior）
///
/// 入力検証 vs ビジネスルール:
/// ✅ Validator: 「価格は0より大きい」
/// ❌ Validator: 「公開中の商品は50%以上値下げできない」← これはDomain層
/// ✅ Validator: 「商品名は200文字以内」
/// ❌ Validator: 「在庫がある商品は削除できない」← これはDomain層
/// </summary>
public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        // ProductId検証
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("商品IDは必須です");

        // Name検証
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("商品名は必須です")
            .MaximumLength(200)
            .WithMessage("商品名は200文字以内で入力してください");

        // Description検証
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("説明は必須です")
            .MaximumLength(2000)
            .WithMessage("説明は2000文字以内で入力してください");

        // Price検証
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("価格は0より大きい値を設定してください")
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("価格は1,000,000以下で設定してください");

        // Stock検証
        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0)
            .WithMessage("在庫数は0以上である必要があります")
            .LessThanOrEqualTo(100_000)
            .WithMessage("在庫数は100,000以下で設定してください");

        // Version検証（楽観的排他制御）
        RuleFor(x => x.Version)
            .GreaterThan(0)
            .WithMessage("バージョン番号が不正です");
    }
}
