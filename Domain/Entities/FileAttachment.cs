using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Entities;

/// <summary>
/// Прикріплений файл до апела або повідомлення
/// </summary>
public class FileAttachment
{
    public int Id { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public FileType FileType { get; private set; }
    public string FileHash { get; private set; } = string.Empty; // SHA256 для дедуплікації
    public bool IsCompressed { get; private set; }
    public string? ThumbnailPath { get; private set; }
    public long UploadedByUserId { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public DateTime? ScannedAt { get; private set; }
    public ScanStatus ScanStatus { get; private set; }
    public string? ScanResult { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Навігаційні властивості
    public BotUser UploadedBy { get; private set; } = null!;
    public ICollection<AppealFileAttachment> AppealAttachments { get; private set; } = new List<AppealFileAttachment>();

    // Конструктор для EF Core
    private FileAttachment() { }

    /// <summary>
    /// Створити новий прикріплений файл
    /// </summary>
    public static FileAttachment Create(
        string fileName,
        string originalFileName,
        string filePath,
        string contentType,
        long fileSize,
        FileType fileType,
        string fileHash,
        long uploadedByUserId,
        bool isCompressed = false,
        string? thumbnailPath = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Ім'я файла не може бути порожнім", nameof(fileName));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Шлях до файла не може бути порожнім", nameof(filePath));

        if (fileSize <= 0)
            throw new ArgumentException("Розмір файла має бути більше 0", nameof(fileSize));

        if (uploadedByUserId <= 0)
            throw new ArgumentException("ID користувача має бути більше 0", nameof(uploadedByUserId));

        return new FileAttachment
        {
            FileName = fileName,
            OriginalFileName = originalFileName,
            FilePath = filePath,
            ContentType = contentType,
            FileSize = fileSize,
            FileType = fileType,
            FileHash = fileHash,
            IsCompressed = isCompressed,
            ThumbnailPath = thumbnailPath,
            UploadedByUserId = uploadedByUserId,
            UploadedAt = DateTime.UtcNow,
            ScanStatus = ScanStatus.Pending,
            IsDeleted = false
        };
    }

    /// <summary>
    /// Оновити результат сканування
    /// </summary>
    public void UpdateScanResult(ScanStatus status, string? result = null)
    {
        ScanStatus = status;
        ScanResult = result;
        ScannedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Встановити шлях до мініатюри
    /// </summary>
    public void SetThumbnailPath(string thumbnailPath)
    {
        if (string.IsNullOrWhiteSpace(thumbnailPath))
            throw new ArgumentException("Шлях до мініатюри не може бути порожнім", nameof(thumbnailPath));

        ThumbnailPath = thumbnailPath;
    }

    /// <summary>
    /// Позначити файл як видалений
    /// </summary>
    public void MarkAsDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Відновити видалений файл
    /// </summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
    }

    /// <summary>
    /// Перевірити чи файл пройшов сканування
    /// </summary>
    public bool IsScanned => ScanStatus != ScanStatus.Pending;

    /// <summary>
    /// Перевірити чи файл безпечний для завантаження
    /// </summary>
    public bool IsSafeToDownload => ScanStatus == ScanStatus.Safe || ScanStatus == ScanStatus.Skipped;

    /// <summary>
    /// Отримати розмір файла в зручному форматі
    /// </summary>
    public string GetFormattedFileSize()
    {
        const int unit = 1024;
        if (FileSize < unit) return $"{FileSize} B";
        
        int exp = (int)(Math.Log(FileSize) / Math.Log(unit));
        string pre = "KMGTPE"[exp - 1].ToString();
        return $"{FileSize / Math.Pow(unit, exp):F1} {pre}B";
    }

    /// <summary>
    /// Перевірити чи це зображення
    /// </summary>
    public bool IsImage => FileType == FileType.Image;

    /// <summary>
    /// Перевірити чи це документ
    /// </summary>
    public bool IsDocument => FileType == FileType.Document;

    /// <summary>
    /// Перевірити чи це відео
    /// </summary>
    public bool IsVideo => FileType == FileType.Video;

    /// <summary>
    /// Перевірити чи це аудіо
    /// </summary>
    public bool IsAudio => FileType == FileType.Audio;
}