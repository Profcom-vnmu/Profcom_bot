namespace StudentUnionBot.Application.Common.Attributes;

/// <summary>
/// Атрибут для позначення команд, які потребують rate limiting
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RateLimitAttribute : Attribute
{
    /// <summary>
    /// Назва ресурсу для rate limiting (наприклад, "CreateAppeal", "SendMessage")
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// Конструктор з назвою дії
    /// </summary>
    /// <param name="action">Назва дії для rate limiting</param>
    public RateLimitAttribute(string action)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be null or whitespace", nameof(action));
        
        Action = action;
    }
}
