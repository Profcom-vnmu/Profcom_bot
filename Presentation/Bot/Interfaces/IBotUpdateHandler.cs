using Telegram.Bot.Types;

namespace StudentUnionBot.Presentation.Bot.Interfaces;

/// <summary>
/// Інтерфейс для головного обробника оновлень бота
/// </summary>
public interface IBotUpdateHandler
{
    /// <summary>
    /// Обробляє оновлення від Telegram
    /// </summary>
    Task HandleUpdateAsync(Update update, CancellationToken cancellationToken);

    /// <summary>
    /// Обробляє помилки polling
    /// </summary>
    Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken);
}