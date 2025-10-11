# 📋 Action Plan: Конкретні завдання для завершення проєкту

**Базовий звіт:** [ЗВІТ_АНАЛІЗ_РЕАЛІЗАЦІЇ_2025-10-11.md](./ЗВІТ_АНАЛІЗ_РЕАЛІЗАЦІЇ_2025-10-11.md)  
**Дата створення:** 11 жовтня 2025

---

## 🎯 Sprint 1: Критичні TODO та багі (1 тиждень)

### Завдання 1.1: Множинні файли для звернень ⚠️ HIGH PRIORITY

**Локація:** `Domain/Entities/Appeal.cs`, `Application/Appeals/Commands/CreateAppeal/`

**Поточна проблема:**
```csharp
// Зараз:
public string? PhotoFileId { get; private set; }
public string? DocumentFileId { get; private set; }
// Обмеження: тільки 1 фото + 1 документ
```

**Що зробити:**
1. [ ] Додати навігаційну властивість до Appeal:
   ```csharp
   public ICollection<AppealFileAttachment> FileAttachments { get; private set; }
   ```
2. [ ] Видалити старі поля `PhotoFileId`, `DocumentFileId` з AppealMessage
3. [ ] Оновити `CreateAppealCommand` для прийому `List<FileAttachmentDto>`
4. [ ] Оновити `CreateAppealCommandHandler` для збереження множинних файлів
5. [ ] Створити міграцію `RemoveOldFileFields`
6. [ ] Оновити Telegram handlers для множинних MediaGroup

**Файли для редагування:**
- `Domain/Entities/Appeal.cs`
- `Application/Appeals/Commands/CreateAppeal/CreateAppealCommand.cs`
- `Application/Appeals/Commands/CreateAppeal/CreateAppealCommandHandler.cs`
- `Presentation/Bot/Handlers/Appeals/AppealHandler.cs`
- Нова міграція

**Час:** 4-6 годин

---

### Завдання 1.2: Додати Category до Event entity ⚠️ HIGH PRIORITY

**Локація:** `Domain/Entities/Event.cs`

**Проблема:**
```csharp
// TODO коментар у CreateEventCommandHandler.cs:
// TODO: Додати Category field в Entity
```

**Що зробити:**
1. [ ] Додати enum `EventCategory` до `Domain/Enums/EventEnums.cs`:
   ```csharp
   public enum EventCategory
   {
       Educational,    // Освітні
       Social,         // Соціальні
       Cultural,       // Культурні
       Sports,         // Спортивні
       Volunteer,      // Волонтерські
       Career,         // Кар'єрні
       Entertainment   // Розважальні
   }
   ```

2. [ ] Додати поле до `Event.cs`:
   ```csharp
   public EventCategory Category { get; private set; }
   ```

3. [ ] Оновити factory method `Event.Create()`
4. [ ] Додати extension method `GetDisplayName()` для `EventCategory`
5. [ ] Оновити DbContext конфігурацію (index на Category)
6. [ ] Створити міграцію `AddEventCategoryField`
7. [ ] Оновити всі команди/запити (CreateEvent, UpdateEvent)
8. [ ] Додати фільтрацію за категорією у `GetUpcomingEventsQuery`

**Файли для редагування:**
- `Domain/Enums/EventEnums.cs`
- `Domain/Entities/Event.cs`
- `Infrastructure/Data/BotDbContext.cs`
- `Application/Events/Commands/CreateEvent/CreateEventCommand.cs`
- `Application/Events/Queries/GetUpcomingEvents/GetUpcomingEventsQuery.cs`
- Нова міграція

**Час:** 2-3 години

---

### Завдання 1.3: IsArchived поле для News ⚠️ MEDIUM PRIORITY

**Локація:** `Domain/Entities/News.cs`

**Проблема:**
```csharp
// TODO: Додати поле IsArchived до ентіті News
```

**Що зробити:**
1. [ ] Додати поле до `News.cs`:
   ```csharp
   public bool IsArchived { get; private set; }
   public DateTime? ArchivedAt { get; private set; }
   public long? ArchivedByUserId { get; private set; }
   ```

