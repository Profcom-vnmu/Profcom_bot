using Microsoft.EntityFrameworkCore;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Infrastructure.Data;

namespace StudentUnionBot.Infrastructure.Repositories;

/// <summary>
/// Репозиторій для роботи з навантаженням адміністраторів
/// </summary>
public class AdminWorkloadRepository : BaseRepository<AdminWorkload>, IAdminWorkloadRepository
{
    public AdminWorkloadRepository(BotDbContext context) : base(context)
    {
    }

    public async Task<AdminWorkload?> GetByAdminIdAsync(long adminId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(w => w.Admin)
            .Include(w => w.CategoryExpertises)
            .FirstOrDefaultAsync(w => w.AdminId == adminId, cancellationToken);
    }

    public async Task<IEnumerable<AdminWorkload>> GetAvailableAdminsOrderedByWorkloadAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(w => w.Admin)
            .Include(w => w.CategoryExpertises)
            .Where(w => w.IsAvailable && 
                       (w.Admin.Role == UserRole.Admin || w.Admin.Role == UserRole.SuperAdmin))
            .OrderBy(w => w.ActiveAppealsCount)
            .ThenByDescending(w => w.LastActivityAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AdminWorkload>> GetAdminsWithCategoryExpertiseAsync(AppealCategory category, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(w => w.Admin)
            .Include(w => w.CategoryExpertises)
            .Where(w => w.IsAvailable &&
                       (w.Admin.Role == UserRole.Admin || w.Admin.Role == UserRole.SuperAdmin) &&
                       w.CategoryExpertises.Any(e => e.Category == category))
            .OrderBy(w => w.ActiveAppealsCount)
            .ThenByDescending(w => w.CategoryExpertises
                .Where(e => e.Category == category)
                .Max(e => e.ExperienceLevel))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminWorkload?> GetBestAvailableAdminForCategoryAsync(AppealCategory category, CancellationToken cancellationToken = default)
    {
        // Спочатку шукаємо адмінів з експертизою в категорії
        var adminWithExpertise = await DbSet
            .Include(w => w.Admin)
            .Include(w => w.CategoryExpertises)
            .Where(w => w.IsAvailable &&
                       (w.Admin.Role == UserRole.Admin || w.Admin.Role == UserRole.SuperAdmin) &&
                       w.CategoryExpertises.Any(e => e.Category == category))
            .OrderByDescending(w => w.CategoryExpertises
                .Where(e => e.Category == category)
                .Max(e => e.CalculateExpertiseScore()))
            .ThenBy(w => w.ActiveAppealsCount)
            .FirstOrDefaultAsync(cancellationToken);

        if (adminWithExpertise != null)
        {
            return adminWithExpertise;
        }

        // Якщо немає експертів, беремо адміна з найменшим навантаженням
        return await DbSet
            .Include(w => w.Admin)
            .Include(w => w.CategoryExpertises)
            .Where(w => w.IsAvailable &&
                       (w.Admin.Role == UserRole.Admin || w.Admin.Role == UserRole.SuperAdmin))
            .OrderBy(w => w.ActiveAppealsCount)
            .ThenByDescending(w => w.LastActivityAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<AdminWorkload>> GetWorkloadStatsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(w => w.Admin)
            .Include(w => w.CategoryExpertises)
            .Where(w => w.Admin.Role == UserRole.Admin || w.Admin.Role == UserRole.SuperAdmin)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminCategoryExpertise?> GetCategoryExpertiseAsync(long adminId, AppealCategory category, CancellationToken cancellationToken = default)
    {
        return await Context.Set<AdminCategoryExpertise>()
            .FirstOrDefaultAsync(e => e.AdminId == adminId && e.Category == category, cancellationToken);
    }

    public async Task<AdminCategoryExpertise> CreateOrUpdateExpertiseAsync(long adminId, AppealCategory category, int experienceLevel, CancellationToken cancellationToken = default)
    {
        var expertise = await GetCategoryExpertiseAsync(adminId, category, cancellationToken);

        if (expertise == null)
        {
            expertise = AdminCategoryExpertise.Create(adminId, category, experienceLevel);
            Context.Set<AdminCategoryExpertise>().Add(expertise);
        }
        else
        {
            expertise.SetExperienceLevel(experienceLevel);
        }

        return expertise;
    }

    public async Task<IEnumerable<AdminCategoryExpertise>> GetAdminExpertisesAsync(long adminId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<AdminCategoryExpertise>()
            .Where(e => e.AdminId == adminId)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateExpertiseStatsAsync(long adminId, AppealCategory category, bool isSuccessful, CancellationToken cancellationToken = default)
    {
        var expertise = await GetCategoryExpertiseAsync(adminId, category, cancellationToken);
        
        if (expertise != null)
        {
            expertise.RecordResolution(isSuccessful);
        }
        else
        {
            // Створюємо базову експертизу якщо не існує
            expertise = AdminCategoryExpertise.Create(adminId, category, 1);
            expertise.RecordResolution(isSuccessful);
            Context.Set<AdminCategoryExpertise>().Add(expertise);
        }
    }
}