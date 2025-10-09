using MediatR;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Events.Commands.UnregisterFromEvent;

public record UnregisterFromEventCommand(long UserId, int EventId) : IRequest<Result<bool>>;