2. [ ] Додати метод:
   ```csharp
   public void Archive(long userId)
   {
       if (IsArchived) throw new DomainException("News already archived");
       IsArchived = true;
       ArchivedAt = DateTime.UtcNow;
       ArchivedByUserId = userId;
   }
   ```

3. [ ] Створити міграцію `AddNewsArchiveFields`
4. [ ] Створити `ArchiveNewsCommand` + Handler + Validator
5. [ ] Оновити запити (виключати архівні новини)
6. [ ] Додати фільтр у `GetAllNewsQuery` для показу архіву адмінам
7. [ ] Додати Telegram handler для архівації

**Файли для редагування:**
- `Domain/Entities/News.cs`
- `Application/News/Commands/ArchiveNews/` (нова папка)
- `Application/News/Queries/GetAllNews/GetAllNewsQueryHandler.cs`
- `Presentation/Bot/Handlers/Admin/NewsManagementHandler.cs`
- Нова міграція

**Час:** 3-4 години

---

### Завдання 1.4: Перевірки прав доступу (Authorization) ⚠️ HIGH PRIORITY

**Локація:** Багато handlers

**Проблеми:**
```csharp
// TODO: Додати перевірку прав доступу для публікації
// TODO: Додати перевірку прав доступу для видалення
// TODO: Додати перевірку прав доступу (автор або адміністратор)
```

**Що зробити:**
1. [ ] Створити атрибут `[RequirePermission(Permission.PublishNews)]`
2. [ ] Оновити `AuthorizationBehavior` для зчитування атрибутів
3. [ ] Додати атрибути до команд:
   ```csharp
   [RequirePermission(Permission.PublishNews)]
   public record PublishNewsCommand(...) : IRequest<Result<NewsDto>>;
   
   [RequirePermission(Permission.DeleteNews)]
   public record DeleteNewsCommand(...) : IRequest<Result<bool>>;
   
   [RequireRole(UserRole.Admin)]
   public record CreateEventCommand(...) : IRequest<Result<EventDto>>;
   ```

4. [ ] Додати перевірку "автор або адміністратор" у `UpdateNewsCommandHandler`:
   ```csharp
   var news = await _unitOfWork.News.GetByIdAsync(request.Id);
   if (news.AuthorId != currentUserId && !user.IsAdmin)
       return Result.Fail("Ви можете редагувати тільки свої новини");
   ```

5. [ ] Тести для authorization

**Файли для редагування:**
- `Application/Common/Attributes/AuthorizationAttributes.cs`
- `Application/Common/Behaviors/AuthorizationBehavior.cs`
- Всі команди з TODO про права доступу
- `Application/News/Commands/UpdateNews/UpdateNewsCommandHandler.cs`
- `Application/News/Commands/DeleteNews/DeleteNewsCommandHandler.cs`
- `Application/Events/Commands/CreateEvent/CreateEventCommandHandler.cs`

**Час:** 4-5 годин

---

### Завдання 1.5: Push сповіщення для адміністраторів

**Локація:** `Presentation/Bot/Handlers/Admin/AdminAppealHandler.cs:XXX`

**Проблема:**
```csharp
// TODO: Якщо потрібно відправити push-повідомлення
```

**Що зробити:**
1. [ ] У `AssignAppealCommandHandler` додати:
   ```csharp
   await _notificationService.SendAsync(
       userId: assignedAdminId,
       event: NotificationEvent.AppealAssigned,
       title: "Вам призначено звернення",
       message: $"Звернення #{appeal.Id}: {appeal.Subject}",
       channel: NotificationChannel.Push,
       cancellationToken: cancellationToken);
   ```

2. [ ] У `ReplyToAppealCommandHandler` додати сповіщення студенту
3. [ ] У `CloseAppealCommandHandler` додати сповіщення студенту
4. [ ] Перевірити, що `TelegramPushNotificationProvider` відправляє

**Файли для редагування:**
- `Application/Appeals/Commands/AssignAppeal/AssignAppealCommandHandler.cs`
- `Application/Appeals/Commands/ReplyToAppeal/ReplyToAppealCommandHandler.cs`
- `Application/Appeals/Commands/CloseAppeal/CloseAppealCommandHandler.cs`

