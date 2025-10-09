using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Events.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Events.Queries.GetAllEvents;

/// <summary>
/// Обробник запиту для отримання всіх подій (для адміністраторів)
/// </summary>
public class GetAllEventsQueryHandler : IRequestHandler<GetAllEventsQuery, Result<EventListDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<GetAllEventsQueryHandler> _logger;

    public GetAllEventsQueryHandler(
        IEventRepository eventRepository,
        ILogger<GetAllEventsQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<Result<EventListDto>> Handle(GetAllEventsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Отримання всіх подій: Category={Category}, Type={Type}, Status={Status}, Page={Page}, PageSize={PageSize}",
                request.Category, request.Type, request.Status, request.PageNumber, request.PageSize);

            // Отримання подій з репозиторію
            var events = await _eventRepository.GetAllEventsAsync(
                category: request.Category,
                type: request.Type,
                status: request.Status,
                sortByDateAsc: request.SortByDateAsc,
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                cancellationToken: cancellationToken);

            // Підрахунок загальної кількості
            var totalCount = await _eventRepository.GetAllEventsCountAsync(
                category: request.Category,
                type: request.Type,
                status: request.Status,
                cancellationToken: cancellationToken);

            // Маппінг на DTO
            var eventsDto = events.Select(MapToDto).ToList();

            var result = new EventListDto
            {
                Items = eventsDto,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            _logger.LogInformation("Знайдено {Count} подій з {TotalCount}", eventsDto.Count, totalCount);

            return Result<EventListDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні всіх подій");
            return Result<EventListDto>.Fail("Не вдалося завантажити події");
        }
    }

    private static EventDto MapToDto(Domain.Entities.Event eventEntity)
    {
        return new EventDto
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
            Price = 0, // Default value since Event entity doesn't have this field
            Currency = "UAH", // Default value
            Requirements = null, // Default value
            ContactInfo = eventEntity.ContactInfo,
            Tags = null, // Default value
            Language = Language.Ukrainian, // Default value
            AttachmentFileIds = new List<string>() // TODO: Implement if needed
        };
    }
}