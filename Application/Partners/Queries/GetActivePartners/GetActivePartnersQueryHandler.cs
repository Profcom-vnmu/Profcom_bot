using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Partners.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Partners.Queries.GetActivePartners;

/// <summary>
/// Handler для отримання активних партнерів
/// </summary>
public class GetActivePartnersQueryHandler : IRequestHandler<GetActivePartnersQuery, Result<PartnerListDto>>
{
    private readonly IPartnerRepository _partnerRepository;
    private readonly ILogger<GetActivePartnersQueryHandler> _logger;

    public GetActivePartnersQueryHandler(
        IPartnerRepository partnerRepository,
        ILogger<GetActivePartnersQueryHandler> logger)
    {
        _partnerRepository = partnerRepository;
        _logger = logger;
    }

    public async Task<Result<PartnerListDto>> Handle(GetActivePartnersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Отримання партнерів: тип={Type}, тільки featured={OnlyFeatured}",
                request.Type, request.OnlyFeatured);

            var partners = await _partnerRepository.GetActivePartnersAsync(
                request.Type,
                request.OnlyFeatured,
                cancellationToken);

            var partnerDtos = partners.Select(p => new PartnerDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Type = p.Type,
                DiscountInfo = p.DiscountInfo,
                Website = p.Website,
                PhoneNumber = p.PhoneNumber,
                Address = p.Address,
                LogoFileId = p.LogoFileId,
                IsActive = p.IsActive,
                IsFeatured = p.IsFeatured,
                CreatedAt = p.CreatedAt
            }).ToList();

            var result = new PartnerListDto
            {
                Items = partnerDtos,
                TotalCount = partnerDtos.Count
            };

            _logger.LogInformation("Знайдено {Count} партнерів", partnerDtos.Count);

            return Result<PartnerListDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні партнерів");
            return Result<PartnerListDto>.Fail("Не вдалося завантажити партнерів");
        }
    }
}
