using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Common.Models;

/// <summary>
/// Локалізаційний ресурс
/// </summary>
public class LocalizationResource
{
    public string Key { get; set; } = string.Empty;
    public Language Language { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}