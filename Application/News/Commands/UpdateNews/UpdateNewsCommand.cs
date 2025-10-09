using MediatR;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Application.News.DTOs;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.News.Commands.UpdateNews;

/// <summary>
/// Command для оновлення існуючої новини
/// </summary>
public class UpdateNewsCommand : IRequest<Result<NewsDto>>
{
    /// <summary>
    /// ID новини для оновлення
    /// </summary>
    public int NewsId { get; set; }

    /// <summary>
    /// Новий заголовок (null = не змінювати)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Новий контент (null = не змінювати)
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Новий короткий опис (null = не змінювати)
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Нова категорія (null = не змінювати)
    /// </summary>
    public NewsCategory? Category { get; set; }

    /// <summary>
    /// Нові теги (null = не змінювати)
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// ID адміністратора, який оновлює новину
    /// </summary>
    public long EditorId { get; set; }

    /// <summary>
    /// Нові прикріплення (null = не змінювати)
    /// </summary>
    public List<string>? AttachmentFileIds { get; set; }
}