using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Infrastructure.Services.Notifications;

/// <summary>
/// Провайдер для відправки Push сповіщень через Telegram
/// </summary>
public class TelegramPushNotificationProvider : IPushNotificationProvider
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<TelegramPushNotificationProvider> _logger;

    public TelegramPushNotificationProvider(
        ITelegramBotClient botClient,
        ILogger<TelegramPushNotificationProvider> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task<Result> SendPushAsync(long chatId, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Push сповіщення відправлено до {ChatId}", chatId);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка відправки push сповіщення до {ChatId}", chatId);
            return Result.Fail($"Не вдалося відправити push: {ex.Message}");
        }
    }

    public async Task<Result> SendPushWithActionsAsync(long chatId, string message, Dictionary<string, string> actions, CancellationToken cancellationToken = default)
    {
        try
        {
            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(
                actions.Select(a => new[]
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData(a.Key, a.Value)
                })
            );

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Push сповіщення з кнопками відправлено до {ChatId}", chatId);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка відправки push сповіщення з кнопками до {ChatId}", chatId);
            return Result.Fail($"Не вдалося відправити push: {ex.Message}");
        }
    }
}
