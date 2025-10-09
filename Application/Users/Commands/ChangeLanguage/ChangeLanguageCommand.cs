using MediatR;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Users.Commands.ChangeLanguage;

public class ChangeLanguageCommand : IRequest<Result<bool>>
{
    public long TelegramId { get; set; }
    public Language Language { get; set; }
}