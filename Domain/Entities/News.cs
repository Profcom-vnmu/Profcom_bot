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
    
    /// <summary>
    /// Whether the news is archived (soft delete)
    /// </summary>
    public bool IsArchived { get; private set; }
    
    /// <summary>
    /// Multiple file attachments for the news (legacy - use NewsAttachments instead)
    /// </summary>
    private readonly List<FileAttachment> _attachments = new();
    public IReadOnlyCollection<FileAttachment> Attachments => _attachments.AsReadOnly();
    
    /// <summary>
    /// New multiple file attachments system (photos, documents, videos)
    /// </summary>
    private readonly List<NewsAttachment> _newsAttachments = new();
    public IReadOnlyCollection<NewsAttachment> NewsAttachments => _newsAttachments.AsReadOnly();
    
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
            IsPinned = false,
            IsArchived = false
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
    
    public void Archive()
    {
        IsArchived = true;
        IsPublished = false; // Архівовані новини автоматично не публікуються
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Restore()
    {
        IsArchived = false;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void AddAttachment(FileAttachment attachment)
    {
        if (attachment == null)
            throw new ArgumentNullException(nameof(attachment));
            
        _attachments.Add(attachment);
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void RemoveAttachment(FileAttachment attachment)
    {
        if (attachment == null)
            throw new ArgumentNullException(nameof(attachment));
            
        _attachments.Remove(attachment);
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void ClearAttachments()
    {
        _attachments.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    #region NewsAttachment Methods

    /// <summary>
    /// Додати новий attachment до новини
    /// </summary>
    public void AddNewsAttachment(string fileId, FileType fileType, string? fileName = null)
    {
        var order = _newsAttachments.Count;
        var attachment = NewsAttachment.Create(Id, fileId, fileType, order, fileName);
        _newsAttachments.Add(attachment);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Видалити attachment
    /// </summary>
    public void RemoveNewsAttachment(NewsAttachment attachment)
    {
        if (attachment == null)
            throw new ArgumentNullException(nameof(attachment));

        _newsAttachments.Remove(attachment);
        
        // Переіндексуємо порядок
        for (int i = 0; i < _newsAttachments.Count; i++)
        {
            _newsAttachments[i].UpdateDisplayOrder(i);
        }
        
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Очистити всі attachments
    /// </summary>
    public void ClearNewsAttachments()
    {
        _newsAttachments.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Отримати перше фото
    /// </summary>
    public string? GetFirstPhotoFileId()
    {
        return _newsAttachments
            .Where(a => a.FileType == FileType.Image)
            .OrderBy(a => a.DisplayOrder)
            .FirstOrDefault()?.FileId ?? PhotoFileId; // Fallback to legacy field
    }

    /// <summary>
    /// Отримати всі фото
    /// </summary>
    public List<string> GetAllPhotoFileIds()
    {
        return _newsAttachments
            .Where(a => a.FileType == FileType.Image)
            .OrderBy(a => a.DisplayOrder)
            .Select(a => a.FileId)
            .ToList();
    }

    /// <summary>
    /// Отримати всі документи
    /// </summary>
    public List<string> GetAllDocumentFileIds()
    {
        return _newsAttachments
            .Where(a => a.FileType == FileType.Document)
            .OrderBy(a => a.DisplayOrder)
            .Select(a => a.FileId)
            .ToList();
    }

    #endregion
}
