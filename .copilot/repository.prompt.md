# Repository Development Prompt

**Мета:** Інструкції для створення репозиторіїв (Repository Pattern) у проєкті StudentUnionBot для роботи з базою даних через Entity Framework Core.

---

## Структура Repository Pattern

```
Domain/Interfaces/I{Entity}Repository.cs    → Interface
Infrastructure/Repositories/{Entity}Repository.cs → Implementation
Infrastructure/Repositories/BaseRepository.cs → Base class
Infrastructure/Repositories/UnitOfWork.cs → Координація транзакцій
```

---

## 1. Створення інтерфейсу (Domain Layer)

**Файл:** `Domain/Interfaces/I{Entity}Repository.cs`

```csharp
namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Repository interface for {Entity} entity
/// </summary>
public interface I{Entity}Repository
{
    // READ operations (queries)
    Task<{Entity}?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<{Entity}>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<{Entity}>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    
    // With includes (related entities)
    Task<{Entity}?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    
    // Filtering
    Task<IEnumerable<{Entity}>> FindAsync(Expression<Func<{Entity}, bool>> predicate, CancellationToken cancellationToken = default);
    
    // WRITE operations (commands)
    Task<{Entity}> AddAsync({Entity} entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<{Entity}> entities, CancellationToken cancellationToken = default);
    
    void Update({Entity} entity);
    void UpdateRange(IEnumerable<{Entity}> entities);
    
    void Delete({Entity} entity);
    void DeleteRange(IEnumerable<{Entity}> entities);
    
    // Specific business queries
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<{Entity}, bool>>? predicate = null, CancellationToken cancellationToken = default);
}
```

---

## 2. Базовий репозиторій (якщо ще немає)

**Файл:** `Infrastructure/Repositories/BaseRepository.cs`

```csharp
namespace StudentUnionBot.Infrastructure.Repositories;

public class BaseRepository<T> where T : class
{
    protected readonly BotDbContext Context;
    protected readonly DbSet<T> DbSet;

    protected BaseRepository(BotDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(entity, cancellationToken);
        return entry.Entity;
    }

    public virtual async Task AddRangeAsync(
        IEnumerable<T> entities, 
        CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(entities, cancellationToken);
    }

    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public virtual void Delete(T entity)
    {
        DbSet.Remove(entity);
    }

    public virtual async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        return entity != null;
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null, 
        CancellationToken cancellationToken = default)
    {
        return predicate == null
            ? await DbSet.CountAsync(cancellationToken)
            : await DbSet.CountAsync(predicate, cancellationToken);
    }
}
```

---

## 3. Конкретний репозиторій (Implementation)

**Файл:** `Infrastructure/Repositories/{Entity}Repository.cs`

```csharp
namespace StudentUnionBot.Infrastructure.Repositories;

public class {Entity}Repository : BaseRepository<{Entity}>, I{Entity}Repository
{
    public {Entity}Repository(BotDbContext context) : base(context)
    {
    }

    // Override base methods if needed with specific logic
    public override async Task<{Entity}?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(e => e.RelatedEntity) // Include related entities if needed
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    // Implement interface methods
    public async Task<{Entity}?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(e => e.RelatedEntity)
            .ThenInclude(r => r.SubRelatedEntity)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<PagedResult<{Entity}>> GetPagedAsync(
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<{Entity}>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    // Business-specific queries
    public async Task<IEnumerable<{Entity}>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(e => e.IsActive)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<{Entity}?> GetByPropertyAsync(
        string propertyValue, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.PropertyName == propertyValue, cancellationToken);
    }
}
```

---

## 4. Unit of Work Pattern

**Файл:** `Infrastructure/Repositories/UnitOfWork.cs`

