using Telegram.Bot.Types.ReplyMarkups;

namespace StudentUnionBot.Presentation.Bot.Helpers;

/// <summary>
/// Хелпер для створення пагінації в Telegram ботах
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// Створює кнопки навігації для пагінації
    /// </summary>
    /// <param name="currentPage">Поточна сторінка (починається з 1)</param>
    /// <param name="totalPages">Загальна кількість сторінок</param>
    /// <param name="callbackPrefix">Префікс для callback data (напр. "news_page")</param>
    /// <param name="showPageInfo">Чи показувати інформацію про поточну сторінку</param>
    /// <returns>Список кнопок навігації</returns>
    public static List<InlineKeyboardButton> GetNavigationButtons(
        int currentPage, 
        int totalPages, 
        string callbackPrefix,
        bool showPageInfo = true)
    {
        var buttons = new List<InlineKeyboardButton>();

        // Кнопка "Попередня"
        if (currentPage > 1)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData(
                "◀️ Назад",
                $"{callbackPrefix}_{currentPage - 1}"));
        }

        // Інформація про сторінку
        if (showPageInfo)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData(
                $"{currentPage}/{totalPages}",
                "pagination_info"));
        }

        // Кнопка "Наступна"
        if (currentPage < totalPages)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData(
                "Вперед ▶️",
                $"{callbackPrefix}_{currentPage + 1}"));
        }

        return buttons;
    }

    /// <summary>
    /// Створює повну клавіатуру з пагінацією
    /// </summary>
    /// <param name="itemButtons">Кнопки для елементів списку</param>
    /// <param name="currentPage">Поточна сторінка</param>
    /// <param name="totalPages">Загальна кількість сторінок</param>
    /// <param name="callbackPrefix">Префікс для callback пагінації</param>
    /// <param name="backButtonText">Текст кнопки "Назад" (опціонально)</param>
    /// <param name="backButtonCallback">Callback для кнопки "Назад" (опціонально)</param>
    /// <returns>Клавіатура з елементами та пагінацією</returns>
    public static InlineKeyboardMarkup CreatePaginatedKeyboard(
        List<List<InlineKeyboardButton>> itemButtons,
        int currentPage,
        int totalPages,
        string callbackPrefix,
        string? backButtonText = null,
        string? backButtonCallback = null)
    {
        var allButtons = new List<List<InlineKeyboardButton>>(itemButtons);

        // Додаємо навігацію, якщо є більше 1 сторінки
        if (totalPages > 1)
        {
            var navButtons = GetNavigationButtons(currentPage, totalPages, callbackPrefix);
            allButtons.Add(navButtons);
        }

        // Додаємо кнопку "Назад", якщо вказана
        if (!string.IsNullOrWhiteSpace(backButtonText) && !string.IsNullOrWhiteSpace(backButtonCallback))
        {
            allButtons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(backButtonText, backButtonCallback)
            });
        }

        return new InlineKeyboardMarkup(allButtons);
    }

    /// <summary>
    /// Розбиває список кнопок на сторінки
    /// </summary>
    /// <param name="items">Список елементів</param>
    /// <param name="buttonsPerRow">Кількість кнопок в рядку</param>
    /// <param name="itemButtonFactory">Функція для створення кнопки з елемента</param>
    /// <returns>Список рядків кнопок</returns>
    public static List<List<InlineKeyboardButton>> CreateItemButtons<T>(
        IEnumerable<T> items,
        int buttonsPerRow,
        Func<T, InlineKeyboardButton> itemButtonFactory)
    {
        var buttons = new List<List<InlineKeyboardButton>>();
        var currentRow = new List<InlineKeyboardButton>();

        foreach (var item in items)
        {
            currentRow.Add(itemButtonFactory(item));

            if (currentRow.Count >= buttonsPerRow)
            {
                buttons.Add(new List<InlineKeyboardButton>(currentRow));
                currentRow.Clear();
            }
        }

        // Додаємо останній неповний рядок
        if (currentRow.Any())
        {
            buttons.Add(currentRow);
        }

        return buttons;
    }

    /// <summary>
    /// Форматує текст заголовку списку з інформацією про пагінацію
    /// </summary>
    /// <param name="title">Заголовок списку</param>
    /// <param name="totalCount">Загальна кількість елементів</param>
    /// <param name="currentPage">Поточна сторінка</param>
    /// <param name="totalPages">Загальна кількість сторінок</param>
    /// <param name="pageSize">Розмір сторінки</param>
    /// <returns>Форматований заголовок</returns>
    public static string FormatListHeader(
        string title,
        int totalCount,
        int currentPage,
        int totalPages,
        int pageSize)
    {
        var from = (currentPage - 1) * pageSize + 1;
        var to = Math.Min(currentPage * pageSize, totalCount);

        return $"📋 <b>{title}</b>\n\n" +
               $"Показано {from}-{to} з {totalCount}\n" +
               $"Сторінка {currentPage} з {totalPages}\n\n";
    }

    /// <summary>
    /// Обчислює загальну кількість сторінок
    /// </summary>
    /// <param name="totalItems">Загальна кількість елементів</param>
    /// <param name="pageSize">Розмір сторінки</param>
    /// <returns>Кількість сторінок</returns>
    public static int CalculateTotalPages(int totalItems, int pageSize)
    {
        if (totalItems <= 0 || pageSize <= 0)
            return 0;

        return (int)Math.Ceiling((double)totalItems / pageSize);
    }

    /// <summary>
    /// Валідує номер сторінки
    /// </summary>
    /// <param name="pageNumber">Номер сторінки</param>
    /// <param name="totalPages">Загальна кількість сторінок</param>
    /// <returns>Валідний номер сторінки (обмежений діапазоном 1..totalPages)</returns>
    public static int ValidatePageNumber(int pageNumber, int totalPages)
    {
        if (pageNumber < 1)
            return 1;
        if (pageNumber > totalPages)
            return Math.Max(totalPages, 1);
        return pageNumber;
    }

    /// <summary>
    /// Парсить номер сторінки з callback data
    /// </summary>
    /// <param name="callbackData">Callback data (напр. "news_page_2")</param>
    /// <param name="prefix">Префікс callback (напр. "news_page")</param>
    /// <returns>Номер сторінки або null якщо не вдалося розпарсити</returns>
    public static int? ParsePageNumber(string callbackData, string prefix)
    {
        if (string.IsNullOrWhiteSpace(callbackData) || string.IsNullOrWhiteSpace(prefix))
            return null;

        var parts = callbackData.Split('_');
        if (parts.Length < 2)
            return null;

        var pageStr = parts[^1]; // Останній елемент
        if (int.TryParse(pageStr, out var pageNumber))
            return pageNumber;

        return null;
    }
}
