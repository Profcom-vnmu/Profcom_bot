using StudentUnionBot.Domain.Entities;

namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Репозиторій для роботи з контактною інформацією
/// </summary>
public interface IContactInfoRepository : IRepository<ContactInfo>
{
    /// <summary>
    /// Отримати всі активні контакти відсортовані за порядком відображення
    /// </summary>
    Task<List<ContactInfo>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
