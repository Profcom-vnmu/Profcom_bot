using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Users.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Users.Commands.RegisterUser;

/// <summary>
/// Обробник команди реєстрації користувача
/// </summary>
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Реєстрація/оновлення користувача {TelegramId} (@{Username})",
                request.TelegramId,
                request.Username ?? "без username");

            // Перевіряємо чи користувач вже існує
            var existingUser = await _unitOfWork.Users.GetByTelegramIdAsync(request.TelegramId, cancellationToken);

            if (existingUser != null)
            {
                // Оновлюємо існуючого користувача
                existingUser.UpdateBasicInfo(
                    username: request.Username,
                    firstName: request.FirstName,
                    lastName: request.LastName,
                    language: request.Language);

                existingUser.UpdateLastActivity();
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Користувач {TelegramId} успішно оновлено",
                    request.TelegramId);

                return Result<UserDto>.Ok(MapToDto(existingUser));
            }

            // Створюємо нового користувача
            var newUser = BotUser.Create(
                telegramId: request.TelegramId,
                username: request.Username,
                firstName: request.FirstName,
                lastName: request.LastName,
                language: request.Language);

            await _unitOfWork.Users.AddAsync(newUser, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Новий користувач {TelegramId} зареєстрований, Role ПІСЛЯ створення: {Role}",
                request.TelegramId,
                newUser.Role);

            return Result<UserDto>.Ok(MapToDto(newUser));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при реєстрації користувача {TelegramId}", request.TelegramId);
            return Result<UserDto>.Fail("Виникла помилка при реєстрації. Спробуйте пізніше.");
        }
    }

    private static UserDto MapToDto(BotUser user)
    {
        return new UserDto
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
            Language = user.Language.GetCode(),
            JoinedAt = user.JoinedAt,
            IsActive = user.IsActive,
            RoleName = user.Role.GetDisplayName()
        };
    }
}
