using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Сервіс для роботи з файловим сховищем
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Зберегти файл
    /// </summary>
    Task<Result<StoredFileInfo>> StoreFileAsync(
        string fileName, 
        Stream fileStream, 
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати файл
    /// </summary>
    Task<Result<FileStreamInfo>> GetFileAsync(
        string filePath, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Видалити файл
    /// </summary>
    Task<Result> DeleteFileAsync(
        string filePath, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Перевірити чи існує файл
    /// </summary>
    Task<Result<bool>> FileExistsAsync(
        string filePath, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати розмір файла
    /// </summary>
    Task<Result<long>> GetFileSizeAsync(
        string filePath, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Створити мініатюру для зображення
    /// </summary>
    Task<Result<string>> CreateThumbnailAsync(
        string sourceFilePath,
        int maxWidth = 200,
        int maxHeight = 200,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Стиснути зображення
    /// </summary>
    Task<Result<string>> CompressImageAsync(
        string sourceFilePath,
        int quality = 85,
        int maxWidth = 1920,
        int maxHeight = 1080,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Очистити тимчасові файли старше заданого періоду
    /// </summary>
    Task<Result<int>> CleanupTempFilesAsync(
        TimeSpan olderThan,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати URL для публічного доступу до файла (якщо підтримується)
    /// </summary>
    Task<Result<string?>> GetPublicUrlAsync(
        string filePath,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримати інформацію про стан сховища
    /// </summary>
    Task<StorageInfo> GetStorageInfoAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Інформація про стан файлового сховища
/// </summary>
public class StorageInfo
{
    public long TotalSpace { get; set; }
    public long FreeSpace { get; set; }
    public long UsedSpace { get; set; }
    public int FileCount { get; set; }
}

/// <summary>
/// Інформація про збережений файл
/// </summary>
public class StoredFileInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public FileType FileType { get; set; }
    public bool IsCompressed { get; set; }
    public string? ThumbnailPath { get; set; }
}

/// <summary>
/// Інформація про файловий потік
/// </summary>
public class FileStreamInfo : IDisposable
{
    public Stream Stream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Length { get; set; }

    public void Dispose()
    {
        Stream?.Dispose();
    }
}