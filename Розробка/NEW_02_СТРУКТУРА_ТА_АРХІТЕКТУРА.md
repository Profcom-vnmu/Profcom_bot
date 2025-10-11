# 🏗️ Структура та архітектура проекту (Clean Architecture)

## 📁 Повна структура файлів

```
StudentUnionBot/
├── 📄 Program.cs                           # Entry point з Minimal API
├── 📄 StudentUnionBot.csproj               # Конфігурація проекту
├── 📄 appsettings.json                     # Конфігурація (production)
├── 📄 appsettings.Development.json         # Конфігурація (development)
├── 📄 .editorconfig                        # Code style
├── 📄 .gitignore                           # Git ignore
├── 📄 Dockerfile                           # Docker
├── 📄 docker-compose.yml                   # Docker Compose для локальної розробки
├── 📄 render.yaml                          # Конфігурація Render.com
│
├── 📂 Domain/                              # 🎯 Domain Layer (Бізнес-логіка)
│   ├── 📂 Entities/                        # Моделі даних
│   │   ├── Appeal.cs                      # Звернення
│   │   ├── AppealMessage.cs               # Повідомлення звернення
│   │   ├── BotUser.cs                     # Користувач
│   │   ├── News.cs                        # Новина
│   │   ├── MessageTemplate.cs             # Шаблон відповіді
│   │   ├── ContactInfo.cs                 # Контакти
│   │   ├── Event.cs                       # Захід
│   │   ├── EventParticipant.cs            # Учасник заходу
│   │   ├── AdminLog.cs                    # Лог адміна
│   │   └── RateLimitEntry.cs              # Rate limiting
│   │
│   ├── 📂 Enums/                          # Enum типи
│   │   ├── AppealCategory.cs              # Категорії звернень
│   │   ├── AppealStatus.cs                # Статуси звернень
│   │   ├── AppealPriority.cs              # Пріоритети звернень
│   │   ├── NewsCategory.cs                # Категорії новин
│   │   ├── NewsPriority.cs                # Пріоритети новин
│   │   ├── UserRole.cs                    # Ролі користувачів
│   │   ├── ContactType.cs                 # Типи контактів
│   │   ├── EventCategory.cs               # Категорії заходів
│   │   ├── ParticipantStatus.cs           # Статуси учасників
│   │   └── AdminAction.cs                 # Дії адміністратора
│   │
│   ├── 📂 ValueObjects/                   # Value Objects
│   │   ├── NotificationSettings.cs        # Налаштування сповіщень
│   │   └── EmailVerification.cs           # Верифікація email
│   │
│   └── 📂 Interfaces/                     # Інтерфейси репозиторіїв
│       ├── IAppealRepository.cs
│       ├── IUserRepository.cs
│       ├── INewsRepository.cs
│       ├── IEventRepository.cs
│       ├── IAdminLogRepository.cs
│       └── IUnitOfWork.cs
│
├── 📂 Application/                         # 🎯 Application Layer (Use Cases)
│   ├── 📂 Appeals/                        # Звернення
│   │   ├── 📂 Commands/                   # Команди
│   │   │   ├── CreateAppeal/
│   │   │   │   ├── CreateAppealCommand.cs
│   │   │   │   ├── CreateAppealCommandHandler.cs
│   │   │   │   └── CreateAppealCommandValidator.cs
│   │   │   ├── AddMessage/
│   │   │   │   ├── AddMessageCommand.cs
│   │   │   │   ├── AddMessageCommandHandler.cs
│   │   │   │   └── AddMessageCommandValidator.cs
│   │   │   ├── CloseAppeal/
│   │   │   │   ├── CloseAppealCommand.cs
│   │   │   │   └── CloseAppealCommandHandler.cs
│   │   │   └── ReassignAppeal/
│   │   │       ├── ReassignAppealCommand.cs
│   │   │       └── ReassignAppealCommandHandler.cs
│   │   │
│   │   └── 📂 Queries/                    # Запити
│   │       ├── GetActiveAppeals/
│   │       │   ├── GetActiveAppealsQuery.cs
│   │       │   └── GetActiveAppealsQueryHandler.cs
│   │       ├── GetAppealById/
│   │       │   ├── GetAppealByIdQuery.cs
│   │       │   └── GetAppealByIdQueryHandler.cs
│   │       └── SearchAppeals/
│   │           ├── SearchAppealsQuery.cs
│   │           └── SearchAppealsQueryHandler.cs
│   │
│   ├── 📂 Users/                          # Користувачі
│   │   ├── 📂 Commands/
│   │   │   ├── RegisterUser/
│   │   │   ├── VerifyEmail/
│   │   │   ├── UpdateProfile/
│   │   │   └── UpdateSettings/
│   │   └── 📂 Queries/
│   │       ├── GetUserById/
│   │       ├── GetUserStatistics/
│   │       └── CheckUserRole/
│   │
│   ├── 📂 News/                           # Новини
│   │   ├── 📂 Commands/
│   │   │   ├── CreateNews/
│   │   │   ├── PublishNews/
│   │   │   ├── ScheduleNews/
│   │   │   └── BroadcastNews/
│   │   └── 📂 Queries/
│   │       ├── GetLatestNews/
│   │       └── GetNewsStatistics/
│   │
│   ├── 📂 Events/                         # Заходи
│   │   ├── 📂 Commands/
│   │   │   ├── CreateEvent/
│   │   │   ├── RegisterParticipant/
│   │   │   └── CancelRegistration/
│   │   └── 📂 Queries/
│   │       ├── GetUpcomingEvents/
│   │       └── GetEventParticipants/
│   │
│   ├── 📂 DTOs/                           # Data Transfer Objects
│   │   ├── AppealDto.cs
│   │   ├── UserDto.cs
│   │   ├── NewsDto.cs
│   │   ├── EventDto.cs
│   │   └── PagedResult.cs
│   │
│   ├── 📂 Mappings/                       # AutoMapper Profiles
│   │   ├── AppealMappingProfile.cs
│   │   ├── UserMappingProfile.cs
│   │   └── NewsMappingProfile.cs
│   │
│   ├── 📂 Behaviors/                      # MediatR Behaviors
│   │   ├── ValidationBehavior.cs          # Валідація
│   │   ├── LoggingBehavior.cs             # Логування
│   │   ├── PerformanceBehavior.cs         # Моніторинг продуктивності
│   │   └── AuthorizationBehavior.cs       # 🔐 Авторизація та дозволи
│   │
│   └── 📂 Interfaces/                     # Інтерфейси сервісів
│       ├── IEmailService.cs
│       ├── ICacheService.cs
│       ├── IRateLimiter.cs
│       └── INotificationService.cs
│
├── 📂 Infrastructure/                      # 🎯 Infrastructure Layer
│   ├── 📂 Data/                           # База даних
│   │   ├── BotDbContext.cs                # DbContext
│   │   ├── BotDbContextFactory.cs         # Design-time factory
│   │   └── 📂 Migrations/                 # EF Core міграції
│   │       ├── 20251008_Initial.cs
│   │       ├── 20251008_AddCategories.cs
│   │       └── ...
│   │
│   ├── 📂 Repositories/                   # Реалізація репозиторіїв
│   │   ├── BaseRepository.cs              # Базовий репозиторій
│   │   ├── AppealRepository.cs
│   │   ├── UserRepository.cs
│   │   ├── NewsRepository.cs
│   │   ├── EventRepository.cs
│   │   ├── AdminLogRepository.cs
│   │   └── UnitOfWork.cs
│   │
│   ├── 📂 Services/                       # Реалізація сервісів
│   │   ├── EmailService.cs                # Email розсилка
│   │   ├── CacheService.cs                # Redis кешування
│   │   ├── RateLimiter.cs                 # Rate limiting
│   │   └── NotificationService.cs         # Push сповіщення
│   │
│   ├── 📂 Caching/                        # Кешування
│   │   ├── RedisCacheService.cs
│   │   ├── MemoryCacheService.cs
│   │   └── CacheKeys.cs
│   │
│   └── 📂 Email/                          # Email сервіс
│       ├── SmtpEmailService.cs
│       └── Templates/                     # Email шаблони
│           ├── VerificationCode.html
│           └── Welcome.html
│
├── 📂 Presentation/                        # 🎯 Presentation Layer
│   ├── 📂 Bot/                            # Telegram Bot
│   │   ├── 📂 Handlers/                   # Обробники
│   │   │   ├── 📂 Message/                # Обробка повідомлень
│   │   │   │   ├── MessageHandler.cs      # Головний обробник
│   │   │   │   ├── TextMessageHandler.cs
│   │   │   │   ├── PhotoMessageHandler.cs
│   │   │   │   └── DocumentMessageHandler.cs
│   │   │   │
│   │   │   ├── 📂 Callback/               # Обробка callback queries
│   │   │   │   ├── CallbackHandler.cs     # Головний обробник
│   │   │   │   ├── MenuCallbackHandler.cs
│   │   │   │   ├── AppealCallbackHandler.cs
│   │   │   │   ├── NewsCallbackHandler.cs
│   │   │   │   └── EventCallbackHandler.cs
│   │   │   │
│   │   │   └── 📂 Commands/               # Обробка команд
│   │   │       ├── StartCommandHandler.cs
│   │   │       ├── HelpCommandHandler.cs
│   │   │       ├── CancelCommandHandler.cs
│   │   │       └── SettingsCommandHandler.cs
│   │   │
│   │   ├── 📂 Keyboards/                  # Клавіатури
│   │   │   ├── MainMenuKeyboard.cs
│   │   │   ├── AppealKeyboards.cs
│   │   │   ├── AdminKeyboards.cs
│   │   │   ├── SettingsKeyboards.cs
│   │   │   └── PaginationKeyboard.cs
│   │   │
│   │   ├── 📂 Middlewares/                # Middlewares
│   │   │   ├── RateLimitMiddleware.cs
│   │   │   ├── AuthorizationMiddleware.cs
│   │   │   ├── LoggingMiddleware.cs
│   │   │   └── ExceptionMiddleware.cs
│   │   │
│   │   ├── 📂 States/                     # Стани користувача
│   │   │   ├── UserStateManager.cs        # Менеджер станів
│   │   │   ├── UserState.cs               # Клас стану
│   │   │   ├── DialogState.cs             # Enum діалогових станів
│   │   │   └── StateHandlers/             # Обробники станів
│   │   │       ├── CreateAppealStateHandler.cs
│   │   │       ├── VerifyEmailStateHandler.cs
│   │   │       └── PublishNewsStateHandler.cs
│   │   │
│   │   ├── 📂 Formatters/                 # Форматування повідомлень
│   │   │   ├── AppealFormatter.cs
│   │   │   ├── NewsFormatter.cs
│   │   │   ├── EventFormatter.cs
│   │   │   └── StatisticsFormatter.cs
│   │   │
│   │   └── BotService.cs                  # Головний сервіс бота
│   │
│   ├── 📂 Api/                            # HTTP API
│   │   ├── 📂 Controllers/                # API контролери
│   │   │   ├── WebhookController.cs       # Telegram webhook
│   │   │   ├── HealthController.cs        # Health checks
│   │   │   └── MetricsController.cs       # Prometheus metrics
│   │   │
│   │   └── 📂 Middlewares/                # API middlewares
│   │       ├── GlobalExceptionMiddleware.cs
│   │       └── RequestLoggingMiddleware.cs
│   │
│   └── 📂 Localization/                   # Багатомовність
│       ├── LocalizationService.cs
│       └── Resources/                     # Переклади
│           ├── Messages.uk.json           # Українська
│           └── Messages.en.json           # Англійська
│
├── 📂 Core/                                # 🎯 Shared/Common
│   ├── 📂 Constants/                      # Константи
│   │   ├── BotConstants.cs                # Загальні константи
│   │   ├── CacheKeys.cs                   # Ключі кешу
│   │   ├── RateLimitConstants.cs          # Rate limit правила
│   │   └── RegexPatterns.cs               # Regex шаблони
│   │
│   ├── 📂 Extensions/                     # Extension methods
│   │   ├── StringExtensions.cs
│   │   ├── DateTimeExtensions.cs
│   │   ├── EnumExtensions.cs
│   │   └── TelegramExtensions.cs
│   │
│   ├── 📂 Helpers/                        # Helper classes
│   │   ├── ValidationHelper.cs
│   │   ├── FileHelper.cs
│   │   └── EncryptionHelper.cs
│   │
│   ├── 📂 Exceptions/                     # Custom exceptions
│   │   ├── DomainException.cs
│   │   ├── NotFoundException.cs
│   │   ├── ValidationException.cs
│   │   ├── RateLimitException.cs
│   │   └── UnauthorizedException.cs
│   │
│   └── 📂 Results/                        # Result pattern
│       ├── Result.cs                      # Базовий Result
│       ├── Result{T}.cs                   # Generic Result
│       └── Error.cs                       # Error class
│
├── 📂 Tests/                               # 🎯 Тести
│   ├── 📂 UnitTests/                      # Unit тести
│   │   ├── Domain.Tests/
│   │   ├── Application.Tests/
│   │   └── Infrastructure.Tests/
│   │
│   ├── 📂 IntegrationTests/               # Integration тести
│   │   ├── Api.Tests/
│   │   └── Bot.Tests/
│   │
│   └── 📂 TestHelpers/                    # Допоміжні класи для тестів
│       ├── DatabaseFixture.cs
│       ├── BotClientMock.cs
│       └── TestDataBuilder.cs
│
└── 📂 Розробка/                           # 📚 Документація
    ├── NEW_01_ОПИС_ПРОЕКТУ.md
    ├── NEW_02_СТРУКТУРА_ТА_АРХІТЕКТУРА.md
    ├── NEW_03_ІНСТРУКЦІЇ_РОЗРОБКИ.md
    └── NEW_04_API_REFERENCE.md
```

