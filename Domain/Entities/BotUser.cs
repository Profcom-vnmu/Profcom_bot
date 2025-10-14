using StudentUnionBot.Core.Exceptions;
using StudentUnionBot.Domain.Enums;
using Core;

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
    public Language Language { get; private set; } = Language.Ukrainian;
    public string? TimeZone { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsBanned { get; private set; }
    public string? BanReason { get; private set; }
    public DateTime? ProfileUpdatedAt { get; private set; }
    public DateTime? LastActivityAt { get; private set; }
    public UserRole Role { get; private set; }
    public TutorialStep TutorialStep { get; private set; } = TutorialStep.NotStarted;

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
        Language? language = null)
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
            Language = language ?? Language.Ukrainian
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
    /// Встановлення email для верифікації
    /// </summary>
    public void SetEmailForVerification(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email не може бути порожнім");

        Email = email;
        IsEmailVerified = false;
    }

    /// <summary>
    /// Генерація коду верифікації email
    /// </summary>
    public string GenerateVerificationCode()
    {
        var random = new Random();
        VerificationCode = random.Next(100000, 999999).ToString();
        VerificationCodeExpiry = AppTime.Now.AddMinutes(15); // Використовуємо Kyiv timezone
        return VerificationCode;
    }

    /// <summary>
    /// Верифікація email за кодом
    /// </summary>
    public bool VerifyEmail(string code)
    {
        if (string.IsNullOrWhiteSpace(VerificationCode))
            return false;

        if (VerificationCodeExpiry < AppTime.Now) // Використовуємо Kyiv timezone
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
    public void SetLanguage(Language language)
    {
        Language = language;
    }

    /// <summary>
    /// Оновлення профілю користувача
    /// </summary>
    public void UpdateProfile(string? fullName, string? faculty, int? course, string? group)
    {
        if (fullName != null)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new DomainException("Повне ім'я не може бути порожнім");
            if (fullName.Length > 100)
                throw new DomainException("Повне ім'я не може бути довшим за 100 символів");
            
            FullName = fullName;
        }

        if (faculty != null)
        {
            if (string.IsNullOrWhiteSpace(faculty))
                throw new DomainException("Факультет не може бути порожнім");
            if (faculty.Length > 200)
                throw new DomainException("Назва факультету не може бути довшою за 200 символів");
            
            Faculty = faculty;
        }

        if (course.HasValue)
        {
            if (course.Value < 1 || course.Value > 6)
                throw new DomainException("Курс має бути від 1 до 6");
            
            Course = course.Value;
        }

        if (group != null)
        {
            if (string.IsNullOrWhiteSpace(group))
                throw new DomainException("Група не може бути порожньою");
            if (group.Length > 50)
                throw new DomainException("Назва групи не може бути довшою за 50 символів");
            
            Group = group;
        }

        ProfileUpdatedAt = DateTime.UtcNow;
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
    public void UpdateBasicInfo(string? username, string? firstName, string? lastName, Language? language = null)
    {
        Username = username;
        FirstName = firstName;
        LastName = lastName;
        FullName = BuildFullName(firstName, lastName);
        
        if (language.HasValue)
        {
            Language = language.Value;
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

    /// <summary>
    /// Оновлення прогресу туторіалу
    /// </summary>
    public void UpdateTutorialProgress(TutorialStep step)
    {
        if (!Enum.IsDefined(typeof(TutorialStep), step))
            throw new DomainException("Невалідний крок туторіалу");
        
        TutorialStep = step;
        LastActivityAt = DateTime.UtcNow;
    }

    private static string? BuildFullName(string? firstName, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
            return null;

        return $"{firstName} {lastName}".Trim();
    }
}
