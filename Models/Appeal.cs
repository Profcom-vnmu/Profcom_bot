namespace StudentUnionBot.Models;

public class Appeal
{
    public int Id { get; set; }
    public long StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public AppealStatus Status { get; set; }
    public DateTime? ClosedAt { get; set; }
    
    // Інформація про закриття
    public long? ClosedBy { get; set; } // ID того хто закрив (студент або адміністратор)
    public string? ClosedReason { get; set; } // Причина закриття
    
    // Navigation property
    public ICollection<AppealMessage> Messages { get; set; } = new List<AppealMessage>();
}

public enum AppealStatus
{
    New,                    // 🆕 Нове звернення
    AdminReplied,          // 💬 Є відповідь адміністратора
    StudentReplied,        // 📝 Є відповідь студента
    ClosedByAdmin,         // 🔒 Закрито адміністратором
    ClosedByStudent        // 🔒 Закрито студентом
}