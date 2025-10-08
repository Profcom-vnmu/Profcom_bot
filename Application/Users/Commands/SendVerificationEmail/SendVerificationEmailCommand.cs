using MediatR;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Users.Commands.SendVerificationEmail;

/// <summary>
/// Команда для відправки коду верифікації email
/// </summary>
public class SendVerificationEmailCommand : IRequest<Result<bool>>
{
    /// <summary>
    /// Telegram ID користувача
    /// </summary>
    public long TelegramId { get; set; }

    /// <summary>
    /// Email адреса для верифікації
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
