namespace StudentUnionBot.Application.Contacts.DTOs;

/// <summary>
/// DTO контактної інформації для відображення
/// </summary>
public class ContactDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? TelegramUsername { get; set; }
    public string? Address { get; set; }
    public string? WorkingHours { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO списку контактів
/// </summary>
public class ContactListDto
{
    public List<ContactDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