**Час:** 2 години

---

## 🎯 Sprint 2: CRUD для Partners & Contacts (1 тиждень)

### Завдання 2.1: Partners CRUD Commands

**Що створити:**

1. [ ] **CreatePartnerCommand**
   - Файл: `Application/Partners/Commands/CreatePartner/CreatePartnerCommand.cs`
   - Handler: `CreatePartnerCommandHandler.cs`
   - Validator: `CreatePartnerCommandValidator.cs`
   - Поля: Name, Type, Description, DiscountInfo, PromoCode, Website, Address, PhoneNumber, Social media links, LogoFileId

2. [ ] **UpdatePartnerCommand**
   - Файл: `Application/Partners/Commands/UpdatePartner/UpdatePartnerCommand.cs`
   - Handler: `UpdatePartnerCommandHandler.cs`
   - Validator: `UpdatePartnerCommandValidator.cs`

3. [ ] **DeletePartnerCommand**
   - Файл: `Application/Partners/Commands/DeletePartner/DeletePartnerCommand.cs`
   - Handler: `DeletePartnerCommandHandler.cs` (soft delete - `IsActive = false`)

4. [ ] **Queries:**
   - `GetPartnerByIdQuery` + Handler
   - Оновити `GetActivePartnersQuery` - додати фільтрацію за Type

**Validators rules:**
```csharp
RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
RuleFor(x => x.Type).IsInEnum();
RuleFor(x => x.DiscountInfo).MaximumLength(500);
RuleFor(x => x.Website).Must(BeValidUrl).When(x => !string.IsNullOrEmpty(x.Website));
RuleFor(x => x.PhoneNumber).Matches(@"^\+?[0-9\s\-\(\)]+$").When(x => !string.IsNullOrEmpty(x.PhoneNumber));
```

**Час:** 6-8 годин

---

### Завдання 2.2: Contacts CRUD Commands

**Що створити:**

1. [ ] **CreateContactCommand**
   - Файл: `Application/Contacts/Commands/CreateContact/CreateContactCommand.cs`
   - Handler, Validator

2. [ ] **UpdateContactCommand**
3. [ ] **DeleteContactCommand** (soft delete)
4. [ ] **Queries:**
   - `GetContactByIdQuery`
   - Оновити `GetAllContactsQuery` - додати фільтрацію за Type

**Час:** 5-6 годин

---

### Завдання 2.3: Telegram Handlers для Partners

**Що створити:**

1. [ ] `PartnersManagementHandler.cs` у `Presentation/Bot/Handlers/Admin/`
   - Методи:
     - `HandleCreatePartnerAsync()`
     - `HandleEditPartnerAsync()`
     - `HandleDeletePartnerAsync()`
     - `ShowPartnersListAsync()`
   - State machine для введення даних

2. [ ] Оновити `AdminHandler.cs` - додати кнопку "Партнери"

3. [ ] Оновити `KeyboardFactory.cs` - клавіатури для партнерів

**Час:** 4-5 годин

---

### Завдання 2.4: Telegram Handlers для Contacts

**Аналогічно 2.3**, створити `ContactsManagementHandler.cs`

**Час:** 4-5 годин

---

## 🎯 Sprint 3: Unit Tests (2 тижні)

### Завдання 3.1: Налаштування тестового проєкту

**Що зробити:**

1. [ ] Створити проєкт `StudentUnionBot.Tests` (xUnit)
2. [ ] Додати NuGet пакети:
   ```xml
   <PackageReference Include="xunit" Version="2.6.0" />
   <PackageReference Include="xunit.runner.visualstudio" Version="2.5.0" />
   <PackageReference Include="FluentAssertions" Version="6.12.0" />
   <PackageReference Include="Moq" Version="4.20.0" />
   <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
   <PackageReference Include="MediatR" Version="12.2.0" />
   ```

