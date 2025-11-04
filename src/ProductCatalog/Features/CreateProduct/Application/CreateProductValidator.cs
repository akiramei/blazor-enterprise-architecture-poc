using FluentValidation;

namespace CreateProduct.Application;

/// <summary>
/// 商品作成Validator
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
/// ❌ Validator: 「既に同じ名前の商品が存在する」← これはビジネスルールまたはHandler
/// ✅ Validator: 「商品名は200文字以内」
/// ❌ Validator: 「下書き状態でのみ名前を変更できる」← これはDomain層
/// </summary>
public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
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

        // Currency検証
        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("通貨は必須です")
            .Must(c => c == "JPY" || c == "USD" || c == "EUR")
            .WithMessage("通貨はJPY、USD、EURのいずれかを指定してください");

        // InitialStock検証
        RuleFor(x => x.InitialStock)
            .GreaterThanOrEqualTo(0)
            .WithMessage("在庫数は0以上である必要があります")
            .LessThanOrEqualTo(100_000)
            .WithMessage("在庫数は100,000以下で設定してください");
    }
}
