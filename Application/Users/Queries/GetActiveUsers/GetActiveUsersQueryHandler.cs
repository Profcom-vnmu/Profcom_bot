using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Users.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Users.Queries.GetActiveUsers;

/// <summary>
/// Обробник запиту для отримання списку активних користувачів
/// </summary>
public class GetActiveUsersQueryHandler : IRequestHandler<GetActiveUsersQuery, Result<List<UserDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetActiveUsersQueryHandler> _logger;

    public GetActiveUsersQueryHandler(
        IUserRepository userRepository,
        ILogger<GetActiveUsersQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<List<UserDto>>> Handle(GetActiveUsersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Отримання списку активних користувачів");

            var activeUsers = await _userRepository.GetActiveUsersAsync(cancellationToken);

            var userDtos = activeUsers.Select(user => new UserDto
            {
                TelegramId = user.TelegramId,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Faculty = user.Faculty,
                Course = user.Course,
                Group = user.Group,
                Email = user.Email,
                IsEmailVerified = user.IsEmailVerified,
                Language = user.Language == Language.English ? "en" : "uk",
                JoinedAt = user.JoinedAt,
                IsActive = user.IsActive,
                RoleName = user.Role.ToString(),
                Role = user.Role
            }).ToList();

            _logger.LogInformation("Отримано {Count} активних користувачів", userDtos.Count);
            return Result<List<UserDto>>.Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні списку активних користувачів");
            return Result<List<UserDto>>.Fail($"Помилка при отриманні користувачів: {ex.Message}");
        }
    }
}
