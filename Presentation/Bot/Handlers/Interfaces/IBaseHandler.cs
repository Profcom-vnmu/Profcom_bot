using Telegram.Bot;
using Telegram.Bot.Types;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Presentation.Bot.Handlers.Interfaces;

/// <summary>
/// Базовий інтерфейс для всіх Telegram bot handlers
/// </summary>
public interface IBaseHandler
{
    /// <summary>
    /// Обробка текстових повідомлень з урахуванням стану користувача
    /// </summary>
    Task HandleTextMessageAsync(ITelegramBotClient botClient, Message message, UserConversationState state, CancellationToken cancellationToken);
}