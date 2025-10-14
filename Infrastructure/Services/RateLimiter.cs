using System.Collections.Concurrent;
using StudentUnionBot.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// In-memory реалізація Rate Limiter
/// Використовує Sliding Window алгоритм
/// </summary>
public class RateLimiter : IRateLimiter
{
    private readonly ILogger<RateLimiter> _logger;
    
    // Конфігурація лімітів для різних дій
    // Оновлено 2025-10-11: Зроблено більш м'якими для покращення UX
    private readonly Dictionary<string, RateLimitConfig> _limits = new()
    {
        // Створення звернення: 5 звернень на 30 хвилин (було 1 на 10 хв - занадто жорстко!)
        { "CreateAppeal", new RateLimitConfig { MaxAttempts = 5, WindowMinutes = 30 } },
        
        // Відправка повідомлень: 20 повідомлень на хвилину (збільшено для зручності навігації)
        { "SendMessage", new RateLimitConfig { MaxAttempts = 20, WindowMinutes = 1 } },
        
        // Створення новини (адмін): 10 новин на годину
        { "CreateNews", new RateLimitConfig { MaxAttempts = 10, WindowMinutes = 60 } },
        
        // Реєстрація на подію: 5 реєстрацій на 10 хвилин (збільшено для багатьох подій)
        { "RegisterEvent", new RateLimitConfig { MaxAttempts = 5, WindowMinutes = 10 } }
    };

    // Зберігання спроб: Key = "userId:action", Value = список timestamps
    private readonly ConcurrentDictionary<string, List<DateTime>> _attempts = new();

    public RateLimiter(ILogger<RateLimiter> logger)
    {
        _logger = logger;
    }

    public Task<bool> AllowAsync(long userId, string action, CancellationToken cancellationToken = default)
    {
        if (!_limits.TryGetValue(action, out var config))
        {
            // Якщо ліміт не визначений - дозволяємо
            _logger.LogWarning("Ліміт для дії {Action} не визначено, дозволяємо виконання", action);
            return Task.FromResult(true);
        }

        var key = GetKey(userId, action);
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-config.WindowMinutes);

        // Отримуємо або створюємо список спроб
        var attempts = _attempts.GetOrAdd(key, _ => new List<DateTime>());

        lock (attempts)
        {
            // Видаляємо старі спроби (поза вікном)
            attempts.RemoveAll(t => t < windowStart);

            // Перевіряємо чи не перевищено ліміт
            if (attempts.Count >= config.MaxAttempts)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for user {UserId}, action {Action}. Attempts: {Count}/{Max}",
                    userId,
                    action,
                    attempts.Count,
                    config.MaxAttempts
                );
                return Task.FromResult(false);
            }

            // Додаємо поточну спробу
            attempts.Add(now);

            return Task.FromResult(true);
        }
    }

    public Task ResetAsync(long userId, string action, CancellationToken cancellationToken = default)
    {
        var key = GetKey(userId, action);
        _attempts.TryRemove(key, out _);
        
        _logger.LogInformation(
            "Rate limit reset for user {UserId}, action {Action}",
            userId,
            action
        );

        return Task.CompletedTask;
    }

    public Task<int> GetRemainingAttemptsAsync(long userId, string action, CancellationToken cancellationToken = default)
    {
        if (!_limits.TryGetValue(action, out var config))
        {
            return Task.FromResult(int.MaxValue);
        }

        var key = GetKey(userId, action);
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-config.WindowMinutes);

        if (!_attempts.TryGetValue(key, out var attempts))
        {
            return Task.FromResult(config.MaxAttempts);
        }

        lock (attempts)
        {
            attempts.RemoveAll(t => t < windowStart);
            var remaining = config.MaxAttempts - attempts.Count;
            return Task.FromResult(Math.Max(0, remaining));
        }
    }

    public Task<TimeSpan?> GetTimeUntilResetAsync(long userId, string action, CancellationToken cancellationToken = default)
    {
        if (!_limits.TryGetValue(action, out var config))
        {
            return Task.FromResult<TimeSpan?>(null);
        }

        var key = GetKey(userId, action);

        if (!_attempts.TryGetValue(key, out var attempts) || attempts.Count == 0)
        {
            return Task.FromResult<TimeSpan?>(null);
        }

        lock (attempts)
        {
            var now = DateTime.UtcNow;
            var oldestAttempt = attempts.Min();
            var resetTime = oldestAttempt.AddMinutes(config.WindowMinutes);
            var timeUntilReset = resetTime - now;

            return Task.FromResult<TimeSpan?>(timeUntilReset > TimeSpan.Zero ? timeUntilReset : null);
        }
    }

    private static string GetKey(long userId, string action) => $"{userId}:{action}";

    /// <summary>
    /// Очищення старих записів (можна викликати періодично)
    /// </summary>
    public void Cleanup()
    {
        var now = DateTime.UtcNow;
        var keysToRemove = new List<string>();

        foreach (var kvp in _attempts)
        {
            var attempts = kvp.Value;
            lock (attempts)
            {
                // Видаляємо спроби старші 24 годин
                attempts.RemoveAll(t => t < now.AddHours(-24));
                
                // Якщо список порожній - видаляємо ключ
                if (attempts.Count == 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
        }

        foreach (var key in keysToRemove)
        {
            _attempts.TryRemove(key, out _);
        }
    }

    private class RateLimitConfig
    {
        public int MaxAttempts { get; set; }
        public int WindowMinutes { get; set; }
    }
}
