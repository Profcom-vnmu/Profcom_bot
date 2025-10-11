using MediatR;
using StudentUnionBot.Application.Common.Attributes;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Events.Commands.UnregisterFromEvent;

[RequirePermission(Permission.UnregisterFromEvent)]
public record UnregisterFromEventCommand(long UserId, int EventId) : IRequest<Result<bool>>;
