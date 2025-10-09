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
}