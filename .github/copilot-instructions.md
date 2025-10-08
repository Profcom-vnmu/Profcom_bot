# AI Agent Instructions for StudentUnionBot

## Project Context
Telegram bot for student union (профком) using **Clean Architecture + CQRS pattern**. Currently in **active refactoring** from monolithic to layered architecture in `development` branch.

## 🎯 Critical Architecture Rules

### Layer Structure (Clean Architecture)
```
Domain/        → Entities, Enums, Interfaces (no dependencies)
Application/   → CQRS Commands/Queries, DTOs, Validators (depends on Domain)
Infrastructure/→ DbContext, Repositories, External Services (implements Domain interfaces)
Presentation/  → Bot Handlers, Keyboards, State Management (uses MediatR)
Core/          → Shared utilities, Extensions, Result Pattern
```

**NEVER violate dependency rules**: Domain is independent, Presentation calls Application via MediatR, Infrastructure implements Domain interfaces.

### CQRS Pattern (MediatR)
- **Commands** (`IRequest<Result<T>>`) change state → use `IUnitOfWork`, return `Result<T>`
- **Queries** (`IRequest<Result<T>>`) read data → use `AsNoTracking()`, cache when possible
- ALL business logic goes in Command/Query Handlers, NOT in controllers or bot handlers
- Example: `CreateAppealCommand` → `CreateAppealCommandHandler` → validates, creates entity, saves via `IUnitOfWork`

### Result Pattern (NO Exceptions for Business Logic)
```csharp
// ✅ ALWAYS use Result<T> for business operations
public async Task<Result<AppealDto>> Handle(CreateAppealCommand request)
{
    if (!validation.IsValid)
        return Result<AppealDto>.Fail("Validation error");
    
    var appeal = Appeal.Create(...); // Domain factory method
    await _unitOfWork.Appeals.AddAsync(appeal);
    return Result<AppealDto>.Ok(dto);
}

// ❌ NEVER throw exceptions for business logic errors
// ✅ Exceptions ONLY for infrastructure failures (DB, network)
```

## 🔧 Development Workflow

### Git Branches
- `production` → stable, deployed to Render.com, **NEVER edit directly**
- `development` → active work, all changes go here
- **Always work in development**, merge to production after testing

### EF Core Migrations
```bash
# Create migration (MUST specify output directory)
dotnet ef migrations add MigrationName --output-dir Infrastructure/Data/Migrations

# Apply migrations
dotnet ef database update

# DbContext location: Infrastructure/Data/BotDbContext.cs
```

### Running Locally
```bash
# Set environment
$env:ASPNETCORE_ENVIRONMENT="Development"

# Run (uses SQLite for dev, PostgreSQL for production)
dotnet run
```

## 📝 Code Conventions

### Naming & Structure
- **Entities**: `Domain/Entities/` - immutable properties with `private set`, factory methods (`Appeal.Create()`)
- **Repositories**: `Infrastructure/Repositories/` - inherit `BaseRepository<T>`, implement `I*Repository` from Domain
- **Commands**: `Application/{Feature}/Commands/{Action}/` - end with `Command.cs`, `CommandHandler.cs`, `CommandValidator.cs`
- **Enums**: `Domain/Enums/` - use extension methods for display names (`AppealCategory.GetDisplayName()`)

### Database Queries
```csharp
// ✅ Read-only queries
.AsNoTracking().Include(a => a.Messages).ToListAsync()

// ✅ Pagination always
.Skip((page - 1) * pageSize).Take(pageSize)

// ❌ NEVER load all data without AsNoTracking or pagination
```

### Async/Await
- ALL methods with DB/API calls → `async Task<T>`, suffix `Async`
- ALWAYS pass `CancellationToken` (last parameter)
- Use `ConfigureAwait(false)` in library code

### Validation
- FluentValidation in `*CommandValidator.cs` for ALL commands
- Domain validation in entity factory methods (`Appeal.Create()`)
- Use `Result<T>` to return validation errors, not exceptions

### Telegram Bot Specifics
```csharp
// ✅ Send media files
await _botClient.SendPhotoAsync(chatId, InputFile.FromFileId(photoFileId));

// ✅ Inline keyboards (NOT ReplyKeyboardMarkup)
InlineKeyboardMarkup keyboard = new(new[] {
    new[] { InlineKeyboardButton.WithCallbackData("Text", "callback_data") }
});

// ✅ Parse mode for HTML
parseMode: ParseMode.Html
```

## 🔍 Key Files Reference

- **Domain Logic**: `Domain/Entities/Appeal.cs`, `Domain/Entities/BotUser.cs`
- **DB Context**: `Infrastructure/Data/BotDbContext.cs`
- **Repository Pattern**: `Infrastructure/Repositories/BaseRepository.cs`, `UnitOfWork.cs`
- **Result Pattern**: `Core/Results/Result.cs`
- **Documentation**: `Розробка/NEW_01_ОПИС_ПРОЕКТУ.md`, `NEW_02_СТРУКТУРА_ТА_АРХІТЕКТУРА.md`, `NEW_03_ІНСТРУКЦІЇ_РОЗРОБКИ.md`

## 🚫 Common Pitfalls to Avoid

1. **Dependency Direction**: NEVER reference Infrastructure/Presentation from Domain
2. **Exceptions vs Results**: Use `Result<T>` for business errors, exceptions for infrastructure failures
3. **EF Tracking**: Always use `.AsNoTracking()` for read-only queries
4. **Telegram API**: Media requires `InputFile.FromFileId()`, not raw string
5. **Migrations Path**: Must specify `--output-dir Infrastructure/Data/Migrations`
6. **Branch Safety**: NEVER commit directly to `production`

## 💡 When Adding Features

1. **Create Entity** in `Domain/Entities/` with factory method
2. **Add Repository Interface** in `Domain/Interfaces/`
3. **Implement Repository** in `Infrastructure/Repositories/`
4. **Create Command/Query** in `Application/{Feature}/Commands|Queries/`
5. **Add Validator** with FluentValidation
6. **Create Handler** with business logic, use `IUnitOfWork`
7. **Add Bot Handler** in `Presentation/Bot/Handlers/`
8. **Update DbContext** and create migration

## 📚 Additional Context

- **Ukrainian Language**: All user-facing text in Ukrainian, code/comments in English
- **Rate Limiting**: Check `IRateLimiter.AllowAsync()` before expensive operations
- **Logging**: Use Serilog structured logging with parameters (NOT string interpolation)
- **Admin System**: Admin IDs in `admins.txt`, banned users in `ban.txt` (legacy, will migrate to DB with roles)

---

**Last Updated**: 2025-10-08  
**Architecture Version**: Clean Architecture + CQRS (v2.0)  
**For detailed specs**: See `Розробка/NEW_*.md` files
