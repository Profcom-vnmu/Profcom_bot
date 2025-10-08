using MediatR;
using StudentUnionBot.Application.Appeals.DTOs;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Appeals.Queries.GetAppealById;

/// <summary>
/// Запит для отримання детальної інформації про звернення
/// </summary>
public class GetAppealByIdQuery : IRequest<Result<AppealDetailsDto>>
{
    public int AppealId { get; set; }
    public long RequestUserId { get; set; }
}
