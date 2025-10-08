namespace StudentUnionBot.Application.Common.Interfaces;

/// <summary>
/// Інтерфейс для обмеження частоти запитів (Rate Limiting)
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Перевіряє чи дозволено виконати дію для користувача
    /// </summary>
    /// <param name="userId">ID користувача</param>
    /// <param name="action">Тип дії (наприклад, "CreateAppeal", "SendMessage")</param>
    /// <param name="cancellationToken">Токен скасування</param>
    /// <returns>True якщо дозволено, False якщо перевищено ліміт</returns>
    Task<bool> AllowAsync(long userId, string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Скидає лічильник для користувача та дії
    /// </summary>
    Task ResetAsync(long userId, string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримує залишкову кількість спроб
    /// </summary>
    Task<int> GetRemainingAttemptsAsync(long userId, string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримує час до скидання ліміту
    /// </summary>
    Task<TimeSpan?> GetTimeUntilResetAsync(long userId, string action, CancellationToken cancellationToken = default);
}
