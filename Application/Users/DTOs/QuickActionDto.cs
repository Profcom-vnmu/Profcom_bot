using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Users.DTOs;

/// <summary>
/// DTO для швидких дій користувача
/// </summary>
public class QuickActionDto
{
    public string Title { get; set; } = string.Empty;
    public string CallbackData { get; set; } = string.Empty;
    public string? Description { get; set; }
    public QuickActionType Type { get; set; }
    public int Priority { get; set; }
    public string Emoji { get; set; } = string.Empty;
}
