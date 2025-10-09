using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Domain.Entities;

/// <summary>
/// Represents an event organized by the student union
/// </summary>
public class Event
{
    public int Id { get; private set; }
    
    /// <summary>
    /// Event title/name
    /// </summary>
    public string Title { get; private set; } = string.Empty;
    
    /// <summary>
    /// Full description of the event
    /// </summary>
    public string Description { get; private set; } = string.Empty;
    
    /// <summary>
    /// Short summary for previews
    /// </summary>
    public string? Summary { get; private set; }
    
    /// <summary>
    /// Category of the event for organization
    /// </summary>
    public EventCategory Category { get; private set; }
    
    /// <summary>
    /// Type/category of the event
    /// </summary>
    public EventType Type { get; private set; }
    
    /// <summary>
    /// When the event starts
    /// </summary>
    public DateTime StartDate { get; private set; }
    
    /// <summary>
    /// When the event ends (null for single-time events)
    /// </summary>
    public DateTime? EndDate { get; private set; }
    
    /// <summary>
    /// Location/venue of the event
    /// </summary>
    public string? Location { get; private set; }
    
    /// <summary>
    /// Optional address or additional location details
    /// </summary>
    public string? Address { get; private set; }
    
    /// <summary>
    /// Maximum number of participants (null = unlimited)
    /// </summary>
    public int? MaxParticipants { get; private set; }
    
    /// <summary>
    /// Current number of registered participants
    /// </summary>
    public int CurrentParticipants { get; private set; }
    
    /// <summary>
    /// Whether registration is required
    /// </summary>
    public bool RequiresRegistration { get; private set; }
    
    /// <summary>
    /// Deadline for registration (null = no deadline)
    /// </summary>
    public DateTime? RegistrationDeadline { get; private set; }
    
    /// <summary>
    /// Contact person for the event
    /// </summary>
    public string? ContactPerson { get; private set; }
    
    /// <summary>
    /// Contact phone or Telegram
    /// </summary>
    public string? ContactInfo { get; private set; }
    
    /// <summary>
    /// Admin who created the event
    /// </summary>
    public long OrganizerId { get; private set; }
    
    /// <summary>
    /// Name of the organizer
    /// </summary>
    public string OrganizerName { get; private set; } = string.Empty;
    
    /// <summary>
    /// Optional photo/poster file ID from Telegram
    /// </summary>
    public string? PhotoFileId { get; private set; }
    
    /// <summary>
    /// Current status of the event
    /// </summary>
    public EventStatus Status { get; private set; }
    
    /// <summary>
    /// Whether the event is published and visible
    /// </summary>
    public bool IsPublished { get; private set; }
    
    /// <summary>
    /// When the event info was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }
    
    /// <summary>
    /// When the event info was last updated
    /// </summary>
    public DateTime UpdatedAt { get; private set; }
    
    /// <summary>
    /// Whether the event is featured/highlighted
    /// </summary>
    public bool IsFeatured { get; private set; }
    
    /// <summary>
    /// Registered participants navigation property
    /// </summary>
    private readonly List<BotUser> _registeredParticipants = new();
    public IReadOnlyCollection<BotUser> RegisteredParticipants => _registeredParticipants.AsReadOnly();
    
    // Private constructor for EF Core
    private Event() { }
    
    /// <summary>
    /// Factory method to create a new event
    /// </summary>
    public static Event Create(
        string title,
        string description,
        EventCategory category,
        EventType type,
        DateTime startDate,
        long organizerId,
        string organizerName,
        string? summary = null,
        DateTime? endDate = null,
        string? location = null,
        string? address = null,
        int? maxParticipants = null,
        bool requiresRegistration = false,
        DateTime? registrationDeadline = null,
        string? contactPerson = null,
        string? contactInfo = null,
        string? photoFileId = null,
        bool publishImmediately = true)
    {
        var ev = new Event
        {
            Title = title,
            Description = description,
            Summary = summary,
            Category = category,
            Type = type,
            StartDate = startDate,
            EndDate = endDate,
            Location = location,
            Address = address,
            MaxParticipants = maxParticipants,
            CurrentParticipants = 0,
            RequiresRegistration = requiresRegistration,
            RegistrationDeadline = registrationDeadline,
            ContactPerson = contactPerson,
            ContactInfo = contactInfo,
            OrganizerId = organizerId,
            OrganizerName = organizerName,
            PhotoFileId = photoFileId,
            Status = EventStatus.Planned,
            IsPublished = publishImmediately,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsFeatured = false
        };
        
        return ev;
    }
    
    public void Update(
        string title,
        string description,
        EventCategory category,
        EventType type,
        DateTime startDate,
        string? summary = null,
        DateTime? endDate = null,
        string? location = null,
        string? address = null,
        int? maxParticipants = null,
        bool requiresRegistration = false,
        DateTime? registrationDeadline = null,
        string? contactPerson = null,
        string? contactInfo = null,
        string? photoFileId = null)
    {
        Title = title;
        Description = description;
        Summary = summary;
        Category = category;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        Location = location;
        Address = address;
        MaxParticipants = maxParticipants;
        RequiresRegistration = requiresRegistration;
        RegistrationDeadline = registrationDeadline;
        ContactPerson = contactPerson;
        ContactInfo = contactInfo;
        PhotoFileId = photoFileId;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Publish()
    {
        IsPublished = true;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Unpublish()
    {
        IsPublished = false;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Cancel(string reason = "")
    {
        Status = EventStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Complete()
    {
        Status = EventStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Start()
    {
        Status = EventStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Feature()
    {
        IsFeatured = true;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Unfeature()
    {
        IsFeatured = false;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public bool CanRegisterParticipant()
    {
        if (!RequiresRegistration) return false;
        if (!IsPublished) return false;
        if (Status != EventStatus.Planned) return false;
        if (RegistrationDeadline.HasValue && DateTime.UtcNow > RegistrationDeadline) return false;
        if (MaxParticipants.HasValue && CurrentParticipants >= MaxParticipants) return false;
        
        return true;
    }
    
    public Result<bool> RegisterParticipant(BotUser user)
    {
        if (!CanRegisterParticipant())
            return Result<bool>.Fail("Реєстрація на цю подію недоступна");
        
        if (IsUserRegistered(user.TelegramId))
            return Result<bool>.Fail("Ви вже зареєстровані на цю подію");
            
        _registeredParticipants.Add(user);
        CurrentParticipants++;
        UpdatedAt = DateTime.UtcNow;
        
        return Result<bool>.Ok(true);
    }
    
    public Result<bool> UnregisterParticipant(BotUser user)
    {
        if (!IsUserRegistered(user.TelegramId))
            return Result<bool>.Fail("Ви не зареєстровані на цю подію");
        
        var participant = _registeredParticipants.FirstOrDefault(p => p.TelegramId == user.TelegramId);
        if (participant != null)
        {
            _registeredParticipants.Remove(participant);
            if (CurrentParticipants > 0)
            {
                CurrentParticipants--;
            }
            UpdatedAt = DateTime.UtcNow;
        }
        
        return Result<bool>.Ok(true);
    }
    
    public bool IsUserRegistered(long userId)
    {
        return _registeredParticipants.Any(p => p.TelegramId == userId);
    }
}