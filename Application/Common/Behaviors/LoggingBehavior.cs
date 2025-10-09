using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace StudentUnionBot.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior для structured logging всіх MediatR requests з performance metrics
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid().ToString("N")[..8]; // Короткий ID для трекінгу

        // Логуємо початок обробки запиту
        using var activity = new Activity($"MediatR.{requestName}").Start();
        activity?.SetTag("request.name", requestName);
        activity?.SetTag("request.id", requestId);

        _logger.LogInformation(
            "Starting request {RequestName} [{RequestId}]",
            requestName,
            requestId
        );

        // Логуємо параметри запиту (тільки в Debug режимі для безпеки)
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            try
            {
                var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                _logger.LogDebug(
                    "Request {RequestName} [{RequestId}] parameters: {RequestParameters}",
                    requestName,
                    requestId,
                    requestJson
                );
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    "Could not serialize request {RequestName} [{RequestId}] parameters: {Error}",
                    requestName,
                    requestId,
                    ex.Message
                );
            }
        }

        // Вимірюємо час виконання
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Виконуємо наступний behavior або handler
            var response = await next();
            
            stopwatch.Stop();
            
            // Логуємо успішне завершення
            _logger.LogInformation(
                "Completed request {RequestName} [{RequestId}] in {ElapsedMs}ms",
                requestName,
                requestId,
                stopwatch.ElapsedMilliseconds
            );

            // Додаємо метрики до activity
            activity?.SetTag("request.duration_ms", stopwatch.ElapsedMilliseconds);
            activity?.SetTag("request.success", true);

            // Логуємо результат (тільки в Debug режимі)
            if (_logger.IsEnabled(LogLevel.Debug) && response != null)
            {
                try
                {
                    var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    _logger.LogDebug(
                        "Request {RequestName} [{RequestId}] result: {Response}",
                        requestName,
                        requestId,
                        responseJson
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(
                        "Could not serialize response for {RequestName} [{RequestId}]: {Error}",
                        requestName,
                        requestId,
                        ex.Message
                    );
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Логуємо помилку
            _logger.LogError(ex,
                "Request {RequestName} [{RequestId}] failed after {ElapsedMs}ms: {ErrorMessage}",
                requestName,
                requestId,
                stopwatch.ElapsedMilliseconds,
                ex.Message
            );

            // Додаємо метрики помилки до activity
            activity?.SetTag("request.duration_ms", stopwatch.ElapsedMilliseconds);
            activity?.SetTag("request.success", false);
            activity?.SetTag("request.error", ex.Message);

            throw; // Re-throw для подальшої обробки
        }
    }
}