namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Сервіс для відправки email
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Відправка email з кодом верифікації
    /// </summary>
    Task<bool> SendVerificationCodeAsync(string toEmail, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Відправка повідомлення про нове звернення адміністратору
    /// </summary>
    Task<bool> SendNewAppealNotificationAsync(string toEmail, int appealId, string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Відправка повідомлення про відповідь на звернення користувачу
    /// </summary>
    Task<bool> SendAppealReplyNotificationAsync(string toEmail, int appealId, string replyText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Відправка повідомлення про нову новину
    /// </summary>
    Task<bool> SendNewsNotificationAsync(string toEmail, string newsTitle, string newsSummary, string newsUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Відправка повідомлення про нову подію
    /// </summary>
    Task<bool> SendEventNotificationAsync(string toEmail, string eventTitle, DateTime eventDate, string eventLocation, string eventUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Відправка нагадування про подію
    /// </summary>
    Task<bool> SendEventReminderAsync(string toEmail, string eventTitle, DateTime eventDate, string eventLocation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Відправка підтвердження реєстрації на подію
    /// </summary>
    Task<bool> SendEventRegistrationConfirmationAsync(string toEmail, string eventTitle, DateTime eventDate, string eventLocation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Відправка довільного email з шаблоном
    /// </summary>
    Task<bool> SendTemplateEmailAsync(string toEmail, string templateName, Dictionary<string, object> templateData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Відправка масового розсилання
    /// </summary>
    Task<bool> SendBulkEmailAsync(List<string> toEmails, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
