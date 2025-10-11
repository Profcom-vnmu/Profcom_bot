using Telegram.Bot;
using Telegram.Bot.Types;

namespace StudentUnionBot.Presentation.Bot.Handlers.Interfaces;

/// <summary>
/// Інтерфейс для обробки звернень користувачів
/// </summary>
public interface IAppealHandler : IBaseHandler
{
    /// <summary>
    /// Обробка вибору категорії звернення
    /// </summary>
    Task HandleAppealCategorySelectionAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Обробка перегляду звернення
    /// </summary>
    Task HandleAppealViewCallbackAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Обробка списку власних звернень
    /// </summary>
    Task HandleMyAppealsCallbackAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Обробка створення нового звернення
    /// </summary>
    Task HandleAppealCreateCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Відображає меню вибору фільтрів для звернень
    /// </summary>
    Task HandleAppealsFiltersMenuCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Відображає меню вибору статусу для фільтрації
    /// </summary>
    Task HandleAppealsFilterStatusMenuCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Відображає меню вибору категорії для фільтрації
    /// </summary>
    Task HandleAppealsFilterCategoryMenuCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Застосовує фільтр статусу звернення
    /// </summary>
    Task HandleAppealsFilterStatusCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Застосовує фільтр категорії звернення
    /// </summary>
    Task HandleAppealsFilterCategoryCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Очищає фільтр статусу звернень
    /// </summary>
    Task HandleAppealsClearStatusFilterCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Очищає фільтр категорії звернень
    /// </summary>
    Task HandleAppealsClearCategoryFilterCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Очищає всі фільтри звернень
    /// </summary>
    Task HandleAppealsClearAllFiltersCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
}