---

## 🏛️ Clean Architecture Layers

### 1️⃣ Domain Layer (Центр)
**Відповідальність:** Бізнес-логіка, правила домену, сутності

**Залежності:** Немає (незалежний шар)

**Містить:**
- Entities (Appeal, BotUser, News, etc.)
- Enums (AppealStatus, UserRole, etc.)
- Value Objects (NotificationSettings, EmailVerification)
- Domain Interfaces (IAppealRepository, IUserRepository)
- Domain Events (опціонально)

**Правила:**
- ❌ Не залежить від інших шарів
- ❌ Не містить інфраструктурного коду
- ✅ Містить тільки бізнес-логіку
- ✅ POCO classes (Plain Old CLR Objects)

### 2️⃣ Application Layer (Use Cases)
**Відповідальність:** Оркестрація бізнес-логіки, use cases

**Залежності:** Domain Layer

**Містить:**
- Commands (CreateAppealCommand, PublishNewsCommand)
- Queries (GetActiveAppealsQuery, GetUserStatisticsQuery)
- Command/Query Handlers
- DTOs (AppealDto, UserDto)
- Validators (FluentValidation)
- Mapping Profiles (AutoMapper)
- Service Interfaces

**Правила:**
- ✅ Залежить тільки від Domain
- ❌ Не залежить від Infrastructure або Presentation
- ✅ Містить бізнес-логіку use cases
- ✅ Використовує MediatR для CQRS

