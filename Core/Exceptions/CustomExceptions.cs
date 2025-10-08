namespace StudentUnionBot.Core.Exceptions;

/// <summary>
/// Базовий виняток для domain логіки
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Виняток коли сутність не знайдена
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} з ключем '{key}' не знайдено") { }

    public NotFoundException(string message) : base(message) { }
}

/// <summary>
/// Виняток валідації
/// </summary>
public class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("Виникли помилки валідації")
    {
        Errors = errors as IReadOnlyDictionary<string, string[]>
            ?? new Dictionary<string, string[]>(errors);
    }

    public ValidationException(string propertyName, string errorMessage)
        : base("Виникли помилки валідації")
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { errorMessage } }
        };
    }
}

/// <summary>
/// Виняток перевищення ліміту запитів
/// </summary>
public class RateLimitException : Exception
{
    public DateTime RetryAfter { get; }

    public RateLimitException(DateTime retryAfter)
        : base($"Перевищено ліміт запитів. Спробуйте після {retryAfter:HH:mm:ss}")
    {
        RetryAfter = retryAfter;
    }

    public RateLimitException(string message) : base(message) { }
}

/// <summary>
/// Виняток неавторизованого доступу
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException() : base("Доступ заборонено") { }

    public UnauthorizedException(string message) : base(message) { }
}
