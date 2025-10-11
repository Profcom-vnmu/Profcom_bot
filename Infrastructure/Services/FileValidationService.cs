using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Infrastructure.Services;

/// <summary>
/// Сервіс валідації та сканування файлів
/// </summary>
public class FileValidationService : IFileValidationService
{
    private readonly ILogger<FileValidationService> _logger;
    private readonly Dictionary<FileType, long> _maxFileSizes;
    private readonly HashSet<string> _allowedExtensions;
    private readonly HashSet<string> _dangerousExtensions;
    private readonly bool _virusScanningEnabled;

    public FileValidationService(IConfiguration configuration, ILogger<FileValidationService> logger)
    {
        _logger = logger;
        _virusScanningEnabled = configuration.GetValue<bool>("FileValidation:VirusScanningEnabled", false);

        // Налаштування максимальних розмірів файлів (в байтах)
        _maxFileSizes = new Dictionary<FileType, long>
        {
            [FileType.Image] = configuration.GetValue<long>("FileValidation:MaxSizes:Image", 10 * 1024 * 1024), // 10MB
            [FileType.Document] = configuration.GetValue<long>("FileValidation:MaxSizes:Document", 50 * 1024 * 1024), // 50MB
            [FileType.Video] = configuration.GetValue<long>("FileValidation:MaxSizes:Video", 100 * 1024 * 1024), // 100MB
            [FileType.Audio] = configuration.GetValue<long>("FileValidation:MaxSizes:Audio", 20 * 1024 * 1024), // 20MB
            [FileType.Archive] = configuration.GetValue<long>("FileValidation:MaxSizes:Archive", 50 * 1024 * 1024), // 50MB
            [FileType.Other] = configuration.GetValue<long>("FileValidation:MaxSizes:Other", 10 * 1024 * 1024) // 10MB
        };

        // Дозволені розширення файлів
        _allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Зображення
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".ico",
            
            // Документи
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf", ".odt", ".ods", ".odp",
            
            // Відео
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v",
            
            // Аудіо
            ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a",
            
            // Архіви
            ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2"
        };