### 3️⃣ Infrastructure Layer
**Відповідальність:** Реалізація технічних деталей

**Залежності:** Domain, Application

**Містить:**
- DbContext та Migrations
- Repositories (реалізація інтерфейсів з Domain)
- External Services (Email, SMS, Payment)
- Caching (Redis)
- File Storage
- Third-party integrations

**Правила:**
- ✅ Реалізує інтерфейси з Domain/Application
- ✅ Містить технічні деталі (БД, файли, API)
- ✅ Dependency Injection registration

### 4️⃣ Presentation Layer
**Відповідальність:** Взаємодія з користувачем

**Залежності:** Application (через MediatR)

**Містить:**
- Telegram Bot Handlers
- HTTP API Controllers
- Middlewares
- Keyboards та UI
- Localization

**Правила:**
- ✅ Тільки UI логіка
- ✅ Викликає Commands/Queries через MediatR
- ❌ Не містить бізнес-логіки
- ❌ Не звертається до DbContext напряму

---

## 🔄 CQRS Pattern (Command Query Responsibility Segregation)

### Commands (Зміна стану)
```csharp
// Command - що ми хочемо зробити
public record CreateAppealCommand(
    long StudentId,
    AppealCategory Category,
    string Subject,
    string Message
) : IRequest<Result<AppealDto>>;

// Command Handler - як ми це робимо
public class CreateAppealCommandHandler 
    : IRequestHandler<CreateAppealCommand, Result<AppealDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateAppealCommand> _validator;
    private readonly IRateLimiter _rateLimiter;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateAppealCommandHandler> _logger;

    public async Task<Result<AppealDto>> Handle(
        CreateAppealCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Валідація
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return Result<AppealDto>.Fail(validationResult.Errors);

        // 2. Rate Limiting
        if (!await _rateLimiter.AllowAsync(request.StudentId, "CreateAppeal"))
            return Result<AppealDto>.Fail("Rate limit exceeded");

        // 3. Бізнес-логіка
        var student = await _unitOfWork.Users.GetByIdAsync(request.StudentId);
        if (student == null)
            return Result<AppealDto>.Fail("Student not found");

        var appeal = Appeal.Create(
            request.StudentId,
            student.FullName ?? "Unknown",
            request.Category,
            request.Subject,
            request.Message
        );

        // 4. Збереження
        await _unitOfWork.Appeals.AddAsync(appeal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Побічні ефекти
        await SendAdminNotifications(appeal, cancellationToken);

        // 6. Повернення результату
        var dto = _mapper.Map<AppealDto>(appeal);
        return Result<AppealDto>.Ok(dto);
    }
}

// Validator
public class CreateAppealCommandValidator : AbstractValidator<CreateAppealCommand>
{
    public CreateAppealCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .GreaterThan(0);

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Message)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(4000);

        RuleFor(x => x.Category)
            .IsInEnum();
    }
}
```

