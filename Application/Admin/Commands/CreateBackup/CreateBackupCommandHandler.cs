using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;
using System.IO.Compression;

namespace StudentUnionBot.Application.Admin.Commands.CreateBackup;

public class CreateBackupCommandHandler : IRequestHandler<CreateBackupCommand, Result<BackupResultDto>>
{
    private readonly IBackupService _backupService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateBackupCommandHandler> _logger;

    public CreateBackupCommandHandler(
        IBackupService backupService,
        IUnitOfWork unitOfWork,
        ILogger<CreateBackupCommandHandler> logger)
    {
        _backupService = backupService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<BackupResultDto>> Handle(CreateBackupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Перевіряємо, чи користувач є адміністратором
            var user = await _unitOfWork.Users.GetByTelegramIdAsync(request.AdminId, cancellationToken);
            if (user == null)
            {
                return Result<BackupResultDto>.Fail("Користувача не знайдено");
            }

            if (user.Role < Domain.Enums.UserRole.Admin)
            {
                return Result<BackupResultDto>.Fail("Недостатньо прав для створення резервної копії");
            }

            // Створюємо резервну копію
            var backupResult = await _backupService.CreateBackupAsync(cancellationToken);

            if (!backupResult.IsSuccess)
            {
                return Result<BackupResultDto>.Fail(backupResult.Error);
            }

            var backupInfo = backupResult.Value!;
            
            _logger.LogInformation(
                "Створено резервну копію БД адміністратором {AdminId}. Файл: {FileName}, Розмір: {Size} bytes",
                request.AdminId,
                backupInfo.BackupFileName,
                backupInfo.FileSizeBytes);

            return Result<BackupResultDto>.Ok(backupInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при створенні резервної копії БД адміністратором {AdminId}", request.AdminId);
            return Result<BackupResultDto>.Fail("Виникла помилка при створенні резервної копії");
        }
    }
}