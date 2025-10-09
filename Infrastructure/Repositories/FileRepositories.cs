using Microsoft.EntityFrameworkCore;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Infrastructure.Data;

namespace StudentUnionBot.Infrastructure.Repositories;

/// <summary>
/// Репозиторій для роботи з файлами
/// </summary>
public class FileAttachmentRepository : BaseRepository<FileAttachment>, IFileAttachmentRepository
{
    private readonly BotDbContext _context;

    public FileAttachmentRepository(BotDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<FileAttachment?> GetByHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        return await _context.FileAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FileHash == fileHash, cancellationToken);
    }

    public async Task<List<FileAttachment>> GetFilesByUserAsync(int userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return await _context.FileAttachments
            .AsNoTracking()
            .Where(f => f.UploadedByUserId == userId && !f.IsDeleted)
            .OrderByDescending(f => f.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FileAttachment>> GetFilesByTypeAsync(Domain.Enums.FileType fileType, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return await _context.FileAttachments
            .AsNoTracking()
            .Where(f => f.FileType == fileType && !f.IsDeleted)
            .OrderByDescending(f => f.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FileAttachment>> GetFilesPendingScanAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FileAttachments
            .AsNoTracking()
            .Where(f => f.ScanStatus == Domain.Enums.ScanStatus.Pending && !f.IsDeleted)
            .OrderBy(f => f.UploadedAt)
            .Take(100) // Обмежуємо кількість для обробки
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FileAttachment>> SearchFilesByNameAsync(string searchTerm, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return await _context.FileAttachments
            .AsNoTracking()
            .Where(f => (f.FileName.Contains(searchTerm) || f.OriginalFileName.Contains(searchTerm)) && !f.IsDeleted)
            .OrderByDescending(f => f.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FileAttachment>> GetLargeFilesAsync(long minSizeBytes, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return await _context.FileAttachments
            .AsNoTracking()
            .Where(f => f.FileSize >= minSizeBytes && !f.IsDeleted)
            .OrderByDescending(f => f.FileSize)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<FileUsageStats> GetUsageStatsAsync(CancellationToken cancellationToken = default)
    {
        var totalFiles = await _context.FileAttachments.CountAsync(f => !f.IsDeleted, cancellationToken);
        var totalSizeBytes = await _context.FileAttachments
            .Where(f => !f.IsDeleted)
            .SumAsync(f => f.FileSize, cancellationToken);

        var typeStats = await _context.FileAttachments
            .Where(f => !f.IsDeleted)
            .GroupBy(f => f.FileType)
            .Select(g => new { FileType = g.Key, Count = g.Count(), TotalSize = g.Sum(f => f.FileSize) })
            .ToListAsync(cancellationToken);

        var scanStats = await _context.FileAttachments
            .Where(f => !f.IsDeleted)
            .GroupBy(f => f.ScanStatus)
            .Select(g => new { ScanStatus = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var recentUploads = await _context.FileAttachments
            .AsNoTracking()
            .Where(f => !f.IsDeleted && f.UploadedAt >= DateTime.UtcNow.AddDays(-7))
            .CountAsync(cancellationToken);

        return new FileUsageStats
        {
            TotalFiles = totalFiles,
            TotalSizeBytes = totalSizeBytes,
            AverageFileSizeBytes = totalFiles > 0 ? totalSizeBytes / totalFiles : 0,
            RecentUploads = recentUploads,
            FileTypeStats = typeStats.Select(ts => new FileTypeStats
            {
                FileType = ts.FileType,
                Count = ts.Count,
                TotalSizeBytes = ts.TotalSize,
                AverageSizeBytes = ts.Count > 0 ? ts.TotalSize / ts.Count : 0
            }).ToList(),
            ScanStatusStats = scanStats.Select(ss => new ScanStatusStats
            {
                ScanStatus = ss.ScanStatus,
                Count = ss.Count,
                Percentage = totalFiles > 0 ? (double)ss.Count / totalFiles * 100 : 0
            }).ToList()
        };
    }

    public async Task<int> GetUserFileCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.FileAttachments
            .CountAsync(f => f.UploadedByUserId == userId && !f.IsDeleted, cancellationToken);
    }

    public async Task<long> GetUserTotalFileSizeAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.FileAttachments
            .Where(f => f.UploadedByUserId == userId && !f.IsDeleted)
            .SumAsync(f => f.FileSize, cancellationToken);
    }

    public async Task<List<FileAttachment>> GetFilesForCleanupAsync(DateTime olderThan, bool includeUnscanned = false, CancellationToken cancellationToken = default)
    {
        var query = _context.FileAttachments
            .AsNoTracking()
            .Where(f => f.IsDeleted && f.DeletedAt.HasValue && f.DeletedAt.Value < olderThan);

        if (includeUnscanned)
        {
            query = query.Union(_context.FileAttachments.Where(f => f.ScanStatus == Domain.Enums.ScanStatus.Error && f.UploadedAt < olderThan));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> DeleteOldOrphanedFilesAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        // Знаходимо файли які:
        // 1. Старші за вказану дату
        // 2. Помічені як видалені АБО не прикріплені до жодного звернення
        // 3. Не використовуються в активних зверненнях
        var orphanedFileIds = await _context.FileAttachments
            .Where(f => f.UploadedAt < olderThan &&
                       (f.IsDeleted ||
                        !_context.AppealFileAttachments.Any(afa => afa.FileAttachmentId == f.Id)))
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);

        if (orphanedFileIds.Count == 0)
            return 0;

        // Видаляємо файли з бази даних
        var filesToDelete = await _context.FileAttachments
            .Where(f => orphanedFileIds.Contains(f.Id))
            .ToListAsync(cancellationToken);

        _context.FileAttachments.RemoveRange(filesToDelete);

        // Повертаємо кількість видалених файлів
        return filesToDelete.Count;
    }
}

/// <summary>
/// Репозиторій для роботи з прив'язками файлів до апеляцій
/// </summary>
public class AppealFileAttachmentRepository : BaseRepository<AppealFileAttachment>, IAppealFileAttachmentRepository
{
    private readonly BotDbContext _context;

    public AppealFileAttachmentRepository(BotDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<AppealFileAttachment>> GetByAppealIdAsync(int appealId, CancellationToken cancellationToken = default)
    {
        return await _context.AppealFileAttachments
            .AsNoTracking()
            .Include(afa => afa.FileAttachment)
            .Where(afa => afa.AppealId == appealId)
            .OrderBy(afa => afa.AttachedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AppealFileAttachment>> GetByFileIdAsync(int fileId, CancellationToken cancellationToken = default)
    {
        return await _context.AppealFileAttachments
            .AsNoTracking()
            .Include(afa => afa.Appeal)
            .Where(afa => afa.FileAttachmentId == fileId)
            .OrderBy(afa => afa.AttachedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AppealFileAttachment>> GetEvidenceFilesAsync(int appealId, CancellationToken cancellationToken = default)
    {
        return await _context.AppealFileAttachments
            .AsNoTracking()
            .Include(afa => afa.FileAttachment)
            .Where(afa => afa.AppealId == appealId && afa.IsEvidence)
            .OrderBy(afa => afa.AttachedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int appealId, int fileId, CancellationToken cancellationToken = default)
    {
        return await _context.AppealFileAttachments
            .AnyAsync(afa => afa.AppealId == appealId && afa.FileAttachmentId == fileId, cancellationToken);
    }

    public async Task<int> GetFileCountByAppealAsync(int appealId, CancellationToken cancellationToken = default)
    {
        return await _context.AppealFileAttachments
            .CountAsync(afa => afa.AppealId == appealId, cancellationToken);
    }

    public async Task<long> GetTotalFileSizeByAppealAsync(int appealId, CancellationToken cancellationToken = default)
    {
        return await _context.AppealFileAttachments
            .Include(afa => afa.FileAttachment)
            .Where(afa => afa.AppealId == appealId)
            .SumAsync(afa => afa.FileAttachment.FileSize, cancellationToken);
    }

    public async Task<List<AppealFileAttachment>> GetAttachmentsByUserAsync(int userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return await _context.AppealFileAttachments
            .AsNoTracking()
            .Include(afa => afa.FileAttachment)
            .Include(afa => afa.Appeal)
            .Where(afa => afa.AttachedByUserId == userId)
            .OrderByDescending(afa => afa.AttachedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AppealFileAttachment>> GetRecentAttachmentsAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        return await _context.AppealFileAttachments
            .AsNoTracking()
            .Include(afa => afa.FileAttachment)
            .Include(afa => afa.Appeal)
            .OrderByDescending(afa => afa.AttachedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}