### Queries (Читання даних)
```csharp
// Query - що ми хочемо отримати
public record GetActiveAppealsQuery(
    AppealCategory? Category = null,
    int Page = 1,
    int PageSize = 10
) : IRequest<Result<PagedResult<AppealDto>>>;

// Query Handler - як ми це отримуємо
public class GetActiveAppealsQueryHandler 
    : IRequestHandler<GetActiveAppealsQuery, Result<PagedResult<AppealDto>>>
{
    private readonly IAppealRepository _repository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;

    public async Task<Result<PagedResult<AppealDto>>> Handle(
        GetActiveAppealsQuery request, 
        CancellationToken cancellationToken)
    {
        // 1. Спроба отримати з кешу
        var cacheKey = $"active_appeals_{request.Category}_{request.Page}_{request.PageSize}";
        var cached = await _cache.GetAsync<PagedResult<AppealDto>>(cacheKey);
        if (cached != null)
            return Result<PagedResult<AppealDto>>.Ok(cached);

        // 2. Отримання з БД
        var appeals = await _repository.GetActiveAppealsAsync(
            request.Category, 
            cancellationToken
        );

        // 3. Пагінація
        var totalCount = appeals.Count;
        var pagedAppeals = appeals
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        // 4. Mapping
        var dtos = _mapper.Map<List<AppealDto>>(pagedAppeals);

        // 5. Результат
        var result = new PagedResult<AppealDto>(
            dtos,
            totalCount,
            request.Page,
            request.PageSize
        );

        // 6. Кешування
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

        return Result<PagedResult<AppealDto>>.Ok(result);
    }
}
```

