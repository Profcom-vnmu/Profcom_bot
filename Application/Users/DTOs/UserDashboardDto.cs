namespace StudentUnionBot.Application.Users.DTOs;

/// <summary>
/// DTO для персоналізованого dashboard користувача
/// </summary>
public class UserDashboardDto
{
    public UserDto User { get; set; } = null!;
    public List<QuickActionDto> QuickActions { get; set; } = new();
    public List<NotificationItemDto> RecentNotifications { get; set; } = new();
    public UserStatisticsDto Statistics { get; set; } = new();
    public bool IsNewUser { get; set; }
    public DateTime LastVisit { get; set; }
}

/// <summary>
/// Елемент сповіщення для dashboard
/// </summary>
public class NotificationItemDto
{
    public string Message { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string? CallbackData { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; }
}

/// <summary>
/// Статистика користувача
/// </summary>
public class UserStatisticsDto
{
    public int TotalAppeals { get; set; }
    public int ActiveAppeals { get; set; }
    public int NewReplies { get; set; }
    public int RegisteredEvents { get; set; }
    public int UpcomingEvents { get; set; }
}
