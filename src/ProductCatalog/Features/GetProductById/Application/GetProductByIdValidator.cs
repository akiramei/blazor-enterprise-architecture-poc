using FluentValidation;
using ProductCatalog.Shared.Application.DTOs;

namespace GetProductById.Application;

/// <summary>
/// 商品詳細取得Validator
///
/// 【パターン: Query Validator（入力検証）】
///
/// 責務:
/// - Query パラメータの形式検証
/// - 必須パラメータのチェック
/// - ID形式の検証
///
/// 実装ガイド:
/// - AbstractValidator<TQuery> を継承
/// - RuleFor() でフィールドごとに検証ルールを定義
/// - WithMessage() でユーザーフレンドリーなエラーメッセージを設定
///
/// AI実装時の注意:
/// - Queryの場合も入力検証は重要
/// - IDが空のGuidでないことを確認
/// - エラーメッセージは日本語で具体的に
/// - ValidationBehaviorが自動的にエラーをハンドリング
///
/// Query Validator vs Command Validator:
/// ✅ Query Validator: パラメータが必須かどうか、範囲チェック
/// ✅ Command Validator: より複雑な検証（形式、長さ、範囲）
/// </summary>
public sealed class GetProductByIdValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdValidator()
    {
        // ProductId検証
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("商品IDは必須です");
    }
}
