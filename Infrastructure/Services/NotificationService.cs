using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Infrastructure.Services;

/// <summary>
/// Сервіс для відправки сповіщень
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailNotificationProvider _emailProvider;
    private readonly IPushNotificationProvider _pushProvider;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork unitOfWork,
        IEmailNotificationProvider emailProvider,
        IPushNotificationProvider pushProvider,
        ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailProvider = emailProvider;
        _pushProvider = pushProvider;
        _logger = logger;
    }

    public async Task<Result> SendNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!notification.IsReadyToSend())
            {
                return Result.Fail("Сповіщення не готове до відправки");
            }

            Result sendResult;

            switch (notification.Type)
            {
                case NotificationType.Email:
                    sendResult = await SendEmailNotificationAsync(notification, cancellationToken);
                    break;

                case NotificationType.Push:
                    sendResult = await SendPushNotificationAsync(notification, cancellationToken);
                    break;

                case NotificationType.InApp:
                    // In-App сповіщення вже в БД, просто позначаємо як відправлене
                    notification.MarkAsDelivered();
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return Result.Ok();

                default:
                    return Result.Fail($"Непідтримуваний тип сповіщення: {notification.Type}");
            }

            if (sendResult.IsSuccess)
            {
                notification.MarkAsSent();
                notification.MarkAsDelivered();
            }
            else
            {
                notification.MarkAsFailed(sendResult.Error ?? "Невідома помилка");
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return sendResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка відправки сповіщення {NotificationId}", notification.Id);
            notification.MarkAsFailed(ex.Message);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Fail("Помилка відправки сповіщення");
        }
    }

    public async Task<Result<int>> SendBatchNotificationsAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default)
    {
        var sentCount = 0;

        foreach (var notification in notifications)
        {
            var result = await SendNotificationAsync(notification, cancellationToken);
            if (result.IsSuccess)
            {
                sentCount++;
            }
        }

        return Result<int>.Ok(sentCount);
    }

    public async Task<Result> CreateAndSendNotificationAsync(
        long userId,
        NotificationEvent notificationEvent,
        NotificationType type,
        string title,
        string message,
        NotificationPriority priority = NotificationPriority.Normal,
        DateTime? scheduledFor = null,
        int? relatedAppealId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = Notification.Create(
                userId, notificationEvent, type, title, message, priority, scheduledFor, relatedAppealId);

            await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!scheduledFor.HasValue || scheduledFor.Value <= DateTime.UtcNow)
            {
                return await SendNotificationAsync(notification, cancellationToken);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка створення та відправки сповіщення");
            return Result.Fail("Не вдалося створити сповіщення");
        }
    }

    public async Task<Result> SendTemplatedNotificationAsync(
        long userId,
        NotificationEvent notificationEvent,
        NotificationType type,
        Dictionary<string, string> templateData,
        NotificationPriority priority = NotificationPriority.Normal,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Отримуємо користувача для визначення мови
            var user = await _unitOfWork.Users.GetByTelegramIdAsync(userId, cancellationToken);
            var language = user?.Language.ToString().ToLower() ?? "uk";

            // Отримуємо шаблон
            var template = await _unitOfWork.NotificationTemplates.GetTemplateAsync(
                notificationEvent, type, language, cancellationToken);

            if (template == null)
            {
                return Result.Fail($"Шаблон не знайдено для події {notificationEvent}");
            }

            // Обробляємо шаблон
            var title = ProcessTemplate(template.TitleTemplate, templateData);
            var message = ProcessTemplate(template.MessageTemplate, templateData);

            return await CreateAndSendNotificationAsync(
                userId, notificationEvent, type, title, message, priority, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка відправки сповіщення з шаблону");
            return Result.Fail("Не вдалося відправити сповіщення");
        }
    }

    public async Task<Result<int>> NotifyAllAdminsAsync(
        NotificationEvent notificationEvent,
        string title,
        string message,
        NotificationPriority priority = NotificationPriority.Normal,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var admins = await _unitOfWork.Users.GetAdminsAsync(cancellationToken);
            var sentCount = 0;

            foreach (var admin in admins)
            {
                var result = await CreateAndSendNotificationAsync(
                    admin.TelegramId, // Використовуємо TelegramId
                    notificationEvent,
                    NotificationType.Push, // Використовуємо Push для Telegram
                    title,
                    message,
                    priority,
                    cancellationToken: cancellationToken);

                if (result.IsSuccess)
                {
                    sentCount++;
                }
            }

            return Result<int>.Ok(sentCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка відправки сповіщень адміністраторам");
            return Result<int>.Fail("Не вдалося відправити сповіщення адміністраторам");
        }
    }

    public async Task<Result<int>> SendNewsPublishedNotificationAsync(
        int newsId,
        string title,
        string summary,
        NewsCategory category,
        string? photoFileId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending news published notifications for news {NewsId}", newsId);

            // Отримуємо всіх активних користувачів (не заблокованих)
            var allUsers = await _unitOfWork.Users.GetAllAsync(cancellationToken);
            var activeUsers = allUsers.Where(u => !u.IsBanned).ToList();

            var categoryIcon = category.GetEmoji();

            var message = $"{categoryIcon} <b>Нова новина: {title}</b>\n\n{summary}\n\n💬 Читайте більше в розділі \"Новини\"";

            var sentCount = 0;

            foreach (var user in activeUsers)
            {
                try
                {
                    var result = await _pushProvider.SendPushAsync(user.TelegramId, message, cancellationToken);
                    if (result.IsSuccess)
                    {
                        sentCount++;
                    }
                    
                    // Невелика затримка між повідомленнями
                    await Task.Delay(50, cancellationToken);
                }
                catch (Exception userEx)
                {
                    _logger.LogWarning(userEx, "Failed to send news notification to user {UserId}", user.TelegramId);
                }
            }

            _logger.LogInformation("Sent news notification to {SentCount}/{TotalCount} users", sentCount, activeUsers.Count);

            return Result<int>.Ok(sentCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending news published notifications for news {NewsId}", newsId);
            return Result<int>.Fail("Помилка відправки сповіщень про новину");
        }
    }

    public async Task<Result<int>> SendEventCreatedNotificationAsync(
        int eventId,
        string title,
        string summary,
        DateTime eventDate,
        string? location = null,
        string? photoFileId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending event created notifications for event {EventId}", eventId);

            // Отримуємо всіх активних користувачів
            var allUsers = await _unitOfWork.Users.GetAllAsync(cancellationToken);
            var activeUsers = allUsers.Where(u => !u.IsBanned).ToList();

            var eventDateStr = eventDate.ToString("dd.MM.yyyy HH:mm");
            var locationStr = !string.IsNullOrEmpty(location) ? $"\n📍 Місце: {location}" : "";

            var message = $"🎉 <b>Нова подія: {title}</b>\n\n" +
                         $"{summary}\n\n" +
                         $"📅 Дата: {eventDateStr}{locationStr}\n\n" +
                         $"🎫 Реєструйтесь в розділі \"Події\"";

            var sentCount = 0;

            foreach (var user in activeUsers)
            {
                try
                {
                    var result = await _pushProvider.SendPushAsync(user.TelegramId, message, cancellationToken);
                    if (result.IsSuccess)
                    {
                        sentCount++;
                    }
                    
                    // Невелика затримка між повідомленнями
                    await Task.Delay(50, cancellationToken);
                }
                catch (Exception userEx)
                {
                    _logger.LogWarning(userEx, "Failed to send event notification to user {UserId}", user.TelegramId);
                }
            }

            _logger.LogInformation("Sent event notification to {SentCount}/{TotalCount} users", sentCount, activeUsers.Count);

            return Result<int>.Ok(sentCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending event created notifications for event {EventId}", eventId);
            return Result<int>.Fail("Помилка відправки сповіщень про подію");
        }
    }

    private async Task<Result> SendEmailNotificationAsync(Notification notification, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByTelegramIdAsync(notification.UserId, cancellationToken);
            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                return Result.Fail("Email користувача не знайдено");
            }

            return await _emailProvider.SendEmailAsync(
                user.Email, notification.Title, notification.Message, true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка відправки email сповіщення");
            return Result.Fail("Помилка відправки email");
        }
    }

    private async Task<Result> SendPushNotificationAsync(Notification notification, CancellationToken cancellationToken)
    {
        try
        {
            var fullMessage = $"{notification.Event.GetIcon()} <b>{notification.Title}</b>\n\n{notification.Message}";
            // notification.UserId містить TelegramId
            return await _pushProvider.SendPushAsync(notification.UserId, fullMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка відправки push сповіщення");
            return Result.Fail("Помилка відправки push");
        }
    }

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
