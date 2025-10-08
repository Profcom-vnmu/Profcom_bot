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
    Events = 6         // Заходи
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
}
