using Telegram.Bot;
using Telegram.Bot.Types;

namespace StudentUnionBot.Presentation.Bot.Handlers.Interfaces;

/// <summary>
/// Інтерфейс для обробки контентних callback'ів (новини, події, партнери, контакти)
/// </summary>
public interface IContentHandler : IBaseHandler
{
    /// <summary>
    /// Обробка callback'у для списку новин
    /// </summary>
    Task HandleNewsListCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Обробка callback'у для списку подій
    /// </summary>
    Task HandleEventsListCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Обробка callback'у для списку партнерів
    /// </summary>
    Task HandlePartnersListCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Обробка callback'у для списку контактів
    /// </summary>
    Task HandleContactsListCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Обробка callback'у для деталей події
    /// </summary>
    Task HandleEventDetailsCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Обробка callback'у для реєстрації на подію
    /// </summary>
    Task HandleEventRegisterCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Обробка callback'у для скасування реєстрації на подію
    /// </summary>
    Task HandleEventUnregisterCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
}