---

## 🎭 MediatR Pipeline Behaviors

### 1. Validation Behavior
```csharp
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Валідація перед виконанням Handler
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))
            );

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);
        }

        return await next();
    }
}
```

### 2. Logging Behavior
```csharp
public class LoggingBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        _logger.LogInformation("Handling {RequestName}", requestName);
        
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();
        
        _logger.LogInformation(
            "Handled {RequestName} in {ElapsedMs}ms", 
            requestName, 
            sw.ElapsedMilliseconds
        );

        return response;
    }
}
```

### 3. Performance Behavior
```csharp
public class PerformanceBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private const int SlowRequestThreshold = 500; // ms

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > SlowRequestThreshold)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogWarning(
                "Slow Request: {RequestName} took {ElapsedMs}ms",
                requestName,
                sw.ElapsedMilliseconds
            );
        }

        return response;
    }
}
```

### 4. Authorization Behavior 🔐
```csharp
public class AuthorizationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest);
        
        // Перевірка атрибутів авторизації
        var requirePermissionAttr = requestType.GetCustomAttribute<RequirePermissionAttribute>();
        var requireAllPermissionsAttr = requestType.GetCustomAttribute<RequireAllPermissionsAttribute>();
        var requireAdminAttr = requestType.GetCustomAttribute<RequireAdminAttribute>();
        var requireSuperAdminAttr = requestType.GetCustomAttribute<RequireSuperAdminAttribute>();

        // Якщо немає атрибутів авторизації, пропустити перевірку
        if (requirePermissionAttr == null && requireAllPermissionsAttr == null && 
            requireAdminAttr == null && requireSuperAdminAttr == null)
        {
            return await next();
        }

        // Отримати поточного користувача
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to {RequestName}", requestType.Name);
            return CreateUnauthorizedResult("Користувач не авторизований");
        }

        // Перевірка SuperAdmin
        if (requireSuperAdminAttr != null)
        {
            var isSuperAdmin = await _authorizationService.IsSuperAdminAsync(
                currentUserId.Value, cancellationToken);
            
            if (!isSuperAdmin)
            {
                _logger.LogWarning(
                    "User {UserId} attempted SuperAdmin action {RequestName}",
                    currentUserId, requestType.Name);
                return CreateUnauthorizedResult("Недостатньо прав доступу");
            }
        }
        // Перевірка Admin
        else if (requireAdminAttr != null)
        {
            var isAdmin = await _authorizationService.IsAdminAsync(
                currentUserId.Value, cancellationToken);
            
            if (!isAdmin)
            {
                _logger.LogWarning(
                    "User {UserId} attempted Admin action {RequestName}",
                    currentUserId, requestType.Name);
                return CreateUnauthorizedResult("Недостатньо прав доступу");
            }
        }
        // Перевірка всіх дозволів
        else if (requireAllPermissionsAttr != null)
        {
            var hasAllPermissions = await _authorizationService.HasAllPermissionsAsync(
                currentUserId.Value, cancellationToken, requireAllPermissionsAttr.Permissions);
                
            if (!hasAllPermissions)
            {
                _logger.LogWarning(
                    "User {UserId} lacks required permissions for {RequestName}: {Permissions}",
                    currentUserId, requestType.Name, 
                    string.Join(", ", requireAllPermissionsAttr.Permissions));
                return CreateUnauthorizedResult("Недостатньо прав доступу");
            }
        }
        // Перевірка конкретного дозволу або альтернатив
        else if (requirePermissionAttr != null)
        {
            var hasPermission = await _authorizationService.HasPermissionAsync(
                currentUserId.Value, requirePermissionAttr.Permission, cancellationToken);

            // Якщо основний дозвіл відсутній, перевіряємо альтернативи
            if (!hasPermission && requirePermissionAttr.AlternativePermissions != null)
            {
                hasPermission = await _authorizationService.HasAnyPermissionAsync(
                    currentUserId.Value, cancellationToken, 
                    requirePermissionAttr.AlternativePermissions);
            }

            if (!hasPermission)
            {
                var requiredPermissions = requirePermissionAttr.AlternativePermissions != null
                    ? $"{requirePermissionAttr.Permission} або {string.Join(", ", requirePermissionAttr.AlternativePermissions)}"
                    : requirePermissionAttr.Permission.ToString();

                _logger.LogWarning(
                    "User {UserId} lacks permission for {RequestName}: {Permissions}",
                    currentUserId, requestType.Name, requiredPermissions);
                return CreateUnauthorizedResult("Недостатньо прав доступу");
            }
        }

        _logger.LogDebug(
            "Authorization successful for {RequestName} by user {UserId}",
            requestType.Name, currentUserId);

        return await next();
    }

    private static TResponse CreateUnauthorizedResult(string message)
    {
        // Якщо TResponse є Result<T>, створити Fail result
        var responseType = typeof(TResponse);
        
        if (responseType.IsGenericType && 
            responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var resultType = responseType.GetGenericArguments()[0];
            var failMethod = typeof(Result)
                .GetMethod(nameof(Result.Fail), new[] { typeof(string) })
                ?.MakeGenericMethod(resultType);
            
            if (failMethod != null)
            {
                var result = failMethod.Invoke(null, new object[] { message });
                return (TResponse)result!;
            }
        }
        
        // Якщо TResponse є Result, створити Fail result
        if (responseType == typeof(Result))
        {
            var result = Result.Fail(message);
            return (TResponse)(object)result;
        }

        // В іншому випадку кинути виняток
        throw new UnauthorizedAccessException(message);
    }
}
```

