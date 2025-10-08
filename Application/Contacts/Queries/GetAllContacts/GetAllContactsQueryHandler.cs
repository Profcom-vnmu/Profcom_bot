using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Contacts.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Contacts.Queries.GetAllContacts;

/// <summary>
/// Handler для отримання всіх контактів
/// </summary>
public class GetAllContactsQueryHandler : IRequestHandler<GetAllContactsQuery, Result<ContactListDto>>
{
    private readonly IContactInfoRepository _contactRepository;
    private readonly ILogger<GetAllContactsQueryHandler> _logger;

    public GetAllContactsQueryHandler(
        IContactInfoRepository contactRepository,
        ILogger<GetAllContactsQueryHandler> logger)
    {
        _contactRepository = contactRepository;
        _logger = logger;
    }

    public async Task<Result<ContactListDto>> Handle(GetAllContactsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Отримання контактної інформації");

            var contacts = await _contactRepository.GetAllActiveAsync(cancellationToken);

            var contactDtos = contacts.Select(c => new ContactDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                TelegramUsername = c.TelegramUsername,
                Address = c.Address,
                WorkingHours = c.WorkingHours,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            }).ToList();

            var result = new ContactListDto
            {
                Items = contactDtos,
                TotalCount = contactDtos.Count
            };

            _logger.LogInformation("Знайдено {Count} контактів", contactDtos.Count);

            return Result<ContactListDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні контактів");
            return Result<ContactListDto>.Fail("Не вдалося завантажити контакти");
        }
    }
}
