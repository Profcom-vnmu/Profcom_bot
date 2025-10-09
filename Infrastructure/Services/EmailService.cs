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
    private readonly EmailTemplateService _templateService;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _enableSsl;
    private readonly string _botUrl;

    public EmailService(
        ILogger<EmailService> logger,
        IConfiguration configuration,
        EmailTemplateService templateService)
    {
        _logger = logger;
        _configuration = configuration;
        _templateService = templateService;

        _smtpHost = configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "587");
        _smtpUsername = configuration["Email:SmtpUsername"] ?? "";
        _smtpPassword = configuration["Email:SmtpPassword"] ?? "";
        _fromEmail = configuration["Email:FromEmail"] ?? "";
        _fromName = configuration["Email:FromName"] ?? "Студентський Профком ВНМУ";
        _enableSsl = bool.Parse(configuration["Email:EnableSsl"] ?? "true");
        _botUrl = configuration["BotConfiguration:BotUrl"] ?? "https://t.me/StudentUnionBot";
    }

    public async Task<bool> SendVerificationCodeAsync(string toEmail, string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = "Код верифікації Email - Студентський Профком";
            
            var variables = _templateService.CreateVerificationVariables(
                userName: "Шановний студенте", 
                verificationCode: code, 
                expiryMinutes: 15
            );
            
            var body = await _templateService.ProcessTemplateAsync("EmailVerification", variables, cancellationToken);

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

    public async Task<bool> SendNewsNotificationAsync(string toEmail, string newsTitle, string newsSummary, string newsUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = $"📰 Нова новина: {newsTitle}";
            
            var variables = _templateService.CreateNewsVariables(
                newsTitle: newsTitle,
                newsSummary: newsSummary,
                newsCategory: "Загальні новини", 
                newsUrl: newsUrl
            );
            
            var body = await _templateService.ProcessTemplateAsync("NewsNotification", variables, cancellationToken);

            return await SendEmailAsync(toEmail, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відправці повідомлення про новину на {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendEventNotificationAsync(string toEmail, string eventTitle, DateTime eventDate, string eventLocation, string eventUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = $"🎉 Нова подія: {eventTitle}";
            
            var variables = _templateService.CreateEventVariables(
                eventTitle: eventTitle,
                eventDate: eventDate,
                eventLocation: eventLocation,
                eventCategory: "Загальні події",
                eventUrl: eventUrl
            );
            
            var body = await _templateService.ProcessTemplateAsync("EventNotification", variables, cancellationToken);

            return await SendEmailAsync(toEmail, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відправці повідомлення про подію на {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendEventReminderAsync(string toEmail, string eventTitle, DateTime eventDate, string eventLocation, CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = $"⏰ Нагадування: {eventTitle} сьогодні";
            
            var variables = _templateService.CreateEventReminderVariables(
                eventTitle: eventTitle,
                eventDate: eventDate,
                eventLocation: eventLocation
            );
            
            var body = await _templateService.ProcessTemplateAsync("EventReminder", variables, cancellationToken);

            return await SendEmailAsync(toEmail, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відправці нагадування про подію на {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendEventRegistrationConfirmationAsync(string toEmail, string eventTitle, DateTime eventDate, string eventLocation, CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = $"✅ Реєстрація підтверджена: {eventTitle}";
            
            // Використовуємо базовий шаблон події з підтвердженням
            var variables = _templateService.CreateEventVariables(
                eventTitle: eventTitle,
                eventDate: eventDate,
                eventLocation: eventLocation,
                eventCategory: "Підтвердження реєстрації",
                eventUrl: _botUrl
            );
            
            var body = await _templateService.ProcessTemplateAsync("EventNotification", variables, cancellationToken);

            return await SendEmailAsync(toEmail, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відправці підтвердження реєстрації на {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendTemplateEmailAsync(string toEmail, string templateName, Dictionary<string, object> templateData, CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = templateData.GetValueOrDefault("Subject", "Повідомлення від StudentUnionBot")?.ToString() ?? "Повідомлення";
            
            var body = await _templateService.ProcessTemplateAsync(templateName, templateData, cancellationToken);

            return await SendEmailAsync(toEmail, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відправці шаблонного email на {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendBulkEmailAsync(List<string> toEmails, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_smtpUsername) || string.IsNullOrWhiteSpace(_smtpPassword))
        {
            _logger.LogWarning("Email сервіс не налаштовано (відсутні SMTP credentials). Масова розсилка не буде виконана.");
            return false;
        }

        var successCount = 0;
        var totalCount = toEmails.Count;

        _logger.LogInformation("Початок масової розсилки для {Count} адресатів", totalCount);

        try
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = _enableSsl
            };

            foreach (var email in toEmails)
            {
                try
                {
                    var message = new MailMessage
                    {
                        From = new MailAddress(_fromEmail, _fromName),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true
                    };

                    message.To.Add(email);

                    await client.SendMailAsync(message, cancellationToken);
                    successCount++;

                    _logger.LogDebug("Email відправлено на {Email} ({Current}/{Total})", email, successCount, totalCount);

                    // Невелика затримка між відправками для уникнення блокування
                    await Task.Delay(100, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не вдалося відправити email на {Email}", email);
                }
            }

            _logger.LogInformation("Масова розсилка завершена: {Success}/{Total} успішних відправок", successCount, totalCount);
            
            return successCount > 0; // Повертаємо true, якщо хоча б один email відправлено
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при масовій розсилці");
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
