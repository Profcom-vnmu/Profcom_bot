# 🛠️ Інструкції для розробки (Best Practices)

## 🎯 Загальні принципи

### 1. Clean Code Principles
```csharp
✅ ПРАВИЛЬНО:
- Зрозумілі назви змінних та методів
- Одна відповідальність для кожного класу/методу
- Малі методи (не більше 20-30 рядків)
- Коментарі тільки де потрібно пояснення "чому", а не "що"
- DRY (Don't Repeat Yourself)
- SOLID принципи

❌ НЕПРАВИЛЬНО:
- Магічні числа та рядки
- Великі монолітні класи
- Дублювання коду
- Некоментовані складні алгоритми
```

### 2. SOLID Principles

#### S - Single Responsibility Principle
```csharp
// ❌ ПОГАНО - клас робить занадто багато
public class AppealService
{
    public void CreateAppeal() { }
    public void SendEmail() { }
    public void LogToDatabase() { }
    public void GeneratePdf() { }
}

// ✅ ДОБРЕ - кожен клас має одну відповідальність
public class AppealService
{
    private readonly IEmailService _emailService;
    private readonly ILogger _logger;
    private readonly IPdfGenerator _pdfGenerator;
    
    public async Task<Result<Appeal>> CreateAppealAsync(/* params */)
    {
        // Тільки логіка створення звернення
        var appeal = Appeal.Create(/* ... */);
        await _emailService.SendAsync(/* ... */);
        _logger.LogInformation(/* ... */);
        return Result.Ok(appeal);
    }
}
```

#### O - Open/Closed Principle
```csharp
// ✅ ДОБРЕ - відкрито для розширення, закрито для модифікації
public interface INotificationService
{
    Task SendAsync(Notification notification);
}

public class EmailNotificationService : INotificationService { }
public class TelegramNotificationService : INotificationService { }
public class SmsNotificationService : INotificationService { }
```

#### L - Liskov Substitution Principle
```csharp
// ✅ ДОБРЕ - похідні класи можна використовувати замість базового
public abstract class BaseRepository<T>
{
    public virtual async Task<T?> GetByIdAsync(int id) { }
}

public class AppealRepository : BaseRepository<Appeal>
{
    public override async Task<Appeal?> GetByIdAsync(int id) 
    {
        // Може розширити функціонал, але контракт залишається
        return await _dbSet
            .Include(a => a.Messages)
            .FirstOrDefaultAsync(a => a.Id == id);
    }
}
```

#### I - Interface Segregation Principle
```csharp
// ❌ ПОГАНО - великий інтерфейс
public interface IAppealService
{
    Task CreateAsync();
    Task UpdateAsync();
    Task DeleteAsync();
    Task SendEmailAsync();
    Task GeneratePdfAsync();
    Task ExportToExcelAsync();
}

// ✅ ДОБРЕ - розділені інтерфейси
public interface IAppealCommandService
{
    Task<Result<Appeal>> CreateAsync(CreateAppealCommand command);
    Task<Result> UpdateAsync(UpdateAppealCommand command);
    Task<Result> DeleteAsync(int id);
}

public interface IAppealQueryService
{
    Task<Result<AppealDto>> GetByIdAsync(int id);
    Task<Result<List<AppealDto>>> GetAllAsync();
}

public interface IAppealExportService
{
    Task<Result<byte[]>> ExportToPdfAsync(int id);
    Task<Result<byte[]>> ExportToExcelAsync(int id);
}
```

#### D - Dependency Inversion Principle
```csharp
// ✅ ДОБРЕ - залежність від абстракції, а не конкретної реалізації
public class CreateAppealCommandHandler
{
    private readonly IAppealRepository _repository; // Інтерфейс!
    private readonly IEmailService _emailService;   // Інтерфейс!
    
    public CreateAppealCommandHandler(
        IAppealRepository repository,
        IEmailService emailService)
    {
        _repository = repository;
        _emailService = emailService;
    }
}
```

---

## 🏗️ Розробка нових функцій (Step-by-Step)

### Крок 1: Створення Entity (Domain Layer)

