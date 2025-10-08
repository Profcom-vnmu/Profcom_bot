using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Appeals.Commands.UpdatePriority;

/// <summary>
/// Обробник команди зміни пріоритету звернення
/// </summary>
public class UpdatePriorityCommandHandler : IRequestHandler<UpdatePriorityCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdatePriorityCommandHandler> _logger;

    public UpdatePriorityCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdatePriorityCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(UpdatePriorityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Адміністратор {AdminId} змінює пріоритет звернення {AppealId} на {Priority}",
                request.AdminId,
                request.AppealId,
                request.Priority);

            // Перевіряємо чи існує адміністратор
            var admin = await _unitOfWork.Users.GetByTelegramIdAsync(request.AdminId, cancellationToken);
            if (admin == null || admin.Role != UserRole.Admin)
            {
                _logger.LogWarning("Користувач {AdminId} не є адміністратором", request.AdminId);
                return Result<bool>.Fail("У вас немає прав адміністратора");
            }

            // Перевіряємо чи існує звернення
            var appeal = await _unitOfWork.Appeals.GetByIdAsync(request.AppealId, cancellationToken);
            if (appeal == null)
            {
                _logger.LogWarning("Звернення {AppealId} не знайдено", request.AppealId);
                return Result<bool>.Fail("Звернення не знайдено");
            }

            // Зберігаємо старий пріоритет для логування
            var oldPriority = appeal.Priority;

            // Оновлюємо пріоритет через доменний метод
            appeal.UpdatePriority(request.Priority);

            _unitOfWork.Appeals.Update(appeal);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Пріоритет звернення {AppealId} змінено з {OldPriority} на {NewPriority} адміністратором {AdminId}",
                request.AppealId,
                oldPriority,
                request.Priority,
                request.AdminId);

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Помилка при зміні пріоритету звернення {AppealId}",
                request.AppealId);
            return Result<bool>.Fail("Виникла помилка при зміні пріоритету");
        }
    }
}
