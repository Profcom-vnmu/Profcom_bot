using MediatR;
using StudentUnionBot.Application.Users.DTOs;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Users.Queries.GetUserDashboard;

/// <summary>
/// Query для отримання персоналізованого dashboard користувача
/// </summary>
public class GetUserDashboardQuery : IRequest<Result<UserDashboardDto>>
{
    public long TelegramId { get; set; }
}
