namespace StudentUnionBot.Domain.Enums;

/// <summary>
/// Тип партнера
/// </summary>
public enum PartnerType
{
    /// <summary>
    /// Кафе або ресторан
    /// </summary>
    Cafe = 1,
    
    /// <summary>
    /// Магазин
    /// </summary>
    Shop = 2,
    
    /// <summary>
    /// Спортзал або фітнес-центр
    /// </summary>
    Gym = 3,
    
    /// <summary>
    /// Освітній центр (курси, репетиторство)
    /// </summary>
    Education = 4,
    
    /// <summary>
    /// Розважальний заклад
    /// </summary>
    Entertainment = 5,
    
    /// <summary>
    /// Краса та здоров'я (салони, клініки)
    /// </summary>
    BeautyAndHealth = 6,
    
    /// <summary>
    /// Транспорт та подорожі
    /// </summary>
    Transport = 7,
    
    /// <summary>
    /// Онлайн-сервіс
    /// </summary>
    OnlineService = 8,
    
    /// <summary>
    /// Книгарня
    /// </summary>
    Bookstore = 9,
    
    /// <summary>
    /// Копіювальний центр
    /// </summary>
    PrintingService = 10,
    
    /// <summary>
    /// Інший тип партнера
    /// </summary>
    Other = 99
}

/// <summary>
/// Методи розширення для PartnerType
/// </summary>
public static class PartnerTypeExtensions
{
    public static string GetDisplayName(this PartnerType type)
    {
        return type switch
        {
            PartnerType.Cafe => "Кафе/Ресторан",
            PartnerType.Shop => "Магазин",
            PartnerType.Gym => "Спортзал/Фітнес",
            PartnerType.Education => "Освіта",
            PartnerType.Entertainment => "Розваги",
            PartnerType.BeautyAndHealth => "Краса та здоров'я",
            PartnerType.Transport => "Транспорт",
            PartnerType.OnlineService => "Онлайн-сервіс",
            PartnerType.Bookstore => "Книгарня",
            PartnerType.PrintingService => "Копіювання та друк",
            PartnerType.Other => "Інше",
            _ => "Невідомо"
        };
    }
    
    public static string GetEmoji(this PartnerType type)
    {
        return type switch
        {
            PartnerType.Cafe => "☕",
            PartnerType.Shop => "🛍️",
            PartnerType.Gym => "💪",
            PartnerType.Education => "📚",
            PartnerType.Entertainment => "🎮",
            PartnerType.BeautyAndHealth => "💅",
            PartnerType.Transport => "🚗",
            PartnerType.OnlineService => "💻",
            PartnerType.Bookstore => "📖",
            PartnerType.PrintingService => "🖨️",
            PartnerType.Other => "🤝",
            _ => "❓"
        };
    }
}
