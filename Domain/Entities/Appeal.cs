using StudentUnionBot.Core.Exceptions;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Entities;

/// <summary>
/// Звернення студента
/// </summary>
public class Appeal
{
    private Appeal() { }

    public int Id { get; private set; }
    public long StudentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public AppealCategory Category { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public AppealStatus Status { get; private set; }
    public AppealPriority Priority { get; private set; }
    public long? AssignedToAdminId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? FirstResponseAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public long? ClosedBy { get; private set; }
    public string? ClosedReason { get; private set; }
    public int? Rating { get; private set; }
    public string? RatingComment { get; private set; }

    // Navigation properties
    public BotUser Student { get; private set; } = null!;
    public ICollection<AppealMessage> Messages { get; private set; } = new List<AppealMessage>();
    public ICollection<AppealFileAttachment> FileAttachments { get; private set; } = new List<AppealFileAttachment>();

    /// <summary>
    /// Створення нового звернення
    /// </summary>
    public static Appeal Create(
        long studentId,
        string studentName,
        AppealCategory category,
        string subject,
        string message)
    {
        if (studentId <= 0)
            throw new DomainException("Student ID повинен бути більше 0");

        if (string.IsNullOrWhiteSpace(subject))
            throw new DomainException("Тема звернення не може бути порожньою");

        if (string.IsNullOrWhiteSpace(message))
            throw new DomainException("Текст звернення не може бути порожнім");

        if (message.Length < 10)
            throw new DomainException("Текст звернення занадто короткий (мінімум 10 символів)");

        if (message.Length > 4000)
            throw new DomainException("Текст звернення занадто довгий (максимум 4000 символів)");

        var now = DateTime.UtcNow;

        return new Appeal
        {
            StudentId = studentId,
            StudentName = studentName,
            Category = category,
            Subject = subject,
            Message = message,
            Status = AppealStatus.New,
            Priority = AppealPriority.Normal,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Призначення звернення адміністратору
    /// </summary>
    public void AssignTo(long? adminId)
    {
        if (Status == AppealStatus.Closed)
            throw new DomainException("Не можна призначити закрите звернення");

        AssignedToAdminId = adminId;
        
        // Якщо призначається - ставимо статус "В роботі"
        if (adminId.HasValue && Status == AppealStatus.New)
        {
            Status = AppealStatus.InProgress;
        }
        
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Зміна пріоритету звернення
    /// </summary>
    public void UpdatePriority(AppealPriority priority)
    {
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Встановлення часу першої відповіді
    /// </summary>
    public void SetFirstResponse()
    {
        if (!FirstResponseAt.HasValue)
        {
            FirstResponseAt = DateTime.UtcNow;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Встановлення статусу "В роботі"
    /// </summary>
    public void MarkInProgress()
    {
        if (Status == AppealStatus.Closed)
            throw new DomainException("Не можна змінити статус закритого звернення");

        Status = AppealStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Встановлення статусу "Очікує студента"
    /// </summary>
    public void MarkWaitingForStudent()
    {
        if (Status == AppealStatus.Closed)
            throw new DomainException("Не можна змінити статус закритого звернення");

        Status = AppealStatus.WaitingForStudent;
        UpdatedAt = DateTime.UtcNow;

        if (!FirstResponseAt.HasValue)
            FirstResponseAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Встановлення статусу "Очікує адміна"
    /// </summary>
    public void MarkWaitingForAdmin()
    {
        if (Status == AppealStatus.Closed)
            throw new DomainException("Не можна змінити статус закритого звернення");

        Status = AppealStatus.WaitingForAdmin;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Ескалація звернення
    /// </summary>
    public void Escalate()
    {
        if (Status == AppealStatus.Closed)
            throw new DomainException("Не можна ескалювати закрите звернення");

        Status = AppealStatus.Escalated;
        Priority = AppealPriority.High;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Закриття звернення
    /// </summary>
    public void Close(long closedBy, string reason)
    {
        if (Status == AppealStatus.Closed)
            throw new DomainException("Звернення вже закрите");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Причина закриття обов'язкова");

        Status = AppealStatus.Closed;
        ClosedBy = closedBy;
        ClosedReason = reason;
        ClosedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Оцінка звернення студентом
    /// </summary>
    public void Rate(int rating, string? comment = null)
    {
        if (Status != AppealStatus.Closed)
            throw new DomainException("Можна оцінити тільки закрите звернення");

        if (rating < 1 || rating > 5)
            throw new DomainException("Оцінка повинна бути від 1 до 5");

        Rating = rating;
        RatingComment = comment;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Додавання повідомлення до звернення
    /// </summary>
    public void AddMessage(AppealMessage message)
    {
        if (Status == AppealStatus.Closed)
            throw new DomainException("Не можна додати повідомлення до закритого звернення");

        Messages.Add(message);
        UpdatedAt = DateTime.UtcNow;

        // Автоматична зміна статусу
        if (message.IsFromAdmin)
            Status = AppealStatus.WaitingForStudent;
        else
            Status = AppealStatus.WaitingForAdmin;
    }
}

/// <summary>
/// Повідомлення в зверненні
/// </summary>
public class AppealMessage
{
    private AppealMessage() { }

    public int Id { get; private set; }
    public int AppealId { get; private set; }
    public long SenderId { get; private set; }
    public string SenderName { get; private set; } = string.Empty;
    public bool IsFromAdmin { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public DateTime SentAt { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public string? PhotoFileId { get; private set; }
    public string? DocumentFileId { get; private set; }
    public string? DocumentFileName { get; private set; }

    // Navigation property
    public Appeal Appeal { get; private set; } = null!;

    public static AppealMessage Create(
        int appealId,
        long senderId,
        string senderName,
        bool isFromAdmin,
        string text,
        string? photoFileId = null,
        string? documentFileId = null,
        string? documentFileName = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Текст повідомлення не може бути порожнім");

        if (text.Length > 4000)
            throw new DomainException("Повідомлення занадто довге (максимум 4000 символів)");

        return new AppealMessage
        {
            AppealId = appealId,
            SenderId = senderId,
            SenderName = senderName,
            IsFromAdmin = isFromAdmin,
            Text = text,
            SentAt = DateTime.UtcNow,
            IsRead = false,
            PhotoFileId = photoFileId,
            DocumentFileId = documentFileId,
            DocumentFileName = documentFileName
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
