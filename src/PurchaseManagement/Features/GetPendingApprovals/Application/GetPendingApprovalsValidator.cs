using FluentValidation;

namespace GetPendingApprovals.Application;

/// <summary>
/// GetPendingApprovalsQuery バリデーター
///
/// 【パターン: FluentValidation】
///
/// 検証ルール:
/// - PageNumber: 1以上
/// - PageSize: 1以上100以下
/// - SortBy: 許可されたフィールド名のみ
/// </summary>
public sealed class GetPendingApprovalsValidator : AbstractValidator<GetPendingApprovalsQuery>
{
    private static readonly string[] AllowedSortFields =
    {
        "TotalAmount",
        "SubmittedAt",
        "RequestNumber"
    };

    public GetPendingApprovalsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("ページ番号は1以上である必要があります");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("ページサイズは1から100の間である必要があります");

        RuleFor(x => x.SortBy)
            .Must(sortBy => AllowedSortFields.Contains(sortBy))
            .WithMessage($"SortByは次のいずれかである必要があります: {string.Join(", ", AllowedSortFields)}");
    }
}
