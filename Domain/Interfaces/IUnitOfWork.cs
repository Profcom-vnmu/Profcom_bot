namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Unit of Work для роботи з транзакціями
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IAppealRepository Appeals { get; }
    INewsRepository News { get; }
    IEventRepository Events { get; }
    IPartnerRepository Partners { get; }
    IContactInfoRepository Contacts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
