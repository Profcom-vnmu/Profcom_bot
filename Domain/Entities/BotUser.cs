using StudentUnionBot.Core.Exceptions;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Entities;

/// <summary>
/// Користувач бота (студент)
/// </summary>
public class BotUser
{
    // Private constructor для Entity Framework
    private BotUser() { }

    // Властивості
    public long TelegramId { get; private set; }
    public string? Username { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? FullName { get; private set; }
    public string? Faculty { get; private set; }
    public int? Course { get; private set; }
    public string? Group { get; private set; }
    public string? Email { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public string? VerificationCode { get; private set; }
    public DateTime? VerificationCodeExpiry { get; private set; }
    public string Language { get; private set; } = "uk";
    public string? TimeZone { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsBanned { get; private set; }
    public string? BanReason { get; private set; }
    public DateTime? ProfileUpdatedAt { get; private set; }
    public DateTime? LastActivityAt { get; private set; }
    public UserRole Role { get; private set; }

    // Navigation properties
    public ICollection<Appeal> Appeals { get; private set; } = new List<Appeal>();

    /// <summary>
    /// Фабричний метод для створення нового користувача
    /// </summary>
    public static BotUser Create(
        long telegramId,
        string? username,
        string? firstName,
        string? lastName = null,
        string? language = "uk")
    {
        if (telegramId <= 0)
            throw new DomainException("Telegram ID повинен бути більше 0");

        return new BotUser
        {
            TelegramId = telegramId,
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            FullName = BuildFullName(firstName, lastName),
            JoinedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            IsActive = true,
            Role = UserRole.Student,
            Language = language ?? "uk"
        };
    }

    /// <summary>
    /// Оновлення профілю студента
    /// </summary>
    public void UpdateProfile(string? faculty, int? course, string? group, string? email = null)
    {
        if (course.HasValue && (course < 1 || course > 6))
            throw new DomainException("Курс повинен бути від 1 до 6");

        Faculty = faculty;
        Course = course;
        Group = group;
        
        if (!string.IsNullOrWhiteSpace(email))
        {
            Email = email;
            IsEmailVerified = false; // Потребує повторної верифікації
        }

        ProfileUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Генерація коду верифікації email
    /// </summary>
    public string GenerateVerificationCode()
    {
        var random = new Random();
        VerificationCode = random.Next(100000, 999999).ToString();
        VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(15);
        return VerificationCode;
    }

    /// <summary>
    /// Верифікація email за кодом
    /// </summary>
    public bool VerifyEmail(string code)
    {
        if (string.IsNullOrWhiteSpace(VerificationCode))
            return false;

        if (VerificationCodeExpiry < DateTime.UtcNow)
            return false;

        if (VerificationCode != code)
            return false;

        IsEmailVerified = true;
        VerificationCode = null;
        VerificationCodeExpiry = null;
        return true;
    }

    /// <summary>
    /// Зміна мови інтерфейсу
    /// </summary>
    public void SetLanguage(string language)
    {
        if (language != "uk" && language != "en")
            throw new DomainException("Підтримуються тільки мови: uk, en");

        Language = language;
    }

    /// <summary>
    /// Блокування користувача
    /// </summary>
    public void Ban(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Причина блокування обов'язкова");

        IsBanned = true;
        BanReason = reason;
    }

    /// <summary>
    /// Розблокування користувача
    /// </summary>
    public void Unban()
    {
        IsBanned = false;
        BanReason = null;
    }

    /// <summary>
    /// Оновлення останньої активності
    /// </summary>
    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Зміна ролі користувача
    /// </summary>
    public void PromoteToRole(UserRole role)
    {
        if (Role == UserRole.SuperAdmin && role != UserRole.SuperAdmin)
            throw new DomainException("Не можна знизити роль супер-адміністратора");

        Role = role;
    }

    /// <summary>
    /// Оновлення базової інформації (з Telegram)
    /// </summary>
    public void UpdateBasicInfo(string? username, string? firstName, string? lastName, string? language = null)
    {
        Username = username;
        FirstName = firstName;
        LastName = lastName;
        FullName = BuildFullName(firstName, lastName);
        
        if (!string.IsNullOrWhiteSpace(language))
        {
            Language = language;
        }
        
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Оновлення часу останньої активності
    /// </summary>
    public void UpdateLastActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }

    private static string? BuildFullName(string? firstName, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
            return null;

        return $"{firstName} {lastName}".Trim();
    }
}
