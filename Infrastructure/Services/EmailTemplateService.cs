using Microsoft.Extensions.Logging;

namespace StudentUnionBot.Infrastructure.Services;

/// <summary>
/// Сервіс для роботи з email шаблонами
/// </summary>
public class EmailTemplateService
{
    private readonly ILogger<EmailTemplateService> _logger;
    private readonly string _templatesPath;

    public EmailTemplateService(ILogger<EmailTemplateService> logger)
    {
        _logger = logger;
        _templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "EmailTemplates");
    }

    /// <summary>
    /// Завантажує шаблон з файлу та заміняє змінні
    /// </summary>
    public async Task<string> ProcessTemplateAsync(string templateName, Dictionary<string, object> variables, CancellationToken cancellationToken = default)
    {
        try
        {
            var templatePath = Path.Combine(_templatesPath, $"{templateName}.html");
            
            if (!File.Exists(templatePath))
            {
                _logger.LogError("Email template not found: {TemplatePath}", templatePath);
                throw new FileNotFoundException($"Email template '{templateName}' not found");
            }

            var templateContent = await File.ReadAllTextAsync(templatePath, cancellationToken);
            
            // Заміняємо змінні в шаблоні
            var processedContent = ProcessVariables(templateContent, variables);
            
            _logger.LogDebug("Successfully processed email template: {TemplateName}", templateName);
            
            return processedContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email template: {TemplateName}", templateName);
            throw;
        }
    }

    /// <summary>
    /// Заміняє змінні в тексті шаблону
    /// </summary>
    private string ProcessVariables(string template, Dictionary<string, object> variables)
    {
        var result = template;
        
        foreach (var variable in variables)
        {
            var placeholder = $"{{{{{variable.Key}}}}}";
            var value = variable.Value?.ToString() ?? string.Empty;
            
            result = result.Replace(placeholder, value);
        }

        // Обробляємо умовні блоки (спрощена версія Handlebars)
        result = ProcessConditionals(result, variables);
        
        // Додаємо загальні змінні
        result = result.Replace("{{Year}}", DateTime.Now.Year.ToString());
        
        return result;
    }

    /// <summary>
    /// Обробляє умовні блоки в шаблоні (спрощена версія {{#if}} {{/if}})
    /// </summary>
    private string ProcessConditionals(string template, Dictionary<string, object> variables)
    {
        // Обробляємо {{#if VariableName}} ... {{/if}}
        var ifPattern = @"\{\{#if\s+(\w+)\}\}(.*?)\{\{/if\}\}";
        var regex = new System.Text.RegularExpressions.Regex(ifPattern, System.Text.RegularExpressions.RegexOptions.Singleline);
        
        return regex.Replace(template, match =>
        {
            var variableName = match.Groups[1].Value;
            var content = match.Groups[2].Value;
            
            if (variables.TryGetValue(variableName, out var value))
            {
                // Повертаємо контент, якщо змінна існує і не null/false/empty
                if (value != null && !value.Equals(false) && !value.Equals(string.Empty))
                {
                    return content;
                }
            }
            
            return string.Empty; // Видаляємо блок, якщо умова не виконується
        });
    }

    /// <summary>
    /// Створює базові змінні для всіх шаблонів
    /// </summary>
    public Dictionary<string, object> CreateBaseVariables(string? userName = null)
    {
        return new Dictionary<string, object>
        {
            ["Year"] = DateTime.Now.Year,
            ["UserName"] = userName ?? "Шановний студенте",
            ["BotName"] = "StudentUnionBot",
            ["OrganizationName"] = "Профспілковий комітет студентів ВНМУ"
        };
    }

    /// <summary>
    /// Створює змінні для шаблону верифікації email
    /// </summary>
    public Dictionary<string, object> CreateVerificationVariables(string userName, string verificationCode, int expiryMinutes = 15)
    {
        var variables = CreateBaseVariables(userName);
        variables["VerificationCode"] = verificationCode;
        variables["ExpiryMinutes"] = expiryMinutes;
        
        return variables;
    }

    /// <summary>
    /// Створює змінні для шаблону новини
    /// </summary>
    public Dictionary<string, object> CreateNewsVariables(string newsTitle, string newsSummary, string newsCategory, string newsUrl)
    {
        var variables = CreateBaseVariables();
        variables["NewsTitle"] = newsTitle;
        variables["NewsSummary"] = newsSummary;
        variables["NewsCategory"] = newsCategory;
        variables["NewsUrl"] = newsUrl;
        variables["PublishDate"] = DateTime.Now.ToString("dd MMMM yyyy, HH:mm");
        
        return variables;
    }

    /// <summary>
    /// Створює змінні для шаблону події
    /// </summary>
    public Dictionary<string, object> CreateEventVariables(
        string eventTitle, 
        DateTime eventDate, 
        string eventLocation, 
        string eventCategory,
        string eventUrl,
        bool requiresRegistration = false,
        DateTime? registrationDeadline = null,
        int? maxParticipants = null,
        int currentParticipants = 0,
        decimal? eventPrice = null)
    {
        var variables = CreateBaseVariables();
        variables["EventTitle"] = eventTitle;
        variables["EventDate"] = eventDate.ToString("dd MMMM yyyy");
        variables["EventTime"] = eventDate.ToString("HH:mm");
        variables["EventLocation"] = eventLocation;
        variables["EventCategory"] = eventCategory;
        variables["EventUrl"] = eventUrl;
        variables["EventRequiresRegistration"] = requiresRegistration;
        
        if (requiresRegistration && registrationDeadline.HasValue)
        {
            variables["RegistrationDeadline"] = registrationDeadline.Value.ToString("dd MMMM yyyy, HH:mm");
        }
        
        if (maxParticipants.HasValue)
        {
            variables["MaxParticipants"] = maxParticipants.Value;
            variables["RemainingSpots"] = Math.Max(0, maxParticipants.Value - currentParticipants);
        }
        
        if (eventPrice.HasValue && eventPrice > 0)
        {
            variables["EventPrice"] = $"{eventPrice} грн";
        }
        
        return variables;
    }

    /// <summary>
    /// Створює змінні для нагадування про подію
    /// </summary>
    public Dictionary<string, object> CreateEventReminderVariables(string eventTitle, DateTime eventDate, string eventLocation)
    {
        var variables = CreateBaseVariables();
        variables["EventTitle"] = eventTitle;
        variables["EventDate"] = eventDate.ToString("dd MMMM yyyy");
        variables["EventTime"] = eventDate.ToString("HH:mm");
        variables["EventLocation"] = eventLocation;
        
        // Розраховуємо час до події
        var timeUntilEvent = eventDate - DateTime.Now;
        if (timeUntilEvent.TotalDays >= 1)
        {
            variables["TimeUntilEvent"] = $"{(int)timeUntilEvent.TotalDays} днів";
        }
        else if (timeUntilEvent.TotalHours >= 1)
        {
            variables["TimeUntilEvent"] = $"{(int)timeUntilEvent.TotalHours} годин";
        }
        else
        {
            variables["TimeUntilEvent"] = $"{(int)timeUntilEvent.TotalMinutes} хвилин";
        }
        
        return variables;
    }
}