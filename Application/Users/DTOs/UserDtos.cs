namespace StudentUnionBot.Application.Users.DTOs;

/// <summary>
/// DTO для відображення інформації про користувача
/// </summary>
public class UserDto
{
    public long TelegramId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? Faculty { get; set; }
    public int? Course { get; set; }
    public string? Group { get; set; }
    public string? Email { get; set; }
    public bool IsEmailVerified { get; set; }
    public string Language { get; set; } = "uk";
    public DateTime JoinedAt { get; set; }
    public bool IsActive { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

/// <summary>
/// DTO для профілю користувача з додатковою статистикою
/// </summary>
public class UserProfileDto
{
    public long TelegramId { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public string? Faculty { get; set; }
    public int? Course { get; set; }
    public string? Group { get; set; }
    public string? Email { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public int TotalAppeals { get; set; }
    public int ActiveAppeals { get; set; }
    public int ResolvedAppeals { get; set; }
}
