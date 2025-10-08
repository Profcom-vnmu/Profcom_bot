namespace StudentUnionBot.Models;

public class EventsInfo
{
    public int Id { get; set; }
    public string Title { get; set; } = "Заходи";
    public string Content { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedBy { get; set; } // TelegramId адміністратора
}
