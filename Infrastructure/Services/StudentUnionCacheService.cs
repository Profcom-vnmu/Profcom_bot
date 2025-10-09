using Microsoft.Extensions.Logging;
using StudentUnionBot.Domain.Interfaces;
using System.Text.Json;

namespace StudentUnionBot.Infrastructure.Services;

/// <summary>
/// Сервіс для кешування специфічних даних StudentUnionBot
/// </summary>
public class StudentUnionCacheService : IStudentUnionCacheService
{
    private readonly IRedisCacheService _cacheService;
    private readonly ILogger<StudentUnionCacheService> _logger;
    
    // Константи для ключів кешування
    private const string USER_STATE_PREFIX = "user_state";
    private const string NEWS_LIST_PREFIX = "news_list";
    private const string NEWS_PREFIX = "news";
    private const string EVENTS_LIST_PREFIX = "events_list";
    private const string EVENT_PREFIX = "event";
    private const string EVENT_PARTICIPANTS_PREFIX = "event_participants";
    private const string USER_PREFIX = "user";
    private const string RATE_LIMIT_PREFIX = "rate_limit";
    private const string STATISTICS_PREFIX = "statistics";
    private const string TEMP_DATA_PREFIX = "temp";
    
    // Часи життя кешу за замовчуванням
    private static readonly TimeSpan DefaultUserStateExpiry = TimeSpan.FromHours(24);
    private static readonly TimeSpan DefaultNewsExpiry = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan DefaultEventExpiry = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DefaultUserExpiry = TimeSpan.FromHours(1);
    private static readonly TimeSpan DefaultStatisticsExpiry = TimeSpan.FromDays(7);

