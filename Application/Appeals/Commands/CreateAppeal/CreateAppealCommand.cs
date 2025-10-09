using MediatR;
using StudentUnionBot.Application.Appeals.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Appeals.Commands.CreateAppeal;

/// <summary>
/// Команда для створення нового звернення від студента
/// </summary>
public class CreateAppealCommand : IRequest<Result<AppealDto>>
{
    /// <summary>
    /// Telegram ID студента
    /// </summary>
    public long StudentId { get; set; }
    
    /// <summary>
    /// Ім'я студента
    /// </summary>
    public string StudentName { get; set; } = string.Empty;
    
    /// <summary>
    /// Категорія звернення
    /// </summary>
    public AppealCategory Category { get; set; }
    
    /// <summary>
    /// Тема звернення
    /// </summary>
    public string Subject { get; set; } = string.Empty;
    
    /// <summary>
    /// Текст звернення
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// ID фото файлу з Telegram (опціонально)
    /// </summary>
    public string? PhotoFileId { get; set; }

    /// <summary>
    /// ID документу з Telegram (опціонально)
    /// </summary>
    public string? DocumentFileId { get; set; }

    /// <summary>
    /// Назва документу (опціонально)
    /// </summary>
    public string? DocumentFileName { get; set; }
}
