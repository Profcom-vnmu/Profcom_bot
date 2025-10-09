using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Appeals.DTOs;

/// <summary>
/// DTO для відображення звернення
/// </summary>
public class AppealDto
{
    public int Id { get; set; }
    public long StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public AppealCategory Category { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AppealStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public AppealPriority Priority { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public long? AssignedToAdminId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int MessageCount { get; set; }
}

/// <summary>
/// DTO для списку звернень з пагінацією
/// </summary>
public class AppealListDto
{
    public List<AppealDto> Appeals { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// DTO для повідомлення у зверненні
/// </summary>
public class AppealMessageDto
{
    public int Id { get; set; }
    public int AppealId { get; set; }
    public long SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public bool IsFromAdmin { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? PhotoFileId { get; set; }
    public string? DocumentFileId { get; set; }
    public string? DocumentFileName { get; set; }
    public DateTime SentAt { get; set; }
}

/// <summary>
/// DTO з повною інформацією про звернення (включно з повідомленнями)
/// </summary>
public class AppealDetailsDto
{
    public int Id { get; set; }
    public long StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public AppealCategory Category { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AppealStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public AppealPriority Priority { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public long? AssignedToAdminId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public long? ClosedBy { get; set; }
    public string? ClosedReason { get; set; }
    public List<AppealMessageDto> Messages { get; set; } = new();
}
