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
}
