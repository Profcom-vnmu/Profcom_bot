using MediatR;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Events.Commands.RegisterForEvent;

public record RegisterForEventCommand(long UserId, int EventId) : IRequest<Result<bool>>;