---

## 🔐 Система авторизації та дозволів

### Permission Enum (30+ дозволів)

```csharp
public enum Permission
{
    // User permissions
    ViewProfile = 1,
    EditProfile = 2,
    CreateAppeal = 3,
    ViewOwnAppeals = 4,
    
    // News permissions
    ViewNews = 10,
    CreateNews = 11,
    EditNews = 12,
    DeleteNews = 13,
    PublishNews = 14,
    PinNews = 15,
    
    // Events permissions
    ViewEvents = 20,
    CreateEvent = 21,
    EditEvent = 22,
    DeleteEvent = 23,
    RegisterForEvent = 25,
    UnregisterFromEvent = 26,
    ManageEventRegistrations = 28,
    
    // Appeals permissions
    ViewAppeals = 30,
    ViewAllAppeals = 31,
    AssignAppeal = 32,
    ReplyToAppeal = 33,
    CloseAppeal = 34,
    ReopenAppeal = 35,
    EditAppealPriority = 36,
    DeleteAppeal = 37,
    
    // Admin permissions
    ViewAdminPanel = 40,
    ManageUsers = 41,
    PromoteUsers = 42,
    BanUsers = 43,
    ViewUserActivity = 44,
    ViewStatistics = 45,
    CreateBackup = 46,
    RestoreBackup = 47,
    
    // File permissions
    UploadFile = 50,
    DownloadFile = 51,
    DeleteFile = 52,
    
    // System permissions
    ViewLogs = 60,
    ManageSystem = 61,
    ManageConfiguration = 62,
    
    // Notification permissions
    SendNotifications = 90,
    ViewNotificationHistory = 91,
    SendBroadcast = 92
}
```

### Role-Based Permissions

