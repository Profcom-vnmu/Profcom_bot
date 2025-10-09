using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Infrastructure.HealthChecks;

/// <summary>
/// Health Check для перевірки доступності Redis кешу
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IRedisCacheService _cacheService;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(
        IRedisCacheService cacheService,
        ILogger<RedisHealthCheck> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            const string testKey = "health_check_test";
            const string testValue = "test_value";
            var testExpiry = TimeSpan.FromMinutes(1);

            // Тест запису в кеш
            await _cacheService.SetAsync(testKey, testValue, testExpiry, cancellationToken);

            // Тест читання з кешу
            var cachedValue = await _cacheService.GetAsync<string>(testKey, cancellationToken);

            // Тест видалення з кешу
            await _cacheService.RemoveAsync(testKey, cancellationToken);

            if (cachedValue != testValue)
            {
                var message = $"Redis cache test failed: expected '{testValue}', got '{cachedValue}'";
                _logger.LogWarning(message);
                return HealthCheckResult.Degraded(message);
            }

            _logger.LogDebug("Redis health check completed successfully");

            return HealthCheckResult.Healthy("Redis cache is working properly", new Dictionary<string, object>
            {
                { "status", "connected" },
                { "test_operation", "success" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");

            return HealthCheckResult.Unhealthy("Redis cache is not available", ex, new Dictionary<string, object>
            {
                { "status", "disconnected" },
                { "error", ex.Message }
            });
        }
    }
}