    public StudentUnionCacheService(
        IRedisCacheService cacheService,
        ILogger<StudentUnionCacheService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    #region User State Management
    public async Task<string?> GetUserStateAsync(long userId, CancellationToken cancellationToken = default)
    {
        var key = GetUserStateKey(userId);
        return await _cacheService.GetAsync<string>(key, cancellationToken);
    }

    public async Task SetUserStateAsync(long userId, string state, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var key = GetUserStateKey(userId);
        var effectiveExpiry = expiry ?? DefaultUserStateExpiry;
        
        await _cacheService.SetAsync(key, state, effectiveExpiry, cancellationToken);
        _logger.LogDebug("User state cached for user {UserId} with expiry {Expiry}", userId, effectiveExpiry);
    }

    public async Task RemoveUserStateAsync(long userId, CancellationToken cancellationToken = default)
    {
        var key = GetUserStateKey(userId);
        await _cacheService.RemoveAsync(key, cancellationToken);
        _logger.LogDebug("User state removed for user {UserId}", userId);
    }
    #endregion

    #region News Caching
    public async Task<T?> GetNewsListAsync<T>(int page, int pageSize, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetNewsListKey(page, pageSize);
        return await _cacheService.GetAsync<T>(key, cancellationToken);
    }

    public async Task SetNewsListAsync<T>(int page, int pageSize, T newsList, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetNewsListKey(page, pageSize);
        await _cacheService.SetAsync(key, newsList, DefaultNewsExpiry, cancellationToken);
        _logger.LogDebug("News list cached for page {Page}, size {PageSize}", page, pageSize);
    }

    public async Task<T?> GetNewsAsync<T>(int newsId, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetNewsKey(newsId);
        return await _cacheService.GetAsync<T>(key, cancellationToken);
    }

    public async Task SetNewsAsync<T>(int newsId, T news, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetNewsKey(newsId);
        await _cacheService.SetAsync(key, news, DefaultNewsExpiry, cancellationToken);
        _logger.LogDebug("News cached with ID {NewsId}", newsId);
    }

    public async Task InvalidateNewsAsync(int? newsId = null, CancellationToken cancellationToken = default)
    {
        if (newsId.HasValue)
        {
            // Інвалідувати конкретну новину
            var newsKey = GetNewsKey(newsId.Value);
            await _cacheService.RemoveAsync(newsKey, cancellationToken);
            _logger.LogInformation("Cache invalidated for news {NewsId}", newsId.Value);
        }
        
        // Інвалідувати всі списки новин
        var pattern = $"{NEWS_LIST_PREFIX}:*";
        await _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
        _logger.LogInformation("News list cache invalidated");
    }
    #endregion

    #region Events Caching
    public async Task<T?> GetEventsListAsync<T>(int page, int pageSize, string? filter = null, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetEventsListKey(page, pageSize, filter);
        return await _cacheService.GetAsync<T>(key, cancellationToken);
    }

    public async Task SetEventsListAsync<T>(int page, int pageSize, T eventsList, string? filter = null, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetEventsListKey(page, pageSize, filter);
        await _cacheService.SetAsync(key, eventsList, DefaultEventExpiry, cancellationToken);
        _logger.LogDebug("Events list cached for page {Page}, size {PageSize}, filter {Filter}", page, pageSize, filter);
    }

    public async Task<T?> GetEventAsync<T>(int eventId, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetEventKey(eventId);
        return await _cacheService.GetAsync<T>(key, cancellationToken);
    }

    public async Task SetEventAsync<T>(int eventId, T eventData, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetEventKey(eventId);
        await _cacheService.SetAsync(key, eventData, DefaultEventExpiry, cancellationToken);
        _logger.LogDebug("Event cached with ID {EventId}", eventId);
    }

    public async Task<T?> GetEventParticipantsAsync<T>(int eventId, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetEventParticipantsKey(eventId);
        return await _cacheService.GetAsync<T>(key, cancellationToken);
    }

    public async Task SetEventParticipantsAsync<T>(int eventId, T participants, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetEventParticipantsKey(eventId);
        await _cacheService.SetAsync(key, participants, DefaultEventExpiry, cancellationToken);
        _logger.LogDebug("Event participants cached for event {EventId}", eventId);
    }

    public async Task InvalidateEventsAsync(int? eventId = null, CancellationToken cancellationToken = default)
    {
        if (eventId.HasValue)
        {
            // Інвалідувати конкретну подію та її учасників
            var eventKey = GetEventKey(eventId.Value);
            var participantsKey = GetEventParticipantsKey(eventId.Value);
            
            await Task.WhenAll(
                _cacheService.RemoveAsync(eventKey, cancellationToken),
                _cacheService.RemoveAsync(participantsKey, cancellationToken)
            );
            
            _logger.LogInformation("Cache invalidated for event {EventId}", eventId.Value);
        }
        
        // Інвалідувати всі списки подій
        var pattern = $"{EVENTS_LIST_PREFIX}:*";
        await _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
        _logger.LogInformation("Events list cache invalidated");
    }
    #endregion

    #region User Data Caching
    public async Task<T?> GetUserAsync<T>(long userId, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetUserKey(userId);
        return await _cacheService.GetAsync<T>(key, cancellationToken);
    }

    public async Task SetUserAsync<T>(long userId, T user, CancellationToken cancellationToken = default) where T : class
    {
        var key = GetUserKey(userId);
        await _cacheService.SetAsync(key, user, DefaultUserExpiry, cancellationToken);
        _logger.LogDebug("User data cached for user {UserId}", userId);
    }

    public async Task RemoveUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var userKey = GetUserKey(userId);
        var stateKey = GetUserStateKey(userId);
        
        await Task.WhenAll(
            _cacheService.RemoveAsync(userKey, cancellationToken),
            _cacheService.RemoveAsync(stateKey, cancellationToken)
        );
        
        _logger.LogDebug("User cache removed for user {UserId}", userId);
    }
    #endregion

    #region Rate Limiting
    public async Task<long> GetUserRequestCountAsync(long userId, CancellationToken cancellationToken = default)
    {
        var key = GetRateLimitKey(userId);
        var count = await _cacheService.GetAsync<string>(key, cancellationToken);
        return long.TryParse(count, out var result) ? result : 0;
    }

