using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Users.Commands.VerifyEmail;

/// <summary>
/// Обробник команди верифікації email
/// </summary>
public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Спроба верифікації email для користувача {TelegramId} з кодом {Code}",
                request.TelegramId,
                request.Code);

            // Отримати користувача
            var user = await _unitOfWork.Users.GetByTelegramIdAsync(request.TelegramId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Користувач з TelegramId {TelegramId} не знайдений", request.TelegramId);
                return Result<bool>.Fail("Користувача не знайдено");
            }

            // Перевірити чи вже верифіковано
            if (user.IsEmailVerified)
            {
                _logger.LogInformation(
                    "Email користувача {TelegramId} вже верифіковано",
                    request.TelegramId);
                return Result<bool>.Ok(true);
            }

            // Перевірити код через domain method
            var isVerified = user.VerifyEmail(request.Code);

            if (!isVerified)
            {
                _logger.LogWarning(
                    "Невірний або прострочений код верифікації для користувача {TelegramId}",
                    request.TelegramId);
                return Result<bool>.Fail("Невірний або прострочений код верифікації");
            }

            // Зберегти зміни
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Email успішно верифіковано для користувача {TelegramId}",
                request.TelegramId);

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Помилка при верифікації email для користувача {TelegramId}",
                request.TelegramId);
            return Result<bool>.Fail("Сталася помилка при верифікації email");
        }
    }
}
