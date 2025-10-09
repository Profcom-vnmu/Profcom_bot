namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Unit of Work для роботи з транзакціями
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IAppealRepository Appeals { get; }
    IAdminWorkloadRepository AdminWorkloads { get; }
    INewsRepository News { get; }
    IEventRepository Events { get; }
    IPartnerRepository Partners { get; }
    IContactInfoRepository Contacts { get; }
    INotificationRepository Notifications { get; }
    INotificationPreferenceRepository NotificationPreferences { get; }
    INotificationTemplateRepository NotificationTemplates { get; }
    IFileAttachmentRepository FileAttachments { get; }
    IAppealFileAttachmentRepository AppealFileAttachments { get; }

    /// <summary>
    /// Отримати generic репозиторій для будь-якої сутності
    /// </summary>
    IRepository<T> GetRepository<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
