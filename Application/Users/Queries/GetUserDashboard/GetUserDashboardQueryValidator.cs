using FluentValidation;

namespace StudentUnionBot.Application.Users.Queries.GetUserDashboard;

/// <summary>
/// Валідатор для GetUserDashboardQuery
/// </summary>
public class GetUserDashboardQueryValidator : AbstractValidator<GetUserDashboardQuery>
{
    public GetUserDashboardQueryValidator()
    {
        RuleFor(x => x.TelegramId)
            .GreaterThan(0)
            .WithMessage("Telegram ID має бути більше 0");
    }
}
