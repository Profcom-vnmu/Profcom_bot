using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Notifications.Commands.SendNotification;

/// <summary>
/// Обробник команди відправки сповіщення
/// </summary>
public class SendNotificationCommandHandler : IRequestHandler<SendNotificationCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SendNotificationCommandHandler> _logger;

    public SendNotificationCommandHandler(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILogger<SendNotificationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Перевіряємо налаштування користувача
            var isEnabled = await _unitOfWork.NotificationPreferences.IsNotificationEnabledAsync(
                request.UserId, request.Event, request.Type, cancellationToken);

            if (!isEnabled)
            {
                _logger.LogInformation("Сповіщення {Event} типу {Type} вимкнено для користувача {UserId}",
                    request.Event, request.Type, request.UserId);
                return Result.Ok(); // Не помилка, просто користувач вимкнув сповіщення
            }

            string title = request.Title;
            string message = request.Message;

            // Якщо використовуємо шаблон
            if (request.UseTemplate && request.TemplateData != null)
            {
                var user = await _unitOfWork.Users.GetByTelegramIdAsync(request.UserId, cancellationToken);
                var language = user?.Language.ToString().ToLower() ?? "uk";

                var template = await _unitOfWork.NotificationTemplates.GetTemplateAsync(
                    request.Event, request.Type, language, cancellationToken);

                if (template != null)
                {
                    title = ProcessTemplate(template.TitleTemplate, request.TemplateData);
                    message = ProcessTemplate(template.MessageTemplate, request.TemplateData);
                }
                else
                {
                    _logger.LogWarning("Шаблон не знайдено для події {Event} типу {Type} мовою {Language}",
                        request.Event, request.Type, language);
                }
            }

            // Створюємо сповіщення
            var notification = Notification.Create(
                userId: request.UserId,
                notificationEvent: request.Event,
                type: request.Type,
                title: title,
                message: message,
                priority: request.Priority,
                scheduledFor: request.ScheduledFor,
                relatedAppealId: request.RelatedAppealId,
                relatedNewsId: request.RelatedNewsId,
                relatedEventId: request.RelatedEventId
            );

            await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Якщо не заплановано на майбутнє - відправляємо зараз
            if (!request.ScheduledFor.HasValue || request.ScheduledFor.Value <= DateTime.UtcNow)
            {
                var sendResult = await _notificationService.SendNotificationAsync(notification, cancellationToken);
                if (!sendResult.IsSuccess)
                {
                    _logger.LogWarning("Не вдалося відправити сповіщення {NotificationId}: {Error}",
                        notification.Id, sendResult.Error);
                }
            }

            _logger.LogInformation("Сповіщення {Event} створено для користувача {UserId}, ID: {NotificationId}",
                request.Event, request.UserId, notification.Id);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка створення сповіщення {Event} для користувача {UserId}",
                request.Event, request.UserId);
            return Result.Fail("Не вдалося створити сповіщення");
        }
    }

    /// <summary>
    /// Обробити шаблон з підстановкою даних
    /// </summary>
    private static string ProcessTemplate(string template, Dictionary<string, string> data)
    {
        var result = template;
        foreach (var kvp in data)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value);
        }
        return result;
    }
}
