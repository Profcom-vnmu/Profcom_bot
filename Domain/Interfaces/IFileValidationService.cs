using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Сервіс для валідації та сканування файлів
/// </summary>
public interface IFileValidationService
{
    /// <summary>
    /// Валідувати файл перед завантаженням
    /// </summary>
    Task<Result<FileValidationResult>> ValidateFileAsync(
        string fileName,
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Сканувати файл на віруси
    /// </summary>
    Task<Result<ScanResult>> ScanFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Перевірити чи дозволений тип файла
    /// </summary>
    bool IsFileTypeAllowed(FileType fileType);

    /// <summary>
    /// Перевірити чи дозволений розмір файла
    /// </summary>
    bool IsFileSizeAllowed(long fileSize, FileType fileType);

    /// <summary>
    /// Отримати максимальний дозволений розмір для типу файла
    /// </summary>
    long GetMaxAllowedSize(FileType fileType);

    /// <summary>
    /// Отримати дозволені розширення файлів
    /// </summary>
    IEnumerable<string> GetAllowedExtensions(FileType? fileType = null);
}

/// <summary>
/// Результат валідації файла
/// </summary>
public class FileValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public FileType DetectedFileType { get; set; }
    public string? SuggestedFileName { get; set; }
    public bool RequiresCompression { get; set; }
    public bool RequiresThumbnail { get; set; }

    public static FileValidationResult Success(FileType fileType, string? suggestedFileName = null)
    {
        return new FileValidationResult
        {
            IsValid = true,
            DetectedFileType = fileType,
            SuggestedFileName = suggestedFileName
        };
    }

    public static FileValidationResult Failure(params string[] errors)
    {
        return new FileValidationResult
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }

    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}

/// <summary>
/// Результат сканування файла
/// </summary>
public class ScanResult
{
    public ScanStatus Status { get; set; }
    public string? Message { get; set; }
    public List<string> DetectedThreats { get; set; } = new();
    public DateTime ScannedAt { get; set; }
    public string? ScannerVersion { get; set; }

    public static ScanResult Safe(string? message = null)
    {
        return new ScanResult
        {
            Status = ScanStatus.Safe,
            Message = message ?? "Файл безпечний",
            ScannedAt = DateTime.UtcNow
        };
    }

    public static ScanResult Threat(IEnumerable<string> threats, string? message = null)
    {
        return new ScanResult
        {
            Status = ScanStatus.Threat,
            Message = message ?? "Виявлено загрози",
            DetectedThreats = threats.ToList(),
            ScannedAt = DateTime.UtcNow
        };
    }

    public static ScanResult Error(string message)
    {
        return new ScanResult
        {
            Status = ScanStatus.Error,
            Message = message,
            ScannedAt = DateTime.UtcNow
        };
    }

    public static ScanResult Skipped(string reason)
    {
        return new ScanResult
        {
            Status = ScanStatus.Skipped,
            Message = reason,
            ScannedAt = DateTime.UtcNow
        };
    }
}