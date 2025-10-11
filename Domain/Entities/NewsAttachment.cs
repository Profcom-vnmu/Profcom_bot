using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Entities;

/// <summary>
/// Прикріплений файл до новини (фото, документ, відео)
/// </summary>
public class NewsAttachment
{
    /// <summary>
    /// Унікальний ID прикріплення
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// ID новини до якої прикріплений файл
    /// </summary>
    public int NewsId { get; private set; }

    /// <summary>
    /// Telegram File ID
    /// </summary>
    public string FileId { get; private set; } = string.Empty;

    /// <summary>
    /// Тип файлу (Photo, Document, Video, Audio)
    /// </summary>
    public FileType FileType { get; private set; }

    /// <summary>
    /// Оригінальна назва файлу (опціонально)
    /// </summary>
    public string? FileName { get; private set; }

    /// <summary>
    /// Порядок відображення (0 - перший)
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// Коли файл було прикріплено
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Навігаційна властивість до новини
    /// </summary>
    public News News { get; private set; } = null!;

    // Private constructor for EF Core
    private NewsAttachment() { }

    /// <summary>
    /// Створити новий attachment для новини
    /// </summary>
    public static NewsAttachment Create(
        int newsId,
        string fileId,
        FileType fileType,
        int displayOrder,
        string? fileName = null)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            throw new ArgumentException("File ID cannot be empty", nameof(fileId));

        return new NewsAttachment
        {
            NewsId = newsId,
            FileId = fileId,
            FileType = fileType,
            FileName = fileName,
            DisplayOrder = displayOrder,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Оновити порядок відображення
    /// </summary>
    public void UpdateDisplayOrder(int newOrder)
    {
        if (newOrder < 0)
            throw new ArgumentException("Display order cannot be negative", nameof(newOrder));

        DisplayOrder = newOrder;
    }

    /// <summary>
    /// Оновити назву файлу
    /// </summary>
    public void UpdateFileName(string fileName)
    {
        FileName = fileName;
    }
}
