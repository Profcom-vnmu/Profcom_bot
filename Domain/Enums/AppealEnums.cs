namespace StudentUnionBot.Domain.Enums;

/// <summary>
/// Категорія звернення
/// </summary>
public enum AppealCategory
{
    Scholarship = 1,    // Стипендія
    Dormitory = 2,      // Гуртожиток
    Events = 3,         // Заходи
    Proposal = 4,       // Пропозиція
    Complaint = 5,      // Скарга
    Other = 6          // Інше
}

/// <summary>
/// Статус звернення
/// </summary>
public enum AppealStatus
{
    New = 1,                // Нове
    InProgress = 2,         // В роботі
    WaitingForStudent = 3,  // Очікує студента
    WaitingForAdmin = 4,    // Очікує адміна
    Escalated = 5,          // Ескальовано
    Resolved = 6,           // Вирішено
    Closed = 7             // Закрито
}

/// <summary>
/// Пріоритет звернення
/// </summary>
public enum AppealPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

/// <summary>
/// Методи розширення для Appeal enums
/// </summary>
public static class AppealEnumExtensions
{
    public static string GetDisplayName(this AppealCategory category)
    {
        return category switch
        {
            AppealCategory.Scholarship => "Стипендія",
            AppealCategory.Dormitory => "Гуртожиток",
            AppealCategory.Events => "Заходи",
            AppealCategory.Proposal => "Пропозиція",
            AppealCategory.Complaint => "Скарга",
            AppealCategory.Other => "Інше",
            _ => "Невідомо"
        };
    }

    public static string GetDisplayName(this AppealStatus status)
    {
        return status switch
        {
            AppealStatus.New => "Нове",
            AppealStatus.InProgress => "В роботі",
            AppealStatus.WaitingForStudent => "Очікує відповіді студента",
            AppealStatus.WaitingForAdmin => "Очікує відповіді адміна",
            AppealStatus.Escalated => "Ескальовано",
            AppealStatus.Resolved => "Вирішено",
            AppealStatus.Closed => "Закрито",
            _ => "Невідомо"
        };
    }

    public static string GetDisplayName(this AppealPriority priority)
    {
        return priority switch
        {
            AppealPriority.Low => "Низький",
            AppealPriority.Normal => "Нормальний",
            AppealPriority.High => "Високий",
            AppealPriority.Urgent => "Терміновий",
            _ => "Невідомо"
        };
    }

    public static string GetEmoji(this AppealCategory category)
    {
        return category switch
        {
            AppealCategory.Scholarship => "💰",
            AppealCategory.Dormitory => "🏠",
            AppealCategory.Events => "🎉",
            AppealCategory.Proposal => "💡",
            AppealCategory.Complaint => "⚠️",
            AppealCategory.Other => "📝",
            _ => "❓"
        };
    }

    public static string GetEmoji(this AppealStatus status)
    {
        return status switch
        {
            AppealStatus.New => "🆕",
            AppealStatus.InProgress => "⏳",
            AppealStatus.WaitingForStudent => "⌛",
            AppealStatus.WaitingForAdmin => "⏰",
            AppealStatus.Escalated => "🔺",
            AppealStatus.Resolved => "✅",
            AppealStatus.Closed => "🔒",
            _ => "❓"
        };
    }

    public static string GetEmoji(this AppealPriority priority)
    {
        return priority switch
        {
            AppealPriority.Low => "🟢",
            AppealPriority.Normal => "🟡",
            AppealPriority.High => "🟠",
            AppealPriority.Urgent => "🔴",
            _ => "⚪"
        };
    }
}