3. [ ] Структура:
   ```
   StudentUnionBot.Tests/
   ├── Domain.Tests/
   │   ├── Entities/
   │   └── Enums/
   ├── Application.Tests/
   │   ├── Appeals/
   │   ├── Events/
   │   ├── News/
   │   └── Users/
   ├── Infrastructure.Tests/
   │   ├── Repositories/
   │   └── Services/
   └── Helpers/
       ├── TestBase.cs
       └── MockFactory.cs
   ```

4. [ ] Створити `TestBase.cs`:
   ```csharp
   public abstract class TestBase
   {
       protected Mock<ILogger<T>> CreateLoggerMock<T>() => new();
       protected BotDbContext CreateInMemoryContext() { /* ... */ }
   }
   ```

**Час:** 2-3 години

---

### Завдання 3.2: Domain Tests

**Що тестувати:**

1. [ ] **Appeal.cs**
   ```csharp
   public class AppealTests
   {
       [Fact]
       public void Create_ValidData_ShouldCreateAppeal()
       {
           // Arrange, Act, Assert
       }
       
       [Fact]
       public void Create_EmptySubject_ShouldThrowDomainException()
       {
           // Arrange
           var subject = "";
           
           // Act & Assert
           Assert.Throws<DomainException>(() => 
               Appeal.Create(123, "Name", AppealCategory.Scholarship, subject, "Message"));
       }
       
       [Fact]
       public void AssignTo_ClosedAppeal_ShouldThrowDomainException() { }
       
       [Fact]
       public void Close_AlreadyClosed_ShouldThrowDomainException() { }
   }
   ```

2. [ ] Аналогічно для `News.cs`, `Event.cs`, `BotUser.cs`

**Час:** 8-10 годин

---

### Завдання 3.3: Application Tests - Command Handlers

**Приклад:**

```csharp
public class CreateAppealCommandHandlerTests : TestBase
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRateLimiter> _rateLimiterMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly CreateAppealCommandHandler _handler;

    public CreateAppealCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _rateLimiterMock = new Mock<IRateLimiter>();
        _notificationServiceMock = new Mock<INotificationService>();
        
        _handler = new CreateAppealCommandHandler(
            _unitOfWorkMock.Object,
            _rateLimiterMock.Object,
            _notificationServiceMock.Object,
            Mock.Of<IAppealAssignmentService>(),
            CreateLoggerMock<CreateAppealCommandHandler>().Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldReturnSuccess()
    {
        // Arrange
        var command = new CreateAppealCommand(
            StudentId: 123,
            StudentName: "Test User",
            Category: AppealCategory.Scholarship,
            Subject: "Test Subject",
            Message: "Test message with more than 10 characters");

        var user = new BotUser { TelegramId = 123, IsBanned = false };
        
        _rateLimiterMock
            .Setup(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.Users.GetByTelegramIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Subject.Should().Be("Test Subject");
        
        _unitOfWorkMock.Verify(
            x => x.Appeals.AddAsync(It.IsAny<Appeal>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_RateLimitExceeded_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateAppealCommand(123, "User", AppealCategory.Scholarship, "Subject", "Message");

        _rateLimiterMock
            .Setup(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("ліміт");
    }

    [Fact]
    public async Task Handle_BannedUser_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateAppealCommand(123, "User", AppealCategory.Scholarship, "Subject", "Message");
        var bannedUser = new BotUser { TelegramId = 123, IsBanned = true, BanReason = "Test ban" };
        
        _rateLimiterMock.Setup(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.Users.GetByTelegramIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bannedUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("заблоковано");
    }
}
```

**Що тестувати:**
- [ ] CreateAppealCommandHandler (всі edge cases)
- [ ] AssignAppealCommandHandler
- [ ] CloseAppealCommandHandler
- [ ] CreateNewsCommandHandler
- [ ] PublishNewsCommandHandler
- [ ] CreateEventCommandHandler
- [ ] RegisterForEventCommandHandler
- [ ] RegisterUserCommandHandler
- [ ] VerifyEmailCommandHandler

**Час:** 16-20 годин

---

### Завдання 3.4: Application Tests - Query Handlers

**Приклад:**