```csharp
// Domain/Entities/Appeal.cs
public class Appeal
{
    // Private constructor - створення тільки через фабричний метод
    private Appeal() { }
    
    public int Id { get; private set; }
    public long StudentId { get; private set; }
    public AppealCategory Category { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public AppealStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    // Navigation properties
    public BotUser Student { get; private set; } = null!;
    public ICollection<AppealMessage> Messages { get; private set; } = new List<AppealMessage>();
    
    // Фабричний метод для створення
    public static Appeal Create(
        long studentId,
        string studentName,
        AppealCategory category,
        string subject,
        string message)
    {
        // Валідація на рівні домену
        if (string.IsNullOrWhiteSpace(subject))
            throw new DomainException("Subject cannot be empty");
            
        if (message.Length < 10)
            throw new DomainException("Message too short");
        
        return new Appeal
        {
            StudentId = studentId,
            Category = category,
            Subject = subject,
            Message = message,
            Status = AppealStatus.New,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    // Бізнес-методи
    public void AssignTo(long adminId)
    {
        if (Status == AppealStatus.Closed)
            throw new DomainException("Cannot assign closed appeal");
            
        AssignedToAdminId = adminId;
        Status = AppealStatus.InProgress;
    }
    
    public void Close(string reason)
    {
        if (Status == AppealStatus.Closed)
            throw new DomainException("Appeal already closed");
            
        Status = AppealStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        ClosedReason = reason;
    }
}
```

### Крок 2: Створення Command (Application Layer)

```csharp
// Application/Appeals/Commands/CreateAppeal/CreateAppealCommand.cs
public record CreateAppealCommand(
    long StudentId,
    AppealCategory Category,
    string Subject,
    string Message
) : IRequest<Result<AppealDto>>;
```

### Крок 3: Створення Validator

```csharp
// Application/Appeals/Commands/CreateAppeal/CreateAppealCommandValidator.cs
public class CreateAppealCommandValidator : AbstractValidator<CreateAppealCommand>
{
    public CreateAppealCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .GreaterThan(0)
            .WithMessage("Invalid student ID");

        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("Subject is required")
            .MaximumLength(200)
            .WithMessage("Subject too long");

        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message is required")
            .MinimumLength(10)
            .WithMessage("Message too short (min 10 characters)")
            .MaximumLength(4000)
            .WithMessage("Message too long (max 4000 characters)");

        RuleFor(x => x.Category)
            .IsInEnum()
            .WithMessage("Invalid category");
    }
}
```

### Крок 4: Створення Handler

```csharp
// Application/Appeals/Commands/CreateAppeal/CreateAppealCommandHandler.cs
public class CreateAppealCommandHandler 
    : IRequestHandler<CreateAppealCommand, Result<AppealDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRateLimiter _rateLimiter;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateAppealCommandHandler> _logger;

    public CreateAppealCommandHandler(
        IUnitOfWork unitOfWork,
        IRateLimiter rateLimiter,
        INotificationService notificationService,
        IMapper mapper,
        ILogger<CreateAppealCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _rateLimiter = rateLimiter;
        _notificationService = notificationService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<AppealDto>> Handle(
        CreateAppealCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Rate Limiting
            var canProceed = await _rateLimiter.AllowAsync(
                request.StudentId, 
                "CreateAppeal",
                cancellationToken
            );
            
            if (!canProceed)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for user {UserId}", 
                    request.StudentId
                );
                return Result<AppealDto>.Fail("Rate limit exceeded. Please wait 10 minutes.");
            }

            // 2. Перевірка користувача
            var student = await _unitOfWork.Users.GetByIdAsync(
                request.StudentId,
                cancellationToken
            );
            
            if (student == null)
            {
                _logger.LogError("Student {StudentId} not found", request.StudentId);
                return Result<AppealDto>.Fail("Student not found");
            }

            if (!student.IsEmailVerified)
            {
                return Result<AppealDto>.Fail("Please verify your email first");
            }

            // 3. Перевірка активних звернень
            var hasActiveAppeal = await _unitOfWork.Appeals
                .HasActiveAppealAsync(request.StudentId, cancellationToken);
                
            if (hasActiveAppeal)
            {
                return Result<AppealDto>.Fail(
                    "You already have an active appeal. Please close it first."
                );
            }

            // 4. Створення звернення (Domain logic)
            var appeal = Appeal.Create(
                request.StudentId,
                student.FullName ?? "Unknown",
                request.Category,
                request.Subject,
                request.Message
            );

            // 5. Збереження в БД
            await _unitOfWork.Appeals.AddAsync(appeal, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Appeal {AppealId} created by user {UserId}",
                appeal.Id,
                request.StudentId
            );

            // 6. Відправка сповіщень адмінам
            await _notificationService.NotifyAdminsAboutNewAppealAsync(
                appeal,
                cancellationToken
            );

            // 7. Mapping та повернення результату
            var dto = _mapper.Map<AppealDto>(appeal);
            return Result<AppealDto>.Ok(dto);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed");
            return Result<AppealDto>.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appeal");
            return Result<AppealDto>.Fail("An error occurred while creating appeal");
        }
    }
}
```

