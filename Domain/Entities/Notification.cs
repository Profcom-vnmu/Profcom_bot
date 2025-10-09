using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Entities;

/// <summary>
/// Сповіщення для користувачів
/// </summary>
public class Notification
{
    public int Id { get; private set; }
    public long UserId { get; private set; } // TelegramId користувача
    public NotificationEvent Event { get; private set; }
    public NotificationType Type { get; private set; }
    public NotificationPriority Priority { get; private set; }
    public NotificationStatus Status { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? Data { get; private set; } // JSON data для додаткової інформації
    public int? RelatedAppealId { get; private set; }
    public int? RelatedNewsId { get; private set; }
    public int? RelatedEventId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ScheduledFor { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }

    // Навігаційні властивості
    public BotUser User { get; private set; } = null!;
    public Appeal? RelatedAppeal { get; private set; }
    public News? RelatedNews { get; private set; }
    public Event? RelatedEvent { get; private set; }

    // Конструктор для EF Core
    private Notification() { }

    /// <summary>
    /// Створити нове сповіщення
    /// </summary>
    public static Notification Create(
        long userId, // TelegramId користувача
        NotificationEvent notificationEvent,
        NotificationType type,
        string title,
        string message,
        NotificationPriority priority = NotificationPriority.Normal,
        DateTime? scheduledFor = null,
        int? relatedAppealId = null,
        int? relatedNewsId = null,
        int? relatedEventId = null,
        string? data = null)
    {
        if (userId <= 0)
            throw new ArgumentException("ID користувача має бути більше нуля", nameof(userId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Заголовок не може бути порожнім", nameof(title));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Повідомлення не може бути порожнім", nameof(message));

        return new Notification
        {
            UserId = userId,
            Event = notificationEvent,
            Type = type,
            Title = title,
            Message = message,
            Priority = priority,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ScheduledFor = scheduledFor,
            RelatedAppealId = relatedAppealId,
            RelatedNewsId = relatedNewsId,
            RelatedEventId = relatedEventId,
            Data = data,
            RetryCount = 0
        };
    }

    /// <summary>
    /// Позначити як відправлене
    /// </summary>
    public void MarkAsSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Позначити як доставлене
    /// </summary>
    public void MarkAsDelivered()
    {
        Status = NotificationStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Позначити як прочитане
    /// </summary>
    public void MarkAsRead()
    {
        Status = NotificationStatus.Read;
        ReadAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Позначити як невдалу спробу
    /// </summary>
    public void MarkAsFailed(string errorMessage)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
    }

    /// <summary>
    /// Скасувати сповіщення
    /// </summary>
    public void Cancel()
    {
        Status = NotificationStatus.Cancelled;
    }

    /// <summary>
    /// Перенести час відправки
    /// </summary>
    public void Reschedule(DateTime newScheduledTime)
    {
        ScheduledFor = newScheduledTime;
        Status = NotificationStatus.Pending;
        ErrorMessage = null;
    }

    /// <summary>
    /// Чи можна повторно спробувати відправити
    /// </summary>
    public bool CanRetry(int maxRetries = 3)
    {
        return Status == NotificationStatus.Failed && RetryCount < maxRetries;
    }

    /// <summary>
    /// Чи сповіщення готове до відправки
    /// </summary>
    public bool IsReadyToSend()
    {
        if (Status != NotificationStatus.Pending)
            return false;

        if (ScheduledFor.HasValue && ScheduledFor.Value > DateTime.UtcNow)
            return false;

        return true;
    }
}

/// <summary>
/// Налаштування сповіщень користувача
/// </summary>
public class NotificationPreference
{
    public int Id { get; private set; }
    public long UserId { get; private set; } // TelegramId користувача
    public NotificationEvent Event { get; private set; }
    public bool EmailEnabled { get; private set; }
    public bool PushEnabled { get; private set; }
    public bool SmsEnabled { get; private set; }
    public bool InAppEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Навігаційні властивості
    public BotUser User { get; private set; } = null!;

    // Конструктор для EF Core
    private NotificationPreference() { }

    /// <summary>
    /// Створити налаштування сповіщень за замовчуванням
    /// </summary>
    public static NotificationPreference CreateDefault(long userId, NotificationEvent notificationEvent)
    {
        return new NotificationPreference
        {
            UserId = userId,
            Event = notificationEvent,
            EmailEnabled = false, // За замовчуванням вимкнено
            PushEnabled = true,   // Telegram push увімкнено
            SmsEnabled = false,   // SMS вимкнено
            InAppEnabled = true,  // In-app увімкнено
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Оновити налаштування
    /// </summary>
    public void Update(bool? emailEnabled = null, bool? pushEnabled = null, bool? smsEnabled = null, bool? inAppEnabled = null)
    {
        if (emailEnabled.HasValue) EmailEnabled = emailEnabled.Value;
        if (pushEnabled.HasValue) PushEnabled = pushEnabled.Value;
        if (smsEnabled.HasValue) SmsEnabled = smsEnabled.Value;
        if (inAppEnabled.HasValue) InAppEnabled = inAppEnabled.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Чи увімкнено тип сповіщення
    /// </summary>
    public bool IsTypeEnabled(NotificationType type)
    {
        return type switch
        {
            NotificationType.Email => EmailEnabled,
            NotificationType.Push => PushEnabled,
            NotificationType.InApp => InAppEnabled,
            _ => false
        };
    }
}

/// <summary>
/// Шаблон сповіщення
/// </summary>
public class NotificationTemplate
{
    public int Id { get; private set; }
    public NotificationEvent Event { get; private set; }
    public NotificationType Type { get; private set; }
    public string Language { get; private set; } = "uk";
    public string TitleTemplate { get; private set; } = string.Empty;
    public string MessageTemplate { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Конструктор для EF Core
    private NotificationTemplate() { }

    /// <summary>
    /// Створити шаблон сповіщення
    /// </summary>
    public static NotificationTemplate Create(
        NotificationEvent notificationEvent,
        NotificationType type,
        string language,
        string titleTemplate,
        string messageTemplate)
    {
        if (string.IsNullOrWhiteSpace(titleTemplate))
            throw new ArgumentException("Шаблон заголовка не може бути порожнім", nameof(titleTemplate));

        if (string.IsNullOrWhiteSpace(messageTemplate))
            throw new ArgumentException("Шаблон повідомлення не може бути порожнім", nameof(messageTemplate));

        return new NotificationTemplate
        {
            Event = notificationEvent,
            Type = type,
            Language = language,
            TitleTemplate = titleTemplate,
            MessageTemplate = messageTemplate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Оновити шаблон
    /// </summary>
    public void Update(string? titleTemplate = null, string? messageTemplate = null)
    {
        if (!string.IsNullOrWhiteSpace(titleTemplate))
            TitleTemplate = titleTemplate;

        if (!string.IsNullOrWhiteSpace(messageTemplate))
            MessageTemplate = messageTemplate;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Активувати/деактивувати шаблон
    /// </summary>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