    public async Task IncrementUserRequestCountAsync(long userId, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var key = GetRateLimitKey(userId);
        await _cacheService.IncrementAsync(key, (long)window.TotalSeconds, cancellationToken);
    }

    public async Task<bool> IsUserRateLimitedAsync(long userId, int maxRequests, TimeSpan window, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentCount = await GetUserRequestCountAsync(userId, cancellationToken);
            
            if (currentCount >= maxRequests)
            {
                _logger.LogWarning("User {UserId} is rate limited: {CurrentCount}/{MaxRequests} requests", 
                    userId, currentCount, maxRequests);
                return true;
            }
            
            await IncrementUserRequestCountAsync(userId, window, cancellationToken);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for user {UserId}, allowing request", userId);
            return false; // У разі помилки дозволяємо запит
        }
    }
    #endregion

    #region Statistics
    public async Task IncrementStatisticAsync(string statistic, CancellationToken cancellationToken = default)
    {
        var key = GetStatisticsKey(statistic);
        await _cacheService.IncrementAsync(key, (long)DefaultStatisticsExpiry.TotalSeconds, cancellationToken);
        _logger.LogDebug("Statistic {Statistic} incremented", statistic);
    }

    public async Task<long> GetStatisticAsync(string statistic, CancellationToken cancellationToken = default)
    {
        var key = GetStatisticsKey(statistic);
        var value = await _cacheService.GetAsync<string>(key, cancellationToken);
        return long.TryParse(value, out var result) ? result : 0;
    }

    public async Task SetStatisticAsync(string statistic, long value, CancellationToken cancellationToken = default)
    {
        var key = GetStatisticsKey(statistic);
        await _cacheService.SetAsync(key, value.ToString(), DefaultStatisticsExpiry, cancellationToken);
        _logger.LogDebug("Statistic {Statistic} set to {Value}", statistic, value);
    }
    #endregion

    #region Temporary Data
    public async Task<T?> GetTemporaryDataAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var cacheKey = GetTempDataKey(key);
        return await _cacheService.GetAsync<T>(cacheKey, cancellationToken);
    }

    public async Task SetTemporaryDataAsync<T>(string key, T data, TimeSpan expiry, CancellationToken cancellationToken = default) where T : class
    {
        var cacheKey = GetTempDataKey(key);
        await _cacheService.SetAsync(cacheKey, data, expiry, cancellationToken);
        _logger.LogDebug("Temporary data set with key {Key} and expiry {Expiry}", key, expiry);
    }

    public async Task RemoveTemporaryDataAsync(string key, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetTempDataKey(key);
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
        _logger.LogDebug("Temporary data removed with key {Key}", key);
    }
    #endregion

    #region Key Generation Helpers
    private static string GetUserStateKey(long userId) => $"{USER_STATE_PREFIX}:{userId}";
    
    private static string GetNewsListKey(int page, int pageSize) => $"{NEWS_LIST_PREFIX}:{page}:{pageSize}";
    
    private static string GetNewsKey(int newsId) => $"{NEWS_PREFIX}:{newsId}";
    
    private static string GetEventsListKey(int page, int pageSize, string? filter = null) => 
        string.IsNullOrEmpty(filter) 
            ? $"{EVENTS_LIST_PREFIX}:{page}:{pageSize}" 
            : $"{EVENTS_LIST_PREFIX}:{page}:{pageSize}:{filter.ToLowerInvariant()}";
    
    private static string GetEventKey(int eventId) => $"{EVENT_PREFIX}:{eventId}";
    
    private static string GetEventParticipantsKey(int eventId) => $"{EVENT_PARTICIPANTS_PREFIX}:{eventId}";
    
    private static string GetUserKey(long userId) => $"{USER_PREFIX}:{userId}";
    
    private static string GetRateLimitKey(long userId) => $"{RATE_LIMIT_PREFIX}:{userId}";
    
    private static string GetStatisticsKey(string statistic) => $"{STATISTICS_PREFIX}:{statistic.ToLowerInvariant()}";
    
    private static string GetTempDataKey(string key) => $"{TEMP_DATA_PREFIX}:{key}";
    #endregion
}