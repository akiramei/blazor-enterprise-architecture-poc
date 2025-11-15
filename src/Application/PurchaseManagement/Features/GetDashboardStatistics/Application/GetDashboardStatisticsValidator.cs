using FluentValidation;

namespace GetDashboardStatistics.Application;

/// <summary>
/// GetDashboardStatisticsQuery バリデーター
/// </summary>
public sealed class GetDashboardStatisticsValidator : AbstractValidator<GetDashboardStatisticsQuery>
{
    public GetDashboardStatisticsValidator()
    {
        RuleFor(x => x.MonthsToInclude)
            .InclusiveBetween(1, 24)
            .WithMessage("月数は1〜24の間である必要があります");

        RuleFor(x => x.TopRequestsCount)
            .InclusiveBetween(1, 100)
            .WithMessage("トップ申請件数は1〜100の間である必要があります");

        RuleFor(x => x.TopDepartmentsCount)
            .InclusiveBetween(1, 50)
            .WithMessage("部門数は1〜50の間である必要があります");
    }
}
