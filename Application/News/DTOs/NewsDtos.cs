namespace StudentUnionBot.Application.News.DTOs;

/// <summary>
/// DTO для відображення новини
/// </summary>
public class NewsDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Category { get; set; } = string.Empty;
    public string CategoryEmoji { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? PhotoFileId { get; set; }
    public string? DocumentFileId { get; set; }
    public bool IsPublished { get; set; }
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishAt { get; set; }
    public int ViewCount { get; set; }
}

/// <summary>
/// DTO для списку новин з пагінацією
/// </summary>
public class NewsListDto
{
    public List<NewsDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
