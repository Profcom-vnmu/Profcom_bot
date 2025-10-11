using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Репозиторій для роботи з подіями
/// </summary>
public interface IEventRepository : IRepository<Event>
{
    /// <summary>
    /// Отримати майбутні події
    /// </summary>
    Task<List<Event>> GetUpcomingEventsAsync(
        EventType? type = null,
        bool onlyFeatured = false,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати кількість майбутніх подій
    /// </summary>
    Task<int> GetUpcomingEventsCountAsync(
        EventType? type = null,
        bool onlyFeatured = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати виділені події
    /// </summary>
    Task<List<Event>> GetFeaturedEventsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати подію з учасниками
    /// </summary>
    Task<Event?> GetByIdWithParticipantsAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Отримати всі події для адміністраторів
    /// </summary>
    Task<List<Event>> GetAllEventsAsync(
        EventCategory? category = null,
        EventType? type = null,
        EventStatus? status = null,
        bool sortByDateAsc = true,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Отримати кількість всіх подій для адміністраторів
    /// </summary>
    Task<int> GetAllEventsCountAsync(
        EventCategory? category = null,
        EventType? type = null,
        EventStatus? status = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Отримати події заплановані для публікації (PublishAt <= now && !IsPublished)
    /// </summary>
    Task<IEnumerable<Event>> GetScheduledForPublicationAsync(
        DateTime currentTime,
        CancellationToken cancellationToken = default);
}
