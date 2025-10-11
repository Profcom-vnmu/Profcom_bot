using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Common.Models;

/// <summary>
/// DTO для прикріплення файлів до новин та подій
/// </summary>
public class FileAttachmentDto
{
    /// <summary>
    /// Telegram File ID
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Тип файлу
    /// </summary>
    public FileType FileType { get; set; }

    /// <summary>
    /// Оригінальна назва файлу (опціонально)
    /// </summary>
    public string? FileName { get; set; }
}
