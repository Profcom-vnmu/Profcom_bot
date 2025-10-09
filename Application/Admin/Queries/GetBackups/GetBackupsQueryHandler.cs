using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Admin.Queries.GetBackups;

public class GetBackupsQueryHandler : IRequestHandler<GetBackupsQuery, Result<List<BackupInfoDto>>>
{
    private readonly IBackupService _backupService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetBackupsQueryHandler> _logger;

    public GetBackupsQueryHandler(
        IBackupService backupService,
        IUnitOfWork unitOfWork,
        ILogger<GetBackupsQueryHandler> logger)
    {
        _backupService = backupService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<BackupInfoDto>>> Handle(GetBackupsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Перевіряємо, чи користувач є адміністратором
            var user = await _unitOfWork.Users.GetByTelegramIdAsync(request.AdminId, cancellationToken);
            if (user == null)
            {
                return Result<List<BackupInfoDto>>.Fail("Користувача не знайдено");
            }

            if (user.Role < Domain.Enums.UserRole.Admin)
            {
                return Result<List<BackupInfoDto>>.Fail("Недостатньо прав для перегляду резервних копій");
            }

            // Отримуємо список резервних копій
            return await _backupService.GetAvailableBackupsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні списку резервних копій для адміністратора {AdminId}", request.AdminId);
            return Result<List<BackupInfoDto>>.Fail("Виникла помилка при отриманні списку резервних копій");
        }
    }
}