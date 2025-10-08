using MediatR;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Appeals.Commands.AssignAppeal;

/// <summary>
/// Команда для призначення звернення адміністратору
/// </summary>
public class AssignAppealCommand : IRequest<Result<bool>>
{
    /// <summary>
    /// ID звернення
    /// </summary>
    public int AppealId { get; set; }

    /// <summary>
    /// ID адміністратора який виконує дію
    /// </summary>
    public long RequestAdminId { get; set; }

    /// <summary>
    /// ID адміністратора якому призначається звернення (null = зняти призначення)
    /// </summary>
    public long? AssignToAdminId { get; set; }
}
