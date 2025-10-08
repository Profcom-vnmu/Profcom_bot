namespace StudentUnionBot.Domain.Enums;

/// <summary>
/// Type/category of event
/// </summary>
public enum EventType
{
    /// <summary>
    /// Cultural event (concerts, exhibitions, etc.)
    /// </summary>
    Cultural = 1,
    
    /// <summary>
    /// Educational event (seminars, workshops, lectures)
    /// </summary>
    Educational = 2,
    
    /// <summary>
    /// Sports event
    /// </summary>
    Sports = 3,
    
    /// <summary>
    /// Social event (parties, gatherings)
    /// </summary>
    Social = 4,
    
    /// <summary>
    /// Volunteer/charity event
    /// </summary>
    Volunteer = 5,
    
    /// <summary>
    /// Career/professional development event
    /// </summary>
    Career = 6,
    
    /// <summary>
    /// Meeting or conference
    /// </summary>
    Meeting = 7,
    
    /// <summary>
    /// Other type of event
    /// </summary>
    Other = 99
}

/// <summary>
/// Current status of an event
/// </summary>
public enum EventStatus
{
    /// <summary>
    /// Event is planned but hasn't started yet
    /// </summary>
    Planned = 1,
    
    /// <summary>
    /// Event is currently in progress
    /// </summary>
    InProgress = 2,
    
    /// <summary>
    /// Event has been completed
    /// </summary>
    Completed = 3,
    
    /// <summary>
    /// Event was cancelled
    /// </summary>
    Cancelled = 4,
    
    /// <summary>
    /// Event is postponed to a later date
    /// </summary>
    Postponed = 5
}

/// <summary>
/// Extension methods for Event enums
/// </summary>
public static class EventEnumExtensions
{
    public static string GetDisplayName(this EventType type)
    {
        return type switch
        {
            EventType.Cultural => "Культурний захід",
            EventType.Educational => "Освітній захід",
            EventType.Sports => "Спортивний захід",
            EventType.Social => "Соціальний захід",
            EventType.Volunteer => "Волонтерство",
            EventType.Career => "Кар'єра та розвиток",
            EventType.Meeting => "Зустріч/Конференція",
            EventType.Other => "Інше",
            _ => "Невідомо"
        };
    }
    
    public static string GetDisplayName(this EventStatus status)
    {
        return status switch
        {
            EventStatus.Planned => "Заплановано",
            EventStatus.InProgress => "В процесі",
            EventStatus.Completed => "Завершено",
            EventStatus.Cancelled => "Скасовано",
            EventStatus.Postponed => "Відкладено",
            _ => "Невідомо"
        };
    }
    
    public static string GetEmoji(this EventType type)
    {
        return type switch
        {
            EventType.Cultural => "🎭",
            EventType.Educational => "📚",
            EventType.Sports => "⚽",
            EventType.Social => "🎉",
            EventType.Volunteer => "🤝",
            EventType.Career => "💼",
            EventType.Meeting => "👥",
            EventType.Other => "📌",
            _ => "❓"
        };
    }
    
    public static string GetEmoji(this EventStatus status)
    {
        return status switch
        {
            EventStatus.Planned => "📅",
            EventStatus.InProgress => "▶️",
            EventStatus.Completed => "✅",
            EventStatus.Cancelled => "❌",
            EventStatus.Postponed => "⏸️",
            _ => "❓"
        };
    }
}
