namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Інтерфейс для кешування специфічних даних StudentUnionBot
/// </summary>
public interface IStudentUnionCacheService
{
    #region User State Management
    /// <summary>
    /// Кеш стану користувача
    /// </summary>
    Task<string?> GetUserStateAsync(long userId, CancellationToken cancellationToken = default);
    
    Task SetUserStateAsync(long userId, string state, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    
    Task RemoveUserStateAsync(long userId, CancellationToken cancellationToken = default);
    #endregion

    #region News Caching
    /// <summary>
    /// Кеш списку новин
    /// </summary>
    Task<T?> GetNewsListAsync<T>(int page, int pageSize, CancellationToken cancellationToken = default) where T : class;
    
    Task SetNewsListAsync<T>(int page, int pageSize, T newsList, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Кеш окремої новини
    /// </summary>
    Task<T?> GetNewsAsync<T>(int newsId, CancellationToken cancellationToken = default) where T : class;
    
    Task SetNewsAsync<T>(int newsId, T news, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Інвалідація кешу новин
    /// </summary>
    Task InvalidateNewsAsync(int? newsId = null, CancellationToken cancellationToken = default);
    #endregion

    #region Events Caching
    /// <summary>
    /// Кеш списку подій
    /// </summary>
    Task<T?> GetEventsListAsync<T>(int page, int pageSize, string? filter = null, CancellationToken cancellationToken = default) where T : class;
    
    Task SetEventsListAsync<T>(int page, int pageSize, T eventsList, string? filter = null, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Кеш окремої події
    /// </summary>
    Task<T?> GetEventAsync<T>(int eventId, CancellationToken cancellationToken = default) where T : class;
    
    Task SetEventAsync<T>(int eventId, T eventData, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Кеш учасників події
    /// </summary>
    Task<T?> GetEventParticipantsAsync<T>(int eventId, CancellationToken cancellationToken = default) where T : class;
    
    Task SetEventParticipantsAsync<T>(int eventId, T participants, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Інвалідація кешу подій
    /// </summary>
    Task InvalidateEventsAsync(int? eventId = null, CancellationToken cancellationToken = default);
    #endregion

    #region User Data Caching
    /// <summary>
    /// Кеш даних користувача
    /// </summary>
    Task<T?> GetUserAsync<T>(long userId, CancellationToken cancellationToken = default) where T : class;
    
    Task SetUserAsync<T>(long userId, T user, CancellationToken cancellationToken = default) where T : class;
    
    Task RemoveUserAsync(long userId, CancellationToken cancellationToken = default);
    #endregion

    #region Rate Limiting
    /// <summary>
    /// Rate limiting для користувачів
    /// </summary>
    Task<long> GetUserRequestCountAsync(long userId, CancellationToken cancellationToken = default);
    
    Task IncrementUserRequestCountAsync(long userId, TimeSpan window, CancellationToken cancellationToken = default);
    
    Task<bool> IsUserRateLimitedAsync(long userId, int maxRequests, TimeSpan window, CancellationToken cancellationToken = default);
    #endregion

    #region Statistics
    /// <summary>
    /// Статистика використання бота
    /// </summary>
    Task IncrementStatisticAsync(string statistic, CancellationToken cancellationToken = default);
    
    Task<long> GetStatisticAsync(string statistic, CancellationToken cancellationToken = default);
    
    Task SetStatisticAsync(string statistic, long value, CancellationToken cancellationToken = default);
    #endregion

    #region Temporary Data
    /// <summary>
    /// Тимчасові дані (наприклад, коди верифікації)
    /// </summary>
    Task<T?> GetTemporaryDataAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    
    Task SetTemporaryDataAsync<T>(string key, T data, TimeSpan expiry, CancellationToken cancellationToken = default) where T : class;
    
    Task RemoveTemporaryDataAsync(string key, CancellationToken cancellationToken = default);
    #endregion
}