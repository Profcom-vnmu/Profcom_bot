using MediatR;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Application.Common.Attributes;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.News.Commands.DeleteNews;

/// <summary>
/// Command для видалення новини
/// </summary>
[RequirePermission(Permission.DeleteNews)]
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