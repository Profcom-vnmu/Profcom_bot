using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Appeals.Commands.CloseAppeal;

/// <summary>
/// Обробник команди закриття звернення
/// </summary>
public class CloseAppealCommandHandler : IRequestHandler<CloseAppealCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CloseAppealCommandHandler> _logger;

    public CloseAppealCommandHandler(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILogger<CloseAppealCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(CloseAppealCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Адміністратор {AdminId} закриває звернення {AppealId} (відхилення: {IsRejection})",
                request.AdminId,
                request.AppealId,
                request.IsRejection);

            // Перевіряємо чи існує адміністратор
            var admin = await _unitOfWork.Users.GetByTelegramIdAsync(request.AdminId, cancellationToken);
            if (admin == null || admin.Role != UserRole.Admin)
            {
                _logger.LogWarning("Користувач {AdminId} не є адміністратором", request.AdminId);
                return Result<bool>.Fail("У вас немає прав адміністратора");
            }

            // Перевіряємо чи існує звернення
            var appeal = await _unitOfWork.Appeals.GetByIdAsync(request.AppealId, cancellationToken);
            if (appeal == null)
            {
                _logger.LogWarning("Звернення {AppealId} не знайдено", request.AppealId);
                return Result<bool>.Fail("Звернення не знайдено");
            }

            // Перевіряємо чи звернення вже закрите
            if (appeal.Status == AppealStatus.Closed || appeal.Status == AppealStatus.Resolved)
            {
                _logger.LogWarning("Звернення {AppealId} вже закрите", request.AppealId);
                return Result<bool>.Fail("Звернення вже закрите");
            }

            // Закриваємо звернення через доменний метод
            appeal.Close(request.AdminId, request.Reason);

            _unitOfWork.Appeals.Update(appeal);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Звернення {AppealId} успішно закрито адміністратором {AdminId}",
                request.AppealId,
                request.AdminId);

            // Відправляємо сповіщення студенту про закриття + запит на оцінку
            var adminName = admin?.FullName ?? "Адміністратор";
            var notificationResult = await _notificationService.CreateAndSendNotificationAsync(
                userId: appeal.StudentId,
                notificationEvent: NotificationEvent.AppealClosed,
                type: NotificationType.Push,
                title: request.IsRejection ? "❌ Звернення відхилено" : "✅ Звернення вирішено",
                message: $"Ваше звернення #{appeal.Id} було {(request.IsRejection ? "відхилено" : "вирішено")}\n" +
                        $"Адміністратор: {adminName}\n" +
                        $"Причина: {request.Reason}\n\n" +
                        $"Будь ласка, оцініть якість обслуговування ⭐",
                priority: NotificationPriority.Normal,
                relatedAppealId: appeal.Id,
                cancellationToken: cancellationToken);

            if (notificationResult.IsSuccess)
            {
                _logger.LogInformation(
                    "Відправлено сповіщення студенту {StudentId} про закриття звернення {AppealId}",
                    appeal.StudentId,
                    appeal.Id);
            }
            else
            {
                _logger.LogWarning(
                    "Не вдалося відправити сповіщення студенту {StudentId} про закриття: {Error}",
                    appeal.StudentId,
                    notificationResult.Error);
            }

            // Відправляємо окреме сповіщення з запитом на рейтинг
            var ratingRequestResult = await _notificationService.CreateAndSendNotificationAsync(
                userId: appeal.StudentId,
                notificationEvent: NotificationEvent.AppealRatingRequest,
                type: NotificationType.Push,
                title: "⭐ Оцініть обслуговування",
                message: $"Дякуємо за звернення!\n\n" +
                        $"Будь ласка, оцініть якість розгляду вашого звернення\n" +
                        $"Це допоможе нам покращити сервіс",
                priority: NotificationPriority.Low,
                relatedAppealId: appeal.Id,
                cancellationToken: cancellationToken);

            if (!ratingRequestResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Не вдалося відправити запит на рейтинг студенту {StudentId}: {Error}",
                    appeal.StudentId,
                    ratingRequestResult.Error);
            }

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Помилка при закритті звернення {AppealId} адміністратором {AdminId}",
                request.AppealId,
                request.AdminId);
            return Result<bool>.Fail("Виникла помилка при закритті звернення");
        }
    }
}
