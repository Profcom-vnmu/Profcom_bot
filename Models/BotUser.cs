namespace StudentUnionBot.Models;

public class BotUser
{
    public long TelegramId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    // Додаткові поля профілю
    public string? FullName { get; set; }       // ПІБ
    public string? Faculty { get; set; }        // Факультет
    public int? Course { get; set; }            // Курс
    public string? Group { get; set; }          // Група
    public string? Email { get; set; }          // Електронна пошта
    public DateTime? ProfileUpdatedAt { get; set; }  // Дата останнього оновлення деталей
}