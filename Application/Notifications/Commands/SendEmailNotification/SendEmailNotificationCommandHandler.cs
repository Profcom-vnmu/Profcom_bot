using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Notifications.Commands.SendEmailNotification;

/// <summary>
/// Handler для відправки email повідомлень
/// </summary>
public class SendEmailNotificationCommandHandler : IRequestHandler<SendEmailNotificationCommand, Result<bool>>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendEmailNotificationCommandHandler> _logger;

    public SendEmailNotificationCommandHandler(
        IEmailService emailService,
        ILogger<SendEmailNotificationCommandHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Sending email notification of type {Type} to {EmailCount} recipients",
                request.Type,
                request.ToEmails.Count
            );

            bool result = request.Type switch
            {
                EmailNotificationType.EmailVerification => await SendVerificationEmail(request, cancellationToken),
                EmailNotificationType.NewAppeal => await SendNewAppealEmail(request, cancellationToken),
                EmailNotificationType.AppealReply => await SendAppealReplyEmail(request, cancellationToken),
                EmailNotificationType.NewsNotification => await SendNewsNotificationEmail(request, cancellationToken),
                EmailNotificationType.EventNotification => await SendEventNotificationEmail(request, cancellationToken),
                EmailNotificationType.EventReminder => await SendEventReminderEmail(request, cancellationToken),
                EmailNotificationType.EventRegistrationConfirmation => await SendEventRegistrationConfirmationEmail(request, cancellationToken),
                EmailNotificationType.CustomTemplate => await SendCustomTemplateEmail(request, cancellationToken),
                EmailNotificationType.CustomHtml => await SendCustomHtmlEmail(request, cancellationToken),
                _ => throw new NotSupportedException($"Email notification type {request.Type} is not supported")
            };

            if (result)
            {
                _logger.LogInformation(
                    "Successfully sent email notification of type {Type}",
                    request.Type
                );
                return Result<bool>.Ok(true);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send email notification of type {Type}",
                    request.Type
                );
                return Result<bool>.Fail("Помилка при відправці email повідомлення");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending email notification of type {Type}",
                request.Type
            );
            return Result<bool>.Fail("Помилка при обробці email повідомлення");
        }
    }

    private async Task<bool> SendVerificationEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        var toEmail = request.ToEmails.FirstOrDefault();
        if (string.IsNullOrEmpty(toEmail))
            return false;

        var code = request.TemplateData.GetValueOrDefault("VerificationCode")?.ToString() ?? "";
        
        return await _emailService.SendVerificationCodeAsync(toEmail, code, cancellationToken);
    }

    private async Task<bool> SendNewAppealEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        var toEmail = request.ToEmails.FirstOrDefault();
        if (string.IsNullOrEmpty(toEmail))
            return false;

        var appealId = Convert.ToInt32(request.TemplateData.GetValueOrDefault("AppealId", 0));
        var subject = request.TemplateData.GetValueOrDefault("Subject")?.ToString() ?? "";
        
        return await _emailService.SendNewAppealNotificationAsync(toEmail, appealId, subject, cancellationToken);
    }

    private async Task<bool> SendAppealReplyEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        var toEmail = request.ToEmails.FirstOrDefault();
        if (string.IsNullOrEmpty(toEmail))
            return false;

        var appealId = Convert.ToInt32(request.TemplateData.GetValueOrDefault("AppealId", 0));
        var replyText = request.TemplateData.GetValueOrDefault("ReplyText")?.ToString() ?? "";
        
        return await _emailService.SendAppealReplyNotificationAsync(toEmail, appealId, replyText, cancellationToken);
    }

    private async Task<bool> SendNewsNotificationEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        var newsTitle = request.TemplateData.GetValueOrDefault("NewsTitle")?.ToString() ?? "";
        var newsSummary = request.TemplateData.GetValueOrDefault("NewsSummary")?.ToString() ?? "";
        var newsUrl = request.TemplateData.GetValueOrDefault("NewsUrl")?.ToString() ?? "";

        if (request.ToEmails.Count == 1)
        {
            return await _emailService.SendNewsNotificationAsync(request.ToEmails[0], newsTitle, newsSummary, newsUrl, cancellationToken);
        }
        else
        {
            // Масова розсилка - використовуємо спеціальний метод
            var subject = $"📰 Нова новина: {newsTitle}";
            var htmlBody = GenerateNewsEmailHtml(newsTitle, newsSummary, newsUrl);
            return await _emailService.SendBulkEmailAsync(request.ToEmails, subject, htmlBody, cancellationToken);
        }
    }

    private async Task<bool> SendEventNotificationEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        var eventTitle = request.TemplateData.GetValueOrDefault("EventTitle")?.ToString() ?? "";
        var eventDate = Convert.ToDateTime(request.TemplateData.GetValueOrDefault("EventDate", DateTime.Now));
        var eventLocation = request.TemplateData.GetValueOrDefault("EventLocation")?.ToString() ?? "";
        var eventUrl = request.TemplateData.GetValueOrDefault("EventUrl")?.ToString() ?? "";

        if (request.ToEmails.Count == 1)
        {
            return await _emailService.SendEventNotificationAsync(request.ToEmails[0], eventTitle, eventDate, eventLocation, eventUrl, cancellationToken);
        }
        else
        {
            // Масова розсилка
            var subject = $"🎉 Нова подія: {eventTitle}";
            var htmlBody = GenerateEventEmailHtml(eventTitle, eventDate, eventLocation, eventUrl);
            return await _emailService.SendBulkEmailAsync(request.ToEmails, subject, htmlBody, cancellationToken);
        }
    }

    private async Task<bool> SendEventReminderEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        var eventTitle = request.TemplateData.GetValueOrDefault("EventTitle")?.ToString() ?? "";
        var eventDate = Convert.ToDateTime(request.TemplateData.GetValueOrDefault("EventDate", DateTime.Now));
        var eventLocation = request.TemplateData.GetValueOrDefault("EventLocation")?.ToString() ?? "";

        var success = true;
        foreach (var email in request.ToEmails)
        {
            var result = await _emailService.SendEventReminderAsync(email, eventTitle, eventDate, eventLocation, cancellationToken);
            if (!result)
                success = false;
        }
        
        return success;
    }

    private async Task<bool> SendEventRegistrationConfirmationEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        var toEmail = request.ToEmails.FirstOrDefault();
        if (string.IsNullOrEmpty(toEmail))
            return false;

        var eventTitle = request.TemplateData.GetValueOrDefault("EventTitle")?.ToString() ?? "";
        var eventDate = Convert.ToDateTime(request.TemplateData.GetValueOrDefault("EventDate", DateTime.Now));
        var eventLocation = request.TemplateData.GetValueOrDefault("EventLocation")?.ToString() ?? "";
        
        return await _emailService.SendEventRegistrationConfirmationAsync(toEmail, eventTitle, eventDate, eventLocation, cancellationToken);
    }

    private async Task<bool> SendCustomTemplateEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        var templateName = request.TemplateData.GetValueOrDefault("TemplateName")?.ToString() ?? "";
        
        var success = true;
        foreach (var email in request.ToEmails)
        {
            var result = await _emailService.SendTemplateEmailAsync(email, templateName, request.TemplateData, cancellationToken);
            if (!result)
                success = false;
        }
        
        return success;
    }

    private async Task<bool> SendCustomHtmlEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.CustomSubject) || string.IsNullOrEmpty(request.CustomHtmlBody))
            return false;
            
        return await _emailService.SendBulkEmailAsync(request.ToEmails, request.CustomSubject, request.CustomHtmlBody, cancellationToken);
    }

    #region HTML Email Templates

    /// <summary>
    /// Генерує HTML для email про нову новину використовуючи існуючий шаблон
    /// </summary>
    private string GenerateNewsEmailHtml(string title, string summary, string url)
    {
        // Завантажуємо шаблон з ресурсів
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "EmailTemplates", "NewsNotification.html");
        
        if (!File.Exists(templatePath))
        {
            _logger.LogWarning("News email template not found at {Path}, using fallback", templatePath);
            return GenerateFallbackNewsHtml(title, summary, url);
        }

        var template = File.ReadAllText(templatePath);
        
        // Замінюємо плейсхолдери
        var html = template
            .Replace("{{NewsTitle}}", title)
            .Replace("{{NewsSummary}}", summary)
            .Replace("{{NewsUrl}}", url ?? "#")
            .Replace("{{NewsCategory}}", "Загальне")
            .Replace("{{PublishDate}}", DateTime.Now.ToString("dd MMMM yyyy, HH:mm", new System.Globalization.CultureInfo("uk-UA")))
            .Replace("{{Year}}", DateTime.Now.Year.ToString());

        return html;
    }

    /// <summary>
    /// Генерує HTML для email про нову подію використовуючи існуючий шаблон
    /// </summary>
    private string GenerateEventEmailHtml(string title, DateTime eventDate, string location, string url)
    {
        // Завантажуємо шаблон з ресурсів
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "EmailTemplates", "EventNotification.html");
        
        if (!File.Exists(templatePath))
        {
            _logger.LogWarning("Event email template not found at {Path}, using fallback", templatePath);
            return GenerateFallbackEventHtml(title, eventDate, location, url);
        }

        var template = File.ReadAllText(templatePath);
        
        // Замінюємо плейсхолдери (спрощена версія без {{#if}})
        var html = template
            .Replace("{{EventTitle}}", title)
            .Replace("{{EventDate}}", eventDate.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("uk-UA")))
            .Replace("{{EventTime}}", eventDate.ToString("HH:mm"))
            .Replace("{{EventLocation}}", location)
            .Replace("{{EventCategory}}", "Загальне")
            .Replace("{{EventUrl}}", url ?? "#")
            .Replace("{{Year}}", DateTime.Now.Year.ToString());

        // Видаляємо блоки з {{#if}} так як у нас немає Handlebars engine
        html = System.Text.RegularExpressions.Regex.Replace(html, @"\{\{#if.*?\}\}.*?\{\{/if\}\}", "", 
            System.Text.RegularExpressions.RegexOptions.Singleline);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"\{\{#unless.*?\}\}.*?\{\{/unless\}\}", "",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        return html;
    }

    /// <summary>
    /// Fallback HTML шаблон для новин якщо файл не знайдено
    /// </summary>
    private string GenerateFallbackNewsHtml(string title, string summary, string url)
    {
        return $@"<!DOCTYPE html><html><body style='font-family: Arial; padding: 20px;'>
            <h2 style='color: #28a745;'>📰 {title}</h2>
            <p>{summary}</p>
            {(string.IsNullOrEmpty(url) ? "" : $"<a href='{url}' style='color: #28a745;'>Читати повністю →</a>")}
            <hr><p style='color: #999; font-size: 12px;'>© {DateTime.Now.Year} Профком ВНМУ</p>
            </body></html>";
    }

    /// <summary>
    /// Fallback HTML шаблон для подій якщо файл не знайдено
    /// </summary>
    private string GenerateFallbackEventHtml(string title, DateTime eventDate, string location, string url)
    {
        return $@"<!DOCTYPE html><html><body style='font-family: Arial; padding: 20px;'>
            <h2 style='color: #007bff;'>🎉 {title}</h2>
            <p><strong>📅 Дата:</strong> {eventDate:dd MMMM yyyy, HH:mm}</p>
            <p><strong>📍 Місце:</strong> {location}</p>
            {(string.IsNullOrEmpty(url) ? "" : $"<a href='{url}' style='color: #007bff;'>Детальніше →</a>")}
            <hr><p style='color: #999; font-size: 12px;'>© {DateTime.Now.Year} Профком ВНМУ</p>
            </body></html>";
    }

    #endregion
}