### Крок 5: Створення DTO

```csharp
// Application/DTOs/AppealDto.cs
public record AppealDto
{
    public int Id { get; init; }
    public long StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public AppealCategory Category { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public AppealStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public int MessagesCount { get; init; }
    public bool HasUnreadMessages { get; init; }
}
```

### Крок 6: AutoMapper Profile

```csharp
// Application/Mappings/AppealMappingProfile.cs
public class AppealMappingProfile : Profile
{
    public AppealMappingProfile()
    {
        CreateMap<Appeal, AppealDto>()
            .ForMember(
                dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category.GetDisplayName())
            )
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.Status.GetDisplayName())
            )
            .ForMember(
                dest => dest.MessagesCount,
                opt => opt.MapFrom(src => src.Messages.Count)
            )
            .ForMember(
                dest => dest.HasUnreadMessages,
                opt => opt.MapFrom(src => src.Messages.Any(m => !m.IsRead))
            );
    }
}
```

### Крок 7: Telegram Handler (Presentation Layer)

```csharp
// Presentation/Bot/Handlers/Callback/AppealCallbackHandler.cs
public class AppealCallbackHandler
{
    private readonly IMediator _mediator;
    private readonly ILocalizationService _localization;
    private readonly ILogger<AppealCallbackHandler> _logger;

    public async Task HandleCreateAppealAsync(
        long chatId,
        long userId,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        // 1. Запит категорії
        var keyboard = InlineKeyboardMarkup.InlineKeyboard(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "📚 Стипендія", 
                    $"appeal_category:{AppealCategory.Scholarship}"
                ),
                InlineKeyboardButton.WithCallbackData(
                    "🏠 Гуртожиток", 
                    $"appeal_category:{AppealCategory.Dormitory}"
                )
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🎉 Заходи", 
                    $"appeal_category:{AppealCategory.Events}"
                ),
                InlineKeyboardButton.WithCallbackData(
                    "⚠️ Скарга", 
                    $"appeal_category:{AppealCategory.Complaint}"
                )
            }
        });

        await _botClient.EditMessageTextAsync(
            chatId,
            callbackQuery.Message.MessageId,
            _localization.Get(userId, "appeal.select_category"),
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

    public async Task HandleCategorySelectedAsync(
        long chatId,
        long userId,
        AppealCategory category,
        CancellationToken cancellationToken)
    {
        // 2. Збереження категорії в стан
        await _stateManager.SetStateAsync(
            userId,
            DialogState.CreatingAppeal,
            new Dictionary<string, object> { ["category"] = category },
            cancellationToken
        );

        // 3. Запит теми
        await _botClient.SendTextMessageAsync(
            chatId,
            _localization.Get(userId, "appeal.enter_subject"),
            cancellationToken: cancellationToken
        );
    }
}
```

---

## 📐 Coding Standards

### Naming Conventions

```csharp
// ✅ Classes - PascalCase
public class AppealService { }

// ✅ Interfaces - I + PascalCase
public interface IAppealRepository { }

// ✅ Methods - PascalCase
public async Task CreateAppealAsync() { }

// ✅ Private fields - _camelCase
private readonly ILogger _logger;

// ✅ Parameters - camelCase
public void Process(int appealId, string message) { }

// ✅ Local variables - camelCase
var isValid = true;

// ✅ Constants - PascalCase
public const int MaxMessageLength = 4000;

// ✅ Enums - PascalCase (singular)
public enum AppealStatus { }

// ✅ Enum values - PascalCase
public enum AppealStatus { New, InProgress, Closed }
```

