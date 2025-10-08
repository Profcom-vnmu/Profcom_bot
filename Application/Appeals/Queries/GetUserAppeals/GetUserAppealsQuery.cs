using MediatR;
using StudentUnionBot.Application.Appeals.DTOs;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Appeals.Queries.GetUserAppeals;

/// <summary>
/// Запит для отримання звернень конкретного користувача
/// </summary>
public class GetUserAppealsQuery : IRequest<Result<List<AppealDto>>>
{
    /// <summary>
    /// Telegram ID користувача
    /// </summary>
    public long UserId { get; set; }
    
    /// <summary>
    /// Кількість записів на сторінці (за замовчуванням 10)
    /// </summary>
    public int PageSize { get; set; } = 10;
    
    /// <summary>
    /// Номер сторінки (починається з 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;
    
    /// <summary>
    /// Чи включати тільки активні звернення (без закритих/відхилених)
    /// </summary>
    public bool OnlyActive { get; set; } = false;
}
