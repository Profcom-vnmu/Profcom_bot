using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Events.DTOs;

/// <summary>
/// DTO події для відображення
/// </summary>
public class EventDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EventType Type { get; set; }
    public string TypeDisplayName => Type.GetDisplayName();
    public string TypeEmoji => Type.GetEmoji();
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Location { get; set; }
    public int? MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public bool RequiresRegistration { get; set; }
    public DateTime? RegistrationDeadline { get; set; }
    public EventStatus Status { get; set; }
    public bool IsFeatured { get; set; }
    public string? PhotoFileId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO списку подій з пагінацією
/// </summary>
public class EventListDto
{
    public List<EventDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
