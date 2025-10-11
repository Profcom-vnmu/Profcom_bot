using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Сервіс для відправки сповіщень
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Відправити сповіщення
    /// </summary>
    Task<Result> SendNotificationAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Відправити групу сповіщень
    /// </summary>
    Task<Result<int>> SendBatchNotificationsAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default);

    /// <summary>
    /// Створити та відправити сповіщення
    /// </summary>
    Task<Result> CreateAndSendNotificationAsync(
        long userId,
        NotificationEvent notificationEvent,
        NotificationType type,
        string title,
        string message,
        NotificationPriority priority = NotificationPriority.Normal,
        DateTime? scheduledFor = null,
        int? relatedAppealId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Відправити сповіщення з використанням шаблону
    /// </summary>
    Task<Result> SendTemplatedNotificationAsync(
        long userId,
        NotificationEvent notificationEvent,
        NotificationType type,
        Dictionary<string, string> templateData,
        NotificationPriority priority = NotificationPriority.Normal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Відправити сповіщення всім адміністраторам
    /// </summary>
    Task<Result<int>> NotifyAllAdminsAsync(
        NotificationEvent notificationEvent,
        string title,
        string message,
        NotificationPriority priority = NotificationPriority.Normal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Відправити сповіщення про опубліковану новину всім користувачам
    /// </summary>
    Task<Result<int>> SendNewsPublishedNotificationAsync(
        int newsId,
        string title,
        string summary,
        NewsCategory category,
        string? photoFileId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Відправити сповіщення про нову подію всім користувачам
    /// </summary>
    Task<Result<int>> SendEventCreatedNotificationAsync(
        int eventId,
        string title,
        string summary,
        DateTime eventDate,
        string? location = null,
        string? photoFileId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Репозиторій для сповіщень
/// </summary>
public interface INotificationRepository : IRepository<Notification>
{
    /// <summary>
    /// Отримати сповіщення користувача
    /// </summary>
    Task<List<Notification>> GetUserNotificationsAsync(long userId, bool includeRead = false, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати непрочитані сповіщення
    /// </summary>
    Task<List<Notification>> GetUnreadNotificationsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати кількість непрочитаних сповіщень
    /// </summary>
    Task<int> GetUnreadCountAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати сповіщення готові до відправки
    /// </summary>
    Task<List<Notification>> GetPendingNotificationsAsync(int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати невдалі сповіщення для повторної спроби
    /// </summary>
    Task<List<Notification>> GetFailedNotificationsForRetryAsync(int maxRetries = 3, CancellationToken cancellationToken = default);

    /// <summary>
    /// Позначити всі сповіщення як прочитані
    /// </summary>
    Task<int> MarkAllAsReadAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Видалити старі сповіщення
    /// </summary>
    Task<int> DeleteOldNotificationsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Репозиторій для налаштувань сповіщень
/// </summary>
public interface INotificationPreferenceRepository : IRepository<NotificationPreference>
{
    /// <summary>
    /// Отримати налаштування користувача
    /// </summary>
    Task<List<NotificationPreference>> GetUserPreferencesAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати налаштування для події
    /// </summary>
    Task<NotificationPreference?> GetPreferenceAsync(long userId, NotificationEvent notificationEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Чи увімкнено тип сповіщення для події
    /// </summary>
    Task<bool> IsNotificationEnabledAsync(long userId, NotificationEvent notificationEvent, NotificationType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Створити налаштування за замовчуванням для всіх подій
    /// </summary>
    Task CreateDefaultPreferencesAsync(long userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Репозиторій для шаблонів сповіщень
/// </summary>
public interface INotificationTemplateRepository : IRepository<NotificationTemplate>
{
    /// <summary>
    /// Отримати шаблон для події
    /// </summary>
    Task<NotificationTemplate?> GetTemplateAsync(NotificationEvent notificationEvent, NotificationType type, string language = "uk", CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати всі активні шаблони
    /// </summary>
    Task<List<NotificationTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати шаблони для мови
    /// </summary>
    Task<List<NotificationTemplate>> GetTemplatesByLanguageAsync(string language, CancellationToken cancellationToken = default);
}

/// <summary>
/// Сервіс для роботи з нагадуваннями
/// </summary>
public interface IReminderService
{
    /// <summary>
    /// Створити нагадування
    /// </summary>
    Task<Result> CreateReminderAsync(
        long userId,
        NotificationEvent notificationEvent,
        DateTime reminderTime,
        string title,
        string message,
        int? relatedAppealId = null,
        int? relatedEventId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Створити автоматичне нагадування для апелу
    /// </summary>
    Task<Result> CreateAppealReminderAsync(int appealId, TimeSpan delay, CancellationToken cancellationToken = default);

    /// <summary>
    /// Створити нагадування про подію
    /// </summary>
    Task<Result> CreateEventRemindersAsync(int eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обробити заплановані нагадування
    /// </summary>
    Task<Result<int>> ProcessScheduledRemindersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Скасувати нагадування
    /// </summary>
    Task<Result> CancelReminderAsync(int notificationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Провайдер для відправки Email
/// </summary>
public interface IEmailNotificationProvider
{
    /// <summary>
    /// Відправити email
    /// </summary>
    Task<Result> SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Відправити email з шаблону
    /// </summary>
    Task<Result> SendTemplatedEmailAsync(string to, string templateName, Dictionary<string, string> templateData, CancellationToken cancellationToken = default);
}

/// <summary>
/// Провайдер для відправки Push сповіщень (Telegram)
/// </summary>
public interface IPushNotificationProvider
{
    /// <summary>
    /// Відправити push сповіщення через Telegram
    /// </summary>
    Task<Result> SendPushAsync(long chatId, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Відправити push з кнопками
    /// </summary>
    Task<Result> SendPushWithActionsAsync(long chatId, string message, Dictionary<string, string> actions, CancellationToken cancellationToken = default);
}
