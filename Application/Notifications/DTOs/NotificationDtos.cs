using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Notifications.DTOs;

/// <summary>
/// DTO для сповіщення
/// </summary>
public class NotificationDto
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public string Event { get; set; } = string.Empty;
    public string EventDisplayName { get; set; } = string.Empty;
    public string EventIcon { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string PriorityIcon { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? RelatedAppealId { get; set; }
    public int? RelatedNewsId { get; set; }
    public int? RelatedEventId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsRead { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}

/// <summary>
/// DTO для налаштувань сповіщень
/// </summary>
public class NotificationPreferenceDto
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public string Event { get; set; } = string.Empty;
    public string EventDisplayName { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public bool InAppEnabled { get; set; }
}

/// <summary>
/// DTO для списку сповіщень
/// </summary>
public class NotificationListDto
{
    public List<NotificationDto> Notifications { get; set; } = new();
    public int UnreadCount { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// DTO для статистики сповіщень
/// </summary>
public class NotificationStatsDto
{
    public int TotalNotifications { get; set; }
    public int SentNotifications { get; set; }
    public int DeliveredNotifications { get; set; }
    public int FailedNotifications { get; set; }
    public int UnreadNotifications { get; set; }
    public Dictionary<string, int> NotificationsByEvent { get; set; } = new();
    public Dictionary<string, int> NotificationsByType { get; set; } = new();
    public Dictionary<string, int> NotificationsByPriority { get; set; } = new();
    public double DeliveryRate { get; set; }
    public double ReadRate { get; set; }
}

/// <summary>
/// DTO для шаблону сповіщення
/// </summary>
public class NotificationTemplateDto
{
    public int Id { get; set; }
    public string Event { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string TitleTemplate { get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
