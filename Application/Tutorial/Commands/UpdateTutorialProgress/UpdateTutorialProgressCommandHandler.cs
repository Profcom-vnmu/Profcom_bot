using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Tutorial.Commands.UpdateTutorialProgress;

/// <summary>
/// Handler для оновлення прогресу туторіалу
/// </summary>
public class UpdateTutorialProgressCommandHandler : IRequestHandler<UpdateTutorialProgressCommand, Result<TutorialStep>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateTutorialProgressCommandHandler> _logger;

    public UpdateTutorialProgressCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateTutorialProgressCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<TutorialStep>> Handle(
        UpdateTutorialProgressCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Отримуємо користувача
            var user = await _unitOfWork.Users.GetByTelegramIdAsync(request.TelegramId, cancellationToken);
            
            if (user == null)
            {
                _logger.LogWarning("Користувача з TelegramId {TelegramId} не знайдено", request.TelegramId);
                return Result<TutorialStep>.Fail("Користувача не знайдено");
            }

            // Оновлюємо прогрес туторіалу
            user.UpdateTutorialProgress(request.Step);
            
            // EF Core автоматично відстежує зміни, просто зберігаємо
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation(
                "Користувач {TelegramId} оновив прогрес туторіалу до кроку {Step}",
                request.TelegramId,
                request.Step);
            
            return Result<TutorialStep>.Ok(request.Step);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при оновленні прогресу туторіалу для користувача {TelegramId}", request.TelegramId);
            return Result<TutorialStep>.Fail("Помилка при оновленні прогресу туторіалу");
        }
    }
}
