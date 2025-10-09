using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Events.Commands.UnregisterFromEvent;

public class UnregisterFromEventCommandHandler : IRequestHandler<UnregisterFromEventCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnregisterFromEventCommandHandler> _logger;

    public UnregisterFromEventCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UnregisterFromEventCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(UnregisterFromEventCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var @event = await _unitOfWork.Events.GetByIdWithParticipantsAsync(request.EventId, cancellationToken);
            if (@event == null)
            {
                return Result<bool>.Fail("Події не знайдено");
            }

            var user = await _unitOfWork.Users.GetByTelegramIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result<bool>.Fail("Користувача не знайдено");
            }

            var unregistrationResult = @event.UnregisterParticipant(user);
            if (!unregistrationResult.IsSuccess)
            {
                return Result<bool>.Fail(unregistrationResult.Error);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} unregistered from event {EventId}", request.UserId, request.EventId);

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering user {UserId} from event {EventId}", request.UserId, request.EventId);
            return Result<bool>.Fail("Помилка при скасуванні реєстрації");
        }
    }
}
