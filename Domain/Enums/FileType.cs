namespace StudentUnionBot.Domain.Enums;

/// <summary>
/// Тип файла
/// </summary>
public enum FileType
{
    Unknown = 0,
    Image = 1,
    Document = 2,
    Video = 3,
    Audio = 4,
    Archive = 5,
    Other = 99
}

/// <summary>
/// Extension methods для FileType
/// </summary>
public static class FileTypeExtensions
{
    private static readonly Dictionary<string, FileType> _mimeTypeMapping = new()
    {
        // Images
        { "image/jpeg", FileType.Image },
        { "image/jpg", FileType.Image },
        { "image/png", FileType.Image },
        { "image/gif", FileType.Image },
        { "image/webp", FileType.Image },
        { "image/svg+xml", FileType.Image },
        { "image/bmp", FileType.Image },

        // Documents
        { "application/pdf", FileType.Document },
        { "application/msword", FileType.Document },
        { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", FileType.Document },
        { "application/vnd.ms-excel", FileType.Document },
        { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", FileType.Document },
        { "application/vnd.ms-powerpoint", FileType.Document },
        { "application/vnd.openxmlformats-officedocument.presentationml.presentation", FileType.Document },
        { "text/plain", FileType.Document },
        { "text/csv", FileType.Document },
        { "application/rtf", FileType.Document },

        // Videos
        { "video/mp4", FileType.Video },
        { "video/avi", FileType.Video },
        { "video/mpeg", FileType.Video },
        { "video/quicktime", FileType.Video },
        { "video/x-msvideo", FileType.Video },
        { "video/webm", FileType.Video },

        // Audio
        { "audio/mpeg", FileType.Audio },
        { "audio/mp3", FileType.Audio },
        { "audio/wav", FileType.Audio },
        { "audio/ogg", FileType.Audio },
        { "audio/aac", FileType.Audio },

        // Archives
        { "application/zip", FileType.Archive },
        { "application/x-rar-compressed", FileType.Archive },
        { "application/x-7z-compressed", FileType.Archive },
        { "application/gzip", FileType.Archive },
        { "application/x-tar", FileType.Archive }
    };

    private static readonly Dictionary<string, FileType> _extensionMapping = new()
    {
        // Images
        { ".jpg", FileType.Image },
        { ".jpeg", FileType.Image },
        { ".png", FileType.Image },
        { ".gif", FileType.Image },
        { ".webp", FileType.Image },
        { ".svg", FileType.Image },
        { ".bmp", FileType.Image },

        // Documents
        { ".pdf", FileType.Document },
        { ".doc", FileType.Document },
        { ".docx", FileType.Document },
        { ".xls", FileType.Document },
        { ".xlsx", FileType.Document },
        { ".ppt", FileType.Document },
        { ".pptx", FileType.Document },
        { ".txt", FileType.Document },
        { ".csv", FileType.Document },
        { ".rtf", FileType.Document },

        // Videos
        { ".mp4", FileType.Video },
        { ".avi", FileType.Video },
        { ".mpeg", FileType.Video },
        { ".mpg", FileType.Video },
        { ".mov", FileType.Video },
        { ".webm", FileType.Video },

        // Audio
        { ".mp3", FileType.Audio },
        { ".wav", FileType.Audio },
        { ".ogg", FileType.Audio },
        { ".aac", FileType.Audio },
        { ".flac", FileType.Audio },

        // Archives
        { ".zip", FileType.Archive },
        { ".rar", FileType.Archive },
        { ".7z", FileType.Archive },
        { ".gz", FileType.Archive },
        { ".tar", FileType.Archive }
    };

    /// <summary>
    /// Отримати тип файла по MIME типу
    /// </summary>
    public static FileType FromMimeType(string mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            return FileType.Unknown;

        return _mimeTypeMapping.GetValueOrDefault(mimeType.ToLowerInvariant(), FileType.Other);
    }

    /// <summary>
    /// Отримати тип файла по розширенню
    /// </summary>
    public static FileType FromExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return FileType.Unknown;

        var ext = extension.ToLowerInvariant();
        if (!ext.StartsWith("."))
            ext = "." + ext;

        return _extensionMapping.GetValueOrDefault(ext, FileType.Other);
    }

    /// <summary>
    /// Отримати іконку для типу файла
    /// </summary>
    public static string GetIcon(this FileType fileType)
    {
        return fileType switch
        {
            FileType.Image => "🖼️",
            FileType.Document => "📄",
            FileType.Video => "🎥",
            FileType.Audio => "🎵",
            FileType.Archive => "📦",
            FileType.Other => "📎",
            FileType.Unknown => "❓",
            _ => "📎"
        };
    }

    /// <summary>
    /// Отримати назву типу файла
    /// </summary>
    public static string GetDisplayName(this FileType fileType)
    {
        return fileType switch
        {
            FileType.Image => "Зображення",
            FileType.Document => "Документ",
            FileType.Video => "Відео",
            FileType.Audio => "Аудіо",
            FileType.Archive => "Архів",
            FileType.Other => "Інший",
            FileType.Unknown => "Невідомий",
            _ => "Файл"
        };
    }

    /// <summary>
    /// Перевірити чи підтримується тип файла для стиснення
    /// </summary>
    public static bool SupportsCompression(this FileType fileType)
    {
        return fileType == FileType.Image;
    }

    /// <summary>
    /// Перевірити чи підтримується генерація мініатюр
    /// </summary>
    public static bool SupportsThumbnails(this FileType fileType)
    {
        return fileType == FileType.Image || fileType == FileType.Video;
    }
}