```csharp
namespace StudentUnionBot.Infrastructure.Repositories;

public interface IUnitOfWork : IDisposable
{
    IAppealRepository Appeals { get; }
    IBotUserRepository Users { get; }
    IEventRepository Events { get; }
    // ... other repositories

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly BotDbContext _context;
    private IDbContextTransaction? _transaction;

    // Lazy initialization of repositories
    private IAppealRepository? _appeals;
    private IBotUserRepository? _users;
    private IEventRepository? _events;

    public UnitOfWork(BotDbContext context)
    {
        _context = context;
    }

    public IAppealRepository Appeals => _appeals ??= new AppealRepository(_context);
    public IBotUserRepository Users => _users ??= new BotUserRepository(_context);
    public IEventRepository Events => _events ??= new EventRepository(_context);

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
```

---

## 5. Реєстрація у Program.cs

```csharp
// Register repositories
builder.Services.AddScoped<IAppealRepository, AppealRepository>();
builder.Services.AddScoped<IBotUserRepository, BotUserRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();

// Register Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

---

## Правила для репозиторіїв

### 1. Query Optimization
```csharp
// ✅ Read-only queries - використовуй AsNoTracking()
var appeals = await DbSet
    .AsNoTracking()
    .Where(a => a.Status == AppealStatus.Open)
    .ToListAsync(cancellationToken);

// ✅ Pagination - ЗАВЖДИ
var pagedItems = await DbSet
    .AsNoTracking()
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(cancellationToken);

// ❌ НЕ завантажуй всі дані
var allData = await DbSet.ToListAsync(); // BAD if table has 100k+ records
```

### 2. Eager Loading vs Explicit Loading
```csharp
// ✅ Eager Loading - коли завжди потрібні зв'язані дані
var appeal = await DbSet
    .Include(a => a.User)
    .Include(a => a.Messages)
    .FirstOrDefaultAsync(a => a.Id == id);

// ✅ Explicit Loading - коли іноді потрібні
var appeal = await DbSet.FindAsync(id);
if (appeal != null && includeMessages)
{
    await Context.Entry(appeal)
        .Collection(a => a.Messages)
        .LoadAsync();
}

// ❌ Lazy Loading - не використовується в проєкті
```

### 3. Async всюди
```csharp
// ✅ Async methods
Task<Entity?> GetByIdAsync(int id, CancellationToken cancellationToken);

// ❌ Sync methods - уникай
Entity GetById(int id); // BAD
```

### 4. CancellationToken
```csharp
// ✅ ЗАВЖДИ приймай CancellationToken
public async Task<Appeal?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
{
    return await DbSet.FindAsync(new object[] { id }, cancellationToken);
}
```

---

## Приклад: AppealRepository

```csharp
public class AppealRepository : BaseRepository<Appeal>, IAppealRepository
{
    public AppealRepository(BotDbContext context) : base(context)
    {
    }

    public async Task<Appeal?> GetByIdWithMessagesAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.Messages)
            .Include(a => a.FileAttachments)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Appeal>> GetByStatusAsync(
        AppealStatus status, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Appeal>> GetAssignedToAdminAsync(
        long adminUserId, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(a => a.AssignedAdminUserId == adminUserId)
            .Where(a => a.Status != AppealStatus.Closed)
            .OrderBy(a => a.Priority)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
```

---

## Використання у Command Handlers

```csharp
public class CreateAppealCommandHandler : IRequestHandler<CreateAppealCommand, Result<AppealDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAppealCommandHandler> _logger;

    public CreateAppealCommandHandler(IUnitOfWork unitOfWork, ILogger<CreateAppealCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<AppealDto>> Handle(
        CreateAppealCommand request, 
        CancellationToken cancellationToken)
    {
        // Get user
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return Result<AppealDto>.Fail("User not found");

        // Create entity
        var appeal = Appeal.Create(request.Title, request.Description, user);

        // Add to repository
        await _unitOfWork.Appeals.AddAsync(appeal, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Appeal {AppealId} created by user {UserId}", appeal.Id, user.Id);

        return Result<AppealDto>.Ok(new AppealDto(appeal));
    }
}
```
