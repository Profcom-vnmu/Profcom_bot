using Microsoft.EntityFrameworkCore;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Infrastructure.Data;

namespace StudentUnionBot.Infrastructure.Repositories;

public class AppealRepository : BaseRepository<Appeal>, IAppealRepository
{
    public AppealRepository(BotDbContext context) : base(context)
    {
    }

    public async Task<List<Appeal>> GetActiveAppealsAsync(
        AppealCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(a => a.Messages)
            .Include(a => a.Student)
            .Where(a => a.Status != AppealStatus.Closed);

        if (category.HasValue)
            query = query.Where(a => a.Category == category.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Appeal>> GetClosedAppealsAsync(
        AppealCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(a => a.Messages)
            .Include(a => a.Student)
            .Where(a => a.Status == AppealStatus.Closed);

        if (category.HasValue)
            query = query.Where(a => a.Category == category.Value);

        return await query
            .OrderByDescending(a => a.ClosedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Appeal>> GetUserAppealsAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(a => a.Messages)
            .Where(a => a.StudentId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Appeal?> GetByIdWithMessagesAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(a => a.Messages.OrderBy(m => m.SentAt))
            .Include(a => a.Student)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Appeal?> GetActiveAppealForStudentAsync(
        long studentId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.Messages)
            .FirstOrDefaultAsync(
                a => a.StudentId == studentId && a.Status != AppealStatus.Closed,
                cancellationToken);
    }

    public async Task<bool> HasActiveAppealAsync(
        long studentId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(
                a => a.StudentId == studentId && a.Status != AppealStatus.Closed,
                cancellationToken);
    }
}
