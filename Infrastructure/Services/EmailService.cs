using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Infrastructure.Services;

/// <summary>
/// Сервіс для відправки email через SMTP
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _enableSsl;

    public EmailService(
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        _smtpHost = configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "587");
        _smtpUsername = configuration["Email:SmtpUsername"] ?? "";
        _smtpPassword = configuration["Email:SmtpPassword"] ?? "";
        _fromEmail = configuration["Email:FromEmail"] ?? "";
        _fromName = configuration["Email:FromName"] ?? "Студентський Профком ВНМУ";
        _enableSsl = bool.Parse(configuration["Email:EnableSsl"] ?? "true");
    }

    public async Task<bool> SendVerificationCodeAsync(string toEmail, string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = "Код верифікаціїEmail - Студентський Профком";
            var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #2c3e50;'>Верифікація Email</h2>
        <p>Вітаємо!</p>
        <p>Ваш код верифікації для підтвердження email адреси:</p>
        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; text-align: center; margin: 20px 0;'>
            <h1 style='color: #007bff; margin: 0; letter-spacing: 5px;'>{code}</h1>
        </div>
        <p><strong>Код діє протягом 15 хвилин.</strong></p>
        <p>Якщо ви не запитували код верифікації, проігноруйте це повідомлення.</p>
        <hr style='border: none; border-top: 1px solid #dee2e6; margin: 30px 0;'>
        <p style='color: #6c757d; font-size: 12px;'>
            З повагою,<br>
            Студентський Профком ВНМУ
        </p>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відправці коду верифікації на {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendNewAppealNotificationAsync(string toEmail, int appealId, string subject, CancellationToken cancellationToken = default)
    {
        try
        {
            var emailSubject = $"Нове звернення #{appealId} - Студентський Профком";
            var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #2c3e50;'>Нове звернення #{appealId}</h2>
        <p><strong>Тема:</strong> {subject}</p>
        <p>Отримано нове звернення від студента.</p>
        <p>Перейдіть до бота для перегляду деталей та відповіді.</p>
        <hr style='border: none; border-top: 1px solid #dee2e6; margin: 30px 0;'>
        <p style='color: #6c757d; font-size: 12px;'>
            Студентський Профком ВНМУ
        </p>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, emailSubject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відправці сповіщення про нове звернення #{AppealId} на {Email}", appealId, toEmail);
            return false;
        }
    }

    public async Task<bool> SendAppealReplyNotificationAsync(string toEmail, int appealId, string replyText, CancellationToken cancellationToken = default)
    {
        try
        {
            var emailSubject = $"Відповідь на звернення #{appealId} - Студентський Профком";
            var body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #2c3e50;'>Відповідь на звернення #{appealId}</h2>
        <p>Отримано відповідь від адміністрації профкому:</p>
        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
            <p style='margin: 0;'>{replyText}</p>
        </div>
        <p>Перейдіть до бота для перегляду повної історії звернення.</p>
        <hr style='border: none; border-top: 1px solid #dee2e6; margin: 30px 0;'>
        <p style='color: #6c757d; font-size: 12px;'>
            З повагою,<br>
            Студентський Профком ВНМУ
        </p>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, emailSubject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відправці сповіщення про відповідь на звернення #{AppealId} на {Email}", appealId, toEmail);
            return false;
        }
    }

    private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_smtpUsername) || string.IsNullOrWhiteSpace(_smtpPassword))
        {
            _logger.LogWarning("Email сервіс не налаштовано (відсутні SMTP credentials). Email не буде відправлено.");
            return false;
        }

        try
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = _enableSsl
            };

            var message = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email успішно відправлено на {Email} з темою '{Subject}'", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відправці email на {Email}", toEmail);
            return false;
        }
    }
}
