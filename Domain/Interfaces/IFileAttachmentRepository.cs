using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Репозиторій для роботи з прикріпленими файлами
/// </summary>
public interface IFileAttachmentRepository : IRepository<FileAttachment>
{
    /// <summary>
    /// Отримати файл за хешем
    /// </summary>
    Task<FileAttachment?> GetByHashAsync(string fileHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати файли користувача
    /// </summary>
    Task<List<FileAttachment>> GetFilesByUserAsync(int userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати файли за типом
    /// </summary>
    Task<List<FileAttachment>> GetFilesByTypeAsync(FileType fileType, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати файли які потребують сканування
    /// </summary>
    Task<List<FileAttachment>> GetFilesPendingScanAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати файли для очистки (видалені або старі тимчасові файли)
    /// </summary>
    Task<List<FileAttachment>> GetFilesForCleanupAsync(DateTime olderThan, bool includeUnscanned = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати статистику використання файлів
    /// </summary>
    Task<FileUsageStats> GetUsageStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Пошук файлів за назвою
    /// </summary>
    Task<List<FileAttachment>> SearchFilesByNameAsync(string searchTerm, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати великі файли (більше заданого розміру)
    /// </summary>
    Task<List<FileAttachment>> GetLargeFilesAsync(long minSizeBytes, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Видалити старі сироти файли (файли які не прикріплені до жодного звернення і старші за вказану дату)
    /// </summary>
    Task<int> DeleteOldOrphanedFilesAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Репозиторій для роботи зі зв'язками файлів та апелів
/// </summary>
public interface IAppealFileAttachmentRepository : IRepository<AppealFileAttachment>
{
    /// <summary>
    /// Отримати всі файли апела
    /// </summary>
    Task<List<AppealFileAttachment>> GetByAppealIdAsync(int appealId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати всі апели з файлом
    /// </summary>
    Task<List<AppealFileAttachment>> GetByFileIdAsync(int fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати файли-докази апела
    /// </summary>
    Task<List<AppealFileAttachment>> GetEvidenceFilesAsync(int appealId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Перевірити чи прикріплений файл до апела
    /// </summary>
    Task<bool> ExistsAsync(int appealId, int fileId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Статистика використання файлів
/// </summary>
public class FileUsageStats
{
    public int TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public long AverageFileSizeBytes { get; set; }
    public int RecentUploads { get; set; }
    public List<FileTypeStats> FileTypeStats { get; set; } = new();
    public List<ScanStatusStats> ScanStatusStats { get; set; } = new();
}

public class FileTypeStats
{
    public FileType FileType { get; set; }
    public int Count { get; set; }
    public long TotalSizeBytes { get; set; }
    public long AverageSizeBytes { get; set; }
}

public class ScanStatusStats
{
    public ScanStatus ScanStatus { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}