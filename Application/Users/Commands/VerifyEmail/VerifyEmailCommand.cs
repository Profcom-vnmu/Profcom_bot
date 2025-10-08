using MediatR;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Users.Commands.VerifyEmail;

/// <summary>
/// Команда для верифікації email за кодом
/// </summary>
public class VerifyEmailCommand : IRequest<Result<bool>>
{
    /// <summary>
    /// Telegram ID користувача
    /// </summary>
    public long TelegramId { get; set; }

    /// <summary>
    /// Код верифікації
    /// </summary>
    public string Code { get; set; } = string.Empty;
}
