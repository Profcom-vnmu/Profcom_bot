using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Users.Commands.SendVerificationEmail;

/// <summary>
/// Обробник команди відправки коду верифікації email
/// </summary>
public class SendVerificationEmailCommandHandler : IRequestHandler<SendVerificationEmailCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendVerificationEmailCommandHandler> _logger;

    public SendVerificationEmailCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<SendVerificationEmailCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(SendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Відправка коду верифікації для користувача {TelegramId} на email {Email}",
                request.TelegramId,
                request.Email);

            // Отримати користувача
            var user = await _unitOfWork.Users.GetByTelegramIdAsync(request.TelegramId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Користувач з TelegramId {TelegramId} не знайдений", request.TelegramId);
                return Result<bool>.Fail("Користувача не знайдено");
            }

            // Згенерувати код верифікації
            var verificationCode = user.GenerateVerificationCode();

            // Зберегти в БД
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Код верифікації згенеровано для користувача {TelegramId}: {Code} (діє до {Expiry})",
                request.TelegramId,
                verificationCode,
                user.VerificationCodeExpiry);

            // Відправити email
            var emailSent = await _emailService.SendVerificationCodeAsync(
                request.Email,
                verificationCode,
                cancellationToken);

            if (!emailSent)
            {
                _logger.LogWarning(
                    "Не вдалося відправити email з кодом верифікації для користувача {TelegramId}",
                    request.TelegramId);
                return Result<bool>.Fail("Не вдалося відправити email. Спробуйте пізніше.");
            }

            _logger.LogInformation(
                "Код верифікації успішно відправлено на {Email} для користувача {TelegramId}",
                request.Email,
                request.TelegramId);

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Помилка при відправці коду верифікації для користувача {TelegramId}",
                request.TelegramId);
            return Result<bool>.Fail("Сталася помилка при відправці коду верифікації");
        }
    }
}
