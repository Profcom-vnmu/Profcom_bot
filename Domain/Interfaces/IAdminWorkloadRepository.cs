using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Репозиторій для роботи з навантаженням адміністраторів
/// </summary>
public interface IAdminWorkloadRepository : IRepository<AdminWorkload>
{
    /// <summary>
    /// Отримати навантаження адміністратора за ID
    /// </summary>
    Task<AdminWorkload?> GetByAdminIdAsync(long adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати доступних адміністраторів з найменшим навантаженням
    /// </summary>
    Task<IEnumerable<AdminWorkload>> GetAvailableAdminsOrderedByWorkloadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати адміністраторів з експертизою в категорії
    /// </summary>
    Task<IEnumerable<AdminWorkload>> GetAdminsWithCategoryExpertiseAsync(AppealCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати найкращого доступного адміністратора для категорії
    /// </summary>
    Task<AdminWorkload?> GetBestAvailableAdminForCategoryAsync(AppealCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати статистику навантаження всіх адміністраторів
    /// </summary>
    Task<IEnumerable<AdminWorkload>> GetWorkloadStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати експертизу адміністратора по категорії
    /// </summary>
    Task<AdminCategoryExpertise?> GetCategoryExpertiseAsync(long adminId, AppealCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Створити або оновити експертизу адміністратора
    /// </summary>
    Task<AdminCategoryExpertise> CreateOrUpdateExpertiseAsync(long adminId, AppealCategory category, int experienceLevel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати всю експертизу адміністратора
    /// </summary>
    Task<IEnumerable<AdminCategoryExpertise>> GetAdminExpertisesAsync(long adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Оновити статистику експертизи після вирішення апела
    /// </summary>
    Task UpdateExpertiseStatsAsync(long adminId, AppealCategory category, bool isSuccessful, CancellationToken cancellationToken = default);
}