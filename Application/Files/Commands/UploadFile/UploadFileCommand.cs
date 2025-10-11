using MediatR;
using StudentUnionBot.Application.Common.Attributes;
using StudentUnionBot.Application.Files.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Files.Commands.UploadFile;

/// <summary>
/// Команда для завантаження файла
/// </summary>
[RequirePermission(Permission.UploadFile)]
public class UploadFileCommand : IRequest<Result<FileAttachmentDto>>
{
    /// <summary>
    /// Потік файла
    /// </summary>
    public Stream FileStream { get; set; } = null!;

    /// <summary>
    /// Назва файла
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME тип файла
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Розмір файла в байтах
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// ID користувача, який завантажує файл
    /// </summary>
    public long UploadedByUserId { get; set; }

    /// <summary>
    /// ID апела, до якого прикріплюється файл (опціонально)
    /// </summary>
    public int? AppealId { get; set; }

    /// <summary>
    /// Опис файла
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Чи є файл доказом
    /// </summary>
    public bool IsEvidence { get; set; } = false;

    /// <summary>
    /// Примусово стиснути зображення
    /// </summary>
    public bool ForceCompression { get; set; } = false;

    /// <summary>
    /// Створити мініатюру для підтримуваних файлів
    /// </summary>
    public bool CreateThumbnail { get; set; } = true;

    /// <summary>
    /// Пропустити сканування на віруси (тільки для надійних джерел)
    /// </summary>
    public bool SkipVirusScan { get; set; } = false;

    /// <summary>
    /// Конструктор для простого завантаження
    /// </summary>
    public UploadFileCommand(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        long uploadedByUserId)
    {
        FileStream = fileStream;
        FileName = fileName;
        ContentType = contentType;
        FileSize = fileSize;
        UploadedByUserId = uploadedByUserId;
    }

    /// <summary>
    /// Конструктор для завантаження з прикріпленням до апела
    /// </summary>
    public UploadFileCommand(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        long uploadedByUserId,
        int appealId,
        string? description = null,
        bool isEvidence = false)
    {
        FileStream = fileStream;
        FileName = fileName;
        ContentType = contentType;
        FileSize = fileSize;
        UploadedByUserId = uploadedByUserId;
        AppealId = appealId;
        Description = description;
        IsEvidence = isEvidence;
    }

    // Конструктор за замовчуванням для MediatR
    public UploadFileCommand() { }

    public void Dispose()
    {
        FileStream?.Dispose();
    }
}