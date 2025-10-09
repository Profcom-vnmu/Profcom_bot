using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Entities;

/// <summary>
/// Експертиза адміністраторів по категоріях апелів для кращого призначення
/// </summary>
public class AdminCategoryExpertise
{
    public int Id { get; private set; }
    public long AdminId { get; private set; }
    public AppealCategory Category { get; private set; }
    public int ExperienceLevel { get; private set; } // 1-5, де 5 - найвищий рівень
    public int SuccessfulResolutions { get; private set; }
    public int TotalResolutions { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Навігаційні властивості
    public BotUser Admin { get; private set; } = null!;
    public AdminWorkload AdminWorkload { get; private set; } = null!;

    // Конструктор для EF Core
    private AdminCategoryExpertise() { }

    /// <summary>
    /// Створити запис експертизи адміністратора по категорії
    /// </summary>
    public static AdminCategoryExpertise Create(long adminId, AppealCategory category, int experienceLevel = 1)
    {
        if (experienceLevel < 1 || experienceLevel > 5)
            throw new ArgumentException("Experience level must be between 1 and 5", nameof(experienceLevel));

        return new AdminCategoryExpertise
        {
            AdminId = adminId,
            Category = category,
            ExperienceLevel = experienceLevel,
            SuccessfulResolutions = 0,
            TotalResolutions = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Оновити статистику після вирішення апела
    /// </summary>
    public void RecordResolution(bool isSuccessful)
    {
        TotalResolutions++;
        if (isSuccessful)
        {
            SuccessfulResolutions++;
        }
        
        // Автоматично підвищуємо рівень експертизи при хороших результатах
        UpdateExperienceLevel();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Встановити рівень експертизи
    /// </summary>
    public void SetExperienceLevel(int level)
    {
        if (level < 1 || level > 5)
            throw new ArgumentException("Experience level must be between 1 and 5", nameof(level));

        ExperienceLevel = level;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Розрахувати успішність вирішень (0.0 - 1.0)
    /// </summary>
    public double GetSuccessRate()
    {
        return TotalResolutions == 0 ? 0.0 : (double)SuccessfulResolutions / TotalResolutions;
    }

    /// <summary>
    /// Розрахувати скор експертизи для призначення
    /// </summary>
    public int CalculateExpertiseScore()
    {
        var baseScore = ExperienceLevel * 20;
        var successBonus = (int)(GetSuccessRate() * 30);
        var experienceBonus = Math.Min(TotalResolutions, 10) * 2; // До 20 балів за досвід

        return baseScore + successBonus + experienceBonus;
    }

    private void UpdateExperienceLevel()
    {
        if (TotalResolutions < 5) return;

        var successRate = GetSuccessRate();
        var newLevel = successRate switch
        {
            >= 0.9 when TotalResolutions >= 20 => 5,
            >= 0.8 when TotalResolutions >= 15 => 4,
            >= 0.7 when TotalResolutions >= 10 => 3,
            >= 0.6 when TotalResolutions >= 5 => 2,
            _ => 1
        };

        ExperienceLevel = Math.Max(ExperienceLevel, newLevel);
    }
}