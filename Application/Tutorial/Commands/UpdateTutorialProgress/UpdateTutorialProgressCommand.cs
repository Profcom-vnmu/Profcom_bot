using MediatR;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Tutorial.Commands.UpdateTutorialProgress;

/// <summary>
/// Команда для оновлення прогресу туторіалу користувача
/// </summary>
public class UpdateTutorialProgressCommand : IRequest<Result<TutorialStep>>
{
    /// <summary>
    /// Telegram ID користувача
    /// </summary>
    public required long TelegramId { get; init; }
    
    /// <summary>
    /// Новий крок туторіалу
    /// </summary>
    public required TutorialStep Step { get; init; }
}
