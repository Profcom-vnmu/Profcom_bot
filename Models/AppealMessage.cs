namespace StudentUnionBot.Models;

public class AppealMessage
{
    public int Id { get; set; }
    public int AppealId { get; set; }
    public long SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public bool IsFromAdmin { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    
    // Чи прочитане повідомлення адміністратором (для індикації нових повідомлень)
    public bool IsReadByAdmin { get; set; }
    
    // Медіа вкладення (зберігаємо file_id від Telegram)
    public string? PhotoFileId { get; set; }
    public string? DocumentFileId { get; set; }
    public string? DocumentFileName { get; set; }
    
    // Navigation property
    public Appeal? Appeal { get; set; }
}
