using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Entities;

/// <summary>
/// Represents a news article published by the student union
/// </summary>
public class News
{
    public int Id { get; private set; }
    
    /// <summary>
    /// News article title
    /// </summary>
    public string Title { get; private set; } = string.Empty;
    
    /// <summary>
    /// Full text content of the news
    /// </summary>
    public string Content { get; private set; } = string.Empty;
    
    /// <summary>
    /// Short summary/excerpt for previews
    /// </summary>
    public string? Summary { get; private set; }
    
    /// <summary>
    /// Category of the news
    /// </summary>
    public NewsCategory Category { get; private set; }
    
    /// <summary>
    /// Admin who created the news
    /// </summary>
    public long AuthorId { get; private set; }
    
    /// <summary>
    /// Name of the author
    /// </summary>
    public string AuthorName { get; private set; } = string.Empty;
    
    /// <summary>
    /// Optional photo/image file ID from Telegram
    /// </summary>
    public string? PhotoFileId { get; private set; }
    
    /// <summary>
    /// Optional document/file ID from Telegram
    /// </summary>
    public string? DocumentFileId { get; private set; }
    
    /// <summary>
    /// Whether the news is published and visible to students
    /// </summary>
    public bool IsPublished { get; private set; }
    
    /// <summary>
    /// Scheduled publication date (null = publish immediately)
    /// </summary>
    public DateTime? PublishAt { get; private set; }
    
    /// <summary>
    /// When the news was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }
    
    /// <summary>
    /// When the news was last updated
    /// </summary>
    public DateTime UpdatedAt { get; private set; }
    
    /// <summary>
    /// Number of times the news was viewed
    /// </summary>
    public int ViewCount { get; private set; }
    
    /// <summary>
    /// Whether the news is pinned (shown at top)
    /// </summary>
    public bool IsPinned { get; private set; }
    
    // Private constructor for EF Core
    private News() { }
    
    /// <summary>
    /// Factory method to create a new news article
    /// </summary>
    public static News Create(
        string title,
        string content,
        NewsCategory category,
        long authorId,
        string authorName,
        string? summary = null,
        string? photoFileId = null,
        string? documentFileId = null,
        bool publishImmediately = true,
        DateTime? publishAt = null)
    {
        var news = new News
        {
            Title = title,
            Content = content,
            Summary = summary,
            Category = category,
            AuthorId = authorId,
            AuthorName = authorName,
            PhotoFileId = photoFileId,
            DocumentFileId = documentFileId,
            IsPublished = publishImmediately,
            PublishAt = publishAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ViewCount = 0,
            IsPinned = false
        };
        
        return news;
    }
    
    public void Update(
        string title,
        string content,
        NewsCategory category,
        string? summary = null,
        string? photoFileId = null,
        string? documentFileId = null)
    {
        Title = title;
        Content = content;
        Summary = summary;
        Category = category;
        PhotoFileId = photoFileId;
        DocumentFileId = documentFileId;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Publish()
    {
        IsPublished = true;
        PublishAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Unpublish()
    {
        IsPublished = false;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Pin()
    {
        IsPinned = true;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Unpin()
    {
        IsPinned = false;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void IncrementViewCount()
    {
        ViewCount++;
    }
}
