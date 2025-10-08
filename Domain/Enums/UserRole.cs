namespace StudentUnionBot.Domain.Enums;

/// <summary>
/// Роль користувача в системі
/// </summary>
public enum UserRole
{
    Student = 1,
    Moderator = 2,
    Admin = 3,
    SuperAdmin = 4
}

/// <summary>
/// Методи розширення для UserRole
/// </summary>
public static class UserRoleExtensions
{
    public static string GetDisplayName(this UserRole role)
    {
        return role switch
        {
            UserRole.Student => "Студент",
            UserRole.Moderator => "Модератор",
            UserRole.Admin => "Адміністратор",
            UserRole.SuperAdmin => "Суперадмін",
            _ => "Невідомо"
        };
    }

    public static string GetEmoji(this UserRole role)
    {
        return role switch
        {
            UserRole.Student => "🎓",
            UserRole.Moderator => "👮",
            UserRole.Admin => "👨‍💼",
            UserRole.SuperAdmin => "👑",
            _ => "❓"
        };
    }
}
