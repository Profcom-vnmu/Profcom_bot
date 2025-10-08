using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Partners.DTOs;

/// <summary>
/// DTO партнера для відображення
/// </summary>
public class PartnerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PartnerType Type { get; set; }
    public string TypeDisplayName => Type.GetDisplayName();
    public string TypeEmoji => Type.GetEmoji();
    public string? DiscountInfo { get; set; }
    public string? Website { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? LogoFileId { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO списку партнерів
/// </summary>
public class PartnerListDto
{
    public List<PartnerDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
