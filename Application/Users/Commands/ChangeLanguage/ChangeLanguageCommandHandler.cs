using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Users.Commands.ChangeLanguage;

public class ChangeLanguageCommandHandler : IRequestHandler<ChangeLanguageCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChangeLanguageCommandHandler> _logger;

    public ChangeLanguageCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ChangeLanguageCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ChangeLanguageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByTelegramIdAsync(request.TelegramId, cancellationToken);
            if (user == null)
            {
                return Result<bool>.Fail("Користувача не знайдено");
            }

            user.SetLanguage(request.Language);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Користувач {TelegramId} змінив мову на {Language}",
                request.TelegramId,
                request.Language);

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при зміні мови для користувача {TelegramId}", request.TelegramId);
            return Result<bool>.Fail("Виникла помилка при зміні мови");
        }
    }
}