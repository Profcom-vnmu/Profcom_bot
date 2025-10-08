using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Appeals.Commands.ReplyToAppeal;

/// <summary>
/// Обробник команди відповіді на звернення
/// </summary>
public class ReplyToAppealCommandHandler : IRequestHandler<ReplyToAppealCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReplyToAppealCommandHandler> _logger;

    public ReplyToAppealCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ReplyToAppealCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(ReplyToAppealCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Адміністратор {AdminId} відповідає на звернення {AppealId}",
                request.AdminId,
                request.AppealId);

            // Перевіряємо чи існує адміністратор
            var admin = await _unitOfWork.Users.GetByTelegramIdAsync(request.AdminId, cancellationToken);
            if (admin == null || admin.Role != UserRole.Admin)
            {
                _logger.LogWarning("Користувач {AdminId} не є адміністратором", request.AdminId);
                return Result<int>.Fail("У вас немає прав адміністратора");
            }

            // Перевіряємо чи існує звернення
            var appeal = await _unitOfWork.Appeals.GetByIdAsync(request.AppealId, cancellationToken);
            if (appeal == null)
            {
                _logger.LogWarning("Звернення {AppealId} не знайдено", request.AppealId);
                return Result<int>.Fail("Звернення не знайдено");
            }

            // Перевіряємо чи звернення не закрите
            if (appeal.Status == AppealStatus.Closed || appeal.Status == AppealStatus.Resolved)
            {
                _logger.LogWarning(
                    "Спроба відповісти на закрите звернення {AppealId}",
                    request.AppealId);
                return Result<int>.Fail("Неможливо відповісти на закрите звернення");
            }

            // Створюємо повідомлення
            var message = AppealMessage.Create(
                appealId: request.AppealId,
                senderId: request.AdminId,
                senderName: request.AdminName,
                isFromAdmin: true,
                text: request.Text,
                photoFileId: request.PhotoFileId,
                documentFileId: request.DocumentFileId,
                documentFileName: request.DocumentFileName
            );

            // Додаємо повідомлення через доменний метод
            appeal.AddMessage(message);
            
            // Встановлюємо час першої відповіді
            appeal.SetFirstResponse();

            // Якщо звернення не призначене - призначаємо поточному адміну
            if (appeal.AssignedToAdminId == null)
            {
                appeal.AssignTo(request.AdminId);
                _logger.LogInformation(
                    "Звернення {AppealId} автоматично призначено адміністратору {AdminId}",
                    request.AppealId,
                    request.AdminId);
            }
            _unitOfWork.Appeals.Update(appeal);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Адміністратор {AdminId} успішно відповів на звернення {AppealId}, створено повідомлення {MessageId}",
                request.AdminId,
                request.AppealId,
                message.Id);

            return Result<int>.Ok(message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Помилка при відповіді адміністратора {AdminId} на звернення {AppealId}",
                request.AdminId,
                request.AppealId);
            return Result<int>.Fail("Виникла помилка при відправці повідомлення");
        }
    }
}
