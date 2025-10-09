using MediatR;
using StudentUnionBot.Application.Users.DTOs;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Users.Queries.GetUserByTelegramId;

/// <summary>
/// Запит для отримання користувача за Telegram ID
/// </summary>
public class GetUserByTelegramIdQuery : IRequest<Result<UserDto?>>
{
    public long TelegramId { get; set; }
}