namespace StudentUnionBot.Domain.Entities;

/// <summary>
/// З'єднання між апелами та прикріпленими файлами (Many-to-Many)
/// </summary>
public class AppealFileAttachment
{
    public int Id { get; private set; }
    public int AppealId { get; private set; }
    public int FileAttachmentId { get; private set; }
    public DateTime AttachedAt { get; private set; }
    public long AttachedByUserId { get; private set; }
    public string? Description { get; private set; }
    public bool IsEvidence { get; private set; } // Чи є цей файл доказом

    // Навігаційні властивості
    public Appeal Appeal { get; private set; } = null!;
    public FileAttachment FileAttachment { get; private set; } = null!;
    public BotUser AttachedBy { get; private set; } = null!;

    // Конструктор для EF Core
    private AppealFileAttachment() { }

    /// <summary>
    /// Створити зв'язок між апелом та файлом
    /// </summary>
    public static AppealFileAttachment Create(
        int appealId,
        int fileAttachmentId,
        long attachedByUserId,
        string? description = null,
        bool isEvidence = false)
    {
        if (appealId <= 0)
            throw new ArgumentException("ID апела має бути більше 0", nameof(appealId));

        if (fileAttachmentId <= 0)
            throw new ArgumentException("ID файла має бути більше 0", nameof(fileAttachmentId));

        if (attachedByUserId <= 0)
            throw new ArgumentException("ID користувача має бути більше 0", nameof(attachedByUserId));

        return new AppealFileAttachment
        {
            AppealId = appealId,
            FileAttachmentId = fileAttachmentId,
            AttachedByUserId = attachedByUserId,
            Description = description,
            IsEvidence = isEvidence,
            AttachedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Оновити опис файла
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description;
    }

    /// <summary>
    /// Позначити файл як доказ
    /// </summary>
    public void MarkAsEvidence()
    {
        IsEvidence = true;
    }

    /// <summary>
    /// Зняти позначку доказу з файла
    /// </summary>
    public void UnmarkAsEvidence()
    {
        IsEvidence = false;
    }
}