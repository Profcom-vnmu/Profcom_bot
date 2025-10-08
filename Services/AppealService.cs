using StudentUnionBot.Models;
using StudentUnionBot.Data;
using Microsoft.EntityFrameworkCore;

namespace StudentUnionBot.Services;

public class AppealService
{
    private readonly BotDbContext _context;

    public AppealService(BotDbContext context)
    {
        _context = context;
    }

    public Appeal CreateAppeal(long studentId, string studentName, string message)
    {
        using var transaction = _context.Database.BeginTransaction();
        
        try
        {
            var appeal = new Appeal
            {
                StudentId = studentId,
                StudentName = studentName,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                Status = AppealStatus.New
            };

            _context.Appeals.Add(appeal);
            _context.SaveChanges();
            
            // Додаємо перше повідомлення до звернення
            var appealMessage = new AppealMessage
            {
                AppealId = appeal.Id,
                SenderId = studentId,
                SenderName = studentName,
                IsFromAdmin = false,
                Text = message,
                SentAt = DateTime.UtcNow,
                IsReadByAdmin = false // Нове повідомлення від студента - непрочитане
            };
            
            _context.AppealMessages.Add(appealMessage);
            _context.SaveChanges();
            
            transaction.Commit();
            return appeal;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public AppealMessage? AddMessage(int appealId, long senderId, string senderName, bool isFromAdmin, string text, 
        string? photoFileId = null, string? documentFileId = null, string? documentFileName = null)
    {
        var appeal = _context.Appeals.FirstOrDefault(a => a.Id == appealId);
        
        // Перевірка чи існує звернення
        if (appeal == null)
            return null;
        
        // Перевірка чи звернення не закрите
        if (appeal.Status == AppealStatus.ClosedByAdmin || appeal.Status == AppealStatus.ClosedByStudent)
            return null;
        
        var message = new AppealMessage
        {
            AppealId = appealId,
            SenderId = senderId,
            SenderName = senderName,
            IsFromAdmin = isFromAdmin,
            Text = text,
            SentAt = DateTime.UtcNow,
            IsReadByAdmin = isFromAdmin, // Якщо від адміна - вже прочитане, якщо від студента - ні
            PhotoFileId = photoFileId,
            DocumentFileId = documentFileId,
            DocumentFileName = documentFileName
        };
        
        _context.AppealMessages.Add(message);
        
        // Оновлюємо статус звернення
        if (isFromAdmin)
        {
            appeal.Status = AppealStatus.AdminReplied;
        }
        else
        {
            // Якщо студент відповідає
            appeal.Status = AppealStatus.StudentReplied;
        }
        
        _context.SaveChanges();
        return message;
    }

    public Appeal? GetActiveAppealForStudent(long studentId)
    {
        return _context.Appeals
            .Include(a => a.Messages)
            .FirstOrDefault(a => a.StudentId == studentId && 
                               (a.Status == AppealStatus.New || 
                                a.Status == AppealStatus.AdminReplied || 
                                a.Status == AppealStatus.StudentReplied));
    }

    public IEnumerable<AppealMessage> GetAppealMessages(int appealId)
    {
        return _context.AppealMessages
            .Where(m => m.AppealId == appealId)
            .OrderBy(m => m.SentAt)
            .ToList();
    }

    public Appeal? CloseAppeal(int appealId, long closedBy, bool isAdmin, string reason)
    {
        var appeal = _context.Appeals.FirstOrDefault(a => a.Id == appealId);
        if (appeal == null) return null;

        appeal.Status = isAdmin ? AppealStatus.ClosedByAdmin : AppealStatus.ClosedByStudent;
        appeal.ClosedAt = DateTime.UtcNow;
        appeal.ClosedBy = closedBy;
        appeal.ClosedReason = reason;
        
        // Додаємо системне повідомлення про закриття в історію
        var closerName = isAdmin ? "Адміністратор" : appeal.StudentName;
        var systemMessage = new AppealMessage
        {
            AppealId = appealId,
            SenderId = closedBy,
            SenderName = "Система",
            IsFromAdmin = isAdmin,
            Text = $"🔒 Звернення закрито ({closerName})\nПричина: {reason}",
            SentAt = DateTime.UtcNow,
            IsReadByAdmin = true
        };
        
        _context.AppealMessages.Add(systemMessage);
        _context.SaveChanges();
        
        return appeal;
    }

    public Appeal? GetAppealById(int appealId)
    {
        return _context.Appeals
            .Include(a => a.Messages)
            .FirstOrDefault(a => a.Id == appealId);
    }

    public IEnumerable<AppealMessage> GetUnreadAdminMessages(int appealId, int? lastViewedMessageId)
    {
        var query = _context.AppealMessages
            .Where(m => m.AppealId == appealId && m.IsFromAdmin);

        if (lastViewedMessageId.HasValue)
        {
            query = query.Where(m => m.Id > lastViewedMessageId.Value);
        }

        return query.OrderBy(m => m.SentAt).ToList();
    }

    public IEnumerable<AppealMessage> GetAllAppealMessages(int appealId)
    {
        return _context.AppealMessages
            .Where(m => m.AppealId == appealId)
            .OrderBy(m => m.SentAt)
            .ToList();
    }

    // Методи для адміністратора
    
    /// <summary>
    /// Отримати всі активні звернення (нові, є відповідь адміна/студента)
    /// </summary>
    public IEnumerable<Appeal> GetActiveAppeals()
    {
        return _context.Appeals
            .Include(a => a.Messages)
            .Where(a => a.Status == AppealStatus.New || 
                       a.Status == AppealStatus.AdminReplied || 
                       a.Status == AppealStatus.StudentReplied)
            .OrderByDescending(a => a.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// Отримати всі звернення (для статистики)
    /// </summary>
    public IQueryable<Appeal> GetAllAppeals()
    {
        return _context.Appeals.Include(a => a.Messages);
    }

    /// <summary>
    /// Отримати всі закриті звернення
    /// </summary>
    public IEnumerable<Appeal> GetClosedAppeals()
    {
        return _context.Appeals
            .Include(a => a.Messages)
            .Where(a => a.Status == AppealStatus.ClosedByAdmin || 
                       a.Status == AppealStatus.ClosedByStudent)
            .OrderByDescending(a => a.ClosedAt ?? a.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// Перевірити чи є непрочитані повідомлення від студента в зверненні
    /// </summary>
    public bool HasUnreadMessages(int appealId)
    {
        return _context.AppealMessages
            .Any(m => m.AppealId == appealId && 
                     !m.IsFromAdmin && 
                     !m.IsReadByAdmin);
    }

    /// <summary>
    /// Позначити всі повідомлення звернення як прочитані адміністратором
    /// </summary>
    public void MarkMessagesAsReadByAdmin(int appealId)
    {
        var unreadMessages = _context.AppealMessages
            .Where(m => m.AppealId == appealId && 
                       !m.IsFromAdmin && 
                       !m.IsReadByAdmin)
            .ToList();

        foreach (var message in unreadMessages)
        {
            message.IsReadByAdmin = true;
        }

        _context.SaveChanges();
    }

    /// <summary>
    /// Кількість непрочитаних повідомлень у зверненні
    /// </summary>
    public int GetUnreadMessagesCount(int appealId)
    {
        return _context.AppealMessages
            .Count(m => m.AppealId == appealId && 
                       !m.IsFromAdmin && 
                       !m.IsReadByAdmin);
    }
}