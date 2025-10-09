using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Events.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Events.Queries.GetUpcomingEvents;

/// <summary>
/// Handler для отримання майбутніх подій з кешуванням
/// </summary>
public class GetUpcomingEventsQueryHandler : IRequestHandler<GetUpcomingEventsQuery, Result<EventListDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IStudentUnionCacheService _cacheService;
    private readonly ILogger<GetUpcomingEventsQueryHandler> _logger;

    public GetUpcomingEventsQueryHandler(
        IEventRepository eventRepository,
        IStudentUnionCacheService cacheService,
        ILogger<GetUpcomingEventsQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<EventListDto>> Handle(GetUpcomingEventsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Отримання майбутніх подій: тип={Type}, сторінка={Page}, розмір={Size}, рекомендовані={OnlyFeatured}",
                request.Type, request.PageNumber, request.PageSize, request.OnlyFeatured);

            // Генеруємо ключ для кешу
            var filter = GenerateFilterKey(request);
            
            // Спробуємо отримати з кешу
            var cachedResult = await _cacheService.GetEventsListAsync<EventListDto>(
                request.PageNumber,
                request.PageSize,
                filter,
                cancellationToken);

            if (cachedResult != null)
            {
                _logger.LogDebug("Події отримано з кешу для фільтру: {Filter}", filter);
                return Result<EventListDto>.Ok(cachedResult);
            }

            // Отримуємо події з БД
            var events = await _eventRepository.GetUpcomingEventsAsync(
                request.Type,
                request.OnlyFeatured,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var totalCount = await _eventRepository.GetUpcomingEventsCountAsync(
                request.Type,
                request.OnlyFeatured,
                cancellationToken);

            var eventDtos = events.Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                Type = e.Type,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location ?? string.Empty,
                MaxParticipants = e.MaxParticipants,
                CurrentParticipants = e.CurrentParticipants,
                RequiresRegistration = e.RequiresRegistration,
                RegistrationDeadline = e.RegistrationDeadline,
                Status = e.Status,
                IsFeatured = e.IsFeatured,
                PhotoFileId = e.PhotoFileId,
                CreatedAt = e.CreatedAt
            }).ToList();

            var result = new EventListDto
            {
                Items = eventDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            // Кешуємо результат
            await _cacheService.SetEventsListAsync(
                request.PageNumber,
                request.PageSize,
                result,
                filter,
                cancellationToken);

            _logger.LogInformation("Знайдено {Count} подій (всього {Total}), результат закешовано", eventDtos.Count, totalCount);

            return Result<EventListDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні майбутніх подій");
            return Result<EventListDto>.Fail("Не вдалося завантажити події");
        }
    }

    private static string GenerateFilterKey(GetUpcomingEventsQuery request)
    {
        var typePart = request.Type?.ToString() ?? "all";
        var featuredPart = request.OnlyFeatured ? "featured" : "all";
        return $"upcoming_events:{typePart}:{featuredPart}";
    }
}
