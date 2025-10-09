using StudentUnionBot.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace StudentUnionBot.Presentation.Bot.Handlers.Interfaces;

/// <summary>
/// Інтерфейс для обробника управління новинами
/// </summary>
public interface INewsManagementHandler : IBaseHandler
{
    /// <summary>
    /// Показати головне меню управління новинами
    /// </summary>
    Task ShowNewsManagementMenuAsync(
        ITelegramBotClient botClient,
        long chatId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Показати список новин
    /// </summary>
    Task ShowNewsListAsync(
        ITelegramBotClient botClient,
        long chatId,
        int page = 1,
        NewsStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Почати створення новини
    /// </summary>
    Task StartCreateNewsAsync(
        ITelegramBotClient botClient,
        long chatId,
        CancellationToken cancellationToken);
}