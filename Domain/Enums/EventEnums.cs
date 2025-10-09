namespace StudentUnionBot.Domain.Enums;

/// <summary>
/// Category of event for better organization
/// </summary>
public enum EventCategory
{
    /// <summary>
    /// Academic events (lectures, seminars, conferences)
    /// </summary>
    Academic = 1,
    
    /// <summary>
    /// Entertainment events (concerts, shows, parties)
    /// </summary>
    Entertainment = 2,
    
    /// <summary>
    /// Sports and fitness events
    /// </summary>
    Sports = 3,
    
    /// <summary>
    /// Community service and volunteer events
    /// </summary>
    Community = 4,
    
    /// <summary>
    /// Professional and career development events
    /// </summary>
    Professional = 5,
    
    /// <summary>
    /// Social networking and informal gatherings
    /// </summary>
    Social = 6,
    
    /// <summary>
    /// Other category
    /// </summary>
    Other = 99
}

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
    /// Event is in draft mode (not published yet)
    /// </summary>
    Draft = 0,
    
    /// <summary>
    /// Event is published and visible to users
    /// </summary>
    Published = 1,
    
    /// <summary>
    /// Event is planned but hasn't started yet
    /// </summary>
    Planned = 2,
    
    /// <summary>
    /// Event is currently in progress
    /// </summary>
    InProgress = 3,
    
    /// <summary>
    /// Event has been completed
    /// </summary>
    Completed = 4,
    
    /// <summary>
    /// Event was cancelled
    /// </summary>
    Cancelled = 5,
    
    /// <summary>
    /// Event is postponed to a later date
    /// </summary>
    Postponed = 6
}

/// <summary>
/// Extension methods for Event enums
/// </summary>
public static class EventEnumExtensions
{
    public static string GetDisplayName(this EventCategory category)
    {
        return category switch
        {
            EventCategory.Academic => "Академічні заходи",
            EventCategory.Entertainment => "Розваги",
            EventCategory.Sports => "Спорт та фітнес",
            EventCategory.Community => "Громадська діяльність",
            EventCategory.Professional => "Професійний розвиток",
            EventCategory.Social => "Соціальні заходи",
            EventCategory.Other => "Інше",
            _ => "Невідомо"
        };
    }

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
            EventStatus.Draft => "Чернетка",
            EventStatus.Published => "Опубліковано",
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
            EventStatus.Draft => "📝",
            EventStatus.Published => "📢",
            EventStatus.Planned => "📅",
            EventStatus.InProgress => "▶️",
            EventStatus.Completed => "✅",
            EventStatus.Cancelled => "❌",
            EventStatus.Postponed => "⏸️",
            _ => "❓"
        };
    }
    
    public static string GetEmoji(this EventCategory category)
    {
        return category switch
        {
            EventCategory.Academic => "🎓",
            EventCategory.Entertainment => "🎉",
            EventCategory.Sports => "⚽",
            EventCategory.Community => "🤝",
            EventCategory.Professional => "💼",
            EventCategory.Social => "👥",
            EventCategory.Other => "📌",
            _ => "❓"
        };
    }
}
