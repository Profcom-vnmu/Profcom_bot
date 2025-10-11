using MediatR;
using StudentUnionBot.Application.Users.DTOs;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Users.Queries.GetActiveUsers;

/// <summary>
/// Запит для отримання списку активних користувачів
/// </summary>
public class GetActiveUsersQuery : IRequest<Result<List<UserDto>>>
{
    // Порожній запит - повертає всіх активних користувачів
}
