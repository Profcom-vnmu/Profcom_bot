using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Entities;

/// <summary>
/// Відстеження навантаження адміністраторів для автоматичного призначення апелів
/// </summary>
public class AdminWorkload
{
    public int Id { get; private set; }
    public long AdminId { get; private set; }
    public int ActiveAppealsCount { get; private set; }
    public int TotalAppealsCount { get; private set; }
    public bool IsAvailable { get; private set; }
    public DateTime LastAssignedAt { get; private set; }
    public DateTime LastActivityAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Навігаційні властивості
    public BotUser Admin { get; private set; } = null!;
    public ICollection<AdminCategoryExpertise> CategoryExpertises { get; private set; } = new List<AdminCategoryExpertise>();

    // Конструктор для EF Core
    private AdminWorkload() { }

    /// <summary>
    /// Створити запис навантаження для адміністратора
    /// </summary>
    public static AdminWorkload Create(long adminId)
    {
        return new AdminWorkload
        {
            AdminId = adminId,
            ActiveAppealsCount = 0,
            TotalAppealsCount = 0,
            IsAvailable = true,
            LastActivityAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Призначити новий апел адміністратору
    /// </summary>
    public void AssignAppeal()
    {
        ActiveAppealsCount++;
        TotalAppealsCount++;
        LastAssignedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Завершити роботу з апелом
    /// </summary>
    public void CompleteAppeal()
    {
        if (ActiveAppealsCount > 0)
        {
            ActiveAppealsCount--;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Змінити доступність адміністратора
    /// </summary>
    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
        LastActivityAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Оновити активність адміністратора
    /// </summary>
    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Розрахувати пріоритет для призначення (менше значення = вищий пріоритет)
    /// </summary>
    public int CalculateAssignmentPriority()
    {
        if (!IsAvailable) return int.MaxValue;

        // Базовий пріоритет на основі кількості активних апелів
        int priority = ActiveAppealsCount * 100;

        // Бонус за недавню активність (менше 24 годин)
        var hoursSinceActivity = (DateTime.UtcNow - LastActivityAt).TotalHours;
        if (hoursSinceActivity < 24)
        {
            priority -= 50;
        }

        // Штраф за довгу неактивність
        if (hoursSinceActivity > 72)
        {
            priority += 200;
        }

        return Math.Max(0, priority);
    }
}