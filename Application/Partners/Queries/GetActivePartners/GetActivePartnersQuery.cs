using MediatR;
using StudentUnionBot.Application.Partners.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Partners.Queries.GetActivePartners;

/// <summary>
/// Query для отримання активних партнерів
/// </summary>
public class GetActivePartnersQuery : IRequest<Result<PartnerListDto>>
{
    /// <summary>
    /// Фільтр за типом партнера
    /// </summary>
    public PartnerType? Type { get; set; }

    /// <summary>
    /// Тільки виділені (featured) партнери
    /// </summary>
    public bool OnlyFeatured { get; set; }
}
