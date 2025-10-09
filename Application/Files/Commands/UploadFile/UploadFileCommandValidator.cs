using FluentValidation;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Files.Commands.UploadFile;

/// <summary>
/// Валідатор для UploadFileCommand
/// </summary>
public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    // Максимальні розміри файлів в байтах
    private const long MaxImageSize = 10 * 1024 * 1024; // 10MB для зображень
    private const long MaxDocumentSize = 50 * 1024 * 1024; // 50MB для документів
    private const long MaxVideoSize = 100 * 1024 * 1024; // 100MB для відео
    private const long MaxAudioSize = 20 * 1024 * 1024; // 20MB для аудіо
    private const long MaxArchiveSize = 50 * 1024 * 1024; // 50MB для архівів
    private const long MaxOtherSize = 25 * 1024 * 1024; // 25MB для інших файлів

    // Дозволені розширення файлів
    private static readonly string[] AllowedExtensions = new[]
    {
        // Images
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp",
        // Documents
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".rtf",
        // Videos
        ".mp4", ".avi", ".mpeg", ".mpg", ".mov", ".webm",
        // Audio
        ".mp3", ".wav", ".ogg", ".aac", ".flac",
        // Archives
        ".zip", ".rar", ".7z"
    };

    public UploadFileCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("Назва файла обов'язкова")
            .MaximumLength(255)
            .WithMessage("Назва файла не може перевищувати 255 символів")
            .Must(HaveValidExtension)
            .WithMessage($"Непідтримуване розширення файла. Дозволені: {string.Join(", ", AllowedExtensions)}");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithMessage("MIME тип файла обов'язковий");

        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .WithMessage("Розмір файла має бути більше 0")
            .Must((command, fileSize) => IsFileSizeValid(command.FileName, fileSize))
            .WithMessage("Розмір файла перевищує дозволений ліміт для даного типу");

        RuleFor(x => x.UploadedByUserId)
            .GreaterThan(0)
            .WithMessage("ID користувача має бути більше 0");

        RuleFor(x => x.AppealId)
            .GreaterThan(0)
            .When(x => x.AppealId.HasValue)
            .WithMessage("ID апела має бути більше 0");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Опис файла не може перевищувати 500 символів");

        RuleFor(x => x.FileStream)
            .NotNull()
            .WithMessage("Потік файла обов'язковий");
    }

    private static bool HaveValidExtension(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension);
    }

    private static bool IsFileSizeValid(string fileName, long fileSize)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var fileType = FileTypeExtensions.FromExtension(extension);

        return fileType switch
        {
            FileType.Image => fileSize <= MaxImageSize,
            FileType.Document => fileSize <= MaxDocumentSize,
            FileType.Video => fileSize <= MaxVideoSize,
            FileType.Audio => fileSize <= MaxAudioSize,
            FileType.Archive => fileSize <= MaxArchiveSize,
            FileType.Other => fileSize <= MaxOtherSize,
            FileType.Unknown => fileSize <= MaxOtherSize,
            _ => fileSize <= MaxOtherSize
        };
    }
}