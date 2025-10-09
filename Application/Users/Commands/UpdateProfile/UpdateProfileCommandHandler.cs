using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Users.Commands.UpdateProfile;

/// <summary>
/// Обробник команди оновлення профілю користувача
/// </summary>
public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProfileCommandHandler> _logger;

    public UpdateProfileCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateProfileCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Оновлення профілю користувача {TelegramId}",
                request.TelegramId);

            // Отримати користувача
            var user = await _unitOfWork.Users.GetByTelegramIdAsync(request.TelegramId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Користувач з TelegramId {TelegramId} не знайдений", request.TelegramId);
                return Result<bool>.Fail("Користувача не знайдено");
            }

            // Оновити профіль через domain method
            user.UpdateProfile(
                fullName: request.FullName?.Trim(),
                faculty: request.Faculty?.Trim(),
                course: request.Course,
                group: request.Group?.Trim());

            // Зберегти зміни
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Профіль користувача {TelegramId} успішно оновлено",
                request.TelegramId);

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Помилка при оновленні профілю користувача {TelegramId}",
                request.TelegramId);
            return Result<bool>.Fail("Сталася помилка при оновленні профілю");
        }
    }
}