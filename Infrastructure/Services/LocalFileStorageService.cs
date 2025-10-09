using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Security.Cryptography;

namespace StudentUnionBot.Infrastructure.Services;

/// <summary>
/// Локальний сервіс файлового сховища
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _tempPath;
    private readonly string _thumbnailPath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _basePath = configuration["FileStorage:BasePath"] ?? "Storage/Files";
        _tempPath = Path.Combine(_basePath, "Temp");
        _thumbnailPath = Path.Combine(_basePath, "Thumbnails");
        _logger = logger;

        EnsureDirectoriesExist();
    }

    public async Task<Result<StoredFileInfo>> StoreFileAsync(string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileType = FileTypeExtensions.FromMimeType(contentType);
            var fileExtension = Path.GetExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            
            // Організовуємо файли по датах та типах
            var dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
            var typeFolder = fileType.ToString();
            var relativePath = Path.Combine(typeFolder, dateFolder, uniqueFileName);
            var fullPath = Path.Combine(_basePath, relativePath);
            
            // Створюємо директорії якщо потрібно
            var directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            // Зберігаємо оригінальний файл
            await using (var fileStreamOutput = File.Create(fullPath))
            {
                await fileStream.CopyToAsync(fileStreamOutput, cancellationToken);
            }

            var fileInfo = new FileInfo(fullPath);
            var fileHash = await ComputeFileHashAsync(fullPath, cancellationToken);

            var result = new StoredFileInfo
            {
                FilePath = relativePath,
                FileName = uniqueFileName,
                FileSize = fileInfo.Length,
                FileHash = fileHash,
                ContentType = contentType,
                FileType = fileType
            };

            // Стискаємо зображення якщо потрібно
            if (fileType == FileType.Image && ShouldCompressImage(fileInfo.Length))
            {
                var compressedPath = await CompressImageAsync(fullPath, cancellationToken: cancellationToken);
                if (compressedPath.IsSuccess)
                {
                    // Замінюємо оригінал стисненим
                    File.Delete(fullPath);
                    File.Move(compressedPath.Value, fullPath);
                    
                    var compressedInfo = new FileInfo(fullPath);
                    result.FileSize = compressedInfo.Length;
                    result.IsCompressed = true;
                    
                    _logger.LogInformation("Зображення стиснено з {OriginalSize} до {CompressedSize}",
                        fileStream.Length, compressedInfo.Length);
                }
            }

            // Створюємо мініатюру якщо підтримується
            if (fileType.SupportsThumbnails())
            {
                var thumbnailResult = await CreateThumbnailAsync(fullPath, cancellationToken: cancellationToken);
                if (thumbnailResult.IsSuccess)
                {
                    result.ThumbnailPath = thumbnailResult.Value;
                }
            }

            _logger.LogInformation("Файл збережено: {FilePath}, розмір: {FileSize}", relativePath, result.FileSize);
            return Result<StoredFileInfo>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка збереження файла {FileName}", fileName);
            return Result<StoredFileInfo>.Fail("Не вдалося зберегти файл");
        }
    }

    public async Task<Result<FileStreamInfo>> GetFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            
            if (!File.Exists(fullPath))
            {
                return Result<FileStreamInfo>.Fail("Файл не знайдено");
            }

            var fileInfo = new FileInfo(fullPath);
            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            
            var fileName = Path.GetFileName(filePath);
            var contentType = GetContentTypeFromExtension(Path.GetExtension(fileName));

            var result = new FileStreamInfo
            {
                Stream = stream,
                FileName = fileName,
                ContentType = contentType,
                Length = fileInfo.Length
            };

            return Result<FileStreamInfo>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка отримання файла {FilePath}", filePath);
            return Result<FileStreamInfo>.Fail("Не вдалося отримати файл");
        }
    }

    public async Task<Result> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Файл видалено: {FilePath}", filePath);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка видалення файла {FilePath}", filePath);
            return Result.Fail("Не вдалося видалити файл");
        }
    }

    public async Task<Result<bool>> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            var exists = File.Exists(fullPath);
            return Result<bool>.Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка перевірки існування файла {FilePath}", filePath);
            return Result<bool>.Fail("Не вдалося перевірити існування файла");
        }
    }

    public async Task<Result<long>> GetFileSizeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, filePath);
            
            if (!File.Exists(fullPath))
            {
                return Result<long>.Fail("Файл не знайдено");
            }

            var fileInfo = new FileInfo(fullPath);
            return Result<long>.Ok(fileInfo.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка отримання розміру файла {FilePath}", filePath);
            return Result<long>.Fail("Не вдалося отримати розмір файла");
        }
    }

    public async Task<Result<string>> CreateThumbnailAsync(string sourceFilePath, int maxWidth = 200, int maxHeight = 200, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullSourcePath = Path.IsPathRooted(sourceFilePath) 
                ? sourceFilePath 
                : Path.Combine(_basePath, sourceFilePath);

            if (!File.Exists(fullSourcePath))
            {
                return Result<string>.Fail("Вихідний файл не знайдено");
            }

            var fileName = Path.GetFileNameWithoutExtension(fullSourcePath);
            var thumbnailFileName = $"{fileName}_thumb_{maxWidth}x{maxHeight}.jpg";
            var thumbnailRelativePath = Path.Combine("Thumbnails", DateTime.UtcNow.ToString("yyyy/MM/dd"), thumbnailFileName);
            var thumbnailFullPath = Path.Combine(_basePath, thumbnailRelativePath);

            // Створюємо директорію для мініатюр
            var thumbnailDir = Path.GetDirectoryName(thumbnailFullPath);
            if (!Directory.Exists(thumbnailDir))
            {
                Directory.CreateDirectory(thumbnailDir!);
            }

            using var image = await Image.LoadAsync(fullSourcePath, cancellationToken);
            
            // Обчислюємо нові розміри зберігаючи пропорції
            var (newWidth, newHeight) = CalculateResizeParameters(image.Width, image.Height, maxWidth, maxHeight);
            
            image.Mutate(x => x.Resize(newWidth, newHeight));

            await image.SaveAsJpegAsync(thumbnailFullPath, new JpegEncoder { Quality = 85 }, cancellationToken);

            _logger.LogInformation("Створено мініатюру: {ThumbnailPath}", thumbnailRelativePath);
            return Result<string>.Ok(thumbnailRelativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка створення мініатюри для {SourcePath}", sourceFilePath);
            return Result<string>.Fail("Не вдалося створити мініатюру");
        }
    }

    public async Task<Result<string>> CompressImageAsync(string sourceFilePath, int quality = 85, int maxWidth = 1920, int maxHeight = 1080, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullSourcePath = Path.IsPathRooted(sourceFilePath) 
                ? sourceFilePath 
                : Path.Combine(_basePath, sourceFilePath);

            if (!File.Exists(fullSourcePath))
            {
                return Result<string>.Fail("Вихідний файл не знайдено");
            }

            var directory = Path.GetDirectoryName(fullSourcePath);
            var fileName = Path.GetFileNameWithoutExtension(fullSourcePath);
            var compressedPath = Path.Combine(directory!, $"{fileName}_compressed.jpg");

            using var image = await Image.LoadAsync(fullSourcePath, cancellationToken);
            
            // Зменшуємо розмір якщо потрібно
            if (image.Width > maxWidth || image.Height > maxHeight)
            {
                var (newWidth, newHeight) = CalculateResizeParameters(image.Width, image.Height, maxWidth, maxHeight);
                image.Mutate(x => x.Resize(newWidth, newHeight));
            }

            await image.SaveAsJpegAsync(compressedPath, new JpegEncoder { Quality = quality }, cancellationToken);

            return Result<string>.Ok(compressedPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка стиснення зображення {SourcePath}", sourceFilePath);
            return Result<string>.Fail("Не вдалося стиснути зображення");
        }
    }

    public async Task<Result<int>> CleanupTempFilesAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - olderThan;
            var deletedCount = 0;

            if (Directory.Exists(_tempPath))
            {
                var tempFiles = Directory.GetFiles(_tempPath, "*", SearchOption.AllDirectories);
                
                foreach (var file in tempFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTimeUtc < cutoffTime)
                    {
                        try
                        {
                            File.Delete(file);
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Не вдалося видалити тимчасовий файл {FilePath}", file);
                        }
                    }
                }
            }

            _logger.LogInformation("Очищено {DeletedCount} тимчасових файлів старше {OlderThan}", 
                deletedCount, olderThan);

            return Result<int>.Ok(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка очищення тимчасових файлів");
            return Result<int>.Fail("Не вдалося очистити тимчасові файли");
        }
    }

    public async Task<Result<string?>> GetPublicUrlAsync(string filePath, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        // Локальне сховище не підтримує публічні URL
        return Result<string?>.Ok(null);
    }

    public Task<StorageInfo> GetStorageInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(_basePath) ?? _basePath);
            var fileCount = Directory.GetFiles(_basePath, "*", SearchOption.AllDirectories).Length;

            return Task.FromResult(new StorageInfo
            {
                TotalSpace = driveInfo.TotalSize,
                FreeSpace = driveInfo.AvailableFreeSpace,
                UsedSpace = driveInfo.TotalSize - driveInfo.AvailableFreeSpace,
                FileCount = fileCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка отримання інформації про сховище");
            return Task.FromResult(new StorageInfo
            {
                TotalSpace = 0,
                FreeSpace = 0,
                UsedSpace = 0,
                FileCount = 0
            });
        }
    }

    private void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(_basePath);
        Directory.CreateDirectory(_tempPath);
        Directory.CreateDirectory(_thumbnailPath);
    }

    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes);
    }

    private static bool ShouldCompressImage(long fileSize)
    {
        const long maxSizeBeforeCompression = 2 * 1024 * 1024; // 2MB
        return fileSize > maxSizeBeforeCompression;
    }

    private static (int width, int height) CalculateResizeParameters(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
    {
        if (originalWidth <= maxWidth && originalHeight <= maxHeight)
        {
            return (originalWidth, originalHeight);
        }

        var ratioX = (double)maxWidth / originalWidth;
        var ratioY = (double)maxHeight / originalHeight;
        var ratio = Math.Min(ratioX, ratioY);

        return ((int)(originalWidth * ratio), (int)(originalHeight * ratio));
    }

    private static string GetContentTypeFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}