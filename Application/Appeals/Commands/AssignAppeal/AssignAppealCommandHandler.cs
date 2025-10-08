using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Appeals.Commands.AssignAppeal;

/// <summary>
/// Обробник команди призначення звернення
/// </summary>
public class AssignAppealCommandHandler : IRequestHandler<AssignAppealCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignAppealCommandHandler> _logger;

    public AssignAppealCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<AssignAppealCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(AssignAppealCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Адміністратор {RequestAdminId} призначає звернення {AppealId} адміністратору {AssignToAdminId}",
                request.RequestAdminId,
                request.AppealId,
                request.AssignToAdminId);

            // Перевіряємо чи існує адміністратор який виконує дію
            var requestAdmin = await _unitOfWork.Users.GetByTelegramIdAsync(request.RequestAdminId, cancellationToken);
            if (requestAdmin == null || requestAdmin.Role != UserRole.Admin)
            {
                _logger.LogWarning("Користувач {RequestAdminId} не є адміністратором", request.RequestAdminId);
                return Result<bool>.Fail("У вас немає прав адміністратора");
            }

            // Перевіряємо чи існує адміністратор якому призначається
            if (request.AssignToAdminId.HasValue)
            {
                var assignToAdmin = await _unitOfWork.Users.GetByTelegramIdAsync(request.AssignToAdminId.Value, cancellationToken);
                if (assignToAdmin == null || assignToAdmin.Role != UserRole.Admin)
                {
                    _logger.LogWarning("Користувач {AssignToAdminId} не є адміністратором", request.AssignToAdminId);
                    return Result<bool>.Fail("Вказаний користувач не є адміністратором");
                }
            }

            // Перевіряємо чи існує звернення
            var appeal = await _unitOfWork.Appeals.GetByIdAsync(request.AppealId, cancellationToken);
            if (appeal == null)
            {
                _logger.LogWarning("Звернення {AppealId} не знайдено", request.AppealId);
                return Result<bool>.Fail("Звернення не знайдено");
            }

            // Оновлюємо призначення через доменний метод
            appeal.AssignTo(request.AssignToAdminId);

            _unitOfWork.Appeals.Update(appeal);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var action = request.AssignToAdminId.HasValue ? "призначено" : "знято призначення";
            _logger.LogInformation(
                "Звернення {AppealId} успішно {Action} адміністратором {RequestAdminId}",
                request.AppealId,
                action,
                request.RequestAdminId);

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Помилка при призначенні звернення {AppealId}",
                request.AppealId);
            return Result<bool>.Fail("Виникла помилка при призначенні звернення");
        }
    }
}
