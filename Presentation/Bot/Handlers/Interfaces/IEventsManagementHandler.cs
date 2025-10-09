using Telegram.Bot;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Presentation.Bot.Handlers.Interfaces;

/// <summary>
/// Інтерфейс для обробника управління подіями
/// </summary>
public interface IEventsManagementHandler : IBaseHandler
{
    /// <summary>
    /// Показати головне меню управління подіями
    /// </summary>
    Task ShowEventsManagementMenuAsync(
        ITelegramBotClient botClient,
        long chatId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Показати список подій
    /// </summary>
    Task ShowEventsListAsync(
        ITelegramBotClient botClient,
        long chatId,
        int page = 1,
        EventStatus? status = null,
        EventType? type = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Почати створення події
    /// </summary>
    Task StartCreateEventAsync(
        ITelegramBotClient botClient,
        long chatId,
        CancellationToken cancellationToken);
}