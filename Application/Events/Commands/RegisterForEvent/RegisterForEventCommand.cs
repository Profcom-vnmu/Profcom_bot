using MediatR;
using StudentUnionBot.Application.Common.Attributes;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Events.Commands.RegisterForEvent;

[RequirePermission(Permission.RegisterForEvent)]
[RateLimit("RegisterEvent")]
public record RegisterForEventCommand(long UserId, int EventId) : IRequest<Result<bool>>;
