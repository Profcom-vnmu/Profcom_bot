using MediatR;
using StudentUnionBot.Application.Appeals.DTOs;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Appeals.Commands.AssignAppeal;

/// <summary>
/// Команда для автоматичного або ручного призначення апела адміністратору
/// </summary>
public class AssignAppealCommand : IRequest<Result<AppealDto>>
{
    /// <summary>
    /// ID апела для призначення
    /// </summary>
    public int AppealId { get; set; }

    /// <summary>
    /// ID адміністратора для призначення (якщо null - автоматичне призначення)
    /// </summary>
    public long? AdminId { get; set; }

    /// <summary>
    /// ID користувача, хто виконує призначення (для логування)
    /// </summary>
    public long AssignedByUserId { get; set; }

    /// <summary>
    /// Причина призначення (для ручного призначення)
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Примусове призначення (ігнорувати доступність адміністратора)
    /// </summary>
    public bool ForceAssignment { get; set; } = false;

    /// <summary>
    /// Конструктор для автоматичного призначення
    /// </summary>
    public AssignAppealCommand(int appealId, long assignedByUserId)
    {
        AppealId = appealId;
        AssignedByUserId = assignedByUserId;
    }

    /// <summary>
    /// Конструктор для ручного призначення
    /// </summary>
    public AssignAppealCommand(int appealId, long adminId, long assignedByUserId, string? reason = null, bool forceAssignment = false)
    {
        AppealId = appealId;
        AdminId = adminId;
        AssignedByUserId = assignedByUserId;
        Reason = reason;
        ForceAssignment = forceAssignment;
    }

    // Конструктор за замовчуванням для MediatR
    public AssignAppealCommand() { }
}
