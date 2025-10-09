namespace StudentUnionBot.Domain.Enums;

/// <summary>
/// Категорія новини
/// </summary>
public enum NewsCategory
{
    Important = 1,      // Важливо
    Education = 2,      // Освітні
    Cultural = 3,       // Культурні
    Sport = 4,          // Спортивні
    Administrative = 5, // Адміністративні
    Events = 6,         // Заходи
    Urgent = 7,         // Терміново
    Event = 8          // Подія (синонім Events для зворотної сумісності)
}

/// <summary>
/// Пріоритет новини
/// </summary>
public enum NewsPriority
{
    Normal = 1,
    High = 2,
    Urgent = 3
}

/// <summary>
/// Статус новини
/// </summary>
public enum NewsStatus
{
    Draft = 1,      // Чернетка
    Published = 2,  // Опублікована
    Archived = 3    // Архівована
}

/// <summary>
/// Методи розширення для News enums
/// </summary>
public static class NewsEnumExtensions
{
    public static string GetDisplayName(this NewsCategory category)
    {
        return category switch
        {
            NewsCategory.Important => "Важливо",
            NewsCategory.Education => "Освітні новини",
            NewsCategory.Cultural => "Культурні події",
            NewsCategory.Sport => "Спорт",
            NewsCategory.Administrative => "Адміністративні",
            NewsCategory.Events => "Заходи",
            NewsCategory.Urgent => "Терміново",
            NewsCategory.Event => "Подія",
            _ => "Інше"
        };
    }

    public static string GetEmoji(this NewsCategory category)
    {
        return category switch
        {
            NewsCategory.Important => "⚠️",
            NewsCategory.Education => "📚",
            NewsCategory.Cultural => "🎭",
            NewsCategory.Sport => "⚽",
            NewsCategory.Administrative => "📋",
            NewsCategory.Events => "🎉",
            NewsCategory.Urgent => "🚨",
            NewsCategory.Event => "📅",
            _ => "📰"
        };
    }

    public static string GetDisplayName(this NewsPriority priority)
    {
        return priority switch
        {
            NewsPriority.Normal => "Нормальний",
            NewsPriority.High => "Високий",
            NewsPriority.Urgent => "Терміновий",
            _ => "Невідомо"
        };
    }

    public static string GetDisplayName(this NewsStatus status)
    {
        return status switch
        {
            NewsStatus.Draft => "Чернетка",
            NewsStatus.Published => "Опублікована",
            NewsStatus.Archived => "Архівована",
            _ => "Невідомо"
        };
    }

    public static string GetEmoji(this NewsStatus status)
    {
        return status switch
        {
            NewsStatus.Draft => "📝",
            NewsStatus.Published => "✅",
            NewsStatus.Archived => "🗃️",
            _ => "❓"
        };
    }
}
