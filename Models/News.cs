namespace StudentUnionBot.Models;

public class News
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NewsCategory Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishAt { get; set; }
    public bool IsPublished { get; set; }
    public string? PhotoFileId { get; set; }  // Telegram file_id для фото
}

public enum NewsCategory
{
    General,
    Events,
    Important,
    Announcement
}