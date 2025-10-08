using MediatR;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Appeals.Commands.ReplyToAppeal;

/// <summary>
/// Команда для відповіді адміністратора на звернення
/// </summary>
public class ReplyToAppealCommand : IRequest<Result<int>>
{
    /// <summary>
    /// ID звернення
    /// </summary>
    public int AppealId { get; set; }

    /// <summary>
    /// ID адміністратора
    /// </summary>
    public long AdminId { get; set; }

    /// <summary>
    /// Ім'я адміністратора
    /// </summary>
    public string AdminName { get; set; } = string.Empty;

    /// <summary>
    /// Текст повідомлення
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// ID файлу фото (якщо є)
    /// </summary>
    public string? PhotoFileId { get; set; }

    /// <summary>
    /// ID файлу документа (якщо є)
    /// </summary>
    public string? DocumentFileId { get; set; }

    /// <summary>
    /// Ім'я файлу документа
    /// </summary>
    public string? DocumentFileName { get; set; }
}
