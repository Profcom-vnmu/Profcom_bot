using MediatR;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.News.Commands.DeleteNews;

/// <summary>
/// Command для видалення новини
/// </summary>
public class DeleteNewsCommand : IRequest<Result<bool>>
{
    /// <summary>
    /// ID новини для видалення
    /// </summary>
    public int NewsId { get; set; }

    /// <summary>
    /// ID адміністратора, який видаляє новину
    /// </summary>
    public long DeleterId { get; set; }

    /// <summary>
    /// Причина видалення (опційно)
    /// </summary>
    public string? DeletionReason { get; set; }

    /// <summary>
    /// Чи потрібно зберегти новину в архіві замість повного видалення
    /// </summary>
    public bool ArchiveInsteadOfDelete { get; set; } = true;
}