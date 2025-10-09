using MediatR;
using StudentUnionBot.Application.Events.DTOs;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Events.Queries.GetEventById;

public record GetEventByIdQuery(int EventId, long? UserId = null) : IRequest<Result<EventDetailsDto>>;
