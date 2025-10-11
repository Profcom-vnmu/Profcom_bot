using System.Reflection;
using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Common.Attributes;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Common.Behaviors;

/// <summary>
/// MediatR behavior для автоматичної перевірки rate limits
/// </summary>
public class RateLimitingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly IRateLimiter _rateLimiter;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RateLimitingBehavior<TRequest, TResponse>> _logger;

    public RateLimitingBehavior(
        IRateLimiter rateLimiter,
        ICurrentUserService currentUserService,
        ILogger<RateLimitingBehavior<TRequest, TResponse>> logger)
    {
        _rateLimiter = rateLimiter;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Перевіряємо наявність RateLimitAttribute
        var rateLimitAttribute = typeof(TRequest).GetCustomAttribute<RateLimitAttribute>();

        // Якщо атрибут відсутній - пропускаємо перевірку
        if (rateLimitAttribute == null)
        {
            return await next();
        }

        // Отримуємо ID поточного користувача
        var userId = _currentUserService.UserId;

        // Якщо користувач не встановлений - пропускаємо (для системних команд)
        if (!userId.HasValue)
        {
            _logger.LogWarning(
                "Rate limiting skipped for {RequestType} - no current user",
                typeof(TRequest).Name
            );
            return await next();
        }

        var action = rateLimitAttribute.Action;

        try
        {
            // Перевіряємо rate limit
            var allowed = await _rateLimiter.AllowAsync(userId.Value, action, cancellationToken);

            if (!allowed)
            {
                // Rate limit перевищено
                _logger.LogWarning(
                    "Rate limit exceeded for user {UserId}, action {Action}, request {RequestType}",
                    userId.Value,
                    action,
                    typeof(TRequest).Name
                );

                // Отримуємо час до скидання ліміту
                var timeUntilReset = await _rateLimiter.GetTimeUntilResetAsync(
                    userId.Value,
                    action,
                    cancellationToken
                );

                var errorMessage = timeUntilReset.HasValue
                    ? $"Перевищено ліміт запитів. Спробуйте через {timeUntilReset.Value.TotalMinutes:F0} хв."
                    : "Перевищено ліміт запитів. Спробуйте пізніше.";

                // Повертаємо Result.Fail якщо TResponse є Result<T>
                return CreateFailResult(errorMessage);
            }

            // Ліміт не перевищено - продовжуємо виконання
            return await next();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking rate limit for user {UserId}, action {Action}",
                userId.Value,
                action
            );

            // У разі помилки - пропускаємо перевірку (fail-open)
            return await next();
        }
    }

    /// <summary>
    /// Створює Result.Fail для різних типів Result
    /// </summary>
    private static TResponse CreateFailResult(string errorMessage)
    {
        var responseType = typeof(TResponse);

        // Якщо TResponse є Result<T>
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var innerType = responseType.GetGenericArguments()[0];
            var failMethod = typeof(Result<>)
                .MakeGenericType(innerType)
                .GetMethod("Fail", new[] { typeof(string) });

            return (TResponse)failMethod!.Invoke(null, new object[] { errorMessage })!;
        }

        // Якщо TResponse є Result
        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Fail(errorMessage);
        }

        // Для інших типів - кидаємо виключення (не повинно статися)
        throw new InvalidOperationException(
            $"RateLimitingBehavior can only be used with Result or Result<T> responses. Got {responseType.Name}"
        );
    }
}
