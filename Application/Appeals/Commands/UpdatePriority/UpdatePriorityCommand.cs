using MediatR;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Appeals.Commands.UpdatePriority;

/// <summary>
/// Команда для зміни пріоритету звернення
/// </summary>
public class UpdatePriorityCommand : IRequest<Result<bool>>
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
    /// Новий пріоритет
    /// </summary>
    public AppealPriority Priority { get; set; }
}
