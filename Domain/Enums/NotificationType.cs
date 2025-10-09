namespace StudentUnionBot.Domain.Enums;

/// <summary>
/// Тип сповіщення
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Email сповіщення
    /// </summary>
    Email = 1,

    /// <summary>
    /// Push сповіщення (Telegram)
    /// </summary>
    Push = 2,

    /// <summary>
    /// Внутрішнє сповіщення (в боті)
    /// </summary>
    InApp = 4
}

/// <summary>
/// Категорія події для сповіщення
/// </summary>
public enum NotificationEvent
{
    /// <summary>
    /// Новий апел створено
    /// </summary>
    AppealCreated = 1,

    /// <summary>
    /// Апел призначено адміністратору
    /// </summary>
    AppealAssigned = 2,

    /// <summary>
    /// Статус апелу змінено
    /// </summary>
    AppealStatusChanged = 3,

    /// <summary>
    /// Нове повідомлення в апелі
    /// </summary>
    AppealMessageReceived = 4,

    /// <summary>
    /// Апел закрито
    /// </summary>
    AppealClosed = 5,

    /// <summary>
    /// Запит на оцінку апелу
    /// </summary>
    AppealRatingRequest = 6,

    /// <summary>
    /// Нагадування про відповідь
    /// </summary>
    AppealResponseReminder = 7,

    /// <summary>
    /// Нова новина опублікована
    /// </summary>
    NewsPublished = 8,

    /// <summary>
    /// Нова подія опублікована
    /// </summary>
    EventPublished = 9,

    /// <summary>
    /// Нагадування про подію
    /// </summary>
    EventReminder = 10,

    /// <summary>
    /// Системне сповіщення
    /// </summary>
    SystemNotification = 11,

    /// <summary>
    /// Сповіщення про верифікацію
    /// </summary>
    VerificationRequired = 12
}

/// <summary>
/// Пріоритет сповіщення
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// Низький пріоритет
    /// </summary>
    Low = 1,

    /// <summary>
    /// Середній пріоритет
    /// </summary>
    Normal = 2,

    /// <summary>
    /// Високий пріоритет
    /// </summary>
    High = 3,

    /// <summary>
    /// Критичний пріоритет
    /// </summary>
    Critical = 4
}

/// <summary>
/// Статус сповіщення
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// Очікує відправки
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Відправлено
    /// </summary>
    Sent = 2,

    /// <summary>
    /// Доставлено
    /// </summary>
    Delivered = 3,

    /// <summary>
    /// Прочитано
    /// </summary>
    Read = 4,

    /// <summary>
    /// Помилка відправки
    /// </summary>
    Failed = 5,

    /// <summary>
    /// Скасовано
    /// </summary>
    Cancelled = 6
}

/// <summary>
/// Розширення для NotificationEvent
/// </summary>
public static class NotificationEventExtensions
{
    public static string GetDisplayName(this NotificationEvent notificationEvent)
    {
        return notificationEvent switch
        {
            NotificationEvent.AppealCreated => "Нове звернення",
            NotificationEvent.AppealAssigned => "Звернення призначено",
            NotificationEvent.AppealStatusChanged => "Статус змінено",
            NotificationEvent.AppealMessageReceived => "Нове повідомлення",
            NotificationEvent.AppealClosed => "Звернення закрито",
            NotificationEvent.AppealRatingRequest => "Запит на оцінку",
            NotificationEvent.AppealResponseReminder => "Нагадування про відповідь",
            NotificationEvent.NewsPublished => "Нова новина",
            NotificationEvent.EventPublished => "Нова подія",
            NotificationEvent.EventReminder => "Нагадування про подію",
            NotificationEvent.SystemNotification => "Системне сповіщення",
            NotificationEvent.VerificationRequired => "Необхідна верифікація",
            _ => "Невідоме сповіщення"
        };
    }

    public static string GetIcon(this NotificationEvent notificationEvent)
    {
        return notificationEvent switch
        {
            NotificationEvent.AppealCreated => "📝",
            NotificationEvent.AppealAssigned => "👤",
            NotificationEvent.AppealStatusChanged => "🔄",
            NotificationEvent.AppealMessageReceived => "💬",
            NotificationEvent.AppealClosed => "✅",
            NotificationEvent.AppealRatingRequest => "⭐",
            NotificationEvent.AppealResponseReminder => "⏰",
            NotificationEvent.NewsPublished => "📰",
            NotificationEvent.EventPublished => "📅",
            NotificationEvent.EventReminder => "⏰",
            NotificationEvent.SystemNotification => "🔔",
            NotificationEvent.VerificationRequired => "✉️",
            _ => "🔔"
        };
    }
}

/// <summary>
/// Розширення для NotificationPriority
/// </summary>
public static class NotificationPriorityExtensions
{
    public static string GetDisplayName(this NotificationPriority priority)
    {
        return priority switch
        {
            NotificationPriority.Low => "Низький",
            NotificationPriority.Normal => "Звичайний",
            NotificationPriority.High => "Високий",
            NotificationPriority.Critical => "Критичний",
            _ => "Невідомий"
        };
    }

    public static string GetIcon(this NotificationPriority priority)
    {
        return priority switch
        {
            NotificationPriority.Low => "🔵",
            NotificationPriority.Normal => "🟢",
            NotificationPriority.High => "🟡",
            NotificationPriority.Critical => "🔴",
            _ => "⚪"
        };
    }
}
