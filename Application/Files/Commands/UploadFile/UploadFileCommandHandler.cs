using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Files.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using System.Security.Cryptography;

namespace StudentUnionBot.Application.Files.Commands.UploadFile;

/// <summary>
/// Обробник команди завантаження файла
/// </summary>
public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, Result<FileAttachmentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileValidationService _fileValidationService;
    private readonly ILogger<UploadFileCommandHandler> _logger;

    public UploadFileCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        IFileValidationService fileValidationService,
        ILogger<UploadFileCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _fileValidationService = fileValidationService;
        _logger = logger;
    }

    public async Task<Result<FileAttachmentDto>> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Початок завантаження файла {FileName} користувачем {UserId}", 
                request.FileName, request.UploadedByUserId);

            // Перевіряємо чи існує користувач
            var user = await _unitOfWork.Users.GetByTelegramIdAsync(request.UploadedByUserId, cancellationToken);
            if (user == null)
            {
                return Result<FileAttachmentDto>.Fail("Користувач не знайдений");
            }

            // Валідуємо файл
            var validationResult = await _fileValidationService.ValidateFileAsync(
                request.FileName, request.FileStream, request.ContentType, cancellationToken);

            if (!validationResult.IsSuccess)
            {
                return Result<FileAttachmentDto>.Fail(validationResult.Error);
            }

            var validation = validationResult.Value;
            if (validation == null || !validation.IsValid)
            {
                var errorMessage = validation?.Errors != null ? string.Join(", ", validation.Errors) : "Невідома помилка валідації";
                return Result<FileAttachmentDto>.Fail($"Файл не пройшов валідацію: {errorMessage}");
            }

            // Скидаємо позицію потоку
            request.FileStream.Position = 0;

            // Обчислюємо хеш файла для дедуплікації
            var fileHash = await ComputeFileHashAsync(request.FileStream, cancellationToken);
            request.FileStream.Position = 0;

            // Перевіряємо чи файл уже існує
            var fileRepo = _unitOfWork.GetRepository<FileAttachment>();
            var existingFile = await ((IFileAttachmentRepository)fileRepo)
                .GetByHashAsync(fileHash, cancellationToken);

            if (existingFile != null && !existingFile.IsDeleted)
            {
                _logger.LogInformation("Знайдено дублікат файла {FileHash}", fileHash);

                // Якщо потрібно прикріпити до апела - створюємо зв'язок
                if (request.AppealId.HasValue)
                {
                    await AttachFileToAppealAsync(existingFile.Id, request.AppealId.Value, 
                        request.UploadedByUserId, request.Description, request.IsEvidence, cancellationToken);
                }

                return Result<FileAttachmentDto>.Ok(MapToDto(existingFile, user));
            }

            // Зберігаємо файл у сховищі
            var storeResult = await _fileStorageService.StoreFileAsync(
                request.FileName, request.FileStream, request.ContentType, cancellationToken);

            if (!storeResult.IsSuccess)
            {
                return Result<FileAttachmentDto>.Fail($"Помилка збереження файла: {storeResult.Error}");
            }

            var storedFile = storeResult.Value;

            // Створюємо запис у базі даних
            var fileAttachment = FileAttachment.Create(
                storedFile.FileName,
                request.FileName,
                storedFile.FilePath,
                storedFile.ContentType,
                storedFile.FileSize,
                storedFile.FileType,
                fileHash,
                request.UploadedByUserId,
                storedFile.IsCompressed,
                storedFile.ThumbnailPath);

            await fileRepo.AddAsync(fileAttachment, cancellationToken);

            // Прикріплюємо до апела якщо потрібно
            if (request.AppealId.HasValue)
            {
                await AttachFileToAppealAsync(fileAttachment.Id, request.AppealId.Value, 
                    request.UploadedByUserId, request.Description, request.IsEvidence, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Запускаємо сканування файла асинхронно (якщо не пропущено)
            if (!request.SkipVirusScan)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var scanResult = await _fileValidationService.ScanFileAsync(storedFile.FilePath, CancellationToken.None);
                        if (scanResult.IsSuccess && scanResult.Value != null)
                        {
                            fileAttachment.UpdateScanResult(scanResult.Value.Status, scanResult.Value.Message);
                            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Помилка при сканування файла {FilePath}", storedFile.FilePath);
                        fileAttachment.UpdateScanResult(ScanStatus.Error, "Помилка сканування");
                        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
                    }
                }, CancellationToken.None);
            }
            else
            {
                fileAttachment.UpdateScanResult(ScanStatus.Skipped, "Сканування пропущено");
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Файл {FileName} успішно завантажено з ID {FileId}", 
                request.FileName, fileAttachment.Id);

            return Result<FileAttachmentDto>.Ok(MapToDto(fileAttachment, user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при завантаженні файла {FileName}", request.FileName);
            return Result<FileAttachmentDto>.Fail("Не вдалося завантажити файл. Спробуйте пізніше.");
        }
    }

    private async Task AttachFileToAppealAsync(
        int fileId, 
        int appealId, 
        long attachedByUserId, 
        string? description, 
        bool isEvidence,
        CancellationToken cancellationToken)
    {
        // Перевіряємо чи існує апел
        var appeal = await _unitOfWork.Appeals.GetByIdAsync(appealId, cancellationToken);
        if (appeal == null)
        {
            throw new InvalidOperationException("Апел не знайдений");
        }

        // Створюємо зв'язок
        var appealFileRepo = _unitOfWork.GetRepository<AppealFileAttachment>();
        var appealFileAttachment = AppealFileAttachment.Create(
            appealId, fileId, attachedByUserId, description, isEvidence);

        await appealFileRepo.AddAsync(appealFileAttachment, cancellationToken);
    }

    private static async Task<string> ComputeFileHashAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes);
    }

    private static FileAttachmentDto MapToDto(FileAttachment file, BotUser user)
    {
        return new FileAttachmentDto
        {
            Id = file.Id,
            FileName = file.FileName,
            OriginalFileName = file.OriginalFileName,
            ContentType = file.ContentType,
            FileSize = file.FileSize,
            FormattedFileSize = file.GetFormattedFileSize(),
            FileType = file.FileType,
            FileTypeIcon = file.FileType.GetIcon(),
            FileTypeName = file.FileType.GetDisplayName(),
            FileHash = file.FileHash,
            IsCompressed = file.IsCompressed,
            ThumbnailPath = file.ThumbnailPath,
            HasThumbnail = !string.IsNullOrEmpty(file.ThumbnailPath),
            UploadedByUserId = file.UploadedByUserId,
            UploadedByUserName = user.FullName ?? user.FirstName ?? "Невідомий",
            UploadedAt = file.UploadedAt,
            ScanStatus = file.ScanStatus,
            ScanStatusIcon = file.ScanStatus.GetIcon(),
            ScanStatusName = file.ScanStatus.GetDisplayName(),
            ScanResult = file.ScanResult,
            ScannedAt = file.ScannedAt,
            IsDeleted = file.IsDeleted,
            DeletedAt = file.DeletedAt,
            IsSafeToDownload = file.IsSafeToDownload
        };
    }
}