### Async/Await

```csharp
// ✅ ДОБРЕ - всі async методи мають суфікс Async
public async Task<Result<Appeal>> CreateAppealAsync(
    CreateAppealCommand command,
    CancellationToken cancellationToken)
{
    var appeal = Appeal.Create(/* ... */);
    await _repository.AddAsync(appeal, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    return Result.Ok(appeal);
}

// ❌ ПОГАНО - немає Async суфікса
public async Task<Result<Appeal>> CreateAppeal() { }

// ✅ ДОБРЕ - використання CancellationToken
public async Task ProcessAsync(CancellationToken cancellationToken)
{
    await _service.DoWorkAsync(cancellationToken);
}

// ✅ ДОБРЕ - ConfigureAwait(false) для library code
public async Task<int> GetCountAsync()
{
    return await _dbContext.Appeals
        .CountAsync()
        .ConfigureAwait(false);
}
```

### Error Handling

```csharp
// ✅ ДОБРЕ - Result Pattern (краще за exceptions для бізнес-логіки)
public async Task<Result<Appeal>> CreateAppealAsync(CreateAppealCommand command)
{
    var validation = await ValidateAsync(command);
    if (!validation.IsSuccess)
        return Result<Appeal>.Fail(validation.Errors);

    var appeal = Appeal.Create(/* ... */);
    return Result<Appeal>.Ok(appeal);
}

// ✅ ДОБРЕ - Try-Catch для інфраструктурних помилок
public async Task<Result> SendEmailAsync(string to, string subject, string body)
{
    try
    {
        await _smtpClient.SendMailAsync(/* ... */);
        return Result.Ok();
    }
    catch (SmtpException ex)
    {
        _logger.LogError(ex, "Failed to send email to {To}", to);
        return Result.Fail("Failed to send email");
    }
}

// ✅ ДОБРЕ - Custom exceptions для domain logic
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public void AssignAppeal(long adminId)
{
    if (Status == AppealStatus.Closed)
        throw new DomainException("Cannot assign closed appeal");
        
    AssignedToAdminId = adminId;
}
```

### Logging

```csharp
// ✅ ДОБРЕ - Structured logging з параметрами
_logger.LogInformation(
    "Appeal {AppealId} created by user {UserId} with category {Category}",
    appeal.Id,
    userId,
    category
);

// ❌ ПОГАНО - String interpolation
_logger.LogInformation($"Appeal {appeal.Id} created by user {userId}");

// ✅ ДОБРЕ - Різні рівні логування
_logger.LogDebug("Debug info");
_logger.LogInformation("Important event");
_logger.LogWarning("Warning condition");
_logger.LogError(ex, "Error occurred");
_logger.LogCritical(ex, "Critical failure");

// ✅ ДОБРЕ - LoggerMessage для high-performance logging
public static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Appeal {AppealId} created by user {UserId}")]
    public static partial void AppealCreated(
        ILogger logger,
        int appealId,
        long userId
    );
}

// Використання
Log.AppealCreated(_logger, appeal.Id, userId);
```

### Dependency Injection

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// ✅ ДОБРЕ - Реєстрація за lifetime
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var token = config["BotToken"];
    return new TelegramBotClient(token);
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAppealRepository, AppealRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IRateLimiter, RateLimiter>();

// ✅ ДОБРЕ - MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});

// ✅ ДОБРЕ - FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// ✅ ДОБРЕ - AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

// ✅ ДОБРЕ - DbContext
builder.Services.AddDbContext<BotDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

// ✅ ДОБРЕ - Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

