using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Infrastructure.Data;

namespace StudentUnionBot.Infrastructure.Repositories;

/// <summary>
/// Unit of Work реалізація для управління транзакціями
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly BotDbContext _context;
    private IDbContextTransaction? _transaction;

    public IUserRepository Users { get; }
    public IAppealRepository Appeals { get; }
    public IAdminWorkloadRepository AdminWorkloads { get; }
    public INewsRepository News { get; }
    public IEventRepository Events { get; }
    public IPartnerRepository Partners { get; }
    public IContactInfoRepository Contacts { get; }
    public INotificationRepository Notifications { get; }
    public INotificationPreferenceRepository NotificationPreferences { get; }
    public INotificationTemplateRepository NotificationTemplates { get; }
    public IFileAttachmentRepository FileAttachments { get; }
    public IAppealFileAttachmentRepository AppealFileAttachments { get; }

    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(
        BotDbContext context,
        ILogger<UserRepository> userLogger)
    {
        _context = context;
        Users = new UserRepository(context, userLogger);
        Appeals = new AppealRepository(context);
        AdminWorkloads = new AdminWorkloadRepository(context);
        News = new NewsRepository(context);
        Events = new EventRepository(context);
        Partners = new PartnerRepository(context);
        Contacts = new ContactInfoRepository(context);
        Notifications = new NotificationRepository(context);
        NotificationPreferences = new NotificationPreferenceRepository(context);
        NotificationTemplates = new NotificationTemplateRepository(context);
        FileAttachments = new FileAttachmentRepository(context);
        AppealFileAttachments = new AppealFileAttachmentRepository(context);
    }

    public IRepository<T> GetRepository<T>() where T : class
    {
        var type = typeof(T);
        if (_repositories.ContainsKey(type))
        {
            return (IRepository<T>)_repositories[type];
        }

        var repository = new GenericRepository<T>(_context);
        _repositories.Add(type, repository);
        return repository;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
