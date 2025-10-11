using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Events.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Events.Commands.CreateEvent;

/// <summary>
/// Handler для створення нової події з підтримкою сповіщень
/// </summary>
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Result<EventDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CreateEventCommandHandler> _logger;

    public CreateEventCommandHandler(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILogger<CreateEventCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result<EventDto>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Creating event with title: {Title} by organizer: {OrganizerId}",
                request.Title,
                request.OrganizerId
            );

            // Отримуємо інформацію про організатора
            var organizer = await _unitOfWork.Users.GetByTelegramIdAsync(request.OrganizerId, cancellationToken);
            if (organizer == null)
            {
                _logger.LogWarning("Organizer with ID {OrganizerId} not found", request.OrganizerId);
                return Result<EventDto>.Fail("Організатор не знайдений");
            }

            // Створюємо подію через domain factory
            var eventEntity = Domain.Entities.Event.Create(
                title: request.Title,
                description: request.Description,
                category: request.Category,
                type: request.Type,
                startDate: request.EventDate,
                organizerId: request.OrganizerId,
                organizerName: organizer.FirstName ?? organizer.Username ?? "Організатор",
                summary: request.Summary,
                endDate: request.EndDate,
                location: request.Location,
                address: null, // Address можна розширити через додаткове поле в запиті
                maxParticipants: request.MaxParticipants,
                requiresRegistration: request.RequiresRegistration,
                registrationDeadline: request.RegistrationDeadline,
                contactPerson: null, // Contact person можна розширити в майбутньому
                contactInfo: request.ContactInfo,
                photoFileId: request.Attachments.FirstOrDefault(a => a.FileType == Domain.Enums.FileType.Image)?.FileId,
                publishImmediately: request.PublishImmediately
            );

            // Додаємо всі attachments до події
            foreach (var attachment in request.Attachments)
            {
                eventEntity.AddEventAttachment(attachment.FileId, attachment.FileType, attachment.FileName);
            }

            // Зберігаємо в базі
            await _unitOfWork.Events.AddAsync(eventEntity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully created event with ID: {EventId}",
                eventEntity.Id
            );

            // Відправляємо сповіщення про нову подію, якщо потрібно
            if (request.SendNotification && eventEntity.IsPublished)
            {
                _logger.LogInformation(
                    "Sending notification for event: {EventId}",
                    eventEntity.Id
                );

                try
                {
                    var notificationResult = await _notificationService.SendEventCreatedNotificationAsync(
                        eventId: eventEntity.Id,
                        title: eventEntity.Title,
                        summary: eventEntity.Summary ?? (eventEntity.Description.Length > 200 
                            ? eventEntity.Description.Substring(0, 197) + "..." 
                            : eventEntity.Description),
                        eventDate: eventEntity.StartDate,
                        location: eventEntity.Location,
                        photoFileId: eventEntity.PhotoFileId,
                        cancellationToken: cancellationToken
                    );

                    if (notificationResult.IsSuccess)
                    {
                        _logger.LogInformation(
                            "Successfully sent notification for event {EventId} to {UserCount} users",
                            eventEntity.Id,
                            notificationResult.Value
                        );
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to send notification for event {EventId}: {Error}",
                            eventEntity.Id,
                            notificationResult.Error
                        );
                    }
                }
                catch (Exception notifEx)
                {
                    // Не припиняємо створення події через помилку сповіщень
                    _logger.LogError(notifEx,
                        "Error sending notification for event {EventId}",
                        eventEntity.Id
                    );
                }
            }

            // Конвертуємо в DTO
            var eventDto = new EventDto
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                Summary = eventEntity.Summary,
                Type = eventEntity.Type,
                Category = eventEntity.Category,
                StartDate = eventEntity.StartDate,
                EndDate = eventEntity.EndDate,
                Location = eventEntity.Location ?? string.Empty,
                MaxParticipants = eventEntity.MaxParticipants,
                CurrentParticipants = eventEntity.CurrentParticipants,
                RequiresRegistration = eventEntity.RequiresRegistration,
                RegistrationDeadline = eventEntity.RegistrationDeadline,
                Status = eventEntity.Status,
                IsFeatured = eventEntity.IsFeatured,
                PhotoFileId = eventEntity.PhotoFileId,
                CreatedAt = eventEntity.CreatedAt,
                UpdatedAt = eventEntity.UpdatedAt,
                OrganizerId = eventEntity.OrganizerId,
                OrganizerName = eventEntity.OrganizerName,
                Price = request.Price,
                Currency = request.Currency,
                Requirements = request.Requirements,
                ContactInfo = request.ContactInfo,
                Tags = request.Tags ?? string.Empty,
                Language = request.Language
            };

            return Result<EventDto>.Ok(eventDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating event with title: {Title}",
                request.Title
            );
            return Result<EventDto>.Fail("Помилка при створенні події");
        }
    }
}