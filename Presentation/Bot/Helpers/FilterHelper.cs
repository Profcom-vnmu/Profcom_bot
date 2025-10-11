using Telegram.Bot.Types.ReplyMarkups;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Presentation.Bot.Helpers;

/// <summary>
/// Допоміжний клас для створення фільтрів у списках
/// </summary>
public static class FilterHelper
{
    /// <summary>
    /// Створює inline keyboard з фільтрами категорій новин
    /// </summary>
    /// <param name="selectedCategory">Обрана категорія (якщо є)</param>
    /// <param name="backCallbackData">Callback для кнопки "Назад"</param>
    /// <returns>InlineKeyboardMarkup з категоріями</returns>
    public static InlineKeyboardMarkup CreateNewsCategoryFilterKeyboard(
        NewsCategory? selectedCategory = null,
        string backCallbackData = "main_menu")
    {
        var buttons = new List<List<InlineKeyboardButton>>();

        // Всі категорії
        var categories = Enum.GetValues<NewsCategory>()
            .Where(c => c != NewsCategory.Event) // Пропускаємо Event (дублікат Events)
            .ToList();

        // Кнопка "Всі новини"
        var allNewsText = selectedCategory == null 
            ? "✅ Всі новини" 
            : "📰 Всі новини";
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(allNewsText, "news_filter_clear")
        });

        // Кнопки категорій (по 2 в ряд)
        for (int i = 0; i < categories.Count; i += 2)
        {
            var row = new List<InlineKeyboardButton>();
            
            var category1 = categories[i];
            var isSelected1 = selectedCategory == category1;
            var text1 = $"{(isSelected1 ? "✅" : category1.GetEmoji())} {category1.GetDisplayName()}";
            row.Add(InlineKeyboardButton.WithCallbackData(text1, $"news_filter_category_{(int)category1}"));

            if (i + 1 < categories.Count)
            {
                var category2 = categories[i + 1];
                var isSelected2 = selectedCategory == category2;
                var text2 = $"{(isSelected2 ? "✅" : category2.GetEmoji())} {category2.GetDisplayName()}";
                row.Add(InlineKeyboardButton.WithCallbackData(text2, $"news_filter_category_{(int)category2}"));
            }

            buttons.Add(row);
        }

        // Кнопка "Назад"
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("🔙 Назад", backCallbackData)
        });

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Створює inline keyboard з фільтрами типів подій
    /// </summary>
    /// <param name="selectedType">Обраний тип (якщо є)</param>
    /// <param name="backCallbackData">Callback для кнопки "Назад"</param>
    /// <returns>InlineKeyboardMarkup з типами подій</returns>
    public static InlineKeyboardMarkup CreateEventTypeFilterKeyboard(
        EventType? selectedType = null,
        string backCallbackData = "main_menu")
    {
        var buttons = new List<List<InlineKeyboardButton>>();

        // Всі типи подій
        var eventTypes = Enum.GetValues<EventType>().ToList();

        // Кнопка "Всі події"
        var allEventsText = selectedType == null 
            ? "✅ Всі події" 
            : "📅 Всі події";
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(allEventsText, "events_filter_clear")
        });

        // Кнопки типів (по 2 в ряд)
        for (int i = 0; i < eventTypes.Count; i += 2)
        {
            var row = new List<InlineKeyboardButton>();
            
            var type1 = eventTypes[i];
            var isSelected1 = selectedType == type1;
            var text1 = $"{(isSelected1 ? "✅" : type1.GetEmoji())} {type1.GetDisplayName()}";
            row.Add(InlineKeyboardButton.WithCallbackData(text1, $"events_filter_type_{(int)type1}"));

            if (i + 1 < eventTypes.Count)
            {
                var type2 = eventTypes[i + 1];
                var isSelected2 = selectedType == type2;
                var text2 = $"{(isSelected2 ? "✅" : type2.GetEmoji())} {type2.GetDisplayName()}";
                row.Add(InlineKeyboardButton.WithCallbackData(text2, $"events_filter_type_{(int)type2}"));
            }

            buttons.Add(row);
        }

        // Кнопка "Назад"
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("🔙 Назад", backCallbackData)
        });

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Створює inline keyboard з фільтрами статусів звернень
    /// </summary>
    /// <param name="selectedStatus">Обраний статус (якщо є)</param>
    /// <param name="backCallbackData">Callback для кнопки "Назад"</param>
    /// <returns>InlineKeyboardMarkup зі статусами</returns>
    public static InlineKeyboardMarkup CreateAppealStatusFilterKeyboard(
        AppealStatus? selectedStatus = null,
        string backCallbackData = "main_menu")
    {
        var buttons = new List<List<InlineKeyboardButton>>();

        // Всі статуси
        var statuses = Enum.GetValues<AppealStatus>().ToList();

        // Кнопка "Всі звернення"
        var allAppealsText = selectedStatus == null 
            ? "✅ Всі звернення" 
            : "📋 Всі звернення";
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(allAppealsText, "appeals_filter_clear")
        });

        // Кнопки статусів (по 2 в ряд)
        for (int i = 0; i < statuses.Count; i += 2)
        {
            var row = new List<InlineKeyboardButton>();
            
            var status1 = statuses[i];
            var isSelected1 = selectedStatus == status1;
            var text1 = $"{(isSelected1 ? "✅" : status1.GetEmoji())} {status1.GetDisplayName()}";
            row.Add(InlineKeyboardButton.WithCallbackData(text1, $"appeals_filter_status_{(int)status1}"));

            if (i + 1 < statuses.Count)
            {
                var status2 = statuses[i + 1];
                var isSelected2 = selectedStatus == status2;
                var text2 = $"{(isSelected2 ? "✅" : status2.GetEmoji())} {status2.GetDisplayName()}";
                row.Add(InlineKeyboardButton.WithCallbackData(text2, $"appeals_filter_status_{(int)status2}"));
            }

            buttons.Add(row);
        }

        // Кнопка "Назад"
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("🔙 Назад", backCallbackData)
        });

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Створює inline keyboard з фільтрами категорій звернень
    /// </summary>
    /// <param name="selectedCategory">Обрана категорія (якщо є)</param>
    /// <param name="backCallbackData">Callback для кнопки "Назад"</param>
    /// <returns>InlineKeyboardMarkup з категоріями</returns>
    public static InlineKeyboardMarkup CreateAppealCategoryFilterKeyboard(
        AppealCategory? selectedCategory = null,
        string backCallbackData = "main_menu")
    {
        var buttons = new List<List<InlineKeyboardButton>>();

        // Всі категорії
        var categories = Enum.GetValues<AppealCategory>().ToList();

        // Кнопка "Всі категорії"
        var allCategoriesText = selectedCategory == null 
            ? "✅ Всі категорії" 
            : "📂 Всі категорії";
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(allCategoriesText, "appeals_filter_category_clear")
        });

        // Кнопки категорій (по 1 в ряд через довгі назви)
        foreach (var category in categories)
        {
            var isSelected = selectedCategory == category;
            var text = $"{(isSelected ? "✅" : category.GetEmoji())} {category.GetDisplayName()}";
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(text, $"appeals_filter_category_{(int)category}")
            });
        }

        // Кнопка "Назад"
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("🔙 Назад", backCallbackData)
        });

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Додає кнопку "Фільтри" до існуючого keyboard
    /// </summary>
    /// <param name="buttons">Список рядків кнопок</param>
    /// <param name="filterCallbackData">Callback для кнопки фільтрів</param>
    /// <param name="hasActiveFilters">Чи є активні фільтри</param>
    public static void AddFilterButton(
        List<List<InlineKeyboardButton>> buttons,
        string filterCallbackData,
        bool hasActiveFilters = false)
    {
        var filterText = hasActiveFilters ? "🔍 Фільтри ✅" : "🔍 Фільтри";
        buttons.Insert(0, new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(filterText, filterCallbackData)
        });
    }

    /// <summary>
    /// Парсить ID категорії новин з callback data
    /// </summary>
    /// <param name="callbackData">Callback data у форматі "news_filter_category_{id}"</param>
    /// <returns>NewsCategory або null</returns>
    public static NewsCategory? ParseNewsCategoryFromCallback(string callbackData)
    {
        if (string.IsNullOrEmpty(callbackData) || !callbackData.StartsWith("news_filter_category_"))
            return null;

        var idStr = callbackData.Replace("news_filter_category_", "");
        if (int.TryParse(idStr, out int categoryId) && Enum.IsDefined(typeof(NewsCategory), categoryId))
        {
            return (NewsCategory)categoryId;
        }

        return null;
    }

    /// <summary>
    /// Парсить ID типу події з callback data
    /// </summary>
    /// <param name="callbackData">Callback data у форматі "events_filter_type_{id}"</param>
    /// <returns>EventType або null</returns>
    public static EventType? ParseEventTypeFromCallback(string callbackData)
    {
        if (string.IsNullOrEmpty(callbackData) || !callbackData.StartsWith("events_filter_type_"))
            return null;

        var idStr = callbackData.Replace("events_filter_type_", "");
        if (int.TryParse(idStr, out int typeId) && Enum.IsDefined(typeof(EventType), typeId))
        {
            return (EventType)typeId;
        }

        return null;
    }

    /// <summary>
    /// Парсить ID статусу звернення з callback data
    /// </summary>
    /// <param name="callbackData">Callback data у форматі "appeals_filter_status_{id}"</param>
    /// <returns>AppealStatus або null</returns>
    public static AppealStatus? ParseAppealStatusFromCallback(string callbackData)
    {
        if (string.IsNullOrEmpty(callbackData) || !callbackData.StartsWith("appeals_filter_status_"))
            return null;

        var idStr = callbackData.Replace("appeals_filter_status_", "");
        if (int.TryParse(idStr, out int statusId) && Enum.IsDefined(typeof(AppealStatus), statusId))
        {
            return (AppealStatus)statusId;
        }

        return null;
    }

    /// <summary>
    /// Парсить ID категорії звернення з callback data
    /// </summary>
    /// <param name="callbackData">Callback data у форматі "appeals_filter_category_{id}"</param>
    /// <returns>AppealCategory або null</returns>
    public static AppealCategory? ParseAppealCategoryFromCallback(string callbackData)
    {
        if (string.IsNullOrEmpty(callbackData) || !callbackData.StartsWith("appeals_filter_category_"))
            return null;

        var idStr = callbackData.Replace("appeals_filter_category_", "");
        if (int.TryParse(idStr, out int categoryId) && Enum.IsDefined(typeof(AppealCategory), categoryId))
        {
            return (AppealCategory)categoryId;
        }

        return null;
    }
}
