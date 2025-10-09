using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Admin.Commands.RestoreBackup;

public class RestoreBackupCommandHandler : IRequestHandler<RestoreBackupCommand, Result<bool>>
{
    private readonly IBackupService _backupService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RestoreBackupCommandHandler> _logger;

    public RestoreBackupCommandHandler(
        IBackupService backupService,
        IUnitOfWork unitOfWork,
        ILogger<RestoreBackupCommandHandler> logger)
    {
        _backupService = backupService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(RestoreBackupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Перевіряємо, чи користувач є адміністратором
            var user = await _unitOfWork.Users.GetByTelegramIdAsync(request.AdminId, cancellationToken);
            if (user == null)
            {
                return Result<bool>.Fail("Користувача не знайдено");
            }

            if (user.Role < Domain.Enums.UserRole.Admin)
            {
                return Result<bool>.Fail("Недостатньо прав для відновлення бази даних");
            }

            // Відновлюємо базу даних
            var restoreResult = await _backupService.RestoreBackupAsync(request.BackupFilePath, cancellationToken);

            if (!restoreResult.IsSuccess)
            {
                return Result<bool>.Fail(restoreResult.Error);
            }
            
            _logger.LogWarning(
                "УВАГА: БД відновлено з резервної копії адміністратором {AdminId}. Файл: {BackupPath}",
                request.AdminId,
                request.BackupFilePath);

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відновленні БД з резервної копії адміністратором {AdminId}", request.AdminId);
            return Result<bool>.Fail("Виникла помилка при відновленні бази даних");
        }
    }
}