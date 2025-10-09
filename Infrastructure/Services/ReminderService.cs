using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Infrastructure.Services;

/// <summary>
/// Сервіс для роботи з нагадуваннями
/// </summary>
public class ReminderService : IReminderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILogger<ReminderService> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result> CreateReminderAsync(
        long userId,
        NotificationEvent notificationEvent,
        DateTime reminderTime,
        string title,
        string message,
        int? relatedAppealId = null,
        int? relatedEventId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (reminderTime <= DateTime.UtcNow)
            {
                return Result.Fail("Час нагадування має бути в майбутньому");
            }

            var notification = Notification.Create(
                userId: userId,
                notificationEvent: notificationEvent,
                type: NotificationType.Push,
                title: title,
                message: message,
                priority: NotificationPriority.Normal,
                scheduledFor: reminderTime,
                relatedAppealId: relatedAppealId,
                relatedEventId: relatedEventId
            );

            await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Нагадування створено для користувача {UserId} на {ReminderTime}",
                userId, reminderTime);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка створення нагадування для користувача {UserId}", userId);
            return Result.Fail("Не вдалося створити нагадування");
        }
    }

    public async Task<Result> CreateAppealReminderAsync(int appealId, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        try
        {
            var appeal = await _unitOfWork.Appeals.GetByIdAsync(appealId, cancellationToken);
            if (appeal == null)
            {
                return Result.Fail("Апел не знайдено");
            }

            if (!appeal.AssignedToAdminId.HasValue)
            {
                return Result.Fail("Апел не призначено адміністратору");
            }

            var reminderTime = DateTime.UtcNow.Add(delay);
            var title = "⏰ Нагадування про апел";
            var message = $"У вас є апел #{appealId}: \"{appeal.Subject}\" без відповіді.";

            return await CreateReminderAsync(
                appeal.AssignedToAdminId.Value,
                NotificationEvent.AppealResponseReminder,
                reminderTime,
                title,
                message,
                relatedAppealId: appealId,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка створення нагадування для апелу {AppealId}", appealId);
            return Result.Fail("Не вдалося створити нагадування");
        }
    }

    public async Task<Result> CreateEventRemindersAsync(int eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
            if (eventEntity == null)
            {
                return Result.Fail("Подію не знайдено");
            }

            // Створюємо нагадування за 24 години та за 1 годину до події
            var reminders = new[]
            {
                eventEntity.StartDate.AddHours(-24),
                eventEntity.StartDate.AddHours(-1)
            };

            var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
            var createdCount = 0;

            foreach (var reminderTime in reminders)
            {
                if (reminderTime > DateTime.UtcNow)
                {
                    var timeUntil = reminderTime == eventEntity.StartDate.AddHours(-24) ? "через 24 години" : "через 1 годину";
                    var title = $"📅 Нагадування про подію";
                    var message = $"Подія \"{eventEntity.Title}\" почнеться {timeUntil}.\n" +
                                $"📍 {eventEntity.Location}\n" +
                                $"🕐 {eventEntity.StartDate:dd.MM.yyyy HH:mm}";

                    foreach (var user in users.Where(u => u.IsActive && !u.IsBanned))
                    {
                        var result = await CreateReminderAsync(
                            user.TelegramId,
                            NotificationEvent.EventReminder,
                            reminderTime,
                            title,
                            message,
                            relatedEventId: eventId,
                            cancellationToken: cancellationToken
                        );

                        if (result.IsSuccess)
                        {
                            createdCount++;
                        }
                    }
                }
            }

            _logger.LogInformation("Створено {Count} нагадувань для події {EventId}", createdCount, eventId);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка створення нагадувань для події {EventId}", eventId);
            return Result.Fail("Не вдалося створити нагадування");
        }
    }

    public async Task<Result<int>> ProcessScheduledRemindersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingNotifications = await _unitOfWork.Notifications.GetPendingNotificationsAsync(100, cancellationToken);
            var sentCount = 0;

            foreach (var notification in pendingNotifications.Where(n => n.IsReadyToSend()))
            {
                var result = await _notificationService.SendNotificationAsync(notification, cancellationToken);
                if (result.IsSuccess)
                {
                    sentCount++;
                }
            }

            _logger.LogInformation("Оброблено {Count} запланованих нагадувань", sentCount);
            return Result<int>.Ok(sentCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка обробки запланованих нагадувань");
            return Result<int>.Fail("Не вдалося обробити нагадування");
        }
    }

    public async Task<Result> CancelReminderAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId, cancellationToken);
            if (notification == null)
            {
                return Result.Fail("Нагадування не знайдено");
            }

            notification.Cancel();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Нагадування {NotificationId} скасовано", notificationId);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка скасування нагадування {NotificationId}", notificationId);
            return Result.Fail("Не вдалося скасувати нагадування");
        }
    }
}
