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
    public INewsRepository News { get; }
    public IEventRepository Events { get; }
    public IPartnerRepository Partners { get; }
    public IContactInfoRepository Contacts { get; }

    public UnitOfWork(
        BotDbContext context,
        ILogger<UserRepository> userLogger)
    {
        _context = context;
        Users = new UserRepository(context, userLogger);
        Appeals = new AppealRepository(context);
        News = new NewsRepository(context);
        Events = new EventRepository(context);
        Partners = new PartnerRepository(context);
        Contacts = new ContactInfoRepository(context);
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
