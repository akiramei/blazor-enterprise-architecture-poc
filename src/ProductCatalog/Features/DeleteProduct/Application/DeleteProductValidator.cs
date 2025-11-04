using FluentValidation;

namespace DeleteProduct.Application;

/// <summary>
/// 商品削除Validator
///
/// 【パターン: Validator（入力検証）】
///
/// 責務:
/// - 入力値の形式検証（null、範囲、長さ、フォーマットなど）
/// - ビジネスルールの検証は含めない（Domain層に委譲）
///
/// 実装ガイド:
/// - AbstractValidator<TCommand> を継承
/// - 削除の場合は検証項目が少ない（IDのみ）
/// - ビジネスルール検証はDomain層で行う
///
/// AI実装時の注意:
/// - Validatorは「入力値そのもの」の検証のみ
/// - ビジネスルールはDomain層（例: 「在庫がある商品は削除できない」など）
/// - 削除の場合、主な検証はIDの妥当性のみ
/// </summary>
public sealed class DeleteProductValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductValidator()
    {
        // ProductId検証
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("商品IDは必須です");
    }
}