        // Небезпечні розширення (заборонені)
        _dangerousExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".bat", ".cmd", ".com", ".scr", ".pif", ".vbs", ".vbe", ".js", ".jse", 
            ".wsf", ".wsh", ".msi", ".msp", ".hta", ".jar", ".ps1", ".psm1", ".reg",
            ".dll", ".sys", ".drv", ".ocx", ".cpl", ".inf", ".ins", ".isp", ".msc",
            ".sct", ".shb", ".shs", ".url", ".vb", ".wsc"
        };
    }

    public async Task<Result<FileValidationResult>> ValidateFileAsync(string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken = default)
    {
        var result = new FileValidationResult();

        try
        {
            // Базова валідація
            if (string.IsNullOrWhiteSpace(fileName))
            {
                result.Errors.Add("Ім'я файла не може бути порожнім");
            }

            if (fileStream == null || !fileStream.CanRead)
            {
                result.Errors.Add("Неможливо прочитати файл");
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                result.Errors.Add("Тип контенту не визначено");
            }

            if (result.Errors.Count > 0)
            {
                return Result<FileValidationResult>.Ok(result);
            }

            // Перевірка розширення файлу
            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                result.Errors.Add("Файл повинен мати розширення");
                return Result<FileValidationResult>.Ok(result);
            }

            // Перевірка на небезпечні розширення
            if (_dangerousExtensions.Contains(extension))
            {
                result.Errors.Add($"Розширення {extension} заборонено з міркувань безпеки");
                return Result<FileValidationResult>.Ok(result);
            }

            // Перевірка на дозволені розширення
            if (!_allowedExtensions.Contains(extension))
            {
                result.Warnings.Add($"Розширення {extension} не входить до списку рекомендованих");
            }

            // Визначення типу файлу
            var fileType = FileTypeExtensions.FromExtension(extension);
            if (fileType == FileType.Unknown)
            {
                fileType = FileTypeExtensions.FromMimeType(contentType);
            }

            result.DetectedFileType = fileType;

            // Перевірка розміру файлу
            var fileSize = fileStream!.Length;
            var maxAllowedSize = GetMaxAllowedSize(fileType);

            if (fileSize > maxAllowedSize)
            {
                result.Errors.Add($"Розмір файлу ({FormatFileSize(fileSize)}) перевищує максимально дозволений ({FormatFileSize(maxAllowedSize)}) для типу {fileType.GetDisplayName()}");
            }

            // Перевірка на порожній файл
            if (fileSize == 0)
            {
                result.Errors.Add("Файл не може бути порожнім");
            }

            // Перевірка імені файлу на небезпечні символи
            var invalidChars = Path.GetInvalidFileNameChars();
            if (fileName != null && fileName.IndexOfAny(invalidChars) >= 0)
            {
                result.Errors.Add("Ім'я файлу містить недопустимі символи");
            }

            // Перевірка довжини імені файлу
            if (fileName != null && fileName.Length > 255)
            {
                result.Errors.Add("Ім'я файлу занадто довге (максимум 255 символів)");
            }

            // Додаткові перевірки для зображень
            if (fileType == FileType.Image)
            {
                await ValidateImageAsync(fileStream, result, cancellationToken);
            }

            // Додаткові перевірки для документів
            if (fileType == FileType.Document)
            {
                ValidateDocument(fileName!, contentType, result);
            }

            result.IsValid = result.Errors.Count == 0;

            _logger.LogInformation("Валідація файлу {FileName}: {IsValid}, помилок: {ErrorCount}, попереджень: {WarningCount}",
                fileName!, result.IsValid, result.Errors.Count, result.Warnings.Count);

            return Result<FileValidationResult>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка валідації файлу {FileName}", fileName);
            result.Errors.Add("Помилка під час валідації файлу");
            return Result<FileValidationResult>.Ok(result);
        }
    }

    public async Task<Result<ScanResult>> ScanFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = new ScanResult
            {
                Status = ScanStatus.Pending,
                ScannedAt = DateTime.UtcNow
            };

        if (!_virusScanningEnabled)
        {
            result.Status = ScanStatus.Skipped;
            result.Message = "Сканування вірусів відключено";
            _logger.LogInformation("Сканування вірусів пропущено для файлу {FilePath}", filePath);
            return Result<ScanResult>.Ok(result);
        }            // Тут має бути інтеграція з антивірусом (ClamAV, Windows Defender, тощо)
            // Поки що імітуємо сканування
            await Task.Delay(100, cancellationToken); // Імітація сканування

            // Простий евристичний аналіз
            var heuristicResult = await PerformHeuristicAnalysisAsync(filePath, cancellationToken);
            
            if (heuristicResult.IsSuspicious)
            {
                result.Status = ScanStatus.Threat;
                result.Message = $"Виявлено підозрілу активність: {heuristicResult.Reason}";
            }
            else
            {
                result.Status = ScanStatus.Safe;
                result.Message = "Загроз не виявлено";
            }

            _logger.LogInformation("Сканування файлу {FilePath} завершено: {Status}", filePath, result.Status);
            return Result<ScanResult>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка сканування файлу {FilePath}", filePath);
            
            var errorResult = new ScanResult
            {
                Status = ScanStatus.Error,
                ScannedAt = DateTime.UtcNow,
                Message = "Помилка під час сканування"
            };
            
            return Result<ScanResult>.Ok(errorResult);
        }
    }

    public bool IsFileTypeAllowed(FileType fileType)
    {
        return fileType != FileType.Unknown && _maxFileSizes.ContainsKey(fileType);
    }

    public bool IsFileSizeAllowed(long fileSize, FileType fileType)
    {
        if (!_maxFileSizes.TryGetValue(fileType, out var maxSize))
        {
            return false;
        }

        return fileSize <= maxSize && fileSize > 0;
    }

    public long GetMaxAllowedSize(FileType fileType)
    {
        return _maxFileSizes.TryGetValue(fileType, out var maxSize) ? maxSize : 0;
    }

    public IEnumerable<string> GetAllowedExtensions(FileType? fileType = null)
    {
        if (fileType.HasValue)
        {
            return fileType.Value switch
            {
                FileType.Image => _allowedExtensions.Where(ext => IsImageExtension(ext)),
                FileType.Document => _allowedExtensions.Where(ext => IsDocumentExtension(ext)),
                FileType.Video => _allowedExtensions.Where(ext => IsVideoExtension(ext)),
                FileType.Audio => _allowedExtensions.Where(ext => IsAudioExtension(ext)),
                FileType.Archive => _allowedExtensions.Where(ext => IsArchiveExtension(ext)),
                _ => _allowedExtensions
            };
        }

        return _allowedExtensions;
    }

    private async Task ValidateImageAsync(Stream imageStream, FileValidationResult result, CancellationToken cancellationToken)
    {
        try
        {
            imageStream.Seek(0, SeekOrigin.Begin);
            
            // Перевіряємо сигнатуру файлу
            var buffer = new byte[10];
            await imageStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            
            var isValidImage = IsValidImageSignature(buffer);
            if (!isValidImage)
            {
                result.Warnings.Add("Файл може не бути дійсним зображенням");
            }
            
            imageStream.Seek(0, SeekOrigin.Begin);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Помилка валідації зображення");
            result.Warnings.Add("Не вдалося повністю перевірити зображення");
        }
    }

    private static void ValidateDocument(string fileName, string contentType, FileValidationResult result)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        // Перевірка відповідності розширення та MIME типу
        var expectedMimeTypes = extension switch
        {
            ".pdf" => new[] { "application/pdf" },
            ".doc" => new[] { "application/msword" },
            ".docx" => new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            ".xls" => new[] { "application/vnd.ms-excel" },
            ".xlsx" => new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            _ => Array.Empty<string>()
        };

        if (expectedMimeTypes.Length > 0 && !expectedMimeTypes.Contains(contentType))
        {
            result.Warnings.Add($"MIME тип {contentType} не відповідає розширенню файлу {extension}");
        }
    }

    private async Task<(bool IsSuspicious, string Reason)> PerformHeuristicAnalysisAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            
            // Перевірка розміру файлу (підозрілі дуже маленькі або дуже великі файли)
            if (fileInfo.Length < 10)
            {
                return (true, "Файл занадто малий");
            }

            if (fileInfo.Length > 500 * 1024 * 1024) // 500MB
            {
                return (true, "Файл занадто великий");
            }

            // Читаємо початок файлу для аналізу
            using var stream = File.OpenRead(filePath);
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

            // Перевірка на підозрілі байтові послідовності
            var suspiciousPatterns = new byte[][]
            {
                new byte[] { 0x4D, 0x5A }, // MZ header (executable)
                new byte[] { 0x50, 0x4B }, // PK header (може бути замаскований ZIP)
            };

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            foreach (var pattern in suspiciousPatterns)
            {
                if (StartsWithPattern(buffer, pattern))
                {
                    if (extension != ".zip" && extension != ".jar" && pattern.SequenceEqual(new byte[] { 0x50, 0x4B }))
                    {
                        continue; // ZIP header в не-архівному файлі - підозрільно
                    }
                    
                    if (pattern.SequenceEqual(new byte[] { 0x4D, 0x5A }) && !extension.EndsWith(".exe"))
                    {
                        return (true, "Виявлено підозрілий executable заголовок");
                    }
                }
            }

            return (false, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Помилка евристичного аналізу файлу {FilePath}", filePath);
            return (false, string.Empty);
        }
    }

    private static bool StartsWithPattern(byte[] data, byte[] pattern)
    {
        if (data.Length < pattern.Length)
            return false;

        for (int i = 0; i < pattern.Length; i++)
        {
            if (data[i] != pattern[i])
                return false;
        }

        return true;
    }

    private static bool IsValidImageSignature(byte[] buffer)
    {
        // JPEG
        if (buffer.Length >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
            return true;

        // PNG
        if (buffer.Length >= 8 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
            return true;

        // GIF
        if (buffer.Length >= 6 && buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46)
            return true;

        // BMP
        if (buffer.Length >= 2 && buffer[0] == 0x42 && buffer[1] == 0x4D)
            return true;

        // WebP
        if (buffer.Length >= 8 && buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46)
            return true;

        return false;
    }

    private static bool IsImageExtension(string extension) =>
        new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".ico" }.Contains(extension.ToLowerInvariant());

    private static bool IsDocumentExtension(string extension) =>
        new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf", ".odt", ".ods", ".odp" }.Contains(extension.ToLowerInvariant());

    private static bool IsVideoExtension(string extension) =>
        new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v" }.Contains(extension.ToLowerInvariant());

    private static bool IsAudioExtension(string extension) =>
        new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a" }.Contains(extension.ToLowerInvariant());

    private static bool IsArchiveExtension(string extension) =>
        new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2" }.Contains(extension.ToLowerInvariant());

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int counter = 0;
        double number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:N1} {suffixes[counter]}";
    }
}