using MediatR;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Users.Commands.UpdateProfile;

/// <summary>
/// Команда для оновлення профілю користувача
/// </summary>
public class UpdateProfileCommand : IRequest<Result<bool>>
{
    /// <summary>
    /// ID користувача в Telegram
    /// </summary>
    public long TelegramId { get; set; }

    /// <summary>
    /// Повне ім'я (опціонально)
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Факультет (опціонально)
    /// </summary>
    public string? Faculty { get; set; }

    /// <summary>
    /// Курс навчання (опціонально)
    /// </summary>
    public int? Course { get; set; }

    /// <summary>
    /// Група (опціонально)
    /// </summary>
    public string? Group { get; set; }
}