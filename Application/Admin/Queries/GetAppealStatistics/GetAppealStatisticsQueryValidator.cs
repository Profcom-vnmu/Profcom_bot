using FluentValidation;

namespace StudentUnionBot.Application.Admin.Queries.GetAppealStatistics;

public class GetAppealStatisticsQueryValidator : AbstractValidator<GetAppealStatisticsQuery>
{
    public GetAppealStatisticsQueryValidator()
    {
        RuleFor(x => x.AdminId)
            .GreaterThan(0)
            .WithMessage("AdminId повинен бути більше 0");

        RuleFor(x => x.Days)
            .GreaterThan(0)
            .LessThanOrEqualTo(365)
            .WithMessage("Кількість днів повинна бути від 1 до 365")
            .When(x => x.Days.HasValue);
    }
}