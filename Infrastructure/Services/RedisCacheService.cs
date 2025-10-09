using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Infrastructure.Services;

/// <summary>
/// Сервіс для роботи з Redis кешем
/// </summary>
public class RedisCacheService : IRedisCacheService
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisCacheService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _database = connectionMultiplexer.GetDatabase();
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            
            if (!value.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            _logger.LogDebug("Cache hit for key: {Key}", key);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from cache for key: {Key}", key);
            return null; // Graceful degradation - повертаємо null замість exception
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            
            await _database.StringSetAsync(key, serializedValue, expiry);
            
            _logger.LogDebug("Set cache value for key: {Key} with expiry: {Expiry}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            // Не кидаємо exception - кеш не критичний для роботи додатка
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
            _logger.LogDebug("Removed cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _connectionMultiplexer.GetEndPoints();
            var server = _connectionMultiplexer.GetServer(endpoints.First());
            
            var keys = server.Keys(pattern: pattern).ToArray();
            
            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
                _logger.LogDebug("Removed {Count} cache keys matching pattern: {Pattern}", keys.Length, pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if cache key exists: {Key}", key);
            return false;
        }
    }

    public async Task ExpireAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.KeyExpireAsync(key, expiry);
            _logger.LogDebug("Set expiry for cache key: {Key} to {Expiry}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting expiry for cache key: {Key}", key);
        }
    }

    public async Task<TimeSpan?> GetTtlAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.KeyTimeToLiveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TTL for cache key: {Key}", key);
            return null;
        }
    }

    public async Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _database.StringIncrementAsync(key, value);
            _logger.LogDebug("Incremented cache key: {Key} by {Value}, new value: {Result}", key, value, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing cache key: {Key}", key);
            return 0;
        }
    }

    public async Task<long> DecrementAsync(string key, long value = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _database.StringDecrementAsync(key, value);
            _logger.LogDebug("Decremented cache key: {Key} by {Value}, new value: {Result}", key, value, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrementing cache key: {Key}", key);
            return 0;
        }
    }

    public async Task<bool> SetIfNotExistsAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            
            var result = await _database.StringSetAsync(key, serializedValue, expiry, When.NotExists);
            
            if (result)
            {
                _logger.LogDebug("Set cache value for new key: {Key}", key);
            }
            else
            {
                _logger.LogDebug("Key already exists, not setting: {Key}", key);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value if not exists for key: {Key}", key);
            return false;
        }
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        var result = new Dictionary<string, T?>();
        
        try
        {
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            var values = await _database.StringGetAsync(redisKeys);
            
            for (int i = 0; i < redisKeys.Length; i++)
            {
                var key = redisKeys[i];
                var value = values[i];
                
                if (value.HasValue)
                {
                    try
                    {
                        result[key!] = JsonSerializer.Deserialize<T>(value!, _jsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Error deserializing cached value for key: {Key}", key);
                        result[key!] = null;
                    }
                }
                else
                {
                    result[key!] = null;
                }
            }
            
            _logger.LogDebug("Retrieved {Count} values from cache", result.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting multiple values from cache");
        }
        
        return result;
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var tasks = keyValuePairs.Select(async kvp =>
            {
                await SetAsync(kvp.Key, kvp.Value, expiry, cancellationToken);
            });
            
            await Task.WhenAll(tasks);
            
            _logger.LogDebug("Set {Count} values in cache", keyValuePairs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting multiple values in cache");
        }
    }

    public async Task FlushAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _connectionMultiplexer.GetEndPoints();
            
            foreach (var endpoint in endpoints)
            {
                var server = _connectionMultiplexer.GetServer(endpoint);
                await server.FlushAllDatabasesAsync();
            }
            
            _logger.LogWarning("Flushed all Redis databases");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing Redis databases");
        }
    }
}