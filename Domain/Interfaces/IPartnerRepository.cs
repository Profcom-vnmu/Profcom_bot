using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Репозиторій для роботи з партнерами
/// </summary>
public interface IPartnerRepository : IRepository<Partner>
{
    /// <summary>
    /// Отримати активних партнерів
    /// </summary>
    Task<List<Partner>> GetActivePartnersAsync(
        PartnerType? type = null,
        bool onlyFeatured = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати виділених партнерів
    /// </summary>
    Task<List<Partner>> GetFeaturedPartnersAsync(CancellationToken cancellationToken = default);
}