```csharp
public class GetUserAppealsQueryHandlerTests : TestBase
{
    [Fact]
    public async Task Handle_UserHasAppeals_ShouldReturnAppeals()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new AppealRepository(context);
        var unitOfWork = new UnitOfWork(context, /* ... */);
        var handler = new GetUserAppealsQueryHandler(unitOfWork, /* ... */);

        var user = new BotUser { TelegramId = 123 };
        context.Users.Add(user);
        
        var appeal1 = Appeal.Create(123, "User", AppealCategory.Scholarship, "Subject 1", "Message 1");
        var appeal2 = Appeal.Create(123, "User", AppealCategory.Dormitory, "Subject 2", "Message 2");
        await repository.AddAsync(appeal1);
        await repository.AddAsync(appeal2);
        await context.SaveChangesAsync();

        var query = new GetUserAppealsQuery(123);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(a => a.Subject == "Subject 1");
    }

    [Fact]
    public async Task Handle_UserHasNoAppeals_ShouldReturnEmptyList()
    {
        // Test implementation
    }
}
```

**Час:** 8-10 годин

---

### Завдання 3.5: Validators Tests

**Приклад:**

```csharp
public class CreateAppealCommandValidatorTests
{
    private readonly CreateAppealCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateAppealCommand(
            StudentId: 123,
            StudentName: "Test User",
            Category: AppealCategory.Scholarship,
            Subject: "Valid Subject",
            Message: "Valid message with more than 10 characters");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptySubject_ShouldFail()
    {
        // Arrange
        var command = new CreateAppealCommand(123, "User", AppealCategory.Scholarship, "", "Valid message");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppealCommand.Subject));
    }

    [Theory]
    [InlineData("Short")]  // Less than 5 chars
    [InlineData("AB")]
    public void Validate_SubjectTooShort_ShouldFail(string subject)
    {
        // Arrange
        var command = new CreateAppealCommand(123, "User", AppealCategory.Scholarship, subject, "Valid message");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_MessageTooShort_ShouldFail()
    {
        // Less than 10 chars
        var command = new CreateAppealCommand(123, "User", AppealCategory.Scholarship, "Subject", "Short");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }
}
```

**Час:** 6-8 годин

---

### Завдання 3.6: Services Tests

**Тестувати:**
1. [ ] `AppealAssignmentService` - логіка призначення
2. [ ] `AuthorizationService` - перевірка прав
3. [ ] `RateLimiter` - sliding window algorithm
4. [ ] `FileValidationService` - валідація файлів

**Приклад:**

```csharp
public class AppealAssignmentServiceTests
{
    [Fact]
    public async Task AssignAppealAsync_ShouldAssignToLeastBusyAdmin()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AppealAssignmentService(/* ... */);

        var admin1 = new BotUser { TelegramId = 1, Role = UserRole.Admin };
        var admin2 = new BotUser { TelegramId = 2, Role = UserRole.Admin };
        
        var workload1 = new AdminWorkload { AdminId = 1, ActiveAppealsCount = 5 };
        var workload2 = new AdminWorkload { AdminId = 2, ActiveAppealsCount = 2 }; // Less busy
        
        context.Users.AddRange(admin1, admin2);
        context.AdminWorkloads.AddRange(workload1, workload2);
        await context.SaveChangesAsync();

        var appeal = Appeal.Create(123, "User", AppealCategory.Scholarship, "Subject", "Message");

        // Act
        var result = await service.AssignAppealAsync(appeal, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TelegramId.Should().Be(2); // Should assign to admin2 (less busy)
    }
}
```

**Час:** 10-12 годин

---

## 🎯 Sprint 4: UX покращення (1 тиждень)

### Завдання 4.1: Inline Pagination

**Локація:** `Presentation/Bot/Keyboards/KeyboardFactory.cs`

