using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace StudentUnionBot.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior для моніторингу performance всіх MediatR requests
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    
    // Поріг для warning про повільні запити (в мілісекундах)
    private const int SlowRequestThresholdMs = 1000;
    
    // Поріг для error про дуже повільні запити (в мілісекундах) 
    private const int VerySlowRequestThresholdMs = 5000;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        // Створюємо activity для distributed tracing
        using var activity = new Activity($"MediatR.Performance.{requestName}").Start();
        
        // Стартуємо вимірювання часу
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Виконуємо наступний behavior або handler
            var response = await next();
            
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            
            // Додаємо метрики до activity
            activity?.SetTag("performance.duration_ms", elapsedMs);
            activity?.SetTag("performance.request_name", requestName);
            
            // Логуємо в залежності від тривалості виконання
            LogPerformanceMetrics(requestName, elapsedMs);
            
            return response;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            
            // Логуємо performance навіть при помилці
            activity?.SetTag("performance.duration_ms", elapsedMs);
            activity?.SetTag("performance.request_name", requestName);
            activity?.SetTag("performance.failed", true);
            
            _logger.LogWarning(
                "Request {RequestName} failed after {ElapsedMs}ms",
                requestName,
                elapsedMs
            );
            
            throw; // Re-throw для подальшої обробки
        }
    }

    private void LogPerformanceMetrics(string requestName, long elapsedMs)
    {
        if (elapsedMs >= VerySlowRequestThresholdMs)
        {
            // Дуже повільний запит - це серйозна проблема
            _logger.LogError(
                "VERY SLOW REQUEST: {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                requestName,
                elapsedMs,
                VerySlowRequestThresholdMs
            );
        }
        else if (elapsedMs >= SlowRequestThresholdMs)
        {
            // Повільний запит - потрібна увага
            _logger.LogWarning(
                "Slow request: {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                requestName,
                elapsedMs,
                SlowRequestThresholdMs
            );
        }
        else
        {
            // Нормальний запит - debug логування
            _logger.LogDebug(
                "Request {RequestName} completed in {ElapsedMs}ms",
                requestName,
                elapsedMs
            );
        }

        // Додаткові метрики для спеціальних типів запитів
        LogSpecialCases(requestName, elapsedMs);
    }

    private void LogSpecialCases(string requestName, long elapsedMs)
    {
        // Моніторимо критичні операції бота
        if (requestName.Contains("Appeal") && elapsedMs > 500)
        {
            _logger.LogInformation(
                "Appeal operation {RequestName} took {ElapsedMs}ms - monitor appeal processing performance",
                requestName,
                elapsedMs
            );
        }
        
        if (requestName.Contains("File") && elapsedMs > 2000)
        {
            _logger.LogInformation(
                "File operation {RequestName} took {ElapsedMs}ms - monitor file processing performance",
                requestName,
                elapsedMs
            );
        }

        if (requestName.Contains("Email") && elapsedMs > 3000)
        {
            _logger.LogInformation(
                "Email operation {RequestName} took {ElapsedMs}ms - monitor email service performance",
                requestName,
                elapsedMs
            );
        }

        // Запити з пагінацією повинні бути швидкими
        if (requestName.Contains("GetList") && elapsedMs > 200)
        {
            _logger.LogWarning(
                "Paginated query {RequestName} took {ElapsedMs}ms - check database indexes",
                requestName,
                elapsedMs
            );
        }
    }
}