using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Інтерфейс репозиторію звернень
/// </summary>
public interface IAppealRepository : IRepository<Appeal>
{
    Task<List<Appeal>> GetActiveAppealsAsync(
        AppealCategory? category = null,
        CancellationToken cancellationToken = default);
    
    Task<List<Appeal>> GetClosedAppealsAsync(
        AppealCategory? category = null,
        CancellationToken cancellationToken = default);
    
    Task<List<Appeal>> GetUserAppealsAsync(
        long userId,
        CancellationToken cancellationToken = default);
    
    Task<Appeal?> GetByIdWithMessagesAsync(
        int id,
        CancellationToken cancellationToken = default);
    
    Task<Appeal?> GetActiveAppealForStudentAsync(
        long studentId,
        CancellationToken cancellationToken = default);
    
    Task<bool> HasActiveAppealAsync(
        long studentId,
        CancellationToken cancellationToken = default);
}
