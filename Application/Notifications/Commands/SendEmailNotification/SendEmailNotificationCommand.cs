using MediatR;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Notifications.Commands.SendEmailNotification;

/// <summary>
/// Command для відправки email повідомлення
/// </summary>
public class SendEmailNotificationCommand : IRequest<Result<bool>>
{
    /// <summary>
    /// Email адреси отримувачів
    /// </summary>
    public List<string> ToEmails { get; set; } = new();

    /// <summary>
    /// Тип повідомлення
    /// </summary>
    public EmailNotificationType Type { get; set; }

    /// <summary>
    /// Дані для шаблону
    /// </summary>
    public Dictionary<string, object> TemplateData { get; set; } = new();

    /// <summary>
    /// Користувацька тема (якщо не використовуємо шаблон)
    /// </summary>
    public string? CustomSubject { get; set; }

    /// <summary>
    /// Користувацький HTML контент (якщо не використовуємо шаблон)
    /// </summary>
    public string? CustomHtmlBody { get; set; }
}

/// <summary>
/// Типи email повідомлень
/// </summary>
public enum EmailNotificationType
{
    /// <summary>
    /// Код верифікації email
    /// </summary>
    EmailVerification = 1,

    /// <summary>
    /// Нове звернення для адмінів
    /// </summary>
    NewAppeal = 2,

    /// <summary>
    /// Відповідь на звернення
    /// </summary>
    AppealReply = 3,

    /// <summary>
    /// Повідомлення про нову новину
    /// </summary>
    NewsNotification = 4,

    /// <summary>
    /// Повідомлення про нову подію
    /// </summary>
    EventNotification = 5,

    /// <summary>
    /// Нагадування про подію
    /// </summary>
    EventReminder = 6,

    /// <summary>
    /// Підтвердження реєстрації на подію
    /// </summary>
    EventRegistrationConfirmation = 7,

    /// <summary>
    /// Користувацький email з шаблоном
    /// </summary>
    CustomTemplate = 8,

    /// <summary>
    /// Користувацький email без шаблону
    /// </summary>
    CustomHtml = 9
}