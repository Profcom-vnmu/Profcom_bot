using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior для автоматичної валідації всіх команд через FluentValidation
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        // Якщо немає валідаторів - продовжуємо
        if (!_validators.Any())
        {
            _logger.LogDebug("No validators found for {RequestName}", requestName);
            return await next();
        }

        _logger.LogDebug("Validating {RequestName} with {ValidatorCount} validators", 
                        requestName, _validators.Count());

        // Створюємо контекст валідації
        var context = new ValidationContext<TRequest>(request);

        // Виконуємо всі валідатори паралельно
        var validationTasks = _validators
            .Select(validator => validator.ValidateAsync(context, cancellationToken));

        var validationResults = await Task.WhenAll(validationTasks);

        // Збираємо всі помилки валідації
        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure != null)
            .ToList();

        if (failures.Count != 0)
        {
            var errorMessages = failures.Select(f => f.ErrorMessage).ToList();
            
            _logger.LogWarning("Validation failed for {RequestName}. Errors: {ValidationErrors}", 
                              requestName, string.Join("; ", errorMessages));

            // Для Result<T> повертаємо Fail результат
            if (typeof(TResponse).IsGenericType && 
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = typeof(TResponse).GetGenericArguments()[0];
                var failMethod = typeof(Result<>)
                    .MakeGenericType(resultType)
                    .GetMethod("Fail", new[] { typeof(IEnumerable<string>) });

                var result = failMethod?.Invoke(null, new object[] { errorMessages });
                return (TResponse)result!;
            }

            // Для простого Result
            if (typeof(TResponse) == typeof(Result))
            {
                var result = Result.Fail(string.Join("; ", errorMessages));
                return (TResponse)(object)result;
            }

            // Якщо це не Result<T> - кидаємо виняток
            throw new ValidationException(failures);
        }

        _logger.LogDebug("Validation passed for {RequestName}", requestName);

        // Валідація успішна - продовжуємо до наступного behavior або handler
        return await next();
    }
}