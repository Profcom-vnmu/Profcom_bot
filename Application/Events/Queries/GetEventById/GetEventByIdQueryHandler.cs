using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Events.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Events.Queries.GetEventById;

public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, Result<EventDetailsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStudentUnionCacheService _cacheService;
    private readonly ILogger<GetEventByIdQueryHandler> _logger;

    public GetEventByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IStudentUnionCacheService cacheService,
        ILogger<GetEventByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<EventDetailsDto>> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Спробуємо отримати базову інформацію події з кешу (без інформації про конкретного користувача)
            if (!request.UserId.HasValue)
            {
                var cachedEvent = await _cacheService.GetEventAsync<EventDetailsDto>(request.EventId, cancellationToken);
                if (cachedEvent != null)
                {
                    _logger.LogDebug("Event {EventId} отримано з кешу", request.EventId);
                    return Result<EventDetailsDto>.Ok(cachedEvent);
                }
            }

            var @event = await _unitOfWork.Events.GetByIdWithParticipantsAsync(request.EventId, cancellationToken);
            if (@event == null)
            {
                return Result<EventDetailsDto>.Fail("Події не знайдено");
            }

            bool isUserRegistered = false;
            if (request.UserId.HasValue)
            {
                isUserRegistered = @event.IsUserRegistered(request.UserId.Value);
            }

            bool canRegister = @event.RequiresRegistration &&
                               @event.CanRegisterParticipant() &&
                               (!request.UserId.HasValue || !isUserRegistered);

            var dto = new EventDetailsDto
            {
                Id = @event.Id,
                Title = @event.Title,
                Description = @event.Description,
                Type = @event.Type,
                StartDate = @event.StartDate,
                EndDate = @event.EndDate,
                Location = @event.Location ?? string.Empty,
                MaxParticipants = @event.MaxParticipants,
                CurrentParticipants = @event.CurrentParticipants,
                RequiresRegistration = @event.RequiresRegistration,
                RegistrationDeadline = @event.RegistrationDeadline,
                Status = @event.Status,
                IsFeatured = @event.IsFeatured,
                PhotoFileId = @event.PhotoFileId,
                CreatedAt = @event.CreatedAt,
                IsUserRegistered = isUserRegistered,
                CanRegister = canRegister
            };

            // Кешуємо тільки базову інформацію без користувацьких даних
            if (!request.UserId.HasValue)
            {
                var cacheDto = new EventDetailsDto
                {
                    Id = dto.Id,
                    Title = dto.Title,
                    Description = dto.Description,
                    Type = dto.Type,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Location = dto.Location,
                    MaxParticipants = dto.MaxParticipants,
                    CurrentParticipants = dto.CurrentParticipants,
                    RequiresRegistration = dto.RequiresRegistration,
                    RegistrationDeadline = dto.RegistrationDeadline,
                    Status = dto.Status,
                    IsFeatured = dto.IsFeatured,
                    PhotoFileId = dto.PhotoFileId,
                    CreatedAt = dto.CreatedAt,
                    IsUserRegistered = false, // Базове значення для кешу
                    CanRegister = dto.RequiresRegistration && @event.CanRegisterParticipant()
                };

                await _cacheService.SetEventAsync(request.EventId, cacheDto, cancellationToken);
                _logger.LogDebug("Event {EventId} закешовано", request.EventId);
            }

            return Result<EventDetailsDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event {EventId}", request.EventId);
            return Result<EventDetailsDto>.Fail("Помилка при отриманні деталей події");
        }
    }
}
