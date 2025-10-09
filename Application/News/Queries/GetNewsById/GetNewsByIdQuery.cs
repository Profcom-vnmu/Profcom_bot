using MediatR;
using StudentUnionBot.Application.News.DTOs;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.News.Queries.GetNewsById;

/// <summary>
/// Запит для отримання новини за ID
/// </summary>
public class GetNewsByIdQuery : IRequest<Result<NewsDto>>
{
    /// <summary>
    /// ID новини
    /// </summary>
    public int NewsId { get; set; }

    public GetNewsByIdQuery(int newsId)
    {
        NewsId = newsId;
    }
}