| Role | Permissions Count | Key Permissions |
|------|-------------------|-----------------|
| **Student** | ~13 | ViewProfile, EditProfile, CreateAppeal, ViewNews, ViewEvents, RegisterForEvent |
| **Moderator** | ~20 | Student + CreateNews, EditNews, CreateEvent, ViewAppeals, ReplyToAppeal |
| **Admin** | ~40 | Moderator + DeleteNews, PublishNews, AssignAppeal, CloseAppeal, ManageUsers, BanUsers, CreateBackup |
| **SuperAdmin** | ALL | Повний доступ до всіх дозволів |

### Permission Hierarchy

```
Student ⊂ Moderator ⊂ Admin ⊂ SuperAdmin
  (13)      (20)       (40)      (ALL)
```

### Використання атрибутів авторизації

```csharp
// Один дозвіл
[RequirePermission(Permission.CreateNews)]
public class CreateNewsCommand : IRequest<Result<NewsDto>> { }

// Основний дозвіл + альтернативи
[RequirePermission(Permission.EditNews, Permission.CreateNews)]
public class UpdateNewsCommand : IRequest<Result<NewsDto>> { }

// Всі дозволи обов'язкові
[RequireAllPermissions(Permission.DeleteNews, Permission.ManageUsers)]
public class DeleteNewsCommand : IRequest<Result<bool>> { }

// Тільки Admin
[RequireAdmin]
public class ViewAdminPanelQuery : IRequest<Result<AdminPanelDto>> { }

// Тільки SuperAdmin
[RequireSuperAdmin]
public class RestoreBackupCommand : IRequest<Result<bool>> { }
```

### IAuthorizationService методи

```csharp
public interface IAuthorizationService
{
    // Перевірка одного дозволу
    Task<bool> HasPermissionAsync(
        long userId, 
        Permission permission, 
        CancellationToken cancellationToken = default);
    
    // Перевірка будь-якого з дозволів
    Task<bool> HasAnyPermissionAsync(
        long userId, 
        CancellationToken cancellationToken = default, 
        params Permission[] permissions);
    
    // Перевірка всіх дозволів
    Task<bool> HasAllPermissionsAsync(
        long userId, 
        CancellationToken cancellationToken = default, 
        params Permission[] permissions);
    
    // Отримати всі дозволи користувача
    Task<IReadOnlyList<Permission>> GetUserPermissionsAsync(
        long userId, 
        CancellationToken cancellationToken = default);
    
    // Отримати роль користувача
    Task<UserRole?> GetUserRoleAsync(
        long userId, 
        CancellationToken cancellationToken = default);
    
    // Перевірка чи є адміністратором
    Task<bool> IsAdminAsync(
        long userId, 
        CancellationToken cancellationToken = default);
    
    // Перевірка чи є суперадміністратором
    Task<bool> IsSuperAdminAsync(
        long userId, 
        CancellationToken cancellationToken = default);
}
```

### Приклади захищених команд

**Appeals:**
- `CreateAppealCommand` - Student+
- `AssignAppealCommand` - `[RequirePermission(Permission.AssignAppeal)]` (Admin+)
- `CloseAppealCommand` - `[RequirePermission(Permission.CloseAppeal)]` (Admin+)
- `ReplyToAppealCommand` - `[RequirePermission(Permission.ReplyToAppeal)]` (Moderator+)
- `UpdatePriorityCommand` - `[RequirePermission(Permission.EditAppealPriority)]` (Admin+)

**News:**
- `CreateNewsCommand` - `[RequirePermission(Permission.CreateNews)]` (Moderator+)
- `UpdateNewsCommand` - `[RequirePermission(Permission.EditNews)]` (Moderator+)
- `DeleteNewsCommand` - `[RequirePermission(Permission.DeleteNews)]` (Admin+)
- `PublishNewsCommand` - `[RequirePermission(Permission.PublishNews)]` (Admin+)

**Events:**
- `CreateEventCommand` - `[RequirePermission(Permission.CreateEvent)]` (Moderator+)
- `RegisterForEventCommand` - `[RequirePermission(Permission.RegisterForEvent)]` (Student+)
- `UnregisterFromEventCommand` - `[RequirePermission(Permission.UnregisterFromEvent)]` (Student+)

**Admin:**
- `CreateBackupCommand` - `[RequirePermission(Permission.CreateBackup)]` (Admin+)
- `RestoreBackupCommand` - `[RequirePermission(Permission.RestoreBackup)]` (Admin+)

