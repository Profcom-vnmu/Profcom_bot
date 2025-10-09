using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.News.DTOs;

/// <summary>
/// DTO для новини з додатковими полями для API
/// </summary>
public class NewsDto
{
    public int Id { get; set; }
    
    /// <summary>
    /// Заголовок новини
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Повний текст новини
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Короткий опис для превʼю
    /// </summary>
    public string? Summary { get; set; }
    
    /// <summary>
    /// Категорія новини
    /// </summary>
    public NewsCategory Category { get; set; }
    
    /// <summary>
    /// ID автора
    /// </summary>
    public long AuthorId { get; set; }
    
    /// <summary>
    /// Імʼя автора
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;
    
    /// <summary>
    /// ID фото з Telegram (якщо є)
    /// </summary>
    public string? PhotoFileId { get; set; }
    
    /// <summary>
    /// ID документа з Telegram (якщо є)
    /// </summary>
    public string? DocumentFileId { get; set; }
    
    /// <summary>
    /// Чи опублікована новина
    /// </summary>
    public bool IsPublished { get; set; }
    
    /// <summary>
    /// Дата публікації (запланована або фактична)
    /// </summary>
    public DateTime? PublishAt { get; set; }
    
    /// <summary>
    /// Дата створення
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Дата останнього оновлення
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Кількість переглядів
    /// </summary>
    public int ViewCount { get; set; }
    
    /// <summary>
    /// Чи закріплена новина
    /// </summary>
    public bool IsPinned { get; set; }
    
    /// <summary>
    /// Теги новини (через кому)
    /// </summary>
    public string Tags { get; set; } = string.Empty;
    
    /// <summary>
    /// Мова новини
    /// </summary>
    public Language Language { get; set; } = Language.Ukrainian;
    
    /// <summary>
    /// Статус новини для відображення в UI
    /// </summary>
    public string StatusDisplay => IsPublished 
        ? (PublishAt <= DateTime.UtcNow ? "Опубліковано" : "Заплановано") 
        : "Чернетка";

    /// <summary>
    /// Короткий превʼю контенту (перші 150 символів)
    /// </summary>
    public string ContentPreview => Summary ?? 
        (Content.Length > 150 ? Content[..150] + "..." : Content);

    /// <summary>
    /// Назва категорії для відображення
    /// </summary>
    public string CategoryDisplay => Category.GetDisplayName();

    /// <summary>
    /// Emoji категорії для відображення
    /// </summary>
    public string CategoryEmoji => Category.GetEmoji();

    /// <summary>
    /// Список файлів, прикріплених до новини
    /// </summary>
    public List<string> AttachmentFileIds { get; set; } = new();
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