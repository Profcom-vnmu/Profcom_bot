namespace StudentUnionBot.Infrastructure.RateLimiting;

/// <summary>
/// Налаштування rate limiting
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Максимальна кількість запитів за вікно
    /// </summary>
    public int PermitLimit { get; set; } = 10;

    /// <summary>
    /// Розмір вікна в секундах
    /// </summary>
    public int WindowInSeconds { get; set; } = 60;

    /// <summary>
    /// Максимальна кількість запитів в черзі
    /// </summary>
    public int QueueLimit { get; set; } = 2;

    /// <summary>
    /// Rate limit для адміністраторів
    /// </summary>
    public int AdminPermitLimit { get; set; } = 50;

    /// <summary>
    /// Чи увімкнено rate limiting
    /// </summary>
    public bool Enabled { get; set; } = true;
}
