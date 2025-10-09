using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Events.Commands.RegisterForEvent;

public class RegisterForEventCommandHandler : IRequestHandler<RegisterForEventCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegisterForEventCommandHandler> _logger;

    public RegisterForEventCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<RegisterForEventCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(RegisterForEventCommand request, CancellationToken cancellationToken)
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

            var registrationResult = @event.RegisterParticipant(user);
            if (!registrationResult.IsSuccess)
            {
                return Result<bool>.Fail(registrationResult.Error);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} registered for event {EventId}", request.UserId, request.EventId);

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {UserId} for event {EventId}", request.UserId, request.EventId);
            return Result<bool>.Fail("Помилка при реєстрації на подію");
        }
    }
}