**Що зробити:**
1. [ ] Створити метод `CreatePaginatedKeyboard<T>()`:
   ```csharp
   public static InlineKeyboardMarkup CreatePaginatedKeyboard<T>(
       List<T> items,
       int currentPage,
       int pageSize,
       Func<T, InlineKeyboardButton> itemToButton,
       string callbackPrefix)
   {
       var buttons = new List<List<InlineKeyboardButton>>();
       
       // Item buttons
       var startIndex = (currentPage - 1) * pageSize;
       var pageItems = items.Skip(startIndex).Take(pageSize);
       foreach (var item in pageItems)
       {
           buttons.Add(new List<InlineKeyboardButton> { itemToButton(item) });
       }
       
       // Navigation buttons
       var navButtons = new List<InlineKeyboardButton>();
       if (currentPage > 1)
           navButtons.Add(InlineKeyboardButton.WithCallbackData("◀️ Назад", $"{callbackPrefix}:prev:{currentPage}"));
       
       navButtons.Add(InlineKeyboardButton.WithCallbackData($"Сторінка {currentPage}", "noop"));
       
       if (items.Count > currentPage * pageSize)
           navButtons.Add(InlineKeyboardButton.WithCallbackData("Вперед ▶️", $"{callbackPrefix}:next:{currentPage}"));
       
       buttons.Add(navButtons);
       return new InlineKeyboardMarkup(buttons);
   }
   ```

2. [ ] Застосувати до:
   - Список новин
   - Список подій
   - Список партнерів
   - Список звернень (для адміна)

**Час:** 4-5 годин

---

### Завдання 4.2: Фільтри за категоріями

**Що зробити:**
1. [ ] Для новин - фільтр за `NewsCategory`
2. [ ] Для подій - фільтр за `EventCategory` (після Sprint 1.2)
3. [ ] Для партнерів - фільтр за `PartnerType`

**Приклад клавіатури:**
```csharp
public static InlineKeyboardMarkup CreateCategoryFilterKeyboard(string activeCategory)
{
    var buttons = new List<List<InlineKeyboardButton>>
    {
        new()
        {
            InlineKeyboardButton.WithCallbackData(
                activeCategory == "All" ? "✅ Всі" : "Всі",
                "news:filter:all"),
            InlineKeyboardButton.WithCallbackData(
                activeCategory == "Educational" ? "✅ Освітні" : "Освітні",
                "news:filter:educational")
        },
        // ... інші категорії
    };
    return new InlineKeyboardMarkup(buttons);
}
```

**Час:** 3-4 години

---

### Завдання 4.3: Rich Formatting

**Що зробити:**
1. [ ] Використовувати HTML formatting для повідомлень:
   ```csharp
   var message = $"<b>Звернення #{appeal.Id}</b>\n\n" +
                 $"<b>Категорія:</b> {appeal.Category.GetDisplayName()}\n" +
                 $"<b>Статус:</b> {appeal.Status.GetDisplayName()}\n" +
                 $"<b>Пріоритет:</b> {appeal.Priority.GetDisplayName()}\n\n" +
                 $"<i>{appeal.Message}</i>";
   
   await botClient.SendTextMessageAsync(
       chatId: chatId,
       text: message,
       parseMode: ParseMode.Html);
   ```

2. [ ] Додати емодзі для статусів:
   ```csharp
   public static string GetEmoji(this AppealStatus status) => status switch
   {
       AppealStatus.New => "🆕",
       AppealStatus.Assigned => "👤",
       AppealStatus.InProgress => "⏳",
       AppealStatus.Resolved => "✅",
       AppealStatus.Closed => "🔒",
       _ => ""
   };
   ```

3. [ ] Застосувати до всіх типів повідомлень

**Час:** 3-4 години

---

## 🎯 Sprint 5: Monitoring & DevOps (1 тиждень)

### Завдання 5.1: Sentry для Error Tracking

**Що зробити:**
1. [ ] Додати NuGet пакет `Sentry.AspNetCore`
2. [ ] У `Program.cs`:
   ```csharp
   builder.WebHost.UseSentry(options =>
   {
       options.Dsn = builder.Configuration["Sentry:Dsn"];
       options.TracesSampleRate = 1.0;
       options.Environment = builder.Environment.EnvironmentName;
   });
   ```

3. [ ] Додати custom event для критичних помилок
4. [ ] Налаштувати alerting в Sentry

**Час:** 2-3 години

---

### Завдання 5.2: GitHub Actions CI/CD

