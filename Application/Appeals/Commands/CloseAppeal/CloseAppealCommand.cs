using MediatR;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Appeals.Commands.CloseAppeal;

/// <summary>
/// Команда для закриття звернення адміністратором
/// </summary>
public class CloseAppealCommand : IRequest<Result<bool>>
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
    /// Причина закриття
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Чи відхилити звернення (true) чи просто закрити (false)
    /// </summary>
    public bool IsRejection { get; set; } = false;
}
