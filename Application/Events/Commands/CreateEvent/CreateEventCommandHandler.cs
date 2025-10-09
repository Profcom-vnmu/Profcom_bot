using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Events.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Events.Commands.CreateEvent;

/// <summary>
/// Handler для створення нової події
/// </summary>
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Result<EventDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateEventCommandHandler> _logger;

    public CreateEventCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateEventCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
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

            // TODO: Перевіряємо права на створення подій
            // if (!organizer.CanCreateEvents)

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
                // TODO: Додати address field в Entity
                address: null,
                maxParticipants: request.MaxParticipants,
                requiresRegistration: request.RequiresRegistration,
                registrationDeadline: request.RegistrationDeadline,
                // TODO: Розділити ContactInfo на Person та Info
                contactPerson: null,
                contactInfo: request.ContactInfo,
                photoFileId: GetFirstImageFile(request.AttachmentFileIds),
                publishImmediately: request.PublishImmediately
            );

            // Зберігаємо в базі
            await _unitOfWork.Events.AddAsync(eventEntity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully created event with ID: {EventId}",
                eventEntity.Id
            );

            // TODO: Якщо потрібно відправити повідомлення
            if (request.SendNotification && eventEntity.IsPublished)
            {
                _logger.LogInformation(
                    "Scheduling notification for event: {EventId}",
                    eventEntity.Id
                );
                // Тут буде логіка відправки повідомлень через NotificationService
            }

            // Конвертуємо в DTO
            var eventDto = new EventDto
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                Summary = eventEntity.Summary,
                Type = eventEntity.Type,
                Category = request.Category, // TODO: Додати в Entity
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
                Language = request.Language,
                AttachmentFileIds = request.AttachmentFileIds
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

    /// <summary>
    /// Отримує перший файл зображення з прикріплених файлів
    /// </summary>
    private string? GetFirstImageFile(List<string> fileIds)
    {
        // TODO: Додати логіку визначення типу файлу
        // Поки що повертаємо перший файл, якщо він є
        return fileIds.FirstOrDefault();
    }
}