**Notifications:**
- `SendNotificationCommand` - `[RequirePermission(Permission.SendNotifications)]` (Admin+)
- `SendBroadcastCommand` - `[RequirePermission(Permission.SendBroadcast)]` (Admin+)

---

## 🗃️ Repository Pattern

### Base Repository
```csharp
public abstract class BaseRepository<T> where T : class
{
    protected readonly BotDbContext _context;
    protected readonly DbSet<T> _dbSet;

    protected BaseRepository(BotDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, ct);
    }

    public virtual async Task<List<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(ct);
    }

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
    }

    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public virtual void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }
}
```

### Specific Repository
```csharp
public interface IAppealRepository
{
    Task<Appeal?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Appeal>> GetActiveAppealsAsync(
        AppealCategory? category = null, 
        CancellationToken ct = default
    );
    Task<List<Appeal>> GetUserAppealsAsync(
        long userId, 
        CancellationToken ct = default
    );
    Task<PagedResult<Appeal>> SearchAppealsAsync(
        string searchTerm,
        int page,
        int pageSize,
        CancellationToken ct = default
    );
    Task AddAsync(Appeal appeal, CancellationToken ct = default);
    void Update(Appeal appeal);
}

public class AppealRepository : BaseRepository<Appeal>, IAppealRepository
{
    public AppealRepository(BotDbContext context) : base(context) { }

    public async Task<List<Appeal>> GetActiveAppealsAsync(
        AppealCategory? category = null,
        CancellationToken ct = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(a => a.Messages)
            .Include(a => a.Student)
            .Where(a => a.Status != AppealStatus.Closed);

        if (category.HasValue)
            query = query.Where(a => a.Category == category.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Appeal>> GetUserAppealsAsync(
        long userId,
        CancellationToken ct = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(a => a.Messages)
            .Where(a => a.StudentId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Appeal>> SearchAppealsAsync(
        string searchTerm,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(a => a.Messages)
            .Include(a => a.Student)
            .Where(a => 
                EF.Functions.Like(a.Subject, $"%{searchTerm}%") ||
                EF.Functions.Like(a.Message, $"%{searchTerm}%") ||
                EF.Functions.Like(a.StudentName, $"%{searchTerm}%")
            );

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Appeal>(items, totalCount, page, pageSize);
    }
}
```

### Unit of Work
```csharp
public interface IUnitOfWork : IDisposable
{
    IAppealRepository Appeals { get; }
    IUserRepository Users { get; }
    INewsRepository News { get; }
    IEventRepository Events { get; }
    IAdminLogRepository AdminLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly BotDbContext _context;
    private IDbContextTransaction? _transaction;

    public IAppealRepository Appeals { get; }
    public IUserRepository Users { get; }
    public INewsRepository News { get; }
    public IEventRepository Events { get; }
    public IAdminLogRepository AdminLogs { get; }

    public UnitOfWork(BotDbContext context)
    {
        _context = context;
        Appeals = new AppealRepository(context);
        Users = new UserRepository(context);
        News = new NewsRepository(context);
        Events = new EventRepository(context);
        AdminLogs = new AdminLogRepository(context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
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

## 📊 Діаграма потоку даних

```
┌─────────────────┐
│  Telegram User  │
└────────┬────────┘
         │
         ▼
┌──────────────────────────────────────────┐
│  Presentation Layer (Bot/API)            │
│  - Message Handlers                      │
│  - Callback Handlers                     │
│  - Middlewares (Auth, RateLimit, Log)    │
└────────┬─────────────────────────────────┘
         │
         │ IRequest<TResponse>
         ▼
┌──────────────────────────────────────────┐
│  MediatR Pipeline                        │
│  1. Validation Behavior                  │
│  2. Logging Behavior                     │
│  3. Performance Behavior                 │
└────────┬─────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────┐
│  Application Layer (CQRS)                │
│  - Command/Query Handlers                │
│  - Validators (FluentValidation)         │
│  - Business Logic Orchestration          │
└────────┬─────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────┐
│  Domain Layer                            │
│  - Entities (Business Rules)             │
│  - Value Objects                         │
│  - Domain Events                         │
└────────┬─────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────┐
│  Infrastructure Layer                    │
│  - Repositories (Data Access)            │
│  - DbContext (EF Core)                   │
│  - External Services (Email, Cache)      │
└────────┬─────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────┐
│  Database (PostgreSQL/SQLite)            │
│  + Redis Cache                           │
└──────────────────────────────────────────┘
```

---

**Версія документа:** 2.0  
**Дата:** 08.10.2025  
**Автор:** AI Assistant  
**Призначення:** Детальний опис архітектури з Clean Architecture та CQRS
