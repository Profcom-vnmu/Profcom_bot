using Telegram.Bot;
using Telegram.Bot.Types;

namespace StudentUnionBot.Presentation.Bot.Handlers.Interfaces;

/// <summary>
/// Інтерфейс для обробки адміністративних розсилок
/// </summary>
public interface IAdminBroadcastHandler : IBaseHandler
{
    /// <summary>
    /// Показує меню розсилок
    /// </summary>
    Task HandleAdminBroadcastMenuCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Обробляє вибір аудиторії для розсилки
    /// </summary>
    Task HandleBroadcastAudienceCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Підтверджує розсилку
    /// </summary>
    Task HandleBroadcastConfirmCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Скасовує розсилку
    /// </summary>
    Task HandleBroadcastCancelCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Обробляє введення повідомлення розсилки
    /// </summary>
    Task HandleBroadcastMessageInputAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Обробляє введення списку email адрес для кастомної розсилки
    /// </summary>
    Task HandleBroadcastCustomEmailsInputAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);
}