using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Сервіс для автоматичного призначення апелів адміністраторам
/// </summary>
public interface IAppealAssignmentService
{
    /// <summary>
    /// Автоматично призначити апел найкращому доступному адміністратору
    /// </summary>
    Task<Result<BotUser>> AssignAppealAsync(Appeal appeal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Призначити апел конкретному адміністратору
    /// </summary>
    Task<Result> AssignAppealToAdminAsync(Appeal appeal, long adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Перепризначити апел іншому адміністратору
    /// </summary>
    Task<Result<BotUser>> ReassignAppealAsync(Appeal appeal, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Знайти найкращого адміністратора для апела на основі категорії та навантаження
    /// </summary>
    Task<Result<BotUser>> FindBestAdminForAppealAsync(AppealCategory category, AppealPriority priority, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати список доступних адміністраторів для категорії
    /// </summary>
    Task<Result<IEnumerable<BotUser>>> GetAvailableAdminsForCategoryAsync(AppealCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Перевірити прострочені апели та ескалувати їх
    /// </summary>
    Task<Result<int>> EscalateOverdueAppealsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Оновити навантаження адміністратора після зміни статусу апела
    /// </summary>
    Task<Result> UpdateAdminWorkloadAsync(long adminId, AppealStatus oldStatus, AppealStatus newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Встановити доступність адміністратора
    /// </summary>
    Task<Result> SetAdminAvailabilityAsync(long adminId, bool isAvailable, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати статистику навантаження адміністраторів
    /// </summary>
    Task<Result<AdminWorkloadStats>> GetWorkloadStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Статистика навантаження адміністраторів
/// </summary>
public class AdminWorkloadStats
{
    public int TotalAdmins { get; set; }
    public int AvailableAdmins { get; set; }
    public int TotalActiveAppeals { get; set; }
    public double AverageAppealsPerAdmin { get; set; }
    public AdminWorkloadInfo MostLoadedAdmin { get; set; } = null!;
    public AdminWorkloadInfo LeastLoadedAdmin { get; set; } = null!;
    public List<CategoryWorkloadInfo> CategoryStats { get; set; } = new();
}

/// <summary>
/// Інформація про навантаження конкретного адміністратора
/// </summary>
public class AdminWorkloadInfo
{
    public long AdminId { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public int ActiveAppeals { get; set; }
    public int TotalAppeals { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime LastActivityAt { get; set; }
}

/// <summary>
/// Статистика навантаження по категорії
/// </summary>
public class CategoryWorkloadInfo
{
    public AppealCategory Category { get; set; }
    public int ActiveAppeals { get; set; }
    public int AvailableExperts { get; set; }
    public double AverageExpertiseLevel { get; set; }
}