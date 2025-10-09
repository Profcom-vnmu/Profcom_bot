using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Users.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Users.Queries.GetUserByTelegramId;

/// <summary>
/// Обробник запиту для отримання користувача за Telegram ID
/// </summary>
public class GetUserByTelegramIdQueryHandler : IRequestHandler<GetUserByTelegramIdQuery, Result<UserDto?>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserByTelegramIdQueryHandler> _logger;

    public GetUserByTelegramIdQueryHandler(
        IUserRepository userRepository,
        ILogger<GetUserByTelegramIdQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<UserDto?>> Handle(GetUserByTelegramIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Отримання користувача з TelegramId: {TelegramId}", request.TelegramId);

            var user = await _userRepository.GetByTelegramIdAsync(request.TelegramId, cancellationToken);

            if (user == null)
            {
                _logger.LogInformation("Користувача з TelegramId {TelegramId} не знайдено", request.TelegramId);
                return Result<UserDto?>.Ok(null);
            }

            var userDto = new UserDto
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
            };

            _logger.LogInformation("Користувача з TelegramId {TelegramId} успішно отримано", request.TelegramId);
            return Result<UserDto?>.Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні користувача з TelegramId: {TelegramId}", request.TelegramId);
            return Result<UserDto?>.Fail($"Помилка при отриманні користувача: {ex.Message}");
        }
    }
}