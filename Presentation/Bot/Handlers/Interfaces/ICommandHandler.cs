using Telegram.Bot;
using Telegram.Bot.Types;

namespace StudentUnionBot.Presentation.Bot.Handlers.Interfaces;

/// <summary>
/// Інтерфейс для обробки команд бота
/// </summary>
public interface ICommandHandler : IBaseHandler
{
    /// <summary>
    /// Обробка команди /start
    /// </summary>
    Task HandleStartCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);
    
    /// <summary>
    /// Обробка команди /help
    /// </summary>
    Task HandleHelpCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);
    
    /// <summary>
    /// Обробка команди /appeal
    /// </summary>
    Task HandleAppealCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);
    
    /// <summary>
    /// Обробка команди /contacts
    /// </summary>
    Task HandleContactsCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Обробляє будь-яку команду
    /// </summary>
    Task HandleCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);
}