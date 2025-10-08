using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using StudentUnionBot.Presentation.Bot.Handlers;

namespace StudentUnionBot.Presentation.Bot.Services;

/// <summary>
/// Background сервіс для запуску Telegram бота
/// </summary>
public class BotBackgroundService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IBotUpdateHandler _updateHandler;
    private readonly ILogger<BotBackgroundService> _logger;

    public BotBackgroundService(
        ITelegramBotClient botClient,
        IBotUpdateHandler updateHandler,
        ILogger<BotBackgroundService> logger)
    {
        _botClient = botClient;
        _updateHandler = updateHandler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Запуск Telegram бота...");

        var me = await _botClient.GetMeAsync(stoppingToken);
        _logger.LogInformation("Бот запущено: @{Username} ({BotName})", me.Username, me.FirstName);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [] // Отримувати всі типи оновлень
        };

        await _botClient.ReceiveAsync(
            updateHandler: _updateHandler.HandleUpdateAsync,
            pollingErrorHandler: _updateHandler.HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Зупинка Telegram бота...");
        return base.StopAsync(cancellationToken);
    }
}