// ✅ ДОБРЕ - Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .WriteTo.Console()
        .WriteTo.File("logs/bot-.txt", rollingInterval: RollingInterval.Day);
});
```

---

## 🧪 Testing

### Unit Test Example

```csharp
// Tests/UnitTests/Application.Tests/Appeals/CreateAppealCommandHandlerTests.cs
public class CreateAppealCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRateLimiter> _rateLimiterMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<CreateAppealCommandHandler>> _loggerMock;
    private readonly CreateAppealCommandHandler _handler;

    public CreateAppealCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _rateLimiterMock = new Mock<IRateLimiter>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<CreateAppealCommandHandler>>();
        
        _handler = new CreateAppealCommandHandler(
            _unitOfWorkMock.Object,
            _rateLimiterMock.Object,
            Mock.Of<INotificationService>(),
            _mapperMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessResult()
    {
        // Arrange
        var command = new CreateAppealCommand(
            StudentId: 123456,
            Category: AppealCategory.Scholarship,
            Subject: "Test Subject",
            Message: "Test message with more than 10 characters"
        );

        var student = new BotUser { TelegramId = 123456, IsEmailVerified = true };
        
        _rateLimiterMock
            .Setup(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.Users.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        _unitOfWorkMock
            .Setup(x => x.Appeals.HasActiveAppealAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _unitOfWorkMock.Verify(
            x => x.Appeals.AddAsync(It.IsAny<Appeal>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_RateLimitExceeded_ReturnsFailureResult()
    {
        // Arrange
        var command = new CreateAppealCommand(
            StudentId: 123456,
            Category: AppealCategory.Scholarship,
            Subject: "Test",
            Message: "Test message"
        );

        _rateLimiterMock
            .Setup(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Rate limit", result.Error);
    }

    [Fact]
    public async Task Handle_UserNotVerified_ReturnsFailureResult()
    {
        // Arrange
        var command = new CreateAppealCommand(
            StudentId: 123456,
            Category: AppealCategory.Scholarship,
            Subject: "Test",
            Message: "Test message"
        );

        var student = new BotUser { TelegramId = 123456, IsEmailVerified = false };

        _rateLimiterMock
            .Setup(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.Users.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("verify your email", result.Error);
    }
}
```

### Integration Test Example

```csharp
// Tests/IntegrationTests/Api.Tests/WebhookControllerTests.cs
public class WebhookControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WebhookControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_Webhook_ValidUpdate_ReturnsOk()
    {
        // Arrange
        var update = new Update
        {
            Message = new Message
            {
                Text = "/start",
                From = new User { Id = 123456, FirstName = "Test" },
                Chat = new Chat { Id = 123456 }
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(update),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/api/webhook", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

---

## 🔧 Development Workflow

### 1. Git Workflow

```bash
# Створення нової гілки для feature
git checkout -b feature/add-event-registration

# Робота над feature
# ... coding ...

# Коміт змін
git add .
git commit -m "feat: add event registration functionality"

# Push до remote
git push origin feature/add-event-registration

# Створення Pull Request на GitHub

# Після review і approval - merge до development
git checkout development
git pull origin development
git merge feature/add-event-registration
git push origin development

# Після тестування в development - merge до main
git checkout main
git pull origin main
git merge development
git push origin main  # Auto-deploy на Render
```

### 2. Commit Message Convention

```bash
# Формат: <type>(<scope>): <subject>

# Types:
feat:     # Нова функціональність
fix:      # Виправлення бага
docs:     # Документація
style:    # Форматування коду
refactor: # Рефакторинг
perf:     # Покращення продуктивності
test:     # Додавання тестів
chore:    # Технічні зміни

# Приклади:
git commit -m "feat(appeals): add category selection"
git commit -m "fix(notifications): resolve null reference exception"
git commit -m "docs(readme): update installation instructions"
git commit -m "refactor(repositories): extract base repository"
git commit -m "perf(queries): add database indexes"
git commit -m "test(appeals): add unit tests for CreateAppealCommand"
```

### 3. Code Review Checklist

```markdown
## Code Review Checklist

### Функціональність
- [ ] Код робить те, що повинен
- [ ] Немає очевидних багів
- [ ] Edge cases оброблені
- [ ] Error handling присутній

### Код
- [ ] Код читабельний та зрозумілий
- [ ] Дотримано naming conventions
- [ ] Немає дублювання коду (DRY)
- [ ] SOLID principles дотримано
- [ ] Коментарі де потрібно

### Тести
- [ ] Unit тести написані
- [ ] Тести проходять
- [ ] Edge cases покриті тестами
- [ ] Code coverage достатній (>80%)

### Performance
- [ ] Немає N+1 queries
- [ ] Використано AsNoTracking() де можливо
- [ ] Кешування застосовано де потрібно
- [ ] Pagination для великих списків

### Безпека
- [ ] Валідація вхідних даних
- [ ] SQL injection неможливий
- [ ] XSS неможливий
- [ ] Rate limiting застосовано

### Документація
- [ ] XML коментарі для публічних API
- [ ] README оновлено якщо потрібно
- [ ] Miграції додані
```

---

## 📊 Performance Best Practices

### Database Queries

```csharp
// ✅ ДОБРЕ - AsNoTracking для read-only
public async Task<List<Appeal>> GetActiveAppealsAsync()
{
    return await _context.Appeals
        .AsNoTracking()
        .Include(a => a.Messages)
        .Where(a => a.Status != AppealStatus.Closed)
        .ToListAsync();
}

// ✅ ДОБРЕ - Projection для DTO
public async Task<List<AppealDto>> GetActiveAppealsDtoAsync()
{
    return await _context.Appeals
        .AsNoTracking()
        .Where(a => a.Status != AppealStatus.Closed)
        .Select(a => new AppealDto
        {
            Id = a.Id,
            Subject = a.Subject,
            CreatedAt = a.CreatedAt
        })
        .ToListAsync();
}

// ❌ ПОГАНО - N+1 query problem
public async Task<List<Appeal>> GetAppealsAsync()
{
    var appeals = await _context.Appeals.ToListAsync();
    foreach (var appeal in appeals)
    {
        // Це створює окремий запит для кожного звернення!
        appeal.Messages = await _context.AppealMessages
            .Where(m => m.AppealId == appeal.Id)
            .ToListAsync();
    }
    return appeals;
}

// ✅ ДОБРЕ - Include для eager loading
public async Task<List<Appeal>> GetAppealsAsync()
{
    return await _context.Appeals
        .Include(a => a.Messages)
        .Include(a => a.Student)
        .ToListAsync();
}

// ✅ ДОБРЕ - Pagination
public async Task<PagedResult<Appeal>> GetPagedAppealsAsync(int page, int pageSize)
{
    var query = _context.Appeals.AsNoTracking();
    
    var totalCount = await query.CountAsync();
    
    var items = await query
        .OrderByDescending(a => a.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    return new PagedResult<Appeal>(items, totalCount, page, pageSize);
}
```

### Caching

```csharp
// ✅ ДОБРЕ - Кешування для часто запитуваних даних
public async Task<List<AppealDto>> GetActiveAppealsAsync(CancellationToken ct)
{
    const string cacheKey = "active_appeals";
    
    // Спроба отримати з кешу
    var cached = await _cache.GetAsync<List<AppealDto>>(cacheKey);
    if (cached != null)
        return cached;
    
    // Якщо немає в кеші - запит до БД
    var appeals = await _repository.GetActiveAppealsAsync(ct);
    var dtos = _mapper.Map<List<AppealDto>>(appeals);
    
    // Збереження в кеш на 5 хвилин
    await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(5));
    
    return dtos;
}

// ✅ ДОБРЕ - Інвалідація кешу при зміні даних
public async Task<Result> CreateAppealAsync(CreateAppealCommand command)
{
    var appeal = Appeal.Create(/* ... */);
    await _repository.AddAsync(appeal);
    await _unitOfWork.SaveChangesAsync();
    
    // Очищення кешу після створення нового звернення
    await _cache.RemoveAsync("active_appeals");
    
    return Result.Ok();
}
```

---

## ✅ Pre-commit Checklist

Перед кожним commit:

- [ ] `dotnet build` - компілюється без помилок
- [ ] `dotnet test` - всі тести проходять
- [ ] `dotnet format` - код відформатовано
- [ ] Немає console.log / debug коду
- [ ] Немає закоментованого коду чи TODO
- [ ] Code review checklist пройдено
- [ ] XML коментарі для публічних методів
- [ ] Naming conventions дотримано
- [ ] SOLID principles дотримано
- [ ] Error handling присутній
- [ ] Logging додано
- [ ] Міграції створені (якщо змінювались моделі)

---

**Версія документа:** 2.0  
**Дата:** 08.10.2025  
**Автор:** AI Assistant  
**Призначення:** Best practices для розробки з Clean Architecture та CQRS
