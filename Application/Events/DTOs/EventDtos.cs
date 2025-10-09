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
    public string? Summary { get; set; }
    public EventType Type { get; set; }
    public EventCategory Category { get; set; }
    public string TypeDisplayName => Type.GetDisplayName();
    public string TypeEmoji => Type.GetEmoji();
    public string CategoryDisplayName => Category.GetDisplayName();
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public int? MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public bool RequiresRegistration { get; set; }
    public DateTime? RegistrationDeadline { get; set; }
    public EventStatus Status { get; set; }
    public bool IsFeatured { get; set; }
    public string? PhotoFileId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Нові поля
    public long OrganizerId { get; set; }
    public string OrganizerName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "UAH";
    public string? Requirements { get; set; }
    public string? ContactInfo { get; set; }
    public string? Tags { get; set; }
    public Language Language { get; set; } = Language.Ukrainian;
    public List<string> AttachmentFileIds { get; set; } = new();
    
    // Computed properties
    public string PriceDisplay => Price == 0 ? "Безкоштовно" : $"{Price} {Currency}";
    public bool IsRegistrationOpen => RequiresRegistration && 
        (RegistrationDeadline == null || RegistrationDeadline > DateTime.UtcNow) &&
        (MaxParticipants == null || CurrentParticipants < MaxParticipants) &&
        Status == EventStatus.Published;
    public string ContentPreview => Summary ?? 
        (Description.Length > 150 ? Description[..150] + "..." : Description);
    public bool IsUpcoming => StartDate > DateTime.UtcNow;
    public bool IsOngoing => StartDate <= DateTime.UtcNow && (EndDate == null || EndDate >= DateTime.UtcNow);
    public bool IsPast => EndDate?.Date < DateTime.UtcNow.Date || (!EndDate.HasValue && StartDate.Date < DateTime.UtcNow.Date);
}

/// <summary>
/// DTO деталей події з інформацією про реєстрацію користувача
/// </summary>
public class EventDetailsDto : EventDto
{
    public bool IsUserRegistered { get; set; }
    public bool CanRegister { get; set; }
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