**Що зробити:**
1. [ ] Створити `.github/workflows/ci.yml`:
   ```yaml
   name: CI

   on:
     push:
       branches: [ development, main ]
     pull_request:
       branches: [ development, main ]

   jobs:
     build:
       runs-on: ubuntu-latest
       steps:
       - uses: actions/checkout@v3
       - name: Setup .NET
         uses: actions/setup-dotnet@v3
         with:
           dotnet-version: 8.0.x
       - name: Restore dependencies
         run: dotnet restore
       - name: Build
         run: dotnet build --no-restore
       - name: Test
         run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
       - name: Upload coverage to Codecov
         uses: codecov/codecov-action@v3
   ```

2. [ ] Створити `.github/workflows/deploy.yml` для auto-deploy до Render.com

**Час:** 3-4 години

---

### Завдання 5.3: Prometheus Metrics

**Що зробити:**
1. [ ] Додати пакет `prometheus-net.AspNetCore`
2. [ ] Додати middleware:
   ```csharp
   app.UseHttpMetrics();
   app.MapMetrics();
   ```

3. [ ] Створити custom metrics:
   ```csharp
   private static readonly Counter AppealCreatedCounter = 
       Metrics.CreateCounter("appeals_created_total", "Total appeals created");
   
   private static readonly Histogram AppealProcessingTime = 
       Metrics.CreateHistogram("appeal_processing_seconds", "Time to process appeal");
   ```

4. [ ] Інтегрувати з Grafana

**Час:** 4-5 годин

---

## 📊 Підсумок часу

| Sprint | Завдання | Час (годин) |
|--------|----------|-------------|
| **Sprint 1** | Критичні TODO | **15-20** |
| Sprint 1.1 | Множинні файли | 4-6 |
| Sprint 1.2 | Event Category | 2-3 |
| Sprint 1.3 | News IsArchived | 3-4 |
| Sprint 1.4 | Authorization | 4-5 |
| Sprint 1.5 | Push сповіщення | 2 |
| **Sprint 2** | CRUD Partners/Contacts | **19-24** |
| Sprint 2.1 | Partners CRUD | 6-8 |
| Sprint 2.2 | Contacts CRUD | 5-6 |
| Sprint 2.3 | Partners Handlers | 4-5 |
| Sprint 2.4 | Contacts Handlers | 4-5 |
| **Sprint 3** | Unit Tests | **50-62** |
| Sprint 3.1 | Setup | 2-3 |
| Sprint 3.2 | Domain Tests | 8-10 |
| Sprint 3.3 | Command Handlers | 16-20 |
| Sprint 3.4 | Query Handlers | 8-10 |
| Sprint 3.5 | Validators | 6-8 |
| Sprint 3.6 | Services | 10-12 |
| **Sprint 4** | UX | **10-13** |
| Sprint 4.1 | Pagination | 4-5 |
| Sprint 4.2 | Filters | 3-4 |
| Sprint 4.3 | Rich Formatting | 3-4 |
| **Sprint 5** | DevOps | **9-12** |
| Sprint 5.1 | Sentry | 2-3 |
| Sprint 5.2 | GitHub Actions | 3-4 |
| Sprint 5.3 | Prometheus | 4-5 |
| **ВСЬОГО** | | **103-131 годин** |

**Орієнтовно:** 3-4 тижні роботи (при 30-40 год/тиждень)

---

## ✅ Definition of Done для кожного завдання

Завдання вважається завершеним, якщо:

- [ ] Код написано згідно з Clean Architecture + CQRS
- [ ] Всі валідатори створені (FluentValidation)
- [ ] Result Pattern використовується
- [ ] Structured logging додано
- [ ] XML коментарі для публічних методів
- [ ] Міграції створені (якщо зміни в БД)
- [ ] Unit тести написані (code coverage >80%)
- [ ] TODO коментарі видалені
- [ ] Code review пройдено
- [ ] `dotnet build` без помилок
- [ ] `dotnet test` всі тести проходять
- [ ] Документація оновлена (якщо потрібно)

---

**Створено:** 11 жовтня 2025  
**Автор:** GitHub Copilot AI Agent  
**Статус:** 📋 Готово до виконання
