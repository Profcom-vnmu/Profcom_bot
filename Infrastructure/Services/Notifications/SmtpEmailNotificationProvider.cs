using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Infrastructure.Services.Notifications;

/// <summary>
/// Провайдер для відправки Email сповіщень через SMTP
/// </summary>
public class SmtpEmailNotificationProvider : IEmailNotificationProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailNotificationProvider> _logger;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _enableSsl;

    public SmtpEmailNotificationProvider(
        IConfiguration configuration,
        ILogger<SmtpEmailNotificationProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _smtpHost = configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        _smtpPort = configuration.GetValue<int>("Email:SmtpPort", 587);
        _smtpUsername = configuration["Email:SmtpUsername"] ?? "";
        _smtpPassword = configuration["Email:SmtpPassword"] ?? "";
        _fromEmail = configuration["Email:FromEmail"] ?? _smtpUsername;
        _fromName = configuration["Email:FromName"] ?? "StudentUnionBot";
        _enableSsl = configuration.GetValue<bool>("Email:EnableSsl", true);
    }

    public async Task<Result> SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
            {
                _logger.LogWarning("Email credentials not configured");
                return Result.Fail("Email не налаштовано");
            }

            using var message = new MailMessage();
            message.From = new MailAddress(_fromEmail, _fromName);
            message.To.Add(to);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isHtml;

            using var smtpClient = new SmtpClient(_smtpHost, _smtpPort);
            smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
            smtpClient.EnableSsl = _enableSsl;

            await smtpClient.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email відправлено до {To} з темою '{Subject}'", to, subject);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка відправки email до {To}", to);
            return Result.Fail($"Не вдалося відправити email: {ex.Message}");
        }
    }

    public async Task<Result> SendTemplatedEmailAsync(string to, string templateName, Dictionary<string, string> templateData, CancellationToken cancellationToken = default)
    {
        try
        {
            // Load template from file
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "EmailTemplates", $"{templateName}.html");
            
            if (!File.Exists(templatePath))
            {
                _logger.LogWarning("Email template not found: {TemplateName}", templateName);
                // Fallback to basic template
                var fallbackSubject = templateData.ContainsKey("Subject") ? templateData["Subject"] : "Сповіщення від StudentUnionBot";
                var body = BuildBasicEmailBody(templateName, templateData);
                return await SendEmailAsync(to, fallbackSubject, body, true, cancellationToken);
            }

            // Read template content
            var templateContent = await File.ReadAllTextAsync(templatePath, cancellationToken);
            
            // Add common variables
            if (!templateData.ContainsKey("Year"))
                templateData["Year"] = DateTime.UtcNow.Year.ToString();

            // Replace all placeholders
            var processedContent = ProcessTemplate(templateContent, templateData);
            
            var emailSubject = templateData.ContainsKey("Subject") ? templateData["Subject"] : "Сповіщення від StudentUnionBot";

            return await SendEmailAsync(to, emailSubject, processedContent, true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка відправки email з шаблону {TemplateName} до {To}", templateName, to);
            return Result.Fail($"Не вдалося відправити email: {ex.Message}");
        }
    }

    private string ProcessTemplate(string template, Dictionary<string, string> data)
    {
        var result = template;
        
        foreach (var kvp in data)
        {
            var placeholder = $"{{{{{kvp.Key}}}}}";
            result = result.Replace(placeholder, kvp.Value);
        }

        return result;
    }

    private string BuildBasicEmailBody(string templateName, Dictionary<string, string> data)
    {
        // Simple HTML email template
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #777; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>StudentUnionBot</h1>
        </div>
        <div class='content'>
            {ProcessTemplateData(data)}
        </div>
        <div class='footer'>
            <p>Це автоматичне повідомлення. Будь ласка, не відповідайте на цей email.</p>
        </div>
    </div>
</body>
</html>";

        return body;
    }

    private string ProcessTemplateData(Dictionary<string, string> data)
    {
        var content = "";
        
        foreach (var kvp in data)
        {
            if (kvp.Key != "Subject")
            {
                content += $"<p><strong>{kvp.Key}:</strong> {kvp.Value}</p>";
            }
        }

        